using Es;
using FSM;
using Game.Vehicle.PGCVehicle;
using Message;
using UnityEngine;

/// <summary>
/// 被束缚状态（挣扎中）：被娃娃机爪子抓住。
/// - 进入即冻结玩家；播放 catch(被抓瞬间) → catch_idle(吊着待机循环)
/// - 被抓玩家的 parent 由 WawajiKVC 在爪子动画事件中挂到 GrabHook（方案A，本状态不处理）
/// - 本人每点一次挣脱：播一次 struggle → 回 catch_idle（由 UI 广播 OnSelfStruggleTap 驱动）
/// - QTE 成功 → FallingState；QTE 超时 → CapturedState（均由网络层驱动）
/// </summary>
public class BoundState : PlayerStateTemplate<PlayerStateController>
{
    private const string AnimDir = "Assets/Loadable/Animations/Vehicle/clawmachine/";

    private string _captorUid; // 娃娃机驾驶员 A 的 uid
    private Transform _grabHook; // 爪头 Bone_WWJ_60（被抓玩家吊点）

    public BoundState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        if (args != null && args.Length > 0 && args[0] != null)
        {
            _captorUid = args[0].ToString();
        }
    }

    public override void EnterMainState()
    {
        base.EnterMainState();

        FreezePlayer(true);

        // 解析驾驶员载具的爪头骨骼（Bone_WWJ_60），被抓玩家每帧吊到此处
        var captorVc = PGCVehicleManager.Inst.GetPGCVehicleController(_captorUid);
        if (captorVc != null && captorVc.VehicleKinematic != null)
        {
            _grabHook = GameObjectEx.FindComponentByName<Transform>(
                captorVc.VehicleKinematic.gameObject, "Bone_WWJ_60");
        }

        // 被抓瞬间 → 吊着待机循环
        var ev = owner.PlayerAnimCtrl?.LoadPlay(AnimDir + "catch", 0);
        if (ev != null) ev.OnCompleteEvent(PlayCatchIdle);
        else PlayCatchIdle();

        // 交互(被抓)音效：自己 1P / 他人 3P（本状态在所有端执行，各端自然分流）
        PlayCraneSound("Crane_Impact");

        if (owner.IsSelf)
        {
            MessageHelper.AddListener(MessageName.OnSelfStruggleTap, OnStruggleTap);
            MessageHelper.Broadcast(MessageName.OnSelfBoundStateChange, true, _captorUid);
        }
        else
        {
            // 远端副本：监听本玩家的挣扎同步，播 struggle
            MessageHelper.AddListener<string>(MessageName.OnWawajiStruggleTap, OnRemoteStruggleTap);
        }
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        // 注意：parent 由 WawajiKVC 处理（方案A），本状态不动 parent。
        // 冻结的解除交给后继状态：去 Falling 会恢复物理；去 Captured 仍保持冻结。
        if (owner.IsSelf)
        {
            MessageHelper.RemoveListener(MessageName.OnSelfStruggleTap, OnStruggleTap);
            MessageHelper.Broadcast(MessageName.OnSelfBoundStateChange, false, _captorUid);
        }
        else
        {
            MessageHelper.RemoveListener<string>(MessageName.OnWawajiStruggleTap, OnRemoteStruggleTap);
        }
    }

    // 远端副本：收到本玩家的挣扎同步时播 struggle
    private void OnRemoteStruggleTap(string uid)
    {
        if (uid == owner.PlayerID) OnStruggleTap();
    }

    public override void MainStateLateUpdate()
    {
        base.MainStateLateUpdate();
        // KinematicCharacterSystem 每帧强制 transform=TransientPosition（无视冻结/parent），
        // 故 WawajiKVC 的 SetParent 挂点无效，必须每帧 Motor.SetPosition 把玩家钉到爪头世界坐标。
        // 与驾驶位一致：所有端都跑。爪头朝向随骨骼乱转，故只跟位置不跟旋转，保留玩家自身朝向。
        if (_grabHook == null || owner.PlayerKCCtrl == null || owner.PlayerKCCtrl.Motor == null) return;
        // 吊点在爪头世界坐标基础上 y 再 -0.5（让角色挂得更靠下，贴合爪子）
        Vector3 hookPos = _grabHook.position;
        hookPos.y -= 0.5f;
        owner.PlayerKCCtrl.Motor.SetPosition(hookPos);
    }

    public override void ReleaseData()
    {
        base.ReleaseData();
        _captorUid = null;
        _grabHook = null;
    }

    private void PlayCatchIdle()
    {
        owner.PlayerAnimCtrl?.LoadPlay(AnimDir + "catch_idle", 0);
    }

    // 本人每点一次挣脱按钮：播一次 struggle，播完回 catch_idle
    private void OnStruggleTap()
    {
        if (!owner.IsMainState(stateID)) return;
        var ev = owner.PlayerAnimCtrl?.LoadPlay(AnimDir + "struggle", 0);
        if (ev != null) ev.OnCompleteEvent(PlayCatchIdle);

        // 挣扎音效：自己 1P / 他人 3P（OnStruggleTap 在本人与远端副本都会触发）
        PlayCraneSound("Crane_Shake");
    }

    // 被抓玩家阶段音效：group=Locomotion_Group，switch=Crane_*，事件按 1P(自己)/3P(他人) 区分，播在玩家 avatar 上。
    private void PlayCraneSound(string craneStage)
    {
        if (owner == null || owner.Wrap == null || owner.Wrap.Avatar == null) return;
        MessageHelper.Broadcast(MessageName.GameSound, "Locomotion_Group", craneStage,
            owner.IsSelf ? "Play_Locomotion_1P" : "Play_Locomotion_3P", owner.Wrap.Avatar);
    }

    private void FreezePlayer(bool freeze)
    {
        if (owner.PlayerKCCtrl == null) return;
        owner.PlayerKCCtrl.SetFreezeCharacter(freeze);
        owner.PlayerKCCtrl.enabled = !freeze;
        if (owner.PlayerKCCtrl.Motor != null)
            owner.PlayerKCCtrl.Motor.SetIsOnSimulate(!freeze);
        owner.PlayerKCCtrl.IsDriveVehicle = freeze;
    }

    public string CaptorUid => _captorUid;
}

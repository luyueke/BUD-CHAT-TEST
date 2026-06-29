using FSM;
using Message;
using UnityEngine;

/// <summary>
/// 下落状态：从被束缚(挣脱)或被劫持(放下)脱离后，从空中坠落到落地。
/// - 进入：脱离父节点(GrabHook/CapturePoint)，播放 falldown_loop 下落循环
/// - 本人(IsSelf)：恢复 KCC 物理，受重力下落；落地后播 falldown(一次性)→播完回 Default
/// - 远端：仅播 falldown_loop，跟随同步位置；待本人落地切 Default 后由状态同步切回（方案①）
/// </summary>
public class FallingState : PlayerStateTemplate<PlayerStateController>
{
    private const string AnimDir = "Assets/Loadable/Animations/Vehicle/clawmachine/";

    private bool _landed;
    private bool _wasAirborne; // 至少经历一帧空中，避免入场瞬间地面误判

    public FallingState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    private Transform PlayerRoot =>
        owner.PlayerKCCtrl != null ? owner.PlayerKCCtrl.transform : owner.transform;

    public override void EnterMainState()
    {
        base.EnterMainState();
        _landed = false;
        _wasAirborne = false;

        // 从 GrabHook / CapturePoint 脱离，开始坠落
        PlayerRoot.SetParent(null, true);

        // 解冻 Motor（所有端）：Bound 在所有端冻结了玩家(simulate off/enabled off/freeze)，
        // 且 BoundState.ExitMainState 不负责解冻（委托给后继状态），必须在此恢复——
        // 否则远端副本(车主/他人视角)卡在空中不下坠（本人端有物理表现正常）。
        RestorePhysics();

        // 下落循环动画（所有端；为空资源时 LoadPlay 内部跳过，不阻断）
        owner.PlayerAnimCtrl?.LoadPlay(AnimDir + "falldown_loop", 0);

        // 掉落音效：自己 1P / 他人 3P（Falling 在所有端进入，各端自然分流）
        PlayCraneSound("Crane_Fall");
    }

    public override void MainStateUpdate()
    {
        base.MainStateUpdate();
        if (!owner.IsSelf || _landed) return;
        if (owner.PlayerKCCtrl == null || owner.PlayerKCCtrl.Motor == null) return;

        bool grounded = owner.PlayerKCCtrl.Motor.GroundingStatus.IsStableOnGround;
        if (!grounded)
        {
            _wasAirborne = true;
            return;
        }
        if (_wasAirborne)
        {
            OnLand();
        }
    }

    private void OnLand()
    {
        _landed = true;
        // 落地音效(本人 1P)：OnLand 只在本人端执行；远端的 3P 在 ExitMainState 落地退出时播。
        PlayCraneSound("Crane_Land");
        // 本人落地权威：广播落地，GameVehicleManager 转 op=24 全房，让远端副本退出 Falling
        // （远端 IsSelf=false，永不会自行落地检测，必须靠本条同步切回 Default，否则卡死 falldown_loop）。
        MessageHelper.Broadcast(MessageName.OnWawajiSelfLanded, owner.PlayerID);
        // 落地动画（一次性）播完回 Default
        var ev = owner.PlayerAnimCtrl?.LoadPlay(AnimDir + "falldown", 0);
        if (ev != null)
        {
            ev.OnCompleteEvent(() =>
            {
                if (owner.IsMainState(stateID)) owner.ExitState(stateID);
            });
        }
        else
        {
            owner.ExitState(stateID);
        }
    }

    public override void ExitMainState()
    {
        base.ExitMainState();
        // LoadPlay(layer0) 会把 CurAniState 钉成 TempClip(9999)，而 locomotion 的 guard
        // (BaseKCC.OnGroundInputs → SetPlayerAniState(Run/Idle, false)) 会因 CurAniState>Land 被挡死。
        // 回到 Default 前必须复位为 Idle，否则之后人能移动但永远不播走/跑/跳。所有端都要复位。
        owner.PlayerAnimCtrl?.SetPlayerAniState(PlayerAniState.Idle);
        if (owner.IsSelf)
        {
            RestorePhysics();
        }
        else
        {
            // 远端落地 3P：远端经 op=24 退出 Falling≈落地时刻；本人已在 OnLand 播过 1P，故仅非本人在此播。
            PlayCraneSound("Crane_Land");
        }
    }

    // 被抓玩家阶段音效：group=Locomotion_Group，switch=Crane_*，事件按 1P(自己)/3P(他人) 区分，播在玩家 avatar 上。
    private void PlayCraneSound(string craneStage)
    {
        if (owner == null || owner.Wrap == null || owner.Wrap.Avatar == null) return;
        MessageHelper.Broadcast(MessageName.GameSound, "Locomotion_Group", craneStage,
            owner.IsSelf ? "Play_Locomotion_1P" : "Play_Locomotion_3P", owner.Wrap.Avatar);
    }

    private void RestorePhysics()
    {
        if (owner.PlayerKCCtrl == null) return;
        owner.PlayerKCCtrl.SetFreezeCharacter(false);
        owner.PlayerKCCtrl.enabled = true;
        if (owner.PlayerKCCtrl.Motor != null)
            owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
        owner.PlayerKCCtrl.IsDriveVehicle = false;
    }
}

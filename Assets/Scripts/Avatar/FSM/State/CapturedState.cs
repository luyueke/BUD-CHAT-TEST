using Es;
using FSM;
using Game.Vehicle.PGCVehicle;
using Message;
using UnityEngine;

/// <summary>
/// 被劫持状态（完全束缚）：挣扎失败后进入。
/// - 进入即冻结玩家
/// - 移动到 A 的娃娃机指定锚点 CapturePoint_{slotIndex}（slot 由车主 A 权威分配）并播放被劫持循环动画
/// - 不可再挣脱；仅当车主 A 收纳载具时，由网络层统一退出本状态
/// </summary>
public class CapturedState : PlayerStateTemplate<PlayerStateController>
{
    private string _captorUid;          // 娃娃机驾驶员 A 的 uid
    private int _slotIndex = -1;        // 车主分配的锚点序号
    private Transform _anchor;          // CapturePoint_{slotIndex}
    private Transform _captorMotor;     // 娃娃机 Motor.transform（朝向源，随 Skill3/4 旋转）
    private Transform _originalParent;
    private bool _attached;

    // 进状态后 0.5s 内从被抓处 lerp 到 catch-point
    private const float MoveDuration = 0.5f;
    private float _moveElapsed;
    private Vector3 _fromPos;
    private Quaternion _fromRot;

    private const string AnimDir = "Assets/Loadable/Animations/Vehicle/clawmachine/";

    public CapturedState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    private Transform PlayerRoot =>
        owner.PlayerKCCtrl != null ? owner.PlayerKCCtrl.transform : owner.transform;

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        if (args != null && args.Length > 0 && args[0] != null)
        {
            _captorUid = args[0].ToString();
        }
        if (args != null && args.Length > 1 && args[1] != null)
        {
            int.TryParse(args[1].ToString(), out _slotIndex);
        }
    }

    public override void EnterMainState()
    {
        base.EnterMainState();
        _attached = false;

        FreezePlayer(true);

        var vehicleCtrl = PGCVehicleManager.Inst.GetPGCVehicleController(_captorUid);
        if (vehicleCtrl != null && vehicleCtrl.VehicleKinematic != null)
        {
            // 娃娃机 Motor.transform 作为朝向源（被抓玩家挂点后朝向与娃娃机一致，随其旋转）
            if (vehicleCtrl.VehicleKinematic.Motor != null)
                _captorMotor = vehicleCtrl.VehicleKinematic.Motor.transform;
            if (_slotIndex >= 0)
            {
                // 预制体锚点命名为 catch-point1 ~ catch-point8（1-based）；slot 为 0-based
                _anchor = GameObjectEx.FindComponentByName<Transform>(
                    vehicleCtrl.VehicleKinematic.gameObject, "catch-point" + (_slotIndex + 1));
            }
        }

        if (_anchor != null)
        {
            _originalParent = PlayerRoot.parent;
            PlayerRoot.SetParent(_anchor, true);
            // 不再瞬移：记录当前(被抓处)位姿，由 MainStateLateUpdate 在 0.5s 内 lerp 到 catch-point
            _fromPos = PlayerRoot.position;
            _fromRot = PlayerRoot.rotation;
            _moveElapsed = 0f;
            _attached = true;
        }

        // 被劫持循环动画：吊在 CapturePoint 上漂浮
        owner.PlayerAnimCtrl?.LoadPlay(AnimDir + "float", 0);

        if (owner.IsSelf)
        {
            MessageHelper.Broadcast(MessageName.OnSelfCapturedStateChange, true);
        }
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        if (_attached && _anchor != null)
        {
            PlayerRoot.SetParent(_originalParent, true);
        }
        _anchor = null;
        _originalParent = null;

        FreezePlayer(false);

        if (owner.IsSelf)
        {
            MessageHelper.Broadcast(MessageName.OnSelfCapturedStateChange, false);
        }
    }

    public override void MainStateLateUpdate()
    {
        base.MainStateLateUpdate();
        // KinematicCharacterSystem 每帧强制 transform=TransientPosition（无视 isOnSimulate / parent），
        // 故 SetParent 不足以让被劫持玩家跟随移动的载具，必须每帧 Motor.SetPosition 写 TransientPosition。
        // 与驾驶位 PGCVehicleState 一致：所有端都跑（保证各客户端都看到玩家吊在 catch-point 上）。
        if (_anchor == null || owner.PlayerKCCtrl == null || owner.PlayerKCCtrl.Motor == null) return;
        // 朝向对齐娃娃机：移动到 catch-point 的 0.5s 内从被抓瞬间朝向 lerp 到娃娃机朝向，
        // 到位后持续跟随娃娃机（其旋转时被抓玩家一起转）。娃娃机朝向取 Motor.transform.rotation。
        Quaternion captorRot = _captorMotor != null ? _captorMotor.rotation : _fromRot;
        if (_moveElapsed < MoveDuration)
        {
            _moveElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_moveElapsed / MoveDuration);
            owner.PlayerKCCtrl.Motor.SetPosition(Vector3.Lerp(_fromPos, _anchor.position, t));
            owner.PlayerKCCtrl.Motor.SetRotation(Quaternion.Slerp(_fromRot, captorRot, t));
        }
        else
        {
            owner.PlayerKCCtrl.Motor.SetPosition(_anchor.position);
            owner.PlayerKCCtrl.Motor.SetRotation(captorRot);
        }
    }

    public override void ReleaseData()
    {
        base.ReleaseData();
        _captorUid = null;
        _slotIndex = -1;
        _attached = false;
        _captorMotor = null;
    }

    private void FreezePlayer(bool freeze)
    {
        if (owner.PlayerKCCtrl == null) return;
        owner.PlayerKCCtrl.SetFreezeCharacter(freeze);
        owner.PlayerKCCtrl.enabled = !freeze;
        if (owner.PlayerKCCtrl.Motor != null)
        {
            owner.PlayerKCCtrl.Motor.SetIsOnSimulate(!freeze);
        }
        owner.PlayerKCCtrl.IsDriveVehicle = freeze;
    }

    public string CaptorUid => _captorUid;
    public int SlotIndex => _slotIndex;
}

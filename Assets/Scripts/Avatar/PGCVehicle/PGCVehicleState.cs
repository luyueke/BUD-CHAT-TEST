using Es;
using FSM;
using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle;
using UnityEngine;

/// <summary>
/// PGC 载具状态
/// 在此状态下，角色的物理模拟被接管，位置跟随载具，输入被转发给载具
/// 支持多种模式：锁定驾驶、自由甲板等，由 VehicleSeatConfig 决定
/// </summary>
public class PGCVehicleState : PlayerStateTemplate<PlayerStateController>
{
    private PGCVehicleController mVehicleController;
    private IPGCVehicle mVehicle;
    private string mUid;
    private PgcVehicleConfig mCurrentSeatConfig;

    // 输入缓存
    private Vector2 mCurrentJoystickInput;
    private bool mJumpTriggered;

    public PGCVehicleState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
        
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        if (args != null && args.Length > 0)
        {
            mVehicleController = args[0] as PGCVehicleController;
            mVehicle = args[0] as IPGCVehicle;
        }
        mUid = owner.PlayerID;
    }

    public override void ReleaseData()
    {
        base.ReleaseData();
        mVehicle = null;
        mVehicleController = null;
        mCurrentSeatConfig = null;
    }

    public override void OnEnter()
    {
        // 0. 先通知载具分配座位/初始化（否则第一次进状态拿不到SeatConfig）
        mVehicle?.OnPassengerEnter(mUid);

        // 1. 获取配置（支持运行时覆盖：驾驶/取消驾驶）
        if (mVehicleController != null) mCurrentSeatConfig = mVehicleController.GetSeatConfig();
        
        // 默认配置防空


        // 2. 设置角色物理状态
        if (owner.PlayerKCCtrl != null)
        {
            if (mCurrentSeatConfig.controlType == "Lock")
            {
                // 锁定模式：冻结KCC，关闭碰撞（避免穿模干扰）
                owner.PlayerKCCtrl.SetFreezeCharacter(true);
                owner.PlayerKCCtrl.Motor.SetIsOnSimulate(false);
            }
            else
            {
                // 自由模式：KCC正常运作，依赖物理在载具上行走
                // 载具必须有Collider支持
                owner.PlayerKCCtrl.SetFreezeCharacter(false);
                owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
            }
        }
        
        // 3. 注册输入事件 (仅针对本机玩家)
        if (owner.IsSelf)
        {
            StateEventManager.Inst.RegisterStateEvent<float, float>(mUid, StateEvent.MoveJoystick, OnMoveJoystick);
            StateEventManager.Inst.RegisterStateEvent(mUid, StateEvent.JumpBtn, OnJumpClick);
        }
        PGCVehicleManager.Inst.SetPlayerAnimOverride(mUid);
        
        // 重置输入缓存
        mCurrentJoystickInput = Vector2.zero;
        mJumpTriggered = false;
    }

    public override void OnExit()
    {
        // 1. 恢复角色物理
        if (owner.PlayerKCCtrl != null)
        {
            owner.PlayerKCCtrl.SetFreezeCharacter(false);
            owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
            //还原动画覆盖
            owner.PlayerAnimCtrl.ClearOverrideSpecialAnim();
            owner.PlayerAnimCtrl.CheckAndOverrideSpecialAnim();
            
            // 如果是锁定模式下车，可能需要重置位置到下车点
            // if (mCurrentSeatConfig.lockCharacter) { ... }
        }

        // 2. 注销输入事件
        if (owner.IsSelf)
        {
            StateEventManager.Inst.UnRegisterStateEvent<float, float>(mUid, StateEvent.MoveJoystick, OnMoveJoystick);
            StateEventManager.Inst.UnRegisterStateEvent(mUid, StateEvent.JumpBtn, OnJumpClick);
        }
        
        mCurrentJoystickInput = Vector2.zero;
        mJumpTriggered = false;
        
        base.OnExit();
    }

    public override void CacheState(PlayerState beState)
    {
        base.CacheState(beState);
        
        // 清零控制输入，避免载具继续使用之前的摇杆操作
        mCurrentJoystickInput = Vector2.zero;
        mJumpTriggered = false;
        
        // 立即向载具发送零输入，确保载具停止
        if (mVehicle != null)
        {
            mVehicle?.Drive(Vector2.zero, false);
        }
    }

    public override void MainStateLateUpdate()
    {
        base.MainStateLateUpdate();

        // 支持运行时切换驾驶/自由走动（比如UI按钮点“取消驾驶”后）
        if (mVehicleController != null)
        {
            var cfg = mVehicleController.GetSeatConfig();
            if (cfg != null) mCurrentSeatConfig = cfg;
        }
        
        // 仅在锁定模式下同步位置
        if (mCurrentSeatConfig != null && mCurrentSeatConfig.controlType == "Lock")
        {
            if (mVehicle != null && owner.PlayerKCCtrl != null)
            {
                var seat = mVehicle.GetSeatTransform(mUid);
                if (seat != null)
                {
                    owner.PlayerKCCtrl.Motor.SetPosition(seat.position);
                    owner.PlayerKCCtrl.Motor.SetRotation(seat.rotation);
                }
            }
        }
        else
        {
            // 平台自由走动：限制活动范围（水平面半径）
        }

        // 统一驱动逻辑：仅本机玩家驱动
        if (owner.IsSelf && mCurrentSeatConfig != null)
        {
            bool isControl = mCurrentSeatConfig.controlType == "Free" || mCurrentSeatConfig.controlType == "Lock";
            if (isControl)
            {
                // 更新相机朝向
                if (owner.PlayerKCCtrl != null && owner.PlayerKCCtrl.CameraTarget != null && mVehicleController != null)
                {
                    mVehicleController.SetDriverCameraRotation(owner.PlayerKCCtrl.CameraTarget.rotation);
                }

                // 发送输入给载具
                mVehicle?.Drive(mCurrentJoystickInput, mJumpTriggered);

                // 消费跳跃信号 (下一帧传 false，从而允许 KVC 重置 jumpConsumed)
                mJumpTriggered = false;
            }
        }
    }

    private void OnMoveJoystick(float axisForward, float axisRight)
    {
        // 缓存输入，不再直接调用 Drive
        mCurrentJoystickInput = new Vector2(axisRight, axisForward);

        // 动态刷新配置（驾驶/取消驾驶）
        if (mVehicleController != null)
        {
            var cfg = mVehicleController.GetSeatConfig();
            if (cfg != null) mCurrentSeatConfig = cfg;
        }
    }

    private void OnJumpClick()
    {
        // 缓存跳跃信号
        mJumpTriggered = true;

        // 动态刷新配置（驾驶/取消驾驶）
        if (mVehicleController != null)
        {
            var cfg = mVehicleController.GetSeatConfig();
            if (cfg != null) mCurrentSeatConfig = cfg;
        }
    }
}


using System.Collections.Generic;
using System.Linq;
using Es;
using Game.Avatar;
using Game.Vehicle.PGCVehicle;
using Game.Vehicle.PGCVehicle.KVC;
using Message;
using UnityEngine;


public class PGCVehicleController : MonoBehaviour, IPGCVehicle {

    private PGCKinematicVehicleController vehicleKinematic;
    public PGCKinematicVehicleController VehicleKinematic => vehicleKinematic;

            // 乘客UID -> 座位索引映射
    protected Dictionary<string, int> passengerSeats = new Dictionary<string, int>();
    
    // 用于“载具持有玩家”：记录上车前的父节点，便于下车恢复
    protected Dictionary<string, Transform> passengerOriginalParents = new Dictionary<string, Transform>();

    private PgcVehicleConfig _vehicleConfig;
    private readonly List<Transform> _seatTransforms = new List<Transform>();
    private bool isInDrive = false;

    private string ownerUid;
    public string OwnerUid => ownerUid;

    // 车型配置ID（如娃娃机 160100003 等），供容量等按车型判定
    public int VehicleConfigId => _vehicleConfig != null ? _vehicleConfig.id : 0;
    

    public void Init(PgcVehicleConfig pgcVehicleConfig, string ownerUid){
        this.ownerUid = ownerUid;
        if (pgcVehicleConfig == null) return;
        _vehicleConfig = pgcVehicleConfig;

        // 找到（或缓存）载具的Kinematic控制器
        vehicleKinematic = GetComponentInChildren<PGCKinematicVehicleController>(true);
        BuildSeatsIfNeeded();

        //联机状态玩家触发的鸣笛事件
        MessageHelper.AddListener<string>(MessageName.OnVehicleHonkingStart, OnVehicleTryHonking);
        MessageHelper.AddListener<string>(MessageName.OnVehicleHonkingStop, OnVehicleHonkingStop);
    }

    public void Release()
    {
        if(vehicleKinematic != null)
        {
            vehicleKinematic.Release();
            RemoveSelfVehicleListener();
        }
        MessageHelper.RemoveListener<string>(MessageName.OnVehicleHonkingStart, OnVehicleTryHonking);
        MessageHelper.RemoveListener<string>(MessageName.OnVehicleHonkingStop, OnVehicleHonkingStop);
    }

    public PgcVehicleConfig GetSeatConfig()
    {
        // 当前工程尚未有独立的“座位配置表”，先用整车配置充当座位配置
        return _vehicleConfig;
    }

    public Transform GetSeatTransform(string uid)
    {
        if (_vehicleConfig == null) return null;
        BuildSeatsIfNeeded();

        if (!passengerSeats.TryGetValue(uid, out var idx))
        {
            idx = FindDriverSeatIndex();
            if (idx < 0) idx = 0;
        }

        if (idx >= 0 && idx < _seatTransforms.Count)
        {
            return _seatTransforms[idx];
        }
        return transform;
    }
    
    /// <summary>
    /// 接收来自 Avatar 的输入信号
    /// </summary>
    /// <param name="input">摇杆输入 (x=Right, y=Forward)</param>
    /// <param name="jump">跳跃键</param>
    public virtual void Drive(Vector2 input, bool jump)
    {
        // 优先走载具KCC（推荐）
        if (vehicleKinematic != null && vehicleKinematic is IPGCVehicleInputDriver inputDriver)
        {
            inputDriver.SetInput(input, jump);
            return;
        }

        // 兼容旧实现：子类覆写Drive/OnJump
        if (jump) OnJump();
    }
    protected virtual void OnJump()
    {
        // 默认跳跃行为，子类可覆盖
    }

    public virtual void OnPassengerEnter(string uid)
    {
        if (!passengerSeats.ContainsKey(uid))
        {
            AssignSeat(uid);
        }
        
        // 可以在这里处理载具重心的变化、播放音效等
        // 如果 isDriver = true，通知 UI 显示载具控制面板
    }

    public virtual void OnPassengerExit(string uid)
    {
        if (passengerSeats.ContainsKey(uid))
        {
            passengerSeats.Remove(uid);
        }

        if (passengerOriginalParents != null && passengerOriginalParents.ContainsKey(uid))
        {
            passengerOriginalParents.Remove(uid);
        }
    }



    protected virtual void AssignSeat(string uid)
    {
        BuildSeatsIfNeeded();

        // 找空位：0 通常视作驾驶位，也允许重复（防御性）
        int seatCount = _seatTransforms.Count > 0 ? _seatTransforms.Count : 1;
        for (int i = 0; i < seatCount; i++)
        {
            if (!passengerSeats.ContainsValue(i))
            {
                passengerSeats[uid] = i;
                return;
            }
        }

        // 如果没位子了，兜底分配到 0
        passengerSeats[uid] = 0;
    }

    protected virtual void AssignSeat(string uid, int seatIndex)
    {
        if (seatIndex < 0 )
        {
            AssignSeat(uid);
            return;
        }

        passengerSeats[uid] = seatIndex;
    }

    protected int FindDriverSeatIndex()
    {
        // 简化：默认第0个座位是驾驶位
        return 0;
    }

    public bool IsHasSeat(){
        
        int seatNum = _vehicleConfig.seatOffset.Count;
        if(seatNum > passengerSeats.Count){
            return true;
        }
        return false;
    }

    private void BuildSeatsIfNeeded()
    {
        if (_seatTransforms.Count > 0) return;
        if (_vehicleConfig == null) return;

        // seatOffset 为空时：默认使用载具root当作驾驶位
        var offsets = _vehicleConfig.seatOffset;
        if (offsets == null || offsets.Count == 0)
        {
            _seatTransforms.Add(transform);
            return;
        }

        for (int i = 0; i < offsets.Count; i++)
        {
            var t = new GameObject($"Seat_{i}").transform;
            t.SetParent(transform, false);
            t.localPosition = offsets[i] * 1.7f;
            t.localRotation = Quaternion.identity;
            _seatTransforms.Add(t);
        }
    }

    /// <summary>
    /// UI/交互调用：进入驾驶（摇杆控制载具，角色锁定到驾驶位）
    /// </summary>
    public void EnterDrive(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;

        var driverSeat = FindDriverSeatIndex();
        if (driverSeat < 0) driverSeat = 0;
        AssignSeat(uid, driverSeat);

        // 运行时覆盖：强制驾驶配置
        // 确保玩家已在载具上（载具持有玩家）
        AddPassenger(uid);

        // 进入PGCVehicle状态（仅用于“驾驶接管”）
        EnterDriveState(uid);
        isInDrive = true;

        // 设置自己的载具监听
        if(AccountDataManager.Inst.IsSelf(uid))
        {
            SetSelfVehicleListener();
        }
    }

    public void EnterPassenger(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;
        AddPassenger(uid);
        EnterPassengerState(uid);
        if(_vehicleConfig.controlType == "Free"){
            CancelPassenger(uid);
        }
    }

    /// <summary>
    /// UI/交互调用：取消驾驶（摇杆回到角色；默认仍在载具上自由走动）
    /// keepInVehicle=false 则直接下车回到默认状态
    /// </summary>
    public void CancelDrive(string uid, bool keepInVehicle = true)
    {
        if (string.IsNullOrEmpty(uid)) return;

        isInDrive = false;

        if (!keepInVehicle)
        {
            RemovePassenger(uid);
            return;
        }

        // 自由走动：不劫持摇杆、不锁角色（平台载具可配合范围限制）
        var seatIndex = passengerSeats.TryGetValue(uid, out var idx) ? idx : 0;


        // 退出“驾驶接管”状态，玩家回到正常状态，可进入其他State
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if (playerStateCtrl != null && playerStateCtrl.ContainsCurrentState(PlayerState.PGCVehicle))
        {
            playerStateCtrl.ExitState(PlayerState.PGCVehicle, false);
        }

        if(AccountDataManager.Inst.IsSelf(uid))
        {
            RemoveSelfVehicleListener();
        }
    }

    public void CancelPassenger(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;
        RemovePassenger(uid);

        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if (playerStateCtrl != null && playerStateCtrl.ContainsCurrentState(PlayerState.Passenger))
        {
            playerStateCtrl.ExitState(PlayerState.Passenger, false);
            playerStateCtrl.PlayerAnimCtrl.ClearOverrideSpecialAnim();
            playerStateCtrl.PlayerAnimCtrl.CheckAndOverrideSpecialAnim();
        }
    }

    /// <summary>
    /// PGCVehicleState 进入时注入驾驶员相机朝向（影响载具移动方向计算）
    /// </summary>
    public void SetDriverCameraRotation(Quaternion cameraRotation)
    {
        if (vehicleKinematic != null && vehicleKinematic is IPGCVehicleDriverContext ctx)
        {
            ctx.SetDriverCameraRotation(cameraRotation);
        }
    }

    /// <summary>
    /// 便于UI直接绑按钮：自己(本机)进入/取消驾驶
    /// </summary>
    public void OnClickEnterDriveSelf()
    {
        EnterDrive(AccountDataManager.Inst.Uid);
    }

    public void OnClickCancelDriveSelf(bool keepInVehicle = true)
    {
        CancelDrive(AccountDataManager.Inst.Uid, keepInVehicle);
    }
    
    /// <summary>
    /// 外部调用：让某个角色上车（载具持有玩家，但不进入“驾驶接管”State）
    /// </summary>
    public void AddPassenger(string uid)
    {
        if (string.IsNullOrEmpty(uid)) return;

        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if (playerStateCtrl == null) return;

        // 让载具持有玩家：挂在载具root下（需要处理层级，PlayerStateController 上面几层才是根节点）
        // A(KCC/Motor/Colliders) -> B -> C -> D(PlayerStateController)
        // 我们要挂载的是 A
        Transform playerRoot = playerStateCtrl.transform; // Default to D
        if (playerStateCtrl.PlayerKCCtrl != null)
        {
            playerRoot = playerStateCtrl.PlayerKCCtrl.transform; // Set to A if KCC exists
        }

        // 记录上车前parent
        if (!passengerOriginalParents.ContainsKey(uid))
        {
            passengerOriginalParents[uid] = playerRoot.parent;
        }

        // 挂载 A 到载具
        playerRoot.SetParent(transform, true);

        // 添加玩家动画控制器
        vehicleKinematic.AddPlayerAnimCtrl(uid);

        // 如果是驾驶/乘坐，可能需要关闭玩家自身的碰撞和Motor，以免与载具冲突
        // 简单处理：上车即关闭物理（无论是驾驶还是乘客，如果都是绑定在座位上移动）
        // 但如果乘客是可以在载具平台上自由走动的（如大船），则不能关。
        // 目前 PGC 载具多为单人/小型载具（如车），上车即固定座位。
        // 假设：进入“驾驶”或“乘客”状态时，FSM会处理具体逻辑，但这里先做物理上的依附。
        if (playerStateCtrl.PlayerKCCtrl != null && playerStateCtrl.PlayerKCCtrl.Motor != null)
        {
            // 只有固定座位的载具需要禁用玩家Motor。如果是大飞船自由走动，则不应禁用。
            // 暂时策略：上车都禁用，如果需要自由走动，则不使用 AddPassenger 这种强绑定方式，或者 AddPassenger 后不禁用。
            // 鉴于 AddPassenger 后会 AssignSeat 并 SetPos，推测是固定座位模式。
            playerStateCtrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(false);
            playerStateCtrl.PlayerKCCtrl.Motor.enabled = false; 
        }

        OnPassengerEnter(uid);

        // 进入座位附近（可选）
        var seat = GetSeatTransform(uid);
        if (seat != null)
        {
            // 位置重置是针对 A 的
            playerRoot.position = seat.position;
            playerRoot.rotation = seat.rotation;
        }

    }

    // ───── AI 伙伴乘客 ─────
    // 伙伴与玩家共用 uid，且不在 AvatarController 里：用独立座位 key、直接按 stateCtrl 操作，
    // 避免与驾驶位(玩家)撞键、避免误操作玩家自己的状态机。
    private const string BuddySeatKey = "#buddy";
    private PlayerStateController _buddyPassenger;

    /// <summary>让 AI 伙伴坐上乘客位：挂到载具、禁用其 Motor、分配非驾驶座并定位，动画交载具驱动。</summary>
    public void EnterPassengerForBuddy(PlayerStateController buddyStateCtrl)
    {
        if (buddyStateCtrl == null) return;

        Transform buddyRoot = buddyStateCtrl.PlayerKCCtrl != null
            ? buddyStateCtrl.PlayerKCCtrl.transform
            : buddyStateCtrl.transform;

        _buddyPassenger = buddyStateCtrl;
        if (!passengerOriginalParents.ContainsKey(BuddySeatKey))
        {
            passengerOriginalParents[BuddySeatKey] = buddyRoot.parent;
        }

        buddyRoot.SetParent(transform, true);

        // 伙伴动画交给载具驱动（idle/run 等被 SetOtherPlayerAnimOverride 覆盖成座位姿势）
        if (buddyStateCtrl.PlayerAnimCtrl != null)
        {
            vehicleKinematic.AddPlayerAnimCtrl(buddyStateCtrl.PlayerAnimCtrl);
        }

        if (buddyStateCtrl.PlayerKCCtrl != null && buddyStateCtrl.PlayerKCCtrl.Motor != null)
        {
            buddyStateCtrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(false);
            buddyStateCtrl.PlayerKCCtrl.Motor.enabled = false;
        }

        // 驾驶位(0)已由司机占用，AssignSeat 取第一个空乘客位
        AssignSeat(BuddySeatKey);
        var seat = GetSeatTransform(BuddySeatKey);
        if (seat != null)
        {
            buddyRoot.position = seat.position;
            buddyRoot.rotation = seat.rotation;
        }
    }

    /// <summary>AI 伙伴下车：解绑载具、还原 Motor 与动画覆盖。必须在载具销毁前调用。</summary>
    public void CancelPassengerForBuddy(PlayerStateController buddyStateCtrl)
    {
        var ctrl = buddyStateCtrl ?? _buddyPassenger;
        if (ctrl == null) return;
        ctrl.IsRidingVehicle = false; // 下车恢复待机（用实际乘客 ctrl，地图共享 buddy 也覆盖）

        Transform buddyRoot = ctrl.PlayerKCCtrl != null
            ? ctrl.PlayerKCCtrl.transform
            : ctrl.transform;

        Vector3 currentPos = buddyRoot.position;
        Quaternion currentRot = buddyRoot.rotation;

        if (passengerOriginalParents.TryGetValue(BuddySeatKey, out var originParent))
        {
            buddyRoot.SetParent(originParent, true);
            passengerOriginalParents.Remove(BuddySeatKey);
        }
        else
        {
            buddyRoot.SetParent(null, true);
        }
        passengerSeats.Remove(BuddySeatKey);

        if (ctrl.PlayerAnimCtrl != null)
        {
            vehicleKinematic.RemovePlayerAnimCtrl(ctrl.PlayerAnimCtrl);
            ctrl.PlayerAnimCtrl.ClearOverrideSpecialAnim();
            ctrl.PlayerAnimCtrl.CheckAndOverrideSpecialAnim();
        }

        if (ctrl.PlayerKCCtrl != null && ctrl.PlayerKCCtrl.Motor != null)
        {
            ctrl.PlayerKCCtrl.Motor.SetPositionAndRotation(currentPos, currentRot);
            ctrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(true);
            ctrl.PlayerKCCtrl.Motor.enabled = true;
        }
        _buddyPassenger = null;

        // 通知伙伴下车：GameAIBuddyManager 据此清状态、AIBuddyStandbyBehaviour 据此恢复待机
        MessageHelper.Broadcast(MessageName.OnBuddyVehicleStateChange, ownerUid, false);
    }

    protected virtual void SetSelfVehicleListener()
    {
        MessageHelper.AddListener<int, bool>(MessageName.OnPlayerUseSkill, OnSkill);
        //MessageHelper.AddListener<string>(MessageName.OnVehicleTryHonking, OnVehicleTryHonking);
    }

    protected virtual void RemoveSelfVehicleListener(){
        MessageHelper.RemoveListener<int, bool>(MessageName.OnPlayerUseSkill, OnSkill);
        //MessageHelper.RemoveListener<string>(MessageName.OnVehicleTryHonking, OnVehicleTryHonking);
    }

    protected virtual void OnVehicleTryHonking(string uid){
        if(ownerUid != uid)
        {
            return;
        }
        vehicleKinematic.ChangeHonkingState(true, _vehicleConfig.honkingAudio);
    }

    protected virtual void OnVehicleHonkingStop(string uid){
        if(ownerUid != uid)
        {
            return;
        }
        vehicleKinematic.ChangeHonkingState(false, _vehicleConfig.honkingAudio);
    }

    /// <summary>
    /// 进入“驾驶接管”状态：由 PGCVehicleState 接管玩家输入并驱动载具
    /// </summary>
    protected void EnterDriveState(string uid)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if (playerStateCtrl != null)
        {
            // 缓存重连参数
            playerStateCtrl.ReconnectIntoState(PlayerState.PGCVehicle, this);
            playerStateCtrl.EnterState(PlayerState.PGCVehicle, this);
        }
    }

    protected void EnterPassengerState(string uid)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if (playerStateCtrl != null)
        {
            // 缓存重连参数
            playerStateCtrl.ReconnectIntoState(PlayerState.Passenger, this);
            playerStateCtrl.EnterState(PlayerState.Passenger, this);
        }

        if(AccountDataManager.Inst.IsSelf(uid))
        {
            vehicleKinematic.CameraTarget = playerStateCtrl.PlayerKCCtrl.CameraTarget;
        }
    }
    /// <summary>
    /// 外部调用：让某个角色下车
    /// </summary>
    public void RemovePassenger(string uid)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(uid);
        if (playerStateCtrl != null)
        {
            // 若仍在“驾驶接管”，先退出
            if (playerStateCtrl.ContainsCurrentState(PlayerState.PGCVehicle))
            {
                playerStateCtrl.ExitState(PlayerState.PGCVehicle, false);
            }

            Vector3 currentPosition = playerStateCtrl.transform.position;
            Quaternion currentRotation = playerStateCtrl.transform.rotation;

            // 确定要操作的根节点 A
            Transform playerRoot = playerStateCtrl.transform;
            if (playerStateCtrl.PlayerKCCtrl != null)
            {
                playerRoot = playerStateCtrl.PlayerKCCtrl.transform;
            }

            // 恢复parent
            if (passengerOriginalParents.TryGetValue(uid, out var originParent))
            {
                playerRoot.SetParent(originParent, true);
            }
            else
            {
                playerRoot.SetParent(null, true);
            }

            // 移除玩家动画控制器
            vehicleKinematic.RemovePlayerAnimCtrl(uid);
            
            // 恢复物理和Motor
            if (playerStateCtrl.PlayerKCCtrl != null && playerStateCtrl.PlayerKCCtrl.Motor != null)
            {
                playerStateCtrl.PlayerKCCtrl.Motor.SetPositionAndRotation(currentPosition, currentRotation);
                playerStateCtrl.PlayerKCCtrl.Motor.SetCapsuleCollisionsActivation(true);
                playerStateCtrl.PlayerKCCtrl.Motor.enabled = true;
                
                // 强制重置一下位置状态，防止Motor认为还在之前的位置
                // playerStateCtrl.PlayerKCCtrl.Motor.SetPosition(playerRoot.position); 
            }
        }

        OnPassengerExit(uid);
    }

    public void RemoveAllPassengers()
    {
        // AI 伙伴乘客：在清表/销毁前解绑（RemovePGCVehicle 的 Destroy 在此之后），
        // 这是所有销毁入口（DiscardVehicle / TrapBox / ClientManager）的统一收口，避免伙伴随载具被销毁
        if (_buddyPassenger != null)
        {
            CancelPassengerForBuddy(null);
        }
        var keys = passengerSeats.Keys.ToArray().Clone() as string[];
        foreach (var key in keys)
        {
            CancelPassenger(key);
        }
        passengerSeats.Clear();
        passengerOriginalParents.Clear();
    }

    private void OnSkill(int skillId, bool isPress)
    {
        if(!isInDrive) return;
        vehicleKinematic.OnSkill(skillId, isPress);
    }
}
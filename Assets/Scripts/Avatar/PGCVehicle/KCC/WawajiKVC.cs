using Es;
using Game.Audio;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle.KVC;
using Message;
using RootMotion.FinalIK;
using System.Collections.Generic;
using UnityEngine;

public class WawajiKVC : PGCVehicleBaseKVC
{
    // --- Refs found by name from prefab hierarchy in OnInit ---
    private Collider _detectCollider;       // Trigger zone for player detection
    private Transform _grabHookTransform;   // Grabbed player is parented here

    // --- Vertical movement ---
    // Skill1 = up, Skill2 = down  (aligned with HotAirBallonKVC)
    private Vector3 _targetVerticalVelocity = Vector3.zero;
    private Vector3 _currentVerticalVelocity = Vector3.zero;
    private float _maxHeight = 180.5f;
    private float _minHeight = 5.5f;
    private bool _isSkill1Pressed = false;
    private bool _isSkill2Pressed = false;

    // --- Rotation ---
    // Skill3 = left turn, Skill4 = right turn  (aligned with HotAirBallonKVC)
    private bool _isSkill3Pressed = false;
    private bool _isSkill4Pressed = false;
    private Quaternion _currentVehicleRotation = Quaternion.identity;
    private bool _isRotationInitialized = false;
    private float _rotationSpeed = 35f;

    // --- Grab state (Skill5 = Wawaji exclusive) ---
    private bool _isCatchAnimPlaying = false;
    private bool _pendingClawRetract = false; // skill1Start(伸出)快播完时翻 isSkilling=false 自动进 skill1End(收回)
    private PlayerStateController _grabbedPlayer = null;
    // AimIK weight 由 skill1Start 真实播放进度驱动：0.66s 起 0→1，0.33s 内拉满(≈0.99s)
    private bool _grabPhaseActive = false;
    private bool _skill1StartSeen = false;  // 是否已真正进入 skill1Start（区分"过渡中未播"与"已收回"）
    private bool _boundLockFired = false;   // 本次抓取是否已在拉满时触发 Bound（每次仅一次）
    private bool _pendingIdleAfterGrab = false; // 抓取动画(skill1End)播完后回 Idle 并复位，可再抓
    // 抓着人时 idle 改播的悬停动画（vehicle_586_01 专用）
    private const string FloatIdleClipPath = "Assets/Loadable/Avatar/DefaultSkin/Vehicle/vehicle_586_01/Ani/clawmachine_float.anim";
    // 抓着人时驾驶员 avatar 的 idle 改播的悬停动画
    private const string PlayerFloatClipPath = "Assets/Loadable/Animations/Vehicle/clawmachine/float_idle.anim";
    // 是否处于持人(float)态：此态下载具与驾驶员恒定 Idle(float)、不切 Run，避免 run/idle 切换导致相位错位
    private bool _holdingFloat = false;
    // Skill5 抓取冷却：成功发起抓取那一刻起算，期间不可再抓（仅 self 端门控 + 冷却表现）
    private const float Skill5Cooldown = 5f;
    private float _skill5CooldownRemaining = 0f;

    // --- AimIK ---
    private AimIK _aimIK;
    // 抓取检测球半径（运行时在 plier 上生成 trigger，仅用于 ClosestPoint 距离判定，可调）
    private const float DetectRadius = 2f;
    // 爪头骨骼链：Bone_WWJ_46(顶) → … → Bone_WWJ_60(爪头/aim transform)，须为父子递降链
    private static readonly string[] AimBoneNames =
    {
        "Bone_WWJ_46", "Bone_WWJ_47", "Bone_WWJ_49", "Bone_WWJ_50", "Bone_WWJ_51", "Bone_WWJ_52",
        "Bone_WWJ_53", "Bone_WWJ_54", "Bone_WWJ_55", "Bone_WWJ_56", "Bone_WWJ_57", "Bone_WWJ_60"
    };
    // Bone_WWJ_60 的局部朝向轴（爪头瞄准方向）。同套骨骼6种载具通用，实测后只调这一个值。
    private static readonly Vector3 AimAxis = new Vector3(0f, 0f, -1f);

    public override void OnInit(KinematicCharacterMotor motor, bool isSelf, bool isReconstruction = false)
    {
        base.OnInit(motor, isSelf, isReconstruction);
        Motor.ForceUnground(1f);

        // 出生时抬到固定高度 5（与 _minHeight 下降下限相互独立）
        Vector3 spawnPos = Motor.transform.position;
        spawnPos.y = 5.5f;
        Motor.SetPosition(spawnPos);

        // 载具生成时把 root 节点缩放到 1.7（须在 SetupAimIK 之前，让 AimIK 基于缩放后的骨骼初始化）
        Transform vehicleRootNode = GameObjectEx.FindComponentByName<Transform>(motor.gameObject, "root");
        if (vehicleRootNode != null)
        {
            vehicleRootNode.localScale = Vector3.one * 1.7f;
        }

        // AimIK：普通态 weight=0（SetupAimIK 内置 0）；抓取时由 GrabPlayer/AfterCharacterUpdate 按秒编排 weight
        SetupAimIK(motor.gameObject);

        // 爪头节点 = Bone_WWJ_60（AimIK 实际驱动的爪头骨骼）；被抓玩家挂到它下方
        _grabHookTransform = GameObjectEx.FindComponentByName<Transform>(motor.gameObject, "Bone_WWJ_60");

        // 检测区由 KVC 在爪头 Bone_WWJ_60 上运行时生成（trigger 球，仅用于 ClosestPoint 距离判定，无需 Rigidbody）
        if (_grabHookTransform != null)
        {
            var detectGo = new GameObject("WawajiDetectZone");
            detectGo.transform.SetParent(_grabHookTransform, false);
            detectGo.transform.localPosition = Vector3.zero;
            var sphere = detectGo.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = DetectRadius;
            _detectCollider = sphere;
        }

        _currentVehicleRotation = motor.transform.rotation;
        _isRotationInitialized = true;
    }

    // 代码动态绑定并配置 AimIK，避免每个载具预制体手动配置（6种娃娃机共用同套骨骼）
    private void SetupAimIK(GameObject vehicleRoot)
    {
        var bones = new Transform[AimBoneNames.Length];
        for (int i = 0; i < AimBoneNames.Length; i++)
        {
            bones[i] = GameObjectEx.FindComponentByName<Transform>(vehicleRoot, AimBoneNames[i]);
            if (bones[i] == null)
            {
                Debug.LogError($"WawajiKVC.SetupAimIK: 找不到骨骼 {AimBoneNames[i]}，AimIK 未配置");
                return;
            }
        }

        _aimIK = vehicleRoot.AddComponent<AimIK>();
        _aimIK.solver.transform = bones[bones.Length - 1]; // Bone_WWJ_60 = 爪头/aim transform
        _aimIK.solver.axis = AimAxis;
        _aimIK.solver.SetChain(bones, vehicleRoot.transform); // 设置骨骼链并 Initiate
        _aimIK.solver.IKPositionWeight = 0f; // 抓取时再把 target 设为被抓玩家
    }

    public override void ApplyConfig(PgcVehicleConfig config)
    {
        base.ApplyConfig(config);

        // 抓取爪子动画改为 GrabPlayer 中独立 LoadPlay 播放，不再绑定到 Skill1 的动画事件，
        // 避免点 Skill1(上升) 误触发抓取逻辑。
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Start);
    }

    public override void OnUIInit()
    {
        // Locked vehicle has no jump — skip base broadcast of OnJumpRemainingTimes
    }

    public override void Release()
    {
        base.Release();
        _isSkill1Pressed = false;
        _isSkill2Pressed = false;
        _isSkill3Pressed = false;
        _isSkill4Pressed = false;
        _isRotationInitialized = false;
        _isCatchAnimPlaying = false;
        ReleaseGrabbedPlayer();
        if (_aimIK != null)
            _aimIK.solver.IKPositionWeight = 0f;
    }

    public override void AfterCharacterUpdate(float deltaTime)
    {
        base.AfterCharacterUpdate(deltaTime);

        // Skill5 抓取冷却倒计时（仅 self 端会被 OnSkill 置数，其余端恒为 0）
        if (_skill5CooldownRemaining > 0f)
            _skill5CooldownRemaining -= deltaTime;

        // AimIK weight 由 skill1Start 真实播放进度驱动（非墙钟，避免过渡延迟导致 weight 抢跑到 1）：
        // 播到 0.66s 才开始 0→1，0.33s 内拉满(≈0.99s)并保持；离开 skill1Start(进收回)即归 0 结束。
        if (_grabPhaseActive && _aimIK != null && _pgcVehicleAnimCtrl != null)
        {
            float t = _pgcVehicleAnimCtrl.GetStatePlaybackTime("skill1Start");
            if (t < 0f)
            {
                // t<0：过渡中尚未真正进 skill1Start → 保持 0 等待；若已 seen → 已进收回 → 归 0 结束
                if (_skill1StartSeen)
                {
                    _aimIK.solver.IKPositionWeight = 0f;
                    _grabPhaseActive = false;
                }
            }
            else
            {
                _skill1StartSeen = true;
                float w;
                if (t < 0.66f) w = 0f;
                else if (t < 0.99f) w = (t - 0.66f) / 0.33f;
                else w = 1f;
                _aimIK.solver.IKPositionWeight = Mathf.Clamp01(w);

                // 拉满那一刻触发被抓玩家进 Bound（动画驱动，保证"Bound 时 weight 已满"；每次抓取仅一次）
                if (t >= 0.99f && !_boundLockFired && _grabbedPlayer != null)
                {
                    _boundLockFired = true;
                    MessageHelper.Broadcast(MessageName.OnWawajiClawLocked, _grabbedPlayer.PlayerID);
                }
            }
        }

        // 爪子伸出(skill1Start)快播完时翻 isSkilling=false，抢在 0.968→skill1Loop 之前触发 →skill1End(收回)
        if (_pendingClawRetract && _pgcVehicleAnimCtrl != null
            && _pgcVehicleAnimCtrl.IsStatePlayedTo("skill1Start", 0.9f))
        {
            _pgcVehicleAnimCtrl.SetSkilling(false);
            _pendingClawRetract = false;
        }

        // 收回动画(skill1End)播完：回 Idle 并复位抓取态——这样才能再抓(最多8个)、移动恢复 Run、idle 显示 float。
        if (_pendingIdleAfterGrab && _pgcVehicleAnimCtrl != null
            && _pgcVehicleAnimCtrl.IsStatePlayedTo("skill1End", 0.95f))
        {
            _pendingIdleAfterGrab = false;
            _isCatchAnimPlaying = false;        // 本次抓取动画结束，解除"抓取中"门控
            ReleaseGrabbedPlayer();             // 该玩家已 Bound 由状态机接管，KVC 不再持有
            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
        }

        // If the grabbed player's controller disappeared (disconnect/leave), release safely
        if (_grabbedPlayer != null && _grabbedPlayer.PlayerKCCtrl == null)
        {
            _isCatchAnimPlaying = false;
            ReleaseGrabbedPlayer();
            if (_aimIK != null)
                _aimIK.solver.IKPositionWeight = 0f;
        }
    }

    public override void OnLanded()
    {
        base.OnLanded();
        // Reset grab state on unexpected landing to prevent IK staying active
        if (_isCatchAnimPlaying)
        {
            _isCatchAnimPlaying = false;
            ReleaseGrabbedPlayer();
            if (_aimIK != null)
                _aimIK.solver.IKPositionWeight = 0f;
        }
    }

    // ----- Movement -----

    public override void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // 水平移动：响应摇杆（复用基类移动逻辑，与热气球等一致）
        base.VehicleMove(ref currentVelocity, deltaTime);

        // 垂直：Skill1/2 升降在下方叠加，覆盖 currentVelocity.y
        float posY = Motor.transform.position.y;
        if (posY >= _maxHeight && _targetVerticalVelocity.y > 0f)
        {
            currentVelocity.y = 0f;
            _targetVerticalVelocity = _currentVerticalVelocity = Vector3.zero;
        }
        else if (posY <= _minHeight && _targetVerticalVelocity.y < 0f)
        {
            currentVelocity.y = 0f;
            _targetVerticalVelocity = _currentVerticalVelocity = Vector3.zero;
        }
        else
        {
            _currentVerticalVelocity = Vector3.Lerp(_currentVerticalVelocity, _targetVerticalVelocity, deltaTime * 5f);
            currentVelocity.y = _currentVerticalVelocity.y;
        }

        // 移动状态驱动：基类的 UpdateVehicleVelocity 被本类整体覆写，不会自动跑 SetMoveSpeed/UpdateAniState，
        // 故在此显式调用，否则水平移动永远不切 Run。
        if (_pgcVehicleAnimCtrl != null)
        {
            _pgcVehicleAnimCtrl.SetMoveSpeed(currentVelocity);
            // 驾驶员 avatar 也喂 MoveSpeed（run blend 用），与基类一致
            for (int i = 0; i < _playerAnimCtrls.Count; i++)
                _playerAnimCtrls[i]?.SetMoveSpeed(currentVelocity);
            UpdateAniState(currentVelocity);
        }

        // 行驶音效：升降/平移/旋转 任意移动都播 run 音效（与动画状态解耦）
        UpdateDriveSound(currentVelocity);

        // AimIK 的 target 已直接指向被抓玩家，爪头每帧自动朝向，无需手动跟随
    }

    private bool _isDriveMoving = false;
    /// <summary>娃娃机行驶音效：升降(Skill1/2)、平移(水平速度)、旋转(Skill3/4) 任意移动 → run 音效，停 → idle 音效。
    /// 与动画 Idle/Run 状态解耦（持人时动画被强制 float，故不能依赖动画状态驱动音效）。仅在移动态切换时收发。</summary>
    private void UpdateDriveSound(Vector3 currentVelocity)
    {
        if (_pgcVehicleConfig == null) return;
        float horizontalSpeed = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp).magnitude;
        bool moving = horizontalSpeed > 0.1f
                      || _isSkill1Pressed || _isSkill2Pressed   // 升 / 降
                      || _isSkill3Pressed || _isSkill4Pressed;  // 左转 / 右转
        if (moving == _isDriveMoving) return;
        _isDriveMoving = moving;
        MessageHelper.Broadcast(MessageName.StopGameSound,
            IsSelf ? "Stop_Drive_1P" : "Stop_Drive_3P", Motor.gameObject);
        MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive",
            moving ? _pgcVehicleConfig.vRunAnim.audio : _pgcVehicleConfig.vIdleAnim.audio,
            IsSelf ? "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
    }

    /// <summary>娃娃机行驶音效改由 UpdateDriveSound(移动) 驱动，不跟随动画 Idle/Run；仅保留出场 Start 音效。</summary>
    protected override void OnStateChange(PGCVehicleAniState state)
    {
        if (state == PGCVehicleAniState.Start)
            base.OnStateChange(state);
    }

    /// <summary>娃娃机常态悬空，只按水平速度切 Run/Idle（不依赖 grounded，基类悬空时不切）。
    /// 抓取爪子动画(Skill1)与下降(Skill2)按住期间不被移动状态覆盖。</summary>
    protected override void UpdateAniState(Vector3 currentVelocity)
    {
        if (_pgcVehicleAnimCtrl == null) return;
        // 娃娃机常态悬空，但其 Idle/Run/Skill 都是"地面"locomotion 状态（不用 Fall/Jump），
        // 必须恒定 isGround=true，否则 skill1End 无法过渡回 idle（与热气球/飞艇一致）。
        _pgcVehicleAnimCtrl.ParameterAnimation.SetBool(PGCVehicleAnimParameter.isGround, true);
        if (_isCatchAnimPlaying) return;   // 抓取爪子动画(Skill1)期间保留

        // 持人(float)态：载具与驾驶员恒定 Idle(float)，不切 Run——机器悬浮移动时也不跑，
        // 两者始终是同一个 float idle、由 ForceReplayIdle 对齐，杜绝 run/idle 切换导致的相位错位。
        float moveSpeed = _holdingFloat ? 0f : Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp).magnitude;
        if (moveSpeed > 0.1f)
        {
            if (_pgcVehicleAnimCtrl.CurState != PGCVehicleAniState.Run)
            {
                _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Run);
                // 同帧驱动驾驶员 avatar 一起 Run（基类被整体覆写后丢了这段 → 载具 run 时人物不 run）
                for (int i = 0; i < _playerAnimCtrls.Count; i++)
                    _playerAnimCtrls[i]?.SetPlayerAniState(PlayerAniState.Run, false);
            }
        }
        else
        {
            if (_pgcVehicleAnimCtrl.CurState != PGCVehicleAniState.Idle)
            {
                // SetAniState(Idle) 内：持人 float 态会同帧 ForceReplayIdle 驾驶员 idle（相位对齐）；
                // 此处再 SetPlayerAniState(Idle) 对非 float 态生效，float 态因已是 Idle 会 early-return。
                _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
                for (int i = 0; i < _playerAnimCtrls.Count; i++)
                    _playerAnimCtrls[i]?.SetPlayerAniState(PlayerAniState.Idle, false);
            }
        }
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        // Initialise cached rotation on first call (or after Release reset it)
        if (!_isRotationInitialized)
        {
            _currentVehicleRotation = currentRotation;
            _isRotationInitialized = true;
        }

        float turnDirection = 0f;
        if (_isSkill3Pressed && !_isSkill4Pressed)
            turnDirection = -_rotationSpeed;         // 左右反了，交换符号
        else if (_isSkill4Pressed && !_isSkill3Pressed)
            turnDirection = _rotationSpeed;

        if (turnDirection != 0f && stableMovementData.OrientationSharpness > 0f)
        {
            float rotSpeed   = stableMovementData.OrientationSharpness;
            float rotAngle   = turnDirection * rotSpeed * deltaTime;
            Quaternion delta = Quaternion.Euler(0f, rotAngle, 0f);
            currentRotation  = _currentVehicleRotation * delta;
            _currentVehicleRotation = currentRotation;
        }
        else
        {
            if (Quaternion.Angle(_currentVehicleRotation, currentRotation) > 0.1f)
                _currentVehicleRotation = currentRotation;
            else
                currentRotation = _currentVehicleRotation;
        }
    }

    // ----- Skill dispatch -----

    public override void OnSkill(int skillId, bool isPress, string extraJson = null)
    {
        // Skill3 / Skill4: rotation (independent of vertical movement, same as HotAirBallonKVC)
        if (skillId == (int)SkillType.Skill3)
        {
            _isSkill3Pressed = isPress;
            return;
        }
        if (skillId == (int)SkillType.Skill4)
        {
            _isSkill4Pressed = isPress;
            return;
        }

        // Skill1 / Skill2: vertical movement
        if (skillId == (int)SkillType.Skill1)
        {
            _isSkill1Pressed = isPress;
            HandleVerticalSkill(isPress, up: true);
            return;
        }
        if (skillId == (int)SkillType.Skill2)
        {
            _isSkill2Pressed = isPress;
            HandleVerticalSkill(isPress, up: false);
            return;
        }

        // Skill5: Wawaji exclusive — grab player.
        // 仅驾驶员本人(IsSelf)决定目标并广播；实际抓取由 op=20 在所有客户端统一执行 GrabPlayer。
        if (skillId == (int)SkillType.Skill5 && isPress)
        {
            if (IsSelf && !_isCatchAnimPlaying && _skill5CooldownRemaining <= 0f)
            {
                PlayerStateController target = FindNearestTarget();
                if (target != null)
                {
                    // 成功发起抓取：起 5s 冷却 + 通知 UI 播冷却表现
                    _skill5CooldownRemaining = Skill5Cooldown;
                    MessageHelper.Broadcast(MessageName.OnSkill5CoolDown, Skill5Cooldown);
                    MessageHelper.Broadcast(MessageName.OnWawajiTryGrab,
                        AccountDataManager.Inst.Uid, target.PlayerID);
                }
            }
        }
    }

    private void HandleVerticalSkill(bool isPress, bool up)
    {
        if (!isPress)
        {
            _targetVerticalVelocity = _currentVerticalVelocity = Vector3.zero;
            // 上升(Skill1)、下降(Skill2)都只是移动+音效，不切任何 skill 动画 state
            MessageHelper.Broadcast(MessageName.StopGameSound,
                IsSelf ? "Stop_Skill_1P" : "Stop_Skill_3P", Motor.gameObject);
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill",
                up ? _pgcVehicleConfig.vSkill1EndAnim.audio : _pgcVehicleConfig.vSkill2EndAnim.audio,
                IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
            return;
        }

        if (up)
        {
            // Skill1 — 上升：只移动 + 音效，不播 skill 动画（避免与抓取 Skill5 抢 skill1 动画 state）
            _targetVerticalVelocity = new Vector3(0f, _pgcVehicleConfig.skill1Value, 0f);
            Motor.ForceUnground(0.1f);
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill",
                _pgcVehicleConfig.vSkill1StartAnim.audio,
                IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        }
        else
        {
            // Skill2 — 下降：只移动 + 音效，不播 skill2 动画 state（与上升一致）
            _targetVerticalVelocity = new Vector3(0f, -_pgcVehicleConfig.skill2Value, 0f);
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill",
                _pgcVehicleConfig.vSkill2StartAnim.audio,
                IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        }
    }

    // ----- Player detection -----

    /// <summary>Returns true when at least one non-self player is inside DetectCollider.
    /// Call this from UI to enable/disable the Skill5 grab button.</summary>
    public bool HasNearbyPlayer()
    {
        if (_detectCollider == null) return false;

        foreach (var pair in AvatarController.Inst.GetDic())
        {
            PlayerStateController ctrl = pair.Value;
            if (ctrl == null || ctrl.IsSelf || ctrl.PlayerKCCtrl == null) continue;
            if (IsInsideDetectCollider(ctrl.PlayerKCCtrl.transform.position))
                return true;
        }
        return false;
    }

    /// <summary>是否有"可抓取"目标（检测区内且处于 Default 态）——与实际抓取条件 FindNearestTarget 完全一致。
    /// 供 UI 轮询决定 Skill5 按钮置灰/可用。</summary>
    public override bool HasGrabbableTarget()
    {
        return FindNearestTarget() != null;
    }

    /// <summary>抓着人(≥1)时把 idle 改播 float 悬停，松空(0人)还原。由网络层按持有数变化驱动。</summary>
    public override void SetIdleFloating(bool floating)
    {
        // 先设驾驶员 float（置位 _driverIdleFloatOverridden + 覆盖玩家 idle clip），
        // 再驱动载具——这样之后载具每次进 Idle(SetAniState) 都会同帧重播驾驶员 idle，两者 float 同相位。
        _holdingFloat = floating;
        _pgcVehicleAnimCtrl?.SetDriverIdleFloating(floating, PlayerFloatClipPath);
        _pgcVehicleAnimCtrl?.SetIdleFloating(floating, FloatIdleClipPath);
    }

    private bool IsInsideDetectCollider(Vector3 pos)
    {
        // 检测区相对爪头在世界 y 下移 2：等效把被测点上移 2（与父节点 1.7 缩放无关）
        pos.y += 2.8f;
        return Vector3.Distance(_detectCollider.ClosestPoint(pos), pos) < 0.05f;
    }

    // ----- Grab logic (Skill5) -----

    /// <summary>找出 DetectCollider 内距离最近的非自己玩家（纯查询，无副作用）。</summary>
    private PlayerStateController FindNearestTarget()
    {
        if (_detectCollider == null) return null;

        PlayerStateController nearest = null;
        float nearestDist = float.MaxValue;
        Vector3 vehiclePos = Motor.transform.position;

        foreach (var pair in AvatarController.Inst.GetDic())
        {
            PlayerStateController ctrl = pair.Value;
            if (ctrl == null || ctrl.IsSelf || ctrl.PlayerKCCtrl == null) continue;

            // 只有处于默认基础状态(Default)的玩家才能被抓（在表情/载具/被抓等任何其他状态都不行）
            if (!ctrl.IsMainState(PlayerState.Default)) continue;

            Vector3 pos = ctrl.PlayerKCCtrl.transform.position;
            if (!IsInsideDetectCollider(pos)) continue;

            float dist = Vector3.Distance(vehiclePos, pos);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = ctrl;
            }
        }
        return nearest;
    }

    /// <summary>抓取指定玩家（由 op=20 在所有客户端统一调用，保证各端目标一致）。
    /// 仅负责爪子表现（AimIK weight 按秒编排 + skill1Start→skill1End 动画）；被抓玩家进 Bound 及跟随由状态机处理。</summary>
    public override void GrabPlayer(string targetUid)
    {
        if (_isCatchAnimPlaying) return;
        if (string.IsNullOrEmpty(targetUid)) return;

        PlayerStateController target = AvatarController.Inst.GetPlayerStateCtrl(targetUid);
        if (target == null || target.PlayerKCCtrl == null) return;

        _grabbedPlayer = target;
        _isCatchAnimPlaying = true;

        // 选中玩家：AimIK target 指向该玩家；weight 从 0 开始，由 AfterCharacterUpdate 按秒编排升降
        if (_aimIK != null)
        {
            _aimIK.solver.target = _grabbedPlayer.PlayerKCCtrl.transform;
            _aimIK.solver.IKPositionWeight = 0f;
        }
        _grabPhaseActive = true;
        _skill1StartSeen = false;
        _boundLockFired = false;
        _pendingIdleAfterGrab = true;

        MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill",
            _pgcVehicleConfig.vSkill1StartAnim.audio,
            IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);

        // 播爪子动画：走专用 controller 的 Skill1 状态机（skill1Start/Loop/End 的 clip 已配为爪子动画）。
        // 先 isSkilling=true 进 skill1Start(伸出)，再由 _pendingClawRetract 在快播完时翻 false 进 skill1End(收回)。
        _pgcVehicleAnimCtrl.SetSkilling(true);
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Skill1);
        _pendingClawRetract = true;
    }

    /// <summary>释放当前抓取的玩家（由 op=21 挣脱成功调用）：复位爪子并解除 B 的 parent。</summary>
    public override void ReleasePlayer()
    {
        _isCatchAnimPlaying = false;
        _grabPhaseActive = false;
        _skill1StartSeen = false;
        _boundLockFired = false;
        _pendingIdleAfterGrab = false;
        _pendingClawRetract = false;
        _pgcVehicleAnimCtrl.SetSkilling(false);
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
        if (_aimIK != null)
        {
            _aimIK.solver.target = null;
            _aimIK.solver.IKPositionWeight = 0f;
        }
        ReleaseGrabbedPlayer();
    }

    private void ReleaseGrabbedPlayer()
    {
        if (_grabbedPlayer == null) return;
        if (_grabbedPlayer.PlayerKCCtrl != null)
            _grabbedPlayer.PlayerKCCtrl.transform.SetParent(null, true);
        _grabbedPlayer = null;
    }

    public override void ChangeAirBanner(string strParam) { }
}

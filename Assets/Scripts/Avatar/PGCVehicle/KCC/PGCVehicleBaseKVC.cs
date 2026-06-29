using System.Collections.Generic;
using Game.KinematicCharacter;
using Es;
using UnityEngine;
using Game.Audio;
using Game.Avatar;
using Message;

namespace Game.Vehicle.PGCVehicle.KVC
{
    /// <summary>
    /// PGC载具的 KCC 逻辑基类（KVC = Kinematic Vehicle Controller）
    /// - 由载具侧的 PGCVehicleKinematicController 适配到 KinematicCharacterMotor 的回调管线
    /// - 复用 KinematicCharacterMotor 的碰撞/贴地/跳跃(Grace Time)能力
    /// - 每种特殊载具：继承本类，覆写 UpdateVehicleVelocity/UpdateRotation 等即可客制化
    /// </summary>
    public abstract class PGCVehicleBaseKVC
    {
        [Header("Movement Data (对表/对齐BaseKCC的参数结构)")]
        [SerializeField] protected StableMovementData stableMovementData = new StableMovementData
        {
            MaxStableMoveSpeed = 6f,
            StableMovementSharpness = 15f,
            OrientationSharpness = 10f,
            OrientationMethod = OrientationMethod.TowardsMovement
        };

        [SerializeField] protected AirMovementData airMovementData = new AirMovementData
        {
            MaxAirMoveSpeed = 6f,
            AirAccelerationSpeed = 10f,
            Drag = 0.15f
        };

        [SerializeField] protected JumpingData jumpingData = new JumpingData
        {
            AllowJumpingWhenSliding = true,
            JumpUpSpeed = 10f,
            JumpScalableForwardSpeed = 0f,
            JumpPreGroundingGraceTime = 0.1f,
            JumpPostGroundingGraceTime = 0.1f
        };

        [SerializeField] protected MiscData miscData = new MiscData
        {
            IgnoredColliders = new List<Collider>(),
            BonusOrientationMethod = BonusOrientationMethod.None,
            BonusOrientationSharpness = 10f,
            Gravity = new Vector3(0, -25f, 0)
        };

        protected KinematicCharacterMotor Motor;

        protected Vector3 _moveInputVector;
        protected Vector3 _lookInputVector;
        protected Quaternion _driverCameraRotation = Quaternion.identity;

        protected bool _jumpRequested;
        protected bool _jumpConsumed;
        protected bool _jumpedThisFrame;
        protected float _timeSinceJumpRequested = Mathf.Infinity;
        protected float _timeSinceLastAbleToJump;
        protected Vector3 _internalVelocityAdd = Vector3.zero;
        protected PGCVehicleAnimCtrl _pgcVehicleAnimCtrl;
        protected List<PlayerAnimationCtrl> _playerAnimCtrls = new List<PlayerAnimationCtrl>();
        protected PgcVehicleConfig _pgcVehicleConfig;


        // ----- Configurable (from PgcVehicleConfig; defaults come from serialized data above) -----
        private bool _useGravityInVehicle = false;
        private float _jumpCooldown = 0f;
        private float _jumpCooldownTimer = 0f;
        private int _maxJumpCount = 1;
        private int _remainingJumps = 1;
        private bool _isSelf = false;
        public bool IsSelf
        {
            get { return _isSelf; }
            set { _isSelf = value; }
        }

        protected bool _isReconstruction = false;


        // 分离上升和下落重力
        protected float _gravityUp = 25f;
        protected float _gravityFall = 25f;

        /// <summary>初始化：由 PGCVehicleKinematicController 在 Awake/Start 调用</summary>
        public virtual void OnInit(KinematicCharacterMotor motor, bool isSelf, bool isReconstruction = false)
        {
            Motor = motor;
            _isSelf = isSelf;
            _isReconstruction = isReconstruction;
            ResetJumpState();
            _pgcVehicleAnimCtrl = Motor.GetComponentInChildren<PGCVehicleAnimCtrl>();
            _pgcVehicleAnimCtrl.OnStateChange += OnStateChange;
        }

        public virtual void OnUIInit()
        {
            MessageHelper.Broadcast(MessageName.OnJumpRemainingTimes, _maxJumpCount);
        }

        public virtual void Release()
        {
            _playerAnimCtrls.Clear();
            AkSoundManager.Inst.StopAll(Motor.gameObject);
        }

        /// <summary>
        /// 应用整车配置（缺省字段保持KVC默认值）。由 PGCKinematicVehicleController.InitWithConfig 调用。
        /// </summary>
        public virtual void ApplyConfig(PgcVehicleConfig config)
        {
            if (config == null) return;
            _pgcVehicleConfig = config;

            // 速度
            if (config.moveVelocity > 0f)
            {
                stableMovementData.MaxStableMoveSpeed = config.moveVelocity;
                airMovementData.MaxAirMoveSpeed = config.moveVelocity;
            }

            // 转向/朝向响应（此处按“sharpness”理解；若表里是deg/s，可在子类里重映射）
            if (config.angleVelocity > 0f)
            {
                stableMovementData.OrientationSharpness = config.angleVelocity;
            }

            // 跳跃
            if (config.jumpVelocity > 0f)
            {
                jumpingData.JumpUpSpeed = config.jumpVelocity;
            }

            if (config.jumpCoolDown > 0f)
            {
                _jumpCooldown = config.jumpCoolDown;
            }

            if (config.maxJumpTime > 0)
            {
                _maxJumpCount = Mathf.Max(1, config.maxJumpTime);
            }

            // 下落/重力
            // 只要配置了 jumpVelocity 或 fallVelocity，就认为需要启用重力
            if (config.jumpVelocity > 0f || config.fallVelocity > 0f)
            {
                _useGravityInVehicle = true;
            }

            // 默认上升重力 (25f 为经验值，也可从 miscData.Gravity.magnitude 获取)
            _gravityUp = 25f;
            if (miscData.Gravity.sqrMagnitude > 0) _gravityUp = miscData.Gravity.magnitude;

            // 下落重力：如果有配置 fallVelocity，则使用它作为下落时的重力加速度；否则与上升重力一致
            if (config.fallVelocity > 0f)
            {
                _gravityFall = Mathf.Abs(config.fallVelocity);
            }
            else
            {
                _gravityFall = _gravityUp;
            }

            //按照乘客数量初始化玩家动画控制器列表
             _playerAnimCtrls = new List<PlayerAnimationCtrl>(config.seatOffset.Count);

            // 注意：不再直接修改 miscData.Gravity，而是在运行时动态计算
            // miscData.Gravity = new Vector3(0f, -Mathf.Abs(config.fallVelocity), 0f);

            ResetJumpState();
        }

        public virtual void AddPlayerAnimCtrl(string uid)
        {
            var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
            if(player == null) return;
            if(_playerAnimCtrls.Contains(player.PlayerAnimCtrl)) return;
            _playerAnimCtrls.Add(player.PlayerAnimCtrl);
        }

        public virtual void RemovePlayerAnimCtrl(string uid)
        {
            var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
            if(player == null) return;
            if(!_playerAnimCtrls.Contains(player.PlayerAnimCtrl)) return;
            _playerAnimCtrls.Remove(player.PlayerAnimCtrl);
        }

        // AI 伙伴乘客：伙伴不在 AvatarController 里（无法按 uid 查），直接按 animCtrl 注册/移除
        public virtual void AddPlayerAnimCtrl(PlayerAnimationCtrl ctrl)
        {
            if(ctrl == null) return;
            if(_playerAnimCtrls.Contains(ctrl)) return;
            _playerAnimCtrls.Add(ctrl);
        }

        public virtual void RemovePlayerAnimCtrl(PlayerAnimationCtrl ctrl)
        {
            if(ctrl == null) return;
            _playerAnimCtrls.Remove(ctrl);
        }

        protected virtual void OnStateChange(PGCVehicleAniState state){
            if(state == PGCVehicleAniState.Start){
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Show", _pgcVehicleConfig.vStartAnim.audio, IsSelf ?  "Play_Show_1P" : "Play_Show_3P", Motor.gameObject);
            }else if(state == PGCVehicleAniState.Idle){
                MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ?  "Stop_Drive_1P" : "Stop_Drive_3P", Motor.gameObject);
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive", _pgcVehicleConfig.vIdleAnim.audio, IsSelf ?  "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
            }else if(state == PGCVehicleAniState.Run){
                MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ?  "Stop_Drive_1P" : "Stop_Drive_3P", Motor.gameObject);
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive", _pgcVehicleConfig.vRunAnim.audio, IsSelf ?  "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
            }else if(state == PGCVehicleAniState.Jump){
                MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ?  "Stop_Drive_1P" : "Stop_Drive_3P", Motor.gameObject);
                MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ?  "Stop_Jump_1P" : "Stop_Jump_3P", Motor.gameObject);
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Jump", _pgcVehicleConfig.vJumpAnim.audio, IsSelf ?  "Play_Jump_1P" : "Play_Jump_3P", Motor.gameObject);
            }else if(state == PGCVehicleAniState.Land){
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive", _pgcVehicleConfig.vLandAnim.audio, IsSelf ?  "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
            }
        }

        public virtual void SetInput(Vector2 moveAxis, bool jump)
        {
            moveAxis = Vector2.ClampMagnitude(moveAxis, 1f);

            // 默认：使用“驾驶员相机平面朝向”计算移动方向（对齐玩家KCC的手感）
            // 若未注入相机旋转，则退化为载具自身朝向
            var camRot = _driverCameraRotation == Quaternion.identity ? Motor.transform.rotation : _driverCameraRotation;
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(camRot * Vector3.forward, Motor != null ? Motor.CharacterUp : Vector3.up).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(camRot * Vector3.up, Motor != null ? Motor.CharacterUp : Vector3.up).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor != null ? Motor.CharacterUp : Vector3.up);

            _moveInputVector = cameraPlanarRotation * new Vector3(moveAxis.x, 0f, moveAxis.y);
            _lookInputVector = stableMovementData.OrientationMethod == OrientationMethod.TowardsMovement
                ? (_moveInputVector.sqrMagnitude > 0f ? _moveInputVector.normalized : Vector3.zero)
                : (stableMovementData.OrientationMethod == OrientationMethod.TowardsCamera ? Motor.transform.forward : Vector3.zero);

            if (jump)
            {
                // 冷却/多段跳控制：缺省不改动旧行为（默认1段跳、无冷却）
                if (_jumpCooldownTimer <= 0f && _remainingJumps > 0)
                {
                    if (!_jumpConsumed)
                    {
                        _timeSinceJumpRequested = 0f;
                        _jumpRequested = true;
                    }
                }
            }
            else
            {
                _jumpRequested = false;
                _jumpConsumed = false;
            }
        }

        public virtual void SetDriverCameraRotation(Quaternion cameraRotation)
        {
            _driverCameraRotation = cameraRotation;
        }

        public virtual void AddVelocity(Vector3 velocity)
        {
            _internalVelocityAdd += velocity;
        }

        public virtual bool IsColliderValidForCollisions(Collider coll)
        {
            if (miscData == null || miscData.IgnoredColliders == null || miscData.IgnoredColliders.Count == 0)
            {
                return true;
            }
            return !miscData.IgnoredColliders.Contains(coll);
        }

        public virtual void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            if (_lookInputVector.sqrMagnitude > 0f && stableMovementData.OrientationSharpness > 0f)
            {
                Vector3 smoothedLookInputDirection =
                    Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-stableMovementData.OrientationSharpness * deltaTime)).normalized;
                currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
            }
        }

        protected virtual void GroundMovement(ref Vector3 currentVelocity, float deltaTime, float maxSpeed)
        {
            float currentVelocityMagnitude = currentVelocity.magnitude;
            Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
            currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

            Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
            Vector3 targetMovementVelocity = reorientedInput * maxSpeed;

            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-stableMovementData.StableMovementSharpness * deltaTime));
        }

        protected virtual void AirMovement(ref Vector3 currentVelocity, float deltaTime, float maxSpeed)
        {
            if (_moveInputVector.sqrMagnitude > 0f)
            {
                Vector3 addedVelocity = _moveInputVector * airMovementData.AirAccelerationSpeed * deltaTime;
                Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

                if (currentVelocityOnInputsPlane.magnitude < maxSpeed)
                {
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, maxSpeed);
                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                }
                else
                {
                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                    {
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                    }
                }

                if (Motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                    {
                        Vector3 perpenticularObstructionNormal =
                            Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                    }
                }

                currentVelocity += addedVelocity;
            }

            // 载具：默认不强加重力（更像“受控移动平台”）；需要重力/下落可在子类里叠加 miscData.Gravity
            currentVelocity *= (1f / (1f + (airMovementData.Drag * deltaTime)));
        }

        protected virtual void HandleJump(ref Vector3 currentVelocity, float deltaTime)
        {
            _jumpedThisFrame = false;
            _timeSinceJumpRequested += deltaTime;

            if (_jumpRequested)
            {
                // // 基础接地/土狼时间检查
                // bool isGroundedOrGrace =
                //     (jumpingData.AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                //     || _timeSinceLastAbleToJump <= jumpingData.JumpPostGroundingGraceTime;

                // 允许跳跃条件：
                // 1. 还有剩余次数
                // 2. 本次请求还没被消费
                // 3. 场景A: 是第一跳 (remaining == max)，则必须接地或在GraceTime内
                // 4. 场景B: 是多段跳 (remaining < max)，则允许在空中跳 (前提是 remaining > 0)
                bool canJump = (_remainingJumps > 0 && !_jumpConsumed);

                // if (_remainingJumps == _maxJumpCount)
                // {
                //     // 第一跳必须接地
                //     canJump &= isGroundedOrGrace;
                // }
                // else: 空中跳不需要接地检查

                if (canJump)
                {
                    Vector3 jumpDirection = Vector3.up;
                    Motor.ForceUnground();

                    // 应用跳跃速度：先减去当前的垂直分量，再加跳跃速度（实现瞬间改写垂直速度）
                    currentVelocity += (jumpDirection * jumpingData.JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                    currentVelocity += (_moveInputVector * jumpingData.JumpScalableForwardSpeed);

                    _jumpRequested = false;
                    _jumpConsumed = true;
                    _jumpedThisFrame = true;
                    _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Jump);

                    if(_playerAnimCtrls.Count > 0)
                    {
                        foreach(var playerAnimCtrl in _playerAnimCtrls)
                        {
                            playerAnimCtrl.SetPlayerAniTriggerAgain(PlayerAniState.Jump);
                        }
                    }

                    MessageHelper.Broadcast(MessageName.OnJumpInputCoolDown, _jumpCooldown);

                    _remainingJumps--;
                    MessageHelper.Broadcast(MessageName.OnJumpRemainingTimes, _remainingJumps);
                    if (_jumpCooldown > 0f)
                    {
                        _jumpCooldownTimer = _jumpCooldown;
                    }
                }
            }
        }

        public virtual void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 对齐 BaseKCC 的 VehicleMove：不区分地面/空中，主要靠输入驱动；无输入时速度归零（可选重力）
            VehicleMove(ref currentVelocity, deltaTime);

            HandleJump(ref currentVelocity, deltaTime);

            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero;
            }

            _pgcVehicleAnimCtrl.SetMoveSpeed(currentVelocity);
            if(_playerAnimCtrls.Count > 0)
            {
                foreach(var playerAnimCtrl in _playerAnimCtrls)
                {
                    playerAnimCtrl.SetMoveSpeed(currentVelocity);
                }
            }
            UpdateAniState(currentVelocity);
        }

        protected virtual void UpdateAniState(Vector3 currentVelocity)
        {
            if (_pgcVehicleAnimCtrl == null) return;

            bool isGrounded = Motor.GroundingStatus.IsStableOnGround || Motor.GroundingStatus.FoundAnyGround;
            _pgcVehicleAnimCtrl.ParameterAnimation.SetBool(PGCVehicleAnimParameter.isGround, isGrounded);

            if (_jumpedThisFrame) return;

            float moveSpeed = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp).magnitude;

            if (isGrounded)
            {
                if (moveSpeed > 0.1f)
                {
                    if (_pgcVehicleAnimCtrl.CurState != PGCVehicleAniState.Run)
                    {
                        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Run);
                        if(_playerAnimCtrls.Count > 0)
                        {
                            foreach(var playerAnimCtrl in _playerAnimCtrls)
                            {
                                playerAnimCtrl.SetPlayerAniState(PlayerAniState.Run, false);
                            }
                        }
                    }
                }
                else
                {
                    if (_pgcVehicleAnimCtrl.CurState != PGCVehicleAniState.Idle && _pgcVehicleAnimCtrl.CurState != PGCVehicleAniState.Land)
                    {
                        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
                        if(_playerAnimCtrls.Count > 0)
                        {
                            foreach(var playerAnimCtrl in _playerAnimCtrls)
                            {
                                playerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle, false);
                            }
                        }
                    }
                    else if (_pgcVehicleAnimCtrl.CurState == PGCVehicleAniState.Land)
                    {
                        // 落地动画播放完毕后，自动切回 Idle
                        if (_pgcVehicleAnimCtrl.IsCurrentAnimFinished())
                        {
                            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
                            if(_playerAnimCtrls.Count > 0)
                            {
                                foreach(var playerAnimCtrl in _playerAnimCtrls)
                                {
                                    playerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle, false);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                float verticalSpeed = Vector3.Dot(currentVelocity, Motor.CharacterUp);
                if (verticalSpeed < -0.1f)
                {
                    // 如果正在播放跳跃动画且未结束，则优先保持 Jump 状态
                    if (_pgcVehicleAnimCtrl.CurState == PGCVehicleAniState.Jump && !_pgcVehicleAnimCtrl.IsCurrentAnimFinished())
                    {
                        return;
                    }

                    // 空中下落时，如果有移动速度，切换到 Run 状态
                    if (moveSpeed > 0.1f)
                    {
                        if (_pgcVehicleAnimCtrl.CurState != PGCVehicleAniState.Run)
                        {
                            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Run);
                            if(_playerAnimCtrls.Count > 0)
                            {
                                foreach(var playerAnimCtrl in _playerAnimCtrls)
                                {
                                    playerAnimCtrl.SetPlayerAniState(PlayerAniState.Run, false);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (_pgcVehicleAnimCtrl.CurState != PGCVehicleAniState.Fall)
                        {
                            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Fall);
                        }
                    }
                }
            }
        }

        public virtual void AfterCharacterUpdate(float deltaTime)
        {
            if (_jumpCooldownTimer > 0f)
            {
                _jumpCooldownTimer -= deltaTime;
                if (_jumpCooldownTimer < 0f) _jumpCooldownTimer = 0f;
            }

            if (_jumpRequested && _timeSinceJumpRequested > jumpingData.JumpPreGroundingGraceTime)
            {
                _jumpRequested = false;
            }

            if (jumpingData.AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
            {
                if (!_jumpedThisFrame)
                {
                    _jumpConsumed = false;
                    
                    // 落地重置多段跳 (仅在未跳跃时重置，防止跳跃当帧被立刻重置)
                    if(_remainingJumps != _maxJumpCount)
                    {
                        MessageHelper.Broadcast(MessageName.OnJumpRemainingTimes, _maxJumpCount);
                    }
                    _remainingJumps = _maxJumpCount;
                }
                _timeSinceLastAbleToJump = 0f;
            }
            else
            {
                _timeSinceLastAbleToJump += deltaTime;
            }
        }

        public virtual void OnLanded() 
        {
            if (_pgcVehicleAnimCtrl != null)
            {
                _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Land);
            }

            if(_playerAnimCtrls.Count > 0)
            {
                foreach(var playerAnimCtrl in _playerAnimCtrls)
                {
                    playerAnimCtrl.ParameterAnimation.SetBool(PlayerAniPrameter.IsGround, true);
                    var animState = _moveInputVector == Vector3.zero ? PlayerAniState.Idle : PlayerAniState.Run;
                    playerAnimCtrl.SetPlayerAniState(animState, false);
                    playerAnimCtrl.ParameterAnimation.SetTrigger(PlayerAniPrameter.LandOn);
                }
            }
            // 播放落地音效
        }
        public virtual void OnLeaveStableGround() { }

        private void ResetJumpState()
        {
            _jumpCooldownTimer = 0f;
            _remainingJumps = Mathf.Max(1, _maxJumpCount);
            MessageHelper.Broadcast(MessageName.OnJumpRemainingTimes, _maxJumpCount);
        }

        /// <summary>
        /// 对齐 BaseKCC.VehicleAirMovement 的载具移动方式：无输入归零；可选重力；阻力使用 airMovementData.Drag
        /// </summary>
        protected virtual void VehicleMove(ref Vector3 currentVelocity, float deltaTime)
        {
            Vector3 up = Motor != null ? Motor.CharacterUp : Vector3.up;
            Vector3 vertical = Vector3.Project(currentVelocity, up);
            Vector3 planar = Vector3.ProjectOnPlane(currentVelocity, up);

            if (_moveInputVector.sqrMagnitude > 0f)
            {
                Vector3 addedVelocity = _moveInputVector * airMovementData.AirAccelerationSpeed * deltaTime;
                Vector3 currentVelocityOnInputsPlane = planar;

                if (currentVelocityOnInputsPlane.magnitude < airMovementData.MaxAirMoveSpeed)
                {
                    Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, airMovementData.MaxAirMoveSpeed);
                    addedVelocity = newTotal - currentVelocityOnInputsPlane;
                }
                else
                {
                    if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                    {
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                    }
                }

                if (Motor != null && Motor.GroundingStatus.FoundAnyGround)
                {
                    if (Vector3.Dot(planar + addedVelocity, addedVelocity) > 0f)
                    {
                        Vector3 perpenticularObstructionNormal =
                            Vector3.Cross(Vector3.Cross(up, Motor.GroundingStatus.GroundNormal), up).normalized;
                        addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                    }
                }

                planar += addedVelocity;
            }
            else
            {
                // 无输入：对齐 BaseKCC 的载具行为 -> 平面速度归零
                planar = Vector3.zero;
                if (!_useGravityInVehicle)
                {
                    vertical = Vector3.zero;
                }
            }

            // 可选重力
            if (_useGravityInVehicle)
            {
                // 根据垂直速度方向选择重力
                float dot = Vector3.Dot(vertical, up);
                float g = (dot > 0) ? _gravityUp : _gravityFall;
                
                // 应用重力 (向下)
                vertical += (-up * g) * deltaTime;
            }

            // Drag（对齐 BaseKCC）
            float drag = airMovementData.Drag;
            if (drag > 0f)
            {
                planar *= (1f / (1f + (drag * deltaTime)));
                vertical *= (1f / (1f + (drag * deltaTime)));
            }

            currentVelocity = planar + vertical;
        }

        // (HandleJump moved above; keep single implementation)

        public virtual void ChangeHonkingState(bool isHonking, string honkingSound)
        {
            if(isHonking)
            {
                MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Whistle", honkingSound, IsSelf ?  "Play_Whistle_1P" : "Play_Whistle_3P", Motor.gameObject);
            }
            else
            {
                MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ?  "Stop_Whistle_1P" : "Stop_Whistle_3P", Motor.gameObject);
            }
        }

        public abstract void OnSkill(int skillId, bool isPress, string extraJson = null);

        public abstract void ChangeAirBanner(string strParam);

        /// <summary>抓取指定玩家（仅娃娃机等特殊载具实现，由网络层 op=20 在所有客户端统一驱动）。</summary>
        public virtual void GrabPlayer(string targetUid) { }

        /// <summary>释放当前抓取的玩家（由网络层 op=21 挣脱成功驱动）。</summary>
        public virtual void ReleasePlayer() { }

        /// <summary>当前是否有可抓取的目标（仅娃娃机实现）。供 UI 决定 Skill5 按钮置灰/可用。</summary>
        public virtual bool HasGrabbableTarget() { return false; }

        /// <summary>抓着人(≥1)时 idle 改播 float，松空(0)还原（仅娃娃机实现）。</summary>
        public virtual void SetIdleFloating(bool floating) { }

    }
}



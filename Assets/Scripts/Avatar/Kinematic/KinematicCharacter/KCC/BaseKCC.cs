using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData.BaseInfo;
using GameData.Manager;
using Newtonsoft.Json;
using UnityEngine;

public interface IKCController
{
    KCCType KCCType { get; }

    bool IsFastRun { get;}

    bool IsMoving { get; }

    Vector3 _gravity { get; set; }

    StableMovementData StableMovementData { get; }
    AirMovementData AirMovementData { get; }
    JumpingData JumpingData { get; }
    MiscData MiscData { get; }
    FastRunData FastRunData { get; }



    void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl);

    void OnExit();

    void SetInputs(ref PlayerCharacterInputs inputs, Vector3 moveInputVector, Quaternion cameraPlanarRotation, Vector3 cameraPlanarDirection);

    void UpdateRotation(ref Quaternion currentRotation, float deltaTime);

    void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime);

    void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime);

    void AfterCharacterUpdate(float deltaTime);

    bool IsColliderValidForCollisions(Collider coll);

    void AddVelocity(Vector3 velocity);

    void OnLanded();

    void OnLeaveStableGround();
}

[Serializable]
public class StableMovementData
{
    /// <summary>
    /// 地面慢跑移动速度
    /// </summary>
    public float MaxStableMoveSpeed;
    /// <summary>
    /// 地面加速度
    /// </summary>
    public float StableMovementSharpness;
    /// <summary>
    /// 地面转速度
    /// </summary>
    public float OrientationSharpness;
    public OrientationMethod OrientationMethod;
}

[Serializable]
public class AirMovementData
{
    /// <summary>
    /// 空中移动速度
    /// </summary>
    public float MaxAirMoveSpeed;
    /// <summary>
    /// 空中加速度
    /// </summary>
    public float AirAccelerationSpeed;
    /// <summary>
    /// 阻力
    /// </summary>
    public float Drag;
}

[Serializable]
public class JumpingData
{
    public bool AllowJumpingWhenSliding;
    public float JumpUpSpeed;
    public float JumpScalableForwardSpeed;
    public float JumpPreGroundingGraceTime;
    public float JumpPostGroundingGraceTime;
}

[Serializable]
public class MiscData
{
    public List<Collider> IgnoredColliders;
    public BonusOrientationMethod BonusOrientationMethod;
    public float BonusOrientationSharpness;
    public Vector3 Gravity;
}



[Serializable]
public class FastRunData
{
    /// <summary>
    /// 开始进入快跑的时间
    /// </summary>
    public float EnterFastTime;
    /// <summary>
    /// 到达最快速度需要的时间
    /// </summary>
    public float MaxFastTime;
    /// <summary>
    /// 地面快跑速度
    /// </summary>
    public float FastRunSpeed;
}

public abstract class BaseKCC : IKCController
{
    protected StableMovementData m_StableMovementData;
    protected AirMovementData m_AirMovementData;
    protected JumpingData m_JumpingData;
    protected MiscData m_MiscData;
    protected FastRunData m_FastRunData;

    protected KinematicCharacterMotor Motor;
    protected PlayerAnimationCtrl PlayerAnimCtrl;

    protected string PlayerID;

    public bool IsSelf {
        get;
        private set;
    }

    protected Vector3 _moveInputVector;
    protected Vector3 _lookInputVector;
    protected bool _jumpRequested = false;
    protected bool _jumpConsumed = false;
    protected bool _jumpedThisFrame = false;
    protected float _timeSinceJumpRequested = Mathf.Infinity;
    protected float _timeSinceLastAbleToJump = 0f;
    protected Vector3 _internalVelocityAdd = Vector3.zero;
    protected float _stableMoveSpeed;
    protected float _airMoveSpeed;
    protected float _stableFastRunOffset;
    protected float _airFastRunOffset;
    protected float _fastRunUsingTime;
    

    /// <summary>
    /// 载具原始速度和加速度（用于退出载具时恢复）
    /// </summary>
    private float _originalMaxAirMoveSpeed;
    private float _originalAirAccelerationSpeed;

    public Vector3 _gravity
    {
        get { return m_MiscData.Gravity; }
        set { m_MiscData.Gravity = value; }
    }
    public abstract KCCType KCCType { get; }
    public StableMovementData StableMovementData => m_StableMovementData;
    public AirMovementData AirMovementData => m_AirMovementData;
    public JumpingData JumpingData => m_JumpingData;
    public MiscData MiscData => m_MiscData;
    public FastRunData FastRunData => m_FastRunData;

    public bool IsFastRun { get; private set; }
    
    public bool IsMoving { get; private set; }


    #region 退出当前场景待清空数据
    private AnimIKController animtorIK;

    private VehicleInfo vehicleInfo;

    private Animator animator;

    private string CurPoseData = string.Empty;

    #endregion

    public BaseKCC(string uid, bool isSelf) {
        IsSelf = isSelf;
        PlayerID = uid;

        m_StableMovementData = new StableMovementData
        {
            MaxStableMoveSpeed = 3.2f,
            StableMovementSharpness = 10000f,
            OrientationSharpness = 10000f,
            OrientationMethod = OrientationMethod.TowardsMovement
        };

        m_AirMovementData = new AirMovementData
        {
            MaxAirMoveSpeed = 4.2f,
            AirAccelerationSpeed = 15f,
            Drag = 0.1f,
        };

        m_JumpingData = new JumpingData
        {
            AllowJumpingWhenSliding = true,
            JumpUpSpeed = 10f,
            JumpScalableForwardSpeed = 0f,
            JumpPreGroundingGraceTime = 0.1f,
            JumpPostGroundingGraceTime = 0.1f,
        };

        m_MiscData = new MiscData
        {
            IgnoredColliders = new List<Collider>(),
            BonusOrientationMethod = BonusOrientationMethod.None,
            BonusOrientationSharpness = 10f,
            Gravity = new Vector3(0, -25f, 0)
        };

        m_FastRunData = new FastRunData
        {
            EnterFastTime = 2f,
            MaxFastTime = 2.5f,
            FastRunSpeed = 6f
        };
    }

    public virtual void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl)
    {
        Motor = motor;
        PlayerAnimCtrl = animCtrl;

        _fastRunUsingTime = m_FastRunData.MaxFastTime - m_FastRunData.EnterFastTime;
        _stableFastRunOffset = m_FastRunData.FastRunSpeed - m_StableMovementData.MaxStableMoveSpeed;
        _airFastRunOffset = m_FastRunData.FastRunSpeed - m_AirMovementData.MaxAirMoveSpeed;
    }

    public virtual void OnExit()
    {
        Motor = null;
        PlayerAnimCtrl = null;
    }

    private void SetMaxMoveSpeed(PlayerCharacterInputs inputs)
    {
        float pressTime = Mathf.Clamp(inputs.PressJoystickTime, m_FastRunData.EnterFastTime, m_FastRunData.MaxFastTime);
        if (GameDataManager.Inst.globalSettingData != null &&
            GameDataManager.Inst.globalSettingData.automaticRunning == 1)
        {
            pressTime = m_FastRunData.EnterFastTime;
        }
        PlayerAnimCtrl.SetPressJoystickTime(pressTime);
        float curFastRunTime =pressTime - m_FastRunData.EnterFastTime;

        if (curFastRunTime > 0)
        {
            if (!IsFastRun)
            {
                IsFastRun = true;
                OnRunStateChange();
                StateEventManager.Inst.TriggerStateEvent(PlayerID, StateEvent.FastRun, IsFastRun);
            }
        }
        else
        {
            if (IsFastRun)
            {
                IsFastRun = false;
                OnRunStateChange();
                StateEventManager.Inst.TriggerStateEvent(PlayerID, StateEvent.FastRun, IsFastRun);

            }
        }

        float statbleAddSpeed = curFastRunTime * _stableFastRunOffset / _fastRunUsingTime;
        float airAddSpeed = curFastRunTime * _airFastRunOffset / _fastRunUsingTime;
        _stableMoveSpeed = m_StableMovementData.MaxStableMoveSpeed + statbleAddSpeed;
        _airMoveSpeed = m_AirMovementData.MaxAirMoveSpeed + airAddSpeed;
    }

    protected virtual void OnRunStateChange() {

    }

    public virtual void SetInputs(ref PlayerCharacterInputs inputs, Vector3 moveInputVector, Quaternion cameraPlanarRotation, Vector3 cameraPlanarDirection)
    {
        if (Motor.IsOnSimulate)
        {
            IsMoving = (inputs.MoveAxisForward != 0 || inputs.MoveAxisRight != 0);
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                OnGroundInputs(moveInputVector, ref inputs);
            }
            else
            {
                OnAirInputs(moveInputVector, ref inputs);
            }

            SetMaxMoveSpeed(inputs);
        }
        
        // Move and look inputs
        _moveInputVector = cameraPlanarRotation * moveInputVector.normalized;

        switch (m_StableMovementData.OrientationMethod)
        {
            case OrientationMethod.TowardsCamera:
                _lookInputVector = cameraPlanarDirection;
                break;
            case OrientationMethod.TowardsMovement:
                _lookInputVector = _moveInputVector.normalized;
                break;
            case OrientationMethod.NoToward:
                _lookInputVector = Vector3.zero;
                break;
        }

        // Jumping input
        if (inputs.JumpDown)
        {
            _timeSinceJumpRequested = 0f;
            _jumpRequested = true;
        }
    }


    protected virtual void OnGroundInputs(Vector3 moveInputVector, ref PlayerCharacterInputs inputs)
    {
        if (Motor.IsDriveVehicle)
        {
            if (animtorIK == null || vehicleInfo == null || animator == null)
            {
                animtorIK = AvatarController.Inst.SelfWrap?.Avatar?.GetComponent<AnimIKController>();
                animator = AvatarController.Inst.SelfWrap?.Avatar?.GetComponent<Animator>();
                vehicleInfo = GameDataManager.Inst.mapGlobalData?.GetCurInfo<VehicleInfo>();
            }
        }
        else
        {
            var aniState = moveInputVector != Vector3.zero ? PlayerAniState.Run : PlayerAniState.Idle;
            PlayerAnimCtrl.SetPlayerAniState(aniState, false);
        }

    }

    protected virtual void OnAirInputs(Vector3 moveInputVector,ref PlayerCharacterInputs inputs)
    {

    }



    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        if (_lookInputVector.sqrMagnitude > 0f && m_StableMovementData.OrientationSharpness > 0f)
        {
            // Smoothly interpolate from current to target look direction
            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-m_StableMovementData.OrientationSharpness * deltaTime)).normalized;

            // Set the current rotation (which will be used by the KinematicCharacterMotor)
            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
        }

        Vector3 currentUp = (currentRotation * Vector3.up);
        if (m_MiscData.BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
        {
            // Rotate from current up to invert gravity
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -_gravity.normalized, 1 - Mathf.Exp(-m_MiscData.BonusOrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }
        else if (m_MiscData.BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
        {
            if (Motor.GroundingStatus.IsStableOnGround)
            {
                Vector3 initialCharacterBottomHemiCenter = Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-m_MiscData.BonusOrientationSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * Motor.Capsule.radius));
            }
            else
            {
                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -_gravity.normalized, 1 - Mathf.Exp(-m_MiscData.BonusOrientationSharpness * deltaTime));
                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
            }
        }
        else
        {
            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-m_MiscData.BonusOrientationSharpness * deltaTime));
            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
        }
    }

    /// <summary>
    /// 地面移动
    /// </summary>
    /// <param name="currentVelocity"></param>
    /// <param name="deltaTime"></param>
    protected virtual void GroundMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        float currentVelocityMagnitude = currentVelocity.magnitude;

        Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;

        // Reorient velocity on slope
        currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

        // Calculate target velocity
        Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
        Vector3 targetMovementVelocity = reorientedInput * _stableMoveSpeed;

        // Smooth movement Velocity
        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1f - Mathf.Exp(-m_StableMovementData.StableMovementSharpness * deltaTime));
    }

    /// <summary>
    /// 空中移动
    /// </summary>
    /// <param name="currentVelocity"></param>
    /// <param name="deltaTime"></param>
    protected virtual void AirMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_moveInputVector.sqrMagnitude > 0f)
        {
            Vector3 addedVelocity = _moveInputVector * m_AirMovementData.AirAccelerationSpeed * deltaTime;

            Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

            // Limit air velocity from inputs
            if (currentVelocityOnInputsPlane.magnitude < _airMoveSpeed)
            {
                // clamp addedVel to make total vel not exceed max vel on inputs plane
                Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, _airMoveSpeed);
                addedVelocity = newTotal - currentVelocityOnInputsPlane;
            }
            else
            {
                // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                {
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                }
            }

            // Prevent air-climbing sloped walls
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                {
                    Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                }
            }

            // Apply added velocity
            currentVelocity += addedVelocity;
        }

        // Gravity
        currentVelocity += _gravity * deltaTime;

        // Drag
        currentVelocity *= (1f / (1f + (m_AirMovementData.Drag * deltaTime)));
    }


    protected void VehicleAirMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_moveInputVector.sqrMagnitude > 0f)//向量平方长度大于0开始计算
        {
            Vector3 addedVelocity = _moveInputVector * m_AirMovementData.AirAccelerationSpeed * deltaTime;

            Vector3 currentVelocityOnInputsPlane = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);

            // Limit air velocity from inputs
            if (currentVelocityOnInputsPlane.magnitude < m_AirMovementData.MaxAirMoveSpeed)
            {
                // clamp addedVel to make total vel not exceed max vel on inputs plane
                Vector3 newTotal = Vector3.ClampMagnitude(currentVelocityOnInputsPlane + addedVelocity, m_AirMovementData.MaxAirMoveSpeed);
                addedVelocity = newTotal - currentVelocityOnInputsPlane;
            }
            else
            {
                // Make sure added vel doesn't go in the direction of the already-exceeding velocity
                if (Vector3.Dot(currentVelocityOnInputsPlane, addedVelocity) > 0f)
                {
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, currentVelocityOnInputsPlane.normalized);
                }
            }

            // Prevent air-climbing sloped walls
            if (Motor.GroundingStatus.FoundAnyGround)
            {
                if (Vector3.Dot(currentVelocity + addedVelocity, addedVelocity) > 0f)
                {
                    Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                    addedVelocity = Vector3.ProjectOnPlane(addedVelocity, perpenticularObstructionNormal);
                }
            }

            // Apply added velocity
            currentVelocity += addedVelocity;

            currentVelocity *= (1f / (1f + (m_AirMovementData.Drag * deltaTime)));
        }
        else
        {
            currentVelocity = Vector3.zero;
        }

    }

    /// <summary>
    /// 处理跳跃
    /// </summary>
    /// <param name="currentVelocity"></param>
    /// <param name="deltaTime"></param>
    protected virtual void HandleJump(ref Vector3 currentVelocity, float deltaTime)
    {
        _jumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;
        if (_jumpRequested)
        {
            // See if we actually are allowed to jump
            if (!_jumpConsumed && ((m_JumpingData.AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround) || _timeSinceLastAbleToJump <= m_JumpingData.JumpPostGroundingGraceTime))
            {
                // Calculate jump direction before ungrounding
                Vector3 jumpDirection = Motor.CharacterUp;
                jumpDirection = new Vector3(0, 1f, 0);
                //Remove ground detection
                // if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
                // {
                //     jumpDirection = Motor.GroundingStatus.GroundNormal;
                // }

                // Makes the character skip ground probing/snapping on its next update.
                // If this line weren't here, the character would remain snapped to the ground when trying to jump. Try commenting this line out and see.
                Motor.ForceUnground();

                // Add to the return velocity and reset jump state
                currentVelocity += (jumpDirection * m_JumpingData.JumpUpSpeed) - Vector3.Project(currentVelocity, Motor.CharacterUp);
                currentVelocity += (_moveInputVector * m_JumpingData.JumpScalableForwardSpeed);
                _jumpRequested = false;
                _jumpConsumed = true;
                _jumpedThisFrame = true;
            }
        }
    }

    /// <summary>
    /// 载具移动
    /// </summary>
    /// <param name="currentVelocity"></param>
    /// <param name="deltaTime"></param>
    public virtual void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        VehicleAirMovement(ref currentVelocity, deltaTime);
        ////载具不区分空中和地面移动方式
        //if (Motor.GroundingStatus.IsStableOnGround)
        //{
        //    GroundMovement(ref currentVelocity, deltaTime);
        //}
        //// Air movement
        //else
        //{
        //    VehicleAirMovement(ref currentVelocity, deltaTime);
        //}

        // Handle jumping
        HandleJump(ref currentVelocity, deltaTime);

        // Take into account additive velocity
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }

        PlayerAnimCtrl.SetVehicleMoveSpeed(currentVelocity);
    }

    
    /// <summary>
    /// 根据当前档位应用载具速度和加速度
    /// </summary>
    public void ApplyVehicleGearSpeed(float accelerSpeed, float maxSpeed)
    {
        if (!Motor.IsDriveVehicle)
            return;
        
        // 首次进入载具模式时保存原始值
        if (_originalMaxAirMoveSpeed == 0 && _originalAirAccelerationSpeed == 0)
        {
            _originalMaxAirMoveSpeed = m_AirMovementData.MaxAirMoveSpeed;
            _originalAirAccelerationSpeed = m_AirMovementData.AirAccelerationSpeed;
        }

        m_AirMovementData.MaxAirMoveSpeed = maxSpeed;
        m_AirMovementData.AirAccelerationSpeed = accelerSpeed;
      
    }
    
    /// <summary>
    /// 退出载具模式时恢复原始速度和加速度
    /// </summary>
    private void RestoreOriginalSpeed()
    {
        if (_originalMaxAirMoveSpeed > 0)
        {
            m_AirMovementData.MaxAirMoveSpeed = _originalMaxAirMoveSpeed;
            _originalMaxAirMoveSpeed = 0;
        }
        if (_originalAirAccelerationSpeed > 0)
        {
            m_AirMovementData.AirAccelerationSpeed = _originalAirAccelerationSpeed;
            _originalAirAccelerationSpeed = 0;
        }

    }

    public virtual void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        // 如果从载具模式退出，恢复原始速度和加速度
        if (!Motor.IsDriveVehicle && (_originalMaxAirMoveSpeed > 0 || _originalAirAccelerationSpeed > 0))
        {
            RestoreOriginalSpeed();
        }
        
        // Ground movement
        if (Motor.GroundingStatus.IsStableOnGround)
        {
            GroundMovement(ref currentVelocity, deltaTime);
        }
        // Air movement
        else
        {
            //VehicleAirMovement(ref currentVelocity, deltaTime);
            AirMovement(ref currentVelocity, deltaTime);
        }

        // Handle jumping
        HandleJump(ref currentVelocity, deltaTime);

        // Take into account additive velocity
        if (_internalVelocityAdd.sqrMagnitude > 0f)
        {
            currentVelocity += _internalVelocityAdd;
            _internalVelocityAdd = Vector3.zero;
        }

        PlayerAnimCtrl.SetMoveSpeed(currentVelocity);
    }

    public virtual void AfterCharacterUpdate(float deltaTime)
    {
        // Handle jumping pre-ground grace period
        if (_jumpRequested && _timeSinceJumpRequested > m_JumpingData.JumpPreGroundingGraceTime)
        {
            _jumpRequested = false;
        }

        if (m_JumpingData.AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
        {
            // If we're on a ground surface, reset jumping values
            if (!_jumpedThisFrame)
            {
                _jumpConsumed = false;
            }
            _timeSinceLastAbleToJump = 0f;
        }
        else
        {
            // Keep track of time since we were last able to jump (for grace period)
            _timeSinceLastAbleToJump += deltaTime;
        }
    }

    public bool IsColliderValidForCollisions(Collider coll)
    {
        if (m_MiscData.IgnoredColliders.Count == 0)
        {
            return true;
        }

        if (m_MiscData.IgnoredColliders.Contains(coll))
        {
            return false;
        }

        return true;
    }

    public void AddVelocity(Vector3 velocity)
    {
        _internalVelocityAdd += velocity;
    }

    public virtual void OnLanded()
    {
        PlayerAnimCtrl.ParameterAnimation.SetBool(PlayerAniPrameter.IsGround, true);
        var animState = _moveInputVector == Vector3.zero ? PlayerAniState.Idle : PlayerAniState.Run;
        PlayerAnimCtrl.SetPlayerAniState(animState, false);
        PlayerAnimCtrl.ParameterAnimation.SetTrigger(PlayerAniPrameter.LandOn);
        // 播放落地音效
        StateEventManager.Inst.TriggerStateEvent(PlayerID, StateEvent.GroundEvent, GroundEvent.Landed);
    }

    public virtual void OnLeaveStableGround()
    {
        if(Motor.IsDriveVehicle)
        {
            if (animtorIK ==  null || vehicleInfo == null || animator == null)
            {
                animtorIK = AvatarController.Inst.SelfWrap?.Avatar?.GetComponent<AnimIKController>();
                animator = AvatarController.Inst.SelfWrap?.Avatar?.GetComponent<Animator>();
                vehicleInfo = GameDataManager.Inst.mapGlobalData?.GetCurInfo<VehicleInfo>();
            }

            // if (vehicleInfo == null || vehicleInfo.curPoseData == CurPoseData || animtorIK == null)
            // {
            //     return;
            // }
            // Debug.LogError("修改旋转和坐标4");
            // animator.enabled = false;
            // CurPoseData = vehicleInfo.curPoseData;
            // var frameData = JsonConvert.DeserializeObject<KeyFrameData>(vehicleInfo.curPoseData);
            // animtorIK.SetKeyFrameData(GameData.PgcData.UgcPoseSubType.Single, frameData);
        }
        else
        {
            PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Jump, false);
            PlayerAnimCtrl.ParameterAnimation.SetBool(PlayerAniPrameter.IsGround, false);
            StateEventManager.Inst.TriggerStateEvent(PlayerID, StateEvent.GroundEvent, GroundEvent.LeaveStableGround);
        }
    }
}

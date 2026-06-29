using System;
using System.IO;
using Game.Avatar;
using Game.Avatar.FSM.DataStructure;
using Game.KinematicCharacter;
using Message;
using Newtonsoft.Json;
using UnityEngine;


[Serializable]
public class SwimKCC : BaseKCC
{


    public class SwimConfig
    {
        public float defaultJumpUpSpeed = 6;
        public float maxStableMoveSpeed = 2;
        public float airDrag = 3f;
        public float waterGravity = -5;
        public float idleTime = 2;
        public float maxAirSpeed = 3.2f;
        public float airAccelerationSpeed = 15;
        public float airFastSpeed = 6;
        public float waterMinSpeed = 0.6f;
    }


    /// <summary>
    /// 角色在水中状态
    /// </summary>
    public enum PlayerWaterStatus
    {
        /// <summary>
        /// 全身浸入水中
        /// </summary>
        AllInWater,
        /// <summary>
        /// 头部露出水面
        /// </summary>
        HeadOutWater,
        /// <summary>
        /// 脚部露出水面
        /// </summary>
        FootOutWater,

        /// <summary>
        /// 全部露出水面
        /// </summary>
        AllOutWater,
    }


    public override KCCType KCCType => KCCType.Swim;

    private PlayerWaterStatus playerWaterStatus = PlayerWaterStatus.AllInWater;

    private GameObject swimEffect;
    private GameObject swimIdleEffect;
    private GameObject swimWalkEffect;
    private GameObject curSwimEffect;

    private SwimConfig swimConfig;
    private BudTimer budTimer = null;

    private bool isJumping = false;
    private Vector3 defaultGravity = Vector3.zero;

    public SwimKCC(string uid, bool isSelf) : base(uid, isSelf)
    {
        swimConfig = new SwimConfig();
    }


    public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl)
    {

#if UNITY_EDITOR
        var configPath =Application.dataPath + "/LocalTest/Swim/SwimConfig.json";
        if (File.Exists(configPath))
        {
            swimConfig = JsonConvert.DeserializeObject<SwimConfig>(File.ReadAllText(configPath));
        }
#endif

        InitSwimEffect();


        JumpingData.JumpUpSpeed = swimConfig.defaultJumpUpSpeed;
        StableMovementData.MaxStableMoveSpeed = swimConfig.maxStableMoveSpeed;
        AirMovementData.Drag = swimConfig.airDrag;
        AirMovementData.AirAccelerationSpeed = swimConfig.airAccelerationSpeed;
        AirMovementData.MaxAirMoveSpeed = swimConfig.maxAirSpeed;
        FastRunData.FastRunSpeed = swimConfig.maxStableMoveSpeed;
        defaultGravity = MiscData.Gravity;
        MiscData.Gravity = new Vector3(0, swimConfig.waterGravity, 0);

        base.OnEnter(motor, animCtrl);
        playerWaterStatus = PlayerWaterStatus.AllInWater;
        MessageHelper.AddListener<PlayerWaterStatus>(StateMessage.StateWaterChange, OnStateChange);
    }

    public override void OnExit()
    {
        ClearSwimEffect();
        MessageHelper.RemoveListener<PlayerWaterStatus>(StateMessage.StateWaterChange, OnStateChange);
        if (budTimer != null)
        {
            TimerManager.Inst.Stop(budTimer);
            budTimer = null;
        }
        base.OnExit();
    }

    private void SetSwimEffect(GameObject effect)
    {
        if (curSwimEffect == effect)
        {
            return;
        }
        if (curSwimEffect != null)
        {
            curSwimEffect.SetActive(false);
        }
        curSwimEffect = effect;
        curSwimEffect.SetActive(true);
    }

    private void InitSwimEffect()
    {
        var playerCenter = AvatarController.Inst.SelfController.transform.Find("Center");
        var playerAnim = AvatarController.Inst.SelfController.PlayerAnimCtrl.transform;
        swimEffect = Loader.Load<GameObject>("Assets/Loadable/AnimationsExpress/Feat/swim_effect/swimming_effect.prefab").Instantiate(playerAnim);
        swimEffect.gameObject.SetActive(false);

        swimIdleEffect = Loader.Load<GameObject>("Assets/Loadable/AnimationsExpress/Feat/swim_effect/swimming_idle_effect.prefab").Instantiate(playerCenter);
        swimIdleEffect.transform.localPosition = Vector3.zero;
        swimIdleEffect.gameObject.SetActive(false);
        swimWalkEffect = Loader.Load<GameObject>("Assets/Loadable/AnimationsExpress/Feat/swim_effect/swimming_walk_effect.prefab").Instantiate(playerCenter);
        swimWalkEffect.gameObject.SetActive(false);
        swimWalkEffect.transform.localPosition = Vector3.zero;
    }

    private void ClearSwimEffect()
    {
        if (swimEffect != null)
        {
            GameObject.Destroy(swimEffect);
            swimEffect = null;
        }
        if (swimIdleEffect != null)
        {
            GameObject.Destroy(swimIdleEffect);
            swimIdleEffect = null;
        }
        if (swimWalkEffect != null)
        {
            GameObject.Destroy(swimWalkEffect);
            swimWalkEffect = null;
        }
    }


    private void OnStateChange(PlayerWaterStatus status)
    {
        playerWaterStatus = status;
        if (status == PlayerWaterStatus.AllOutWater)
        {
            m_MiscData.Gravity = defaultGravity;
            PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle, false);
            if (curSwimEffect != null)
            {
                curSwimEffect.SetActive(false);
            }
        } else if (status == PlayerWaterStatus.HeadOutWater)
        {
            m_MiscData.Gravity = Vector3.zero;
            SetSwimEffect(swimIdleEffect);
            if (budTimer == null)
            {
                budTimer = TimerManager.Inst.RunOnce("SwimmingIdleTimer", swimConfig.idleTime, OnIdleTimer);
            }
            else
            {
                LoggerUtils.Log("SwimmingIdleTimer");
                budTimer.Time = swimConfig.idleTime;
            }
        }
        else
        {
            m_MiscData.Gravity = new Vector3(0, swimConfig.waterGravity, 0);
        }
    }



    public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        base.UpdateVelocity(ref currentVelocity, deltaTime);
        if (currentVelocity.y <= swimConfig.waterMinSpeed)
        {
            isJumping = false;
            var xzVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
            if (xzVelocity.magnitude < swimConfig.waterMinSpeed)
            {
                if (PlayerAnimCtrl.CurAniState != PlayerAniState.Idle)
                {
                    PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle, false);
                    SetSwimEffect(swimIdleEffect);
                    if (budTimer == null)
                    {
                        budTimer = TimerManager.Inst.RunOnce("SwimmingIdleTimer", swimConfig.idleTime, OnIdleTimer);
                    }
                    else
                    {
                        budTimer.Time = swimConfig.idleTime;
                    }
                }
            }
        }
    }



    protected override void OnGroundInputs(Vector3 moveInputVector, ref PlayerCharacterInputs inputs)
    {
        base.OnGroundInputs(moveInputVector, ref inputs);
        if (moveInputVector != Vector3.zero)
        {
            SetSwimEffect(swimWalkEffect);
        }
        else
        {
            SetSwimEffect(swimIdleEffect);
        }
    }


    protected override void OnAirInputs(Vector3 moveInputVector, ref PlayerCharacterInputs inputs)
    {
        base.OnAirInputs(moveInputVector, ref inputs);

        if (playerWaterStatus == PlayerWaterStatus.AllOutWater)
        {
            return;
        }

        if (!inputs.JumpDown)
        {
            if (moveInputVector != Vector3.zero)
            {
                m_MiscData.Gravity = Vector3.zero;

                if (!isJumping && PlayerAnimCtrl.CurChildAniState == PlayerChildAniState.Up)
                {
                    PlayerAnimCtrl.SetPlayerChildAniState(PlayerChildAniState.None);
                }
                PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Run, false);
                SetSwimEffect(swimEffect);
                if (budTimer != null)
                {
                    TimerManager.Inst.Stop(budTimer);
                    budTimer = null;
                }
            }
        }
    }


    public override void OnLeaveStableGround()
    {
        base.OnLeaveStableGround();
        FastRunData.FastRunSpeed = swimConfig.airFastSpeed;
        _airFastRunOffset = m_FastRunData.FastRunSpeed - m_AirMovementData.MaxAirMoveSpeed;
    }

    public override void OnLanded()
    {
        base.OnLanded();
        FastRunData.FastRunSpeed = swimConfig.maxStableMoveSpeed;
        _airFastRunOffset = m_FastRunData.FastRunSpeed - m_AirMovementData.MaxAirMoveSpeed;

    }


    private void OnIdleTimer()
    {
        budTimer = null;
        m_MiscData.Gravity = new Vector3(0, swimConfig.waterGravity, 0);
    }



    protected override void HandleJump(ref Vector3 currentVelocity, float deltaTime)
    {
        _jumpedThisFrame = false;
        _timeSinceJumpRequested += deltaTime;


        // 在水面及水上时，不允许跳跃
        if (playerWaterStatus == PlayerWaterStatus.AllOutWater || playerWaterStatus == PlayerWaterStatus.HeadOutWater)
        {
            if (currentVelocity.y > 0)
            {
                currentVelocity.y = 0;
            }
            _jumpRequested = false;
            return;
        }
        // Calculate jump direction before ungrounding
        if (_jumpRequested)
        {

            isJumping = true;
            PlayerAnimCtrl.SetPlayerChildAniState(PlayerChildAniState.Up);
            PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Jump, false);
            Vector3 jumpDirection = Motor.CharacterUp;
            if (Motor.GroundingStatus.FoundAnyGround && !Motor.GroundingStatus.IsStableOnGround)
            {
                jumpDirection = Motor.GroundingStatus.GroundNormal;
            }

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

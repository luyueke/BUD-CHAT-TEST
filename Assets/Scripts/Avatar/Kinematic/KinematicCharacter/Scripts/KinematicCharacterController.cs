using System;
using System.Collections.Generic;
using Es;
using Game.Avatar.Kinematic.KinematicCharacter.KCC;
using Game.Pet;
using UnityEngine;

namespace Game.KinematicCharacter
{
    public enum KCCType
    {
        Default,
        Skate,
        Ski,
        Swim,
        Selfie,
        LinkEmote//联动表情：双人牵手等
    }

    public enum OrientationMethod
    {
        NoToward,//不旋转
        TowardsCamera,
        TowardsMovement,
    }

    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public float PressJoystickTime;

        public bool Equals(PlayerCharacterInputs other)
        {
            return MoveAxisForward == other.MoveAxisForward
                && MoveAxisRight == other.MoveAxisRight
                && CameraRotation.Equals(other.CameraRotation)
                && JumpDown == other.JumpDown
                && PressJoystickTime >= 2 == other.PressJoystickTime >= 2;
        }
        public void CopyFrom(PlayerCharacterInputs other)
        {
            MoveAxisForward = other.MoveAxisForward;
            MoveAxisRight = other.MoveAxisRight;
            CameraRotation.Set(other.CameraRotation.x, other.CameraRotation.y, other.CameraRotation.z, other.CameraRotation.w);
            JumpDown = other.JumpDown;
            PressJoystickTime = other.PressJoystickTime;
        }

        public bool IsNoMove()
        {
            return MoveAxisForward == 0 && MoveAxisRight == 0 && PressJoystickTime == 0 && !JumpDown;
        }

    }

    public struct AICharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public enum BonusOrientationMethod
    {
        None,
        TowardsGravity,
        TowardsGroundSlopeAndGravity,
    }

    public class KinematicCharacterController : MonoBehaviour, ICharacterController, IMotorGetter
    {
        public PlayerAnimationCtrl PlayerAnimCtrl { get; private set; }
        public KinematicCharacterMotor Motor;
        public Transform CameraTarget;


        [SerializeField] private KCCType CurKCCType;
        public IKCController CurIKCController { get; private set; }

        [SerializeField] private StableMovementData stableMovementData;
        [SerializeField] private AirMovementData airMovementData;
        [SerializeField] private JumpingData jumpingData;
        [SerializeField] private MiscData miscData;
        [SerializeField] private FastRunData fastRunData;

        private Dictionary<KCCType, IKCController> kcController;

        private string playerId;
        public string PlayerID {  get  { return playerId; }  }
        private bool isSelf;
        private Vector3 syncPos;
        public bool IsDriveVehicle = false;
        

        private Func<PlayerCharacterInputs, PlayerCharacterInputs> FrezeeInputAction;

        public KinematicCharacterMotor KCMotor => Motor;

        private void Awake()
        {
            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        private void OnDestroy() {
            if (CurIKCController != null) {
                CurIKCController.OnExit();
            }
        }

        public void Init(PlayerAnimationCtrl animCtrl, string uid, bool isSelf)
        {
            playerId = uid;
            this.isSelf = isSelf;
            PlayerAnimCtrl = animCtrl;

            kcController = new Dictionary<KCCType, IKCController>();


            if (animCtrl is PetAnimationCtrl) {
                kcController.Add(KCCType.Default, new DefaultKCC(uid, this.isSelf));
            } else {
                if (string.IsNullOrEmpty(animCtrl.specialAnimPgcId)) {
                    kcController.Add(KCCType.Default, new DefaultKCC(uid, this.isSelf));
                } else {
                    var specialConfig = DataTables.GetSpecialSkinConfig(animCtrl.specialAnimPgcId);
                    kcController.Add(KCCType.Default, GetSpecialAnimKCC(specialConfig.KCC, uid, this.isSelf));


                }
            }
            kcController.Add(KCCType.Skate, new SkateKCC(uid, this.isSelf));
            kcController.Add(KCCType.Ski, new SkiKCC(uid, this.isSelf));
            kcController.Add(KCCType.Swim, new SwimKCC(uid, this.isSelf));
            kcController.Add(KCCType.Selfie, new SelfieKCC(uid, this.isSelf));
            kcController.Add(KCCType.LinkEmote,new LinkEmoteKCC(uid, this.isSelf));
            TransitionToState(KCCType.Default);
        }

        public BaseKCC GetSpecialAnimKCC(string kccName, string uid, bool isSelf) {
            BaseKCC baseKCC = null;
            switch (kccName) {
                case "LionLanternKCC":
                    baseKCC = new LionLanternKCC(uid, isSelf);
                    break;
                case "SnowboardKCC":
                    baseKCC = new SnowboardKCC(uid, isSelf);
                    break;
                case "GhostCatKCC":
                    baseKCC = new GhostCatKCC(uid, isSelf);
                    break;
                case "SkatingKCC":
                    baseKCC = new SkatingKCC(uid, isSelf);
                    break;
                case "ElfKCC":
                    baseKCC = new ElfKCC(uid, isSelf);
                    break;
                case "HolyangeKCC":
                    baseKCC = new HolyangeKCC(uid, isSelf);
                    break;
                case "LuciferKCC":
                    baseKCC = new LuciferKCC(uid, isSelf);
                    break;
                case "PoseidonKCC":
                    baseKCC = new PoseidonKCC(uid, isSelf);
                    break;
                case "ZongziKCC":
                    baseKCC = new ZongziKCC(uid, isSelf);
                    break;
                case "HuabiKCC":
                    baseKCC = new HuabiKCC(uid, isSelf);
                    Debug.LogError("使用了HuabiKCC!");
                    break;
                case "TiliaKCC":
                    baseKCC = new TiliaKCC(uid, isSelf);
                    Debug.LogError("使用了TiliaKCC!");
                    break;
                case "OceanPrincessKCC":
                    baseKCC = new OceanPrincessKCC(uid, isSelf);
                    break;
                case "HopterKCC":
                    baseKCC = new HopterKCC(uid, isSelf);
                    break;
                case "FishManKCC":
                    baseKCC = new FishManKCC(uid, isSelf);
                    break;
                case "ClownBallKCC":
                    baseKCC = new ClownBallKCC(uid, isSelf);
                    break;
                case "EffectKCC":
                    baseKCC = new EffectKCC(uid, isSelf);
                    break;
                case "Default":
                    baseKCC = new DefaultKCC(uid, isSelf);
                    break;
                default:
                    baseKCC = new DefaultSpecialKCC(uid, isSelf);
                    break;
            }
            return baseKCC;
        }

        public void ChangeSpecialAnimKCC(string kccName) {
            var defaultKCC = kcController[KCCType.Default];
            IKCController baseKCC = null;
            switch (kccName) {
                case "Default":
                    if (defaultKCC is DefaultKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new DefaultKCC(playerId, isSelf);
                    }
                    break;
                case "ElfKCC":
                    if (defaultKCC is ElfKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new ElfKCC(playerId, isSelf);
                    }
                    break;
                case "HolyangeKCC":
                    if (defaultKCC is HolyangeKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new HolyangeKCC(playerId, isSelf);
                    }
                    break;
                case "LuciferKCC":
                    if (defaultKCC is LuciferKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new LuciferKCC(playerId, isSelf);
                    }
                    break;
                case "PoseidonKCC":
                    if (defaultKCC is PoseidonKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new PoseidonKCC(playerId, isSelf);
                    }
                    break;
                case "ZongziKCC":
                    if (defaultKCC is ZongziKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new ZongziKCC(playerId, isSelf);
                    }
                    break;
                case "HuabiKCC":
                    if (defaultKCC is HuabiKCC)
                    {
                        baseKCC = defaultKCC;
                    }
                    else
                    {
                        baseKCC = new HuabiKCC(playerId, isSelf);
                    }
                    break;
                case "TiliaKCC":
                    if (defaultKCC is TiliaKCC)
                    {
                        baseKCC = defaultKCC;
                    }
                    else
                    {
                        baseKCC = new TiliaKCC(playerId, isSelf);
                    }
                    break;
                case "OceanPrincessKCC":
                    if (defaultKCC is OceanPrincessKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new OceanPrincessKCC(playerId, isSelf);
                    }
                    break;
                case "HopterKCC":
                    if (defaultKCC is HopterKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new HopterKCC(playerId, isSelf);
                    }
                    break;
                case "FishManKCC":
                    if (defaultKCC is FishManKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new FishManKCC(playerId, isSelf);
                    }
                    break;
                case "ClownBallKCC":
                    if (defaultKCC is ClownBallKCC)
                    {
                        baseKCC = defaultKCC;
                    }
                    else
                    {
                        baseKCC = new ClownBallKCC(playerId, isSelf);
                    }
                    break;
                case "EffectKCC":
                    if (defaultKCC is EffectKCC) 
                    {
                        baseKCC = defaultKCC;
                    } 
                    else 
                    {
                        baseKCC = new EffectKCC(playerId, isSelf);
                    }
                    break;
                case "GhostCatKCC":
                    if (defaultKCC is GhostCatKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new GhostCatKCC(playerId, isSelf);
                    }
                    break;
                case "SkatingKCC":
                    if (defaultKCC is SkatingKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new SkatingKCC(playerId, isSelf);
                    }
                    break;
                case "SnowboardKCC":
                    if (defaultKCC is SnowboardKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new SnowboardKCC(playerId, isSelf);
                    }
                    break;
                case "LionLanternKCC":
                    if (defaultKCC is LionLanternKCC)
                    {
                        baseKCC = defaultKCC;
                    }
                    else
                    {
                        baseKCC = new LionLanternKCC(playerId, isSelf);
                    }
                    break;
                default:
                    if (defaultKCC is DefaultSpecialKCC) {
                        baseKCC = defaultKCC;
                    } else {
                        baseKCC = new DefaultSpecialKCC(playerId, isSelf);
                    }
                    break;
            }

            if (defaultKCC != baseKCC) {
                bool isNeedChangeKCC = CurIKCController == defaultKCC;
                if (isNeedChangeKCC) {

                    defaultKCC.OnExit();
                    CurIKCController = baseKCC;
                }
                kcController[KCCType.Default] = baseKCC;
                if (isNeedChangeKCC) {
                    OnStateEnter(KCCType.Default, KCCType.Default);
                }

            }
        }

        public void AddIgnoreColliders(Transform other)
        {
            if (other == null) return;
            // 忽略目标玩家的碰撞体
            var targetColliders = other.GetComponentsInChildren<Collider>(true);
            if (targetColliders != null && CurIKCController != null)
            {
                CurIKCController.MiscData.IgnoredColliders.AddRange(targetColliders);
            }
        }

        public void RemoveIgnoreColliders(Transform other)
        {
            if (other == null) return;
            var targetColliders = other.GetComponentsInChildren<Collider>(true);
            if (targetColliders != null && CurIKCController != null)
            {
                foreach (var collider in targetColliders)
                {
                    CurIKCController.MiscData.IgnoredColliders.Remove(collider);
                }
            }
        }

        public void ClearIgnoreColliders()
        {
            if (CurIKCController != null)
            {
                CurIKCController.MiscData.IgnoredColliders.Clear();
            }
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(KCCType newType)
        {
            KCCType tmpInitialState = KCCType.Default;

            if (CurIKCController != null)
            {
                tmpInitialState = CurIKCController.KCCType;
                OnStateExit(tmpInitialState, newType);
            }

            CurKCCType = newType;
            CurIKCController = kcController[newType];
            OnStateEnter(newType, tmpInitialState);
        }

        public IKCController GetKCC(KCCType type) {
            if (kcController.TryGetValue(type, out IKCController kcc)) {
                return kcc;
            }
            return null;
        }


        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(KCCType state, KCCType fromState)
        {
            stableMovementData = kcController[state].StableMovementData;
            airMovementData = kcController[state].AirMovementData;
            jumpingData = kcController[state].JumpingData;
            miscData = kcController[state].MiscData;
            fastRunData = kcController[state].FastRunData;
            kcController[state].OnEnter(Motor, PlayerAnimCtrl);
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(KCCType state, KCCType toState)
        {
            kcController[state].OnExit();
        }

        public void SetFreezeCharacter(bool isFreeze)
        {
            if (isFreeze)
                FrezeeInputAction = SetFreeezeInput;
            else
                FrezeeInputAction = null;
        }

        private PlayerCharacterInputs SetFreeezeInput(PlayerCharacterInputs inputs)
        {
            inputs.MoveAxisForward = 0;
            inputs.MoveAxisRight = 0;
            inputs.JumpDown = false;

            return inputs;
        }

        private void HandlerInputListener(PlayerCharacterInputs inputs)
        {
            StateEventManager.Inst.TriggerStateEvent(playerId, StateEvent.MoveJoystick, inputs.MoveAxisForward, inputs.MoveAxisRight);

            if (inputs.JumpDown)
            {
                StateEventManager.Inst.TriggerStateEvent(playerId, StateEvent.JumpBtn);
            }
        }

        /// <summary>
        /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            HandlerInputListener(inputs);

            if (FrezeeInputAction != null)
            {
                inputs = FrezeeInputAction(inputs);
            }

            // Clamp input
            Vector3 moveInputVector = Vector3.ClampMagnitude(new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward), 1f);

            // Calculate camera direction and rotation on the character plane
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }
            Quaternion cameraPlanarRotation = Quaternion.LookRotation(cameraPlanarDirection, Motor.CharacterUp);
            
            if(IsDriveVehicle){return;}
            CurIKCController.SetInputs(ref inputs, moveInputVector, cameraPlanarRotation, cameraPlanarDirection);
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref AICharacterInputs inputs)
        {
            //_moveInputVector = inputs.MoveVector;
            //_lookInputVector = inputs.LookVector;
        }

bool isSetSyncPos = false;
        public void SetCalculatePos(Vector3 desPos)
        {
            isSetSyncPos = true;
            this.syncPos = desPos;
        }

        private Quaternion _tmpTransientRot;

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now.
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            CurIKCController.UpdateRotation(ref currentRotation, deltaTime);
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now.
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            CurIKCController.UpdateVelocity(ref currentVelocity, deltaTime);
        }

        public void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            CurIKCController.UpdateVehicleVelocity(ref currentVelocity, deltaTime);
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            CurIKCController.AfterCharacterUpdate(deltaTime);
        }

        private Vector3 camerePos = new Vector3(0,1.8f,0);
        public void AfterCharacterMove()
        {
            if (CameraTarget)
            {
                CameraTarget.transform.position = this.transform.position + camerePos;
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return CurIKCController.IsColliderValidForCollisions(coll);
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void AddVelocity(Vector3 velocity)
        {
            CurIKCController.AddVelocity(velocity);
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        protected void OnLanded()
        {
            CurIKCController.OnLanded();
        }

        protected void OnLeaveStableGround()
        {
            CurIKCController.OnLeaveStableGround();
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }

        public bool IsGround()
        {
            return Motor.GroundingStatus.IsStableOnGround;
        }

        /// <summary>
        /// 矫正位置
        /// </summary>
        public void CorrectPosition()
        {
            if (!isSelf && isSetSyncPos)
            {
                isSetSyncPos = false;
                Motor.SetTransientPosition(syncPos);
            }
        }

        public bool IsInitIKController()
        {
            return CurIKCController != null;
        }

        public void OnTeleport()
        {
            StateEventManager.Inst.TriggerStateEvent(playerId, StateEvent.Teleport);
        }
        
        public void SetCameraPos(Vector3 pos)
        {
            camerePos = pos;
        }
    }
}

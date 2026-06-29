using Game.KinematicCharacter;
using Es;
using UnityEngine;

namespace Game.Vehicle.PGCVehicle.KVC
{
    /// <summary>
    /// PGC载具侧的 KinematicCharacterMotor 适配器（与UGC捏车逻辑完全分离）：
    /// - 挂在PGC载具GameObject上（与 KinematicCharacterMotor 同级）
    /// - 内部持有一个 PGCVehicleBaseKVC（可换不同派生类做特殊载具控制）
    /// - 让PGC载具像人物一样走 KinematicCharacterMotor 的碰撞/贴地/跳跃管线
    /// </summary>
    public class PGCKinematicVehicleController : MonoBehaviour, ICharacterController, IMotorGetter, IPGCVehicleInputDriver, IPGCVehicleDriverContext
    {
        public KinematicCharacterMotor Motor;
        [SerializeField] private PGCVehicleBaseKVC vehicleKvc;

        private bool _isSetSyncPos;
        private Vector3 _syncPos;

        public KinematicCharacterMotor KCMotor => Motor;
        public Transform CameraTarget;

        private Vector3 cameraOffset = new Vector3(0, 2.5f, 0);

        private void Awake()
        {
            if (Motor == null)
            {
                Motor = GetComponent<KinematicCharacterMotor>();
            }
            if (Motor != null)
            {
                Motor.CharacterController = this;
            }
        }

        /// <summary>
        /// 由PGCVehicleController.Init调用：注入配置并让KVC按配置刷新参数（缺省走KVC默认值）
        /// </summary>
        public void InitWithConfig(PgcVehicleConfig cfg, bool isSelf, bool isReconstruction = false)
        {
            if (Motor == null)
            {
                Motor = GetComponent<KinematicCharacterMotor>();
            }
            if (Motor != null)
            {
                Motor.CharacterController = this;
            }

            if (vehicleKvc == null)
            {
                vehicleKvc = GetVehicleKVC(cfg.KVC);
            }

            if (vehicleKvc != null && Motor != null)
            {
                vehicleKvc.OnInit(Motor, isSelf, isReconstruction);
                vehicleKvc.ApplyConfig(cfg);
            }

            // if(cfg.seatOffset != null && cfg.seatOffset.Count > 0)
            // {
            //     cameraOffset += cfg.seatOffset[0] * 1.7f;
            // }

        }

        public void OnUIInit(){
            if(vehicleKvc != null)
            {
                vehicleKvc.OnUIInit();
            }
        }

        public void Release(){
            if(vehicleKvc != null)
            {
                vehicleKvc.Release();
            }
        }

        private PGCVehicleBaseKVC GetVehicleKVC(string vehicleKVCName)
        {
            if (string.IsNullOrEmpty(vehicleKVCName))
            {
                return new DefaultKVC();
            }

            switch (vehicleKVCName)
            {
                case "DefaultKVC":
                    return new DefaultKVC();
                case "HotAirBallonKVC":
                    return new HotAirBallonKVC();
                case "TailKVC":
                    return new TailKVC();
                case "AirshipKVC":
                    return new AirshipKVC();
                case "WawajiKVC":
                    return new WawajiKVC();
                case "StageKVC":
                    return new StageKVC();
                default:
                    return new DefaultKVC();
            }
        }

        public void AddPlayerAnimCtrl(string uid)
        {
            if(vehicleKvc != null)
            {
                vehicleKvc.AddPlayerAnimCtrl(uid);
            }
        }

        public void RemovePlayerAnimCtrl(string uid)
        {
            if(vehicleKvc != null)
            {
                vehicleKvc.RemovePlayerAnimCtrl(uid);
            }
        }

        public void AddPlayerAnimCtrl(PlayerAnimationCtrl ctrl)
        {
            if(vehicleKvc != null)
            {
                vehicleKvc.AddPlayerAnimCtrl(ctrl);
            }
        }

        public void RemovePlayerAnimCtrl(PlayerAnimationCtrl ctrl)
        {
            if(vehicleKvc != null)
            {
                vehicleKvc.RemovePlayerAnimCtrl(ctrl);
            }
        }

        /// <summary>给 PGCVehicleController.Drive 调用</summary>
        public void SetInput(Vector2 moveAxis, bool jump)
        {
            if (vehicleKvc == null) return;
            vehicleKvc.SetInput(moveAxis, jump);
        }

        public void SetDriverCameraRotation(Quaternion cameraRotation)
        {
            vehicleKvc?.SetDriverCameraRotation(cameraRotation);
        }

        /// <summary>联机/同步：外部指定一个“下一帧矫正位置”</summary>
        public void SetCalculatePos(Vector3 desPos)
        {
            _isSetSyncPos = true;
            _syncPos = desPos;
        }

        public void BeforeCharacterUpdate(float deltaTime) { }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            vehicleKvc?.UpdateRotation(ref currentRotation, deltaTime);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            // 兜底：即便误把 IsDriveVehicle 关了，也能工作
            vehicleKvc?.UpdateVehicleVelocity(ref currentVelocity, deltaTime);
        }

        public void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            vehicleKvc?.UpdateVehicleVelocity(ref currentVelocity, deltaTime);
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            if (Motor == null || vehicleKvc == null) return;

            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                vehicleKvc.OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                vehicleKvc.OnLeaveStableGround();
            }
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            vehicleKvc?.AfterCharacterUpdate(deltaTime);
        }

        
        public void AfterCharacterMove() {
            if (CameraTarget)
            {
                //根据载具朝向
                CameraTarget.transform.position = this.transform.position + cameraOffset;
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            return vehicleKvc == null || vehicleKvc.IsColliderValidForCollisions(coll);

        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) { }
        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) { }
        public void OnDiscreteCollisionDetected(Collider hitCollider) { }

        public void CorrectPosition()
        {
            if (_isSetSyncPos && Motor != null)
            {
                _isSetSyncPos = false;
                Motor.SetTransientPosition(_syncPos);
            }
        }

        public bool IsInitIKController()
        {
            return vehicleKvc != null;
        }

        public void ChangeHonkingState(bool isHonking, string honkingSound)
        {
            if(vehicleKvc != null)
            {
                vehicleKvc.ChangeHonkingState(isHonking, honkingSound);
            }
        }

        public void OnSkill(int skillId, bool isPress, string extraJson = null)
        {
            if(vehicleKvc != null)
            {
                vehicleKvc.OnSkill(skillId, isPress, extraJson);
            }
        }

        public void GrabPlayer(string targetUid)
        {
            vehicleKvc?.GrabPlayer(targetUid);
        }

        public void ReleasePlayer()
        {
            vehicleKvc?.ReleasePlayer();
        }

        public bool HasGrabbableTarget()
        {
            return vehicleKvc != null && vehicleKvc.HasGrabbableTarget();
        }

        public void SetIdleFloating(bool floating)
        {
            vehicleKvc?.SetIdleFloating(floating);
        }


        public void ChangeAirBanner(string strParam)
        {
            if(vehicleKvc != null)
            {
                vehicleKvc.ChangeAirBanner(strParam);
            }
        }
    }
}



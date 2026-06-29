using Basic;
using Cinemachine;
using Es;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.MapSetting;
using UnityEngine;
using UnityEngine.Events;

namespace UI.UIPanels
{
    public enum CameraBodyType
    {
        Transposer = 1,
        HardLockToTarget = 2
    }

    public enum CameraModeType
    {
        FixedViewNode,
        SelfieMode,
        FirstView,
        Lens
    }

    public class CameraModeConotroller:GameInstance<CameraModeConotroller>
    {
        public const float DEFAULT_FOLLOW_Z = -7f;
        public const float CAMERA_FIELD_OF_VIEW = 50;
        public const float SELFIE_FIELD_OF_VIEW = 60;
        public const float FIRST_VIEW_FIELD_OF_VIEW = 60;
        public const float FIRST_VIEW_Y = 1.5f;
        public const float FIRST_VIEW_Z = 1f;
        public const float CAMERA_ZOOM_MIN_DISTANCE = 2f;
        public const float CAMERA_ZOOM_MAX_DISTANCE = 20f;
        public const float CAMERA_ZOOM_FOV_MIN = 0f;
        public const float CAMERA_ZOOM_FOV_MAX = 100f;

        // Lens（目标镜头）模式：用左摇杆平移镜头节点，上下按钮调整高度
        public const float LENS_FIELD_OF_VIEW = CAMERA_FIELD_OF_VIEW;
        public const float LENS_MOVE_SPEED = 150.0f;     // 左摇杆平移速度（世界单位/秒）
        public const float LENS_UP_DOWN_STEP = 0.25f;  // 上下按钮每次步进（世界单位）
        public const float LENS_UP_DOWN_SPEED = 1.2f;  // 上下按钮按住持续移动速度（世界单位/秒）
        public const float LENS_Y_MIN = 0.3f;
        public const float LENS_Y_MAX = 380.5f;
        
        private CinemachineVirtualCamera newVirCamera;
        private CinemachineVirtualCamera normalVirCamera;
        private Transform virCameraTarget;
        private Transform norCameraTarget;
        private Transform firstViewTarget;
        private CameraModeHandler cameraHandler;
        private bool _lensModeActive;
        private bool _followModeActive;
        private Vector3 _savedNormalFollowOffset;
        private float _savedNormalFOV;
        private UnityAction<Vector2> _lensAxisListener;
        private bool _firstViewOrientationOverridden;
        private OrientationMethod _cachedFirstViewOrientation;
        private IKCController _firstViewOrientationKcc;

        private string newCameraName = "CameraModeVirCamera";
        
        private Vector3 cameraDefaultAngles = new Vector3(13,-154,0);
        private Vector3 cameraDefaultPos = new Vector3(0,1.7f,1.751f);

        /// <summary>
        /// 用于 UI 切换回来时恢复相机模式（不一定要重置相机位置/角度）。
        /// </summary>
        public void EnsureCameraMode(bool forceReset = false)
        {
            ExitFollowMode();

            if (forceReset || newVirCamera == null || cameraHandler == null)
            {
                InitCameraMode();
                return;
            }

            // 相机切换回 CameraMode
            newVirCamera.enabled = true;
            normalVirCamera = GetNormalVirCamera();
            if (normalVirCamera != null) normalVirCamera.enabled = false;

            // 恢复默认跟随点（防止第一人称切换后 Follow/LookAt 留在其它节点）
            var defaultTarget = GetFreeCameraTarget();
            if (newVirCamera != null)
            {
                newVirCamera.Follow = defaultTarget;
                newVirCamera.LookAt = defaultTarget;
                SetVirCameraBodyType(newVirCamera, CameraBodyType.Transposer);
                var transposer = newVirCamera.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_FollowOffset = new Vector3(0, 0, DEFAULT_FOLLOW_Z);
                }
                newVirCamera.m_Lens.FieldOfView = CAMERA_FIELD_OF_VIEW;
            }

            // 输入控制权切回 CameraModeHandler
            cameraHandler.joyStick = MobileJoystick.Inst;
            cameraHandler.RebindVirtualCamera(newVirCamera);
            cameraHandler.SwitchCameraMode(CameraModeType.FixedViewNode);
            InputReceiver.Inst.SetHandle(cameraHandler);
        }

        public void InitCameraMode()
        {
            ExitFollowMode();

            newVirCamera = CreateVirCameraOnCameraMode();
            if (newVirCamera == null)
            {
                return;
            }
            newVirCamera.enabled = true;
            normalVirCamera = GetNormalVirCamera();
            if (normalVirCamera != null)
            {
                normalVirCamera.enabled = false;
            }
            
            cameraHandler = new CameraModeHandler();
            var cam = GlobalCameraManager.Inst.GlobalMainCamera;
            cameraHandler.joyStick = MobileJoystick.Inst;
            cameraHandler.SetCamera(cam, newVirCamera);
            // 首次进入 CameraMode 时，MobileJoystick 可能尚未初始化（Inst == null）。
            // 相机输入（双指缩放/平移）不应依赖摇杆存在，避免这里空引用打断后续输入句柄绑定。
            cameraHandler.joyStick?.OnResetJoystick();
            cameraHandler.SwitchCameraMode(CameraModeType.FixedViewNode);
            InputReceiver.Inst.SetHandle(cameraHandler);

            // 确保 Follow/LookAt 每次初始化都回到默认目标（newVirCamera 复用时也要刷新）
            var defaultTarget = GetFreeCameraTarget();
            newVirCamera.Follow = defaultTarget;
            newVirCamera.LookAt = defaultTarget;
            SetVirCameraBodyType(newVirCamera, CameraBodyType.Transposer);
            var transposer = newVirCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = new Vector3(0, 0, DEFAULT_FOLLOW_Z);
            }
            newVirCamera.m_Lens.FieldOfView = CAMERA_FIELD_OF_VIEW;

            // SetVirCameraBodyType 会创建新的 Transposer 组件，
            // 必须让 handler 重新绑定以刷新内部 camTransposer 引用
            cameraHandler.RebindVirtualCamera(newVirCamera);
        }

        public void EnterLensMode()
        {
            // Lens 依赖于“OC 相机基态”
            ExitFirstViewMode();
            ExitLensMode(); // 防止重复订阅

            // 回到等同 OC 的镜头角度/位置/偏移
            InitCameraMode();

            _lensModeActive = true;
            cameraHandler?.SwitchCameraMode(CameraModeType.Lens);

            var js = MobileJoystick.Inst;
            if (js != null)
            {
                js.BlockMoveAxisOutput = true;
                _lensAxisListener = axis =>
                {
                    if (!_lensModeActive) return;
                    if (cameraHandler == null) return;
                    if (axis.sqrMagnitude <= 0.0001f) return;

                    cameraHandler.MoveByJoystick(axis, LENS_MOVE_SPEED);
                };
                js.OnAxisChange.AddListener(_lensAxisListener);
            }
        }

        public void ExitLensMode()
        {
            _lensModeActive = false;

            var js = MobileJoystick.Inst;
            if (js != null && _lensAxisListener != null)
            {
                js.OnAxisChange.RemoveListener(_lensAxisListener);
            }
            if (js != null) js.BlockMoveAxisOutput = false;
            _lensAxisListener = null;

            cameraHandler?.SwitchCameraMode(CameraModeType.FixedViewNode);
        }

        public bool IsFollowModeActive => _followModeActive;

        /// <summary>
        /// Follow 模式：使用 normalVirCamera（跟随玩家），禁用缩放，复用 CameraModeHandler 处理旋转输入。
        /// </summary>
        public void EnterFollowMode()
        {
            ExitFirstViewMode();
            ExitFollowMode();

            if (cameraHandler == null)
            {
                InitCameraMode();
            }

            normalVirCamera = GetNormalVirCamera();
            if (normalVirCamera != null)
            {
                var transposer = normalVirCamera.GetCinemachineComponent<CinemachineTransposer>();
                _savedNormalFollowOffset = transposer != null ? transposer.m_FollowOffset : Vector3.zero;
                _savedNormalFOV = normalVirCamera.m_Lens.FieldOfView;
            }

            if (newVirCamera != null) newVirCamera.enabled = false;
            if (normalVirCamera != null) normalVirCamera.enabled = true;

            cameraHandler.RebindVirtualCamera(normalVirCamera);
            cameraHandler.joyStick = MobileJoystick.Inst;
            cameraHandler.SwitchCameraMode(CameraModeType.FixedViewNode);
            cameraHandler.DisableMultiTouch = false;
            cameraHandler.PinchZoomFOVOnly = true;
            InputReceiver.Inst.SetHandle(cameraHandler);

            _followModeActive = true;
        }

        public void ExitFollowMode()
        {
            if (!_followModeActive) return;
            _followModeActive = false;

            if (cameraHandler != null)
            {
                cameraHandler.DisableMultiTouch = false;
                cameraHandler.PinchZoomFOVOnly = false;
            }

            normalVirCamera = GetNormalVirCamera();
            if (normalVirCamera != null)
            {
                var transposer = normalVirCamera.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_FollowOffset = _savedNormalFollowOffset;
                }
                normalVirCamera.m_Lens.FieldOfView = _savedNormalFOV;
            }
        }

        public void LensMoveVertical(float deltaY)
        {
            if (!_lensModeActive) return;
            cameraHandler?.MoveVertical(deltaY, LENS_Y_MIN, LENS_Y_MAX);
        }

        public void LensMoveHorizontal(float deltaX)
        {
            if (!_lensModeActive) return;
            cameraHandler?.MoveHorizontal(deltaX);
        }

        public void EnterFirstViewMode()
        {
            EnsureCameraMode();
            ApplyFirstViewOrientationOverride(true);

            var player = AvatarController.Inst != null ? AvatarController.Inst.SelfController : null;
            if (player == null) return;

            var node = GetFirstViewTarget();
            // 第一人称朝向必须跟随角色真实运动朝向（Motor），
            // 不能使用外层 SelfController 节点（其朝向可能与角色朝向不一致）。
            var anchor = player.Motor != null ? player.Motor.transform : player.transform;
            node.SetParent(anchor, false);
            node.localPosition = new Vector3(0f, FIRST_VIEW_Y, FIRST_VIEW_Z);
            node.localEulerAngles = Vector3.zero;

            newVirCamera.Follow = node;
            // 第一人称不使用 LookAt（尤其不能 LookAt 自己），
            // 否则在 Follow -> FirstView 的 blend 过程中会残留上一镜头朝向，出现“看自己脸”。
            newVirCamera.LookAt = null;
            SetVirCameraBodyType(newVirCamera, CameraBodyType.Transposer);
            var transposer = newVirCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = Vector3.zero;
            }
            newVirCamera.m_Lens.FieldOfView = FIRST_VIEW_FIELD_OF_VIEW;
            // 强制刷新 vcam 状态并对齐到第一人称目标朝向，避免 Follow 的朝向残留到首帧。
            newVirCamera.PreviousStateIsValid = false;
            newVirCamera.ForceCameraPosition(node.position, node.rotation);

            cameraHandler.joyStick = MobileJoystick.Inst;
            cameraHandler.RebindVirtualCamera(newVirCamera);
            cameraHandler.SwitchCameraMode(CameraModeType.FirstView);
            cameraHandler.OnCameraMove(); // 刷新 orginForward 等，用于 180° 视野限制
            InputReceiver.Inst.SetHandle(cameraHandler);
        }

        public void ExitFirstViewMode()
        {
            if (newVirCamera == null) return;
            var wasFirstView = cameraHandler != null && cameraHandler.CurrentMode == CameraModeType.FirstView;
            if (wasFirstView)
            {
                AlignPlayerYawToCurrentView();
            }
            ApplyFirstViewOrientationOverride(false);

            var defaultTarget = GetFreeCameraTarget();
            newVirCamera.Follow = defaultTarget;
            newVirCamera.LookAt = defaultTarget;
            SetVirCameraBodyType(newVirCamera, CameraBodyType.Transposer);
            var transposer = newVirCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = new Vector3(0, 0, DEFAULT_FOLLOW_Z);
            }
            newVirCamera.m_Lens.FieldOfView = CAMERA_FIELD_OF_VIEW;

            cameraHandler?.RebindVirtualCamera(newVirCamera);
            cameraHandler?.SwitchCameraMode(CameraModeType.FixedViewNode);
            cameraHandler?.OnCameraMove();
        }

        public void EnterSelfieMode(string selfieId = null)
        {
            var cameraModeNode = GetFreeCameraTarget();
            Transform playerRealNode = AvatarController.Inst.SelfController.transform;
            cameraModeNode.parent = playerRealNode.transform;
            if(!string.IsNullOrEmpty(selfieId)){
                var selfieConfig = DataTables.GetCameraSelfiePose(selfieId);
                if(selfieConfig != null){
                    cameraModeNode.localEulerAngles = selfieConfig.cameraRot;
                    cameraModeNode.localPosition = selfieConfig.cameraPos;
                }else{
                    cameraModeNode.localEulerAngles = cameraDefaultAngles;
                    cameraModeNode.localPosition = cameraDefaultPos;
                }
            }else{
                cameraModeNode.localEulerAngles = cameraDefaultAngles;
                cameraModeNode.localPosition = cameraDefaultPos;
            }

            CinemachineVirtualCamera virCamera = CreateVirCameraOnCameraMode();
            virCamera.enabled = true;
            // 关键：从第一人称/其它模式切换过来时，Follow/LookAt 可能不再是 CameraModeNode
            virCamera.Follow = cameraModeNode;
            virCamera.LookAt = cameraModeNode;
            SetVirCameraBodyType(virCamera, CameraBodyType.HardLockToTarget);
            virCamera.m_Lens.FieldOfView = SELFIE_FIELD_OF_VIEW;
            cameraHandler?.RebindVirtualCamera(virCamera);
            cameraHandler?.SwitchCameraMode(CameraModeType.SelfieMode);

        }

        public void ExitSelfieMode()
        {
            if (newVirCamera == null) return;

            //相机切换
            ResetFreeCamera();
            var defaultTarget = GetFreeCameraTarget();
            newVirCamera.Follow = defaultTarget;
            newVirCamera.LookAt = defaultTarget;
            cameraHandler?.SwitchCameraMode(CameraModeType.FixedViewNode);
            var transposer = newVirCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                transposer.m_FollowOffset = new Vector3(0, 0, DEFAULT_FOLLOW_Z);
            }
            if (newVirCamera.m_Lens.FieldOfView > SELFIE_FIELD_OF_VIEW)
            {
                newVirCamera.m_Lens.FieldOfView = SELFIE_FIELD_OF_VIEW;
            }
            else if (newVirCamera.m_Lens.FieldOfView < CAMERA_FIELD_OF_VIEW)
            {
                newVirCamera.m_Lens.FieldOfView = CAMERA_FIELD_OF_VIEW;
            }
            cameraHandler?.RebindVirtualCamera(newVirCamera);
            cameraHandler?.OnCameraMove();
        }

        /// <summary>
        /// 仅用于“自拍状态被其它状态打断”的场景：
        /// - 退出自拍跟随关系（节点脱离玩家）
        /// - 进入 OC 的可调镜头模式
        /// - 保持打断瞬间的镜头位置/角度不变
        /// </summary>
        public void ExitSelfieModeToOcKeepCurrentPose()
        {
            if (newVirCamera == null) return;

            var cameraModeNode = GetFreeCameraTarget();
            if (cameraModeNode == null) return;

            // 先记录当前主相机位姿，用于恢复到 OC 后保持“所见即所得”。
            var liveCam = GlobalCameraManager.Inst != null ? GlobalCameraManager.Inst.GlobalMainCamera : null;
            var cachedCamPos = liveCam != null ? liveCam.transform.position : Vector3.zero;

            // 解绑自拍：从玩家节点脱离，但保持世界位姿不变。
            var normalCameraTarget = GetNormalCameraTarget();
            var targetParent = normalCameraTarget != null ? normalCameraTarget.parent : null;
            cameraModeNode.SetParent(targetParent, true);

            newVirCamera.Follow = cameraModeNode;
            newVirCamera.LookAt = cameraModeNode;
            SetVirCameraBodyType(newVirCamera, CameraBodyType.Transposer);
            var transposer = newVirCamera.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
            {
                // 计算“当前相机在目标局部空间下的偏移”，保证切到 OC 后首帧不跳镜头。
                var localOffset = Quaternion.Inverse(cameraModeNode.rotation) * (cachedCamPos - cameraModeNode.position);
                transposer.m_FollowOffset = localOffset;
            }

            cameraHandler?.RebindVirtualCamera(newVirCamera);
            cameraHandler?.SwitchCameraMode(CameraModeType.FixedViewNode);
            cameraHandler?.OnCameraMove();
        }


        private void ResetFreeCamera()
        {
            var normalCameraTarget = GetNormalCameraTarget();
            var cameraModeNode = GetFreeCameraTarget();
            if (normalCameraTarget == null || cameraModeNode == null || newVirCamera == null)
            {
                return;
            }

            cameraModeNode.SetParent(normalCameraTarget.parent);
            SetVirCameraBodyType(newVirCamera, CameraBodyType.Transposer);
        }

        public void ExitCameraMode(bool restorePostProcess = true)
        {
            ExitFollowMode();

            var wasFirstView = cameraHandler != null && cameraHandler.CurrentMode == CameraModeType.FirstView;
            if (wasFirstView)
            {
                AlignPlayerYawToCurrentView();
            }
            ExitLensMode();
            ApplyFirstViewOrientationOverride(false);

            // 默认退出相机状态时还原“基础后处理效果”（清除 Lens 调参 / 取消滤镜覆盖）。
            // 某些切换流程（如 ViewMenu 切 Follow）可选择保留当前镜头效果。
            if (restorePostProcess)
            {
                try
                {
                    PostProcessManager.Inst.RestoreDefaultVolume();
                }
                catch
                {
                    // ignore
                }
            }

            var statePlayer = AvatarController.Inst != null ? AvatarController.Inst.SelfStateController : null;
            if (statePlayer != null && statePlayer.ContainsAllState(PlayerState.CameraMode))
            {
                ResetFreeCamera();
                InputReceiver.Inst.SetHandle(null);
                statePlayer.ExitState(PlayerState.CameraMode);
            }

            if (newVirCamera != null)
            {
                newVirCamera.enabled = false;
            }
            if (normalVirCamera != null)
            {
                normalVirCamera.enabled = true;
            }
        }

        /// <summary>
        /// 第一人称退出时：角色保持当前镜头朝向（只对齐 Yaw，不改俯仰）。
        /// </summary>
        private static void AlignPlayerYawToCurrentView()
        {
            var selfCtrl = AvatarController.Inst != null ? AvatarController.Inst.SelfController : null;
            var mainCam = GlobalCameraManager.Inst != null ? GlobalCameraManager.Inst.GlobalMainCamera : null;
            if (selfCtrl == null || mainCam == null) return;

            var flatForward = mainCam.transform.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude <= 0.0001f) return;
            flatForward.Normalize();

            var targetRot = Quaternion.LookRotation(flatForward, Vector3.up);
            if (selfCtrl.Motor != null)
            {
                selfCtrl.Motor.transform.rotation = targetRot;
                selfCtrl.Motor.SetRotation(targetRot);
            }
            else
            {
                selfCtrl.transform.rotation = targetRot;
            }
        }

        /// <summary>
        /// 第一人称下把 KCC 朝向改为 NoToward，避免“移动方向依赖相机朝向”与“相机绑定角色”形成正反馈导致转圈。
        /// </summary>
        private void ApplyFirstViewOrientationOverride(bool enable)
        {
            var selfCtrl = AvatarController.Inst != null ? AvatarController.Inst.SelfController : null;
            var kcc = selfCtrl != null && selfCtrl != null ? selfCtrl.CurIKCController : null;
            if (kcc == null || kcc.StableMovementData == null) return;

            if (enable)
            {
                if (!_firstViewOrientationOverridden)
                {
                    _cachedFirstViewOrientation = kcc.StableMovementData.OrientationMethod;
                    _firstViewOrientationKcc = kcc;
                    _firstViewOrientationOverridden = true;
                }
                kcc.StableMovementData.OrientationMethod = OrientationMethod.NoToward;
                return;
            }

            if (!_firstViewOrientationOverridden) return;
            if (_firstViewOrientationKcc != null && _firstViewOrientationKcc.StableMovementData != null)
            {
                _firstViewOrientationKcc.StableMovementData.OrientationMethod = _cachedFirstViewOrientation;
            }

            _firstViewOrientationKcc = null;
            _firstViewOrientationOverridden = false;
        }

        public CinemachineVirtualCamera GetNormalVirCamera()
        {
            if (normalVirCamera == null)
            {
                var modeCameraObj = GameObject.Find("PlayVirtualCamera");
                normalVirCamera = modeCameraObj?.GetComponent<CinemachineVirtualCamera>();
            }
            return normalVirCamera;
        }
        

        public CinemachineVirtualCamera CreateVirCameraOnCameraMode()
        {
            if (newVirCamera == null)
            {
                //创建虚拟相机
                var normalCamera = GetNormalVirCamera();
                if (normalCamera == null)
                {
                    return null;
                }
                newVirCamera = GameObject.Instantiate(normalCamera, normalCamera.transform.parent);
                newVirCamera.name = newCameraName;
                GetFreeCameraTarget();
                newVirCamera.Follow = virCameraTarget;
                newVirCamera.LookAt = virCameraTarget;
                newVirCamera.AddCinemachineComponent<CinemachineSameAsFollowTarget>();
                newVirCamera.m_Lens.FieldOfView = CAMERA_FIELD_OF_VIEW;
                SetVirCameraBodyType(newVirCamera, CameraBodyType.Transposer);
                var transposer = newVirCamera.GetCinemachineComponent<CinemachineTransposer>();
                transposer.m_FollowOffset = new Vector3(0, 0, DEFAULT_FOLLOW_Z);
                //获取到CinemachineCollider后将碰撞关闭
                var collider = newVirCamera.GetComponent<CinemachineCollider>();
                collider.enabled = false;
            }
            return newVirCamera;
        }

        public Transform GetFreeCameraTarget()
        {
            if (virCameraTarget == null)
            {
                virCameraTarget = new GameObject("CameraModeNode").transform;
            }
            return virCameraTarget;
        }

        public Transform GetFirstViewTarget()
        {
            if (firstViewTarget == null)
            {
                firstViewTarget = new GameObject("FirstViewNode").transform;
            }
            return firstViewTarget;
        }

        public Transform GetNormalCameraTarget()
        {
            var virCamera = GetNormalVirCamera();
            return virCamera != null ? virCamera.Follow : null;
        }
        
        public void SetVirCameraBodyType(CinemachineVirtualCamera virCamera, CameraBodyType type)
        {
            if (virCamera == null) return;

            if (type == CameraBodyType.Transposer)
            {
                // 直接 Add：确保 Body Stage 切回 Transposer（会替换掉 HardLock 等其它 Body）
                var transposer = virCamera.AddCinemachineComponent<CinemachineTransposer>();

                transposer.m_BindingMode = CinemachineTransposer.BindingMode.LockToTarget;
                transposer.m_XDamping = 0;
                transposer.m_YDamping = 0;
                transposer.m_ZDamping = 0;
            }
            else if (type == CameraBodyType.HardLockToTarget)
            {
                virCamera.AddCinemachineComponent<CinemachineHardLockToTarget>();
            }
        }
    }
}
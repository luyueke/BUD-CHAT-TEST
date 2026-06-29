using Cinemachine;
using Game.Avatar;
using Message;
using System;
using UnityEngine;

namespace Game.Utils {
    /// <summary>
    /// 相机工具类，仅给游戏场景内使用，外部切勿使用
    /// </summary>
    public class GameCameraUtils : GameInstance<GameCameraUtils> {
        private const float maxCamDist = 350;
        private Vector3 createOffset = new Vector3(0, 0.5f, 0);
        private Vector3 createDefault = new Vector3(0, 1f, 0);

        private GameObject _mainCameraGo;
        private Camera _mainCamera;

        private GameObject _UICameraGo;
        private Camera _UICamera;

        private GameObject _shotCameraGo;
        private Camera _shotCamera;
        
        private CinemachineVirtualCamera _playVirtualCamera; // 玩家相机参数
        private CinemachineVirtualCamera _editVirtualCamera; // 预设好的场景相机参数

        private CinemachineVirtualCamera _customVirtualCamera; // 自定义动态设计相机参数

        private CinemachineBrain _mainCinemachineBrain;
        public CinemachineBrain MainCinemachineBrain {
            get {
                if (_mainCinemachineBrain == null)
                {
                    var mainCamera = GetMainCamera();
                    CinemachineBrain brain = null;
                    if (mainCamera != null)
                    {
                        mainCamera.TryGetComponent(out brain);
                    }
                    if (brain != null)
                    {
                        _mainCinemachineBrain = brain;
                        _mainCinemachineBrain.m_CameraCutEvent.AddListener(OnCameraCutd);
                        _mainCinemachineBrain.m_CameraActivatedEvent.AddListener(OnCameraActivated);
                    }
                }   
                return _mainCinemachineBrain;
            }
        }

        public VirtualCameraTouchController cinemachineTouchController;

        public Action customVirtualCameraEndAction;

        public Camera GetMainCamera() {
            if (_mainCameraGo == null || _mainCamera == null) {
                _mainCameraGo = GameObject.Find("GlobalMainCamera");
                // _mainCameraGo = GameObject.Find("MainCamera");
                if (_mainCameraGo != null) {
                    _mainCamera = _mainCameraGo.GetComponent<Camera>();
                } else {
                    LoggerUtils.LogError("Can not Find MainCamera!");
                }
            }

            return _mainCamera;
        }
        
        public Camera GetShotCamera() {
            if (_shotCameraGo == null || _shotCamera == null) {
                _shotCameraGo = GameObject.Find("ShotCamera");
                if (_shotCameraGo != null) {
                    _shotCamera = _shotCameraGo.GetComponent<Camera>();
                } else {
                    LoggerUtils.LogError("Can not Find ShotCamera!");
                }
            }

            return _shotCamera;
        }


        public Camera GetUICamera() {
            if (_UICameraGo == null || _UICamera == null) {
                _UICameraGo = GameObject.Find("UICamera");
                if (_UICameraGo != null) {
                    _UICamera = _UICameraGo.GetComponent<Camera>();
                } else {
                    LoggerUtils.LogError("Can not Find UICamera!");
                }
            }
            return _UICamera;
        }

        public Vector3 GetCreatePosition() {
            if (_mainCamera != null) {
                Vector3 screenPos = new Vector3(Screen.width / 2, Screen.height / 3, 0);
                Ray ray = _mainCamera.ScreenPointToRay(screenPos);
                var layerMask = 1 << LayerMask.NameToLayer("PVPArea");
                bool isHit = Physics.Raycast(ray, out RaycastHit hit, maxCamDist, ~layerMask);
                Vector3 targetPosition = isHit ? hit.point + createOffset : createDefault;
                return targetPosition;
            } else {
                return Vector3.zero;
            }
        }


        public CinemachineVirtualCamera GetPlayVirtualCamera() {
            if (_playVirtualCamera == null) {
                _playVirtualCamera = GameObject.Find("PlayVirtualCamera").GetComponent<CinemachineVirtualCamera>();
            }

            return _playVirtualCamera;
        }

        public CinemachineVirtualCamera GetEditVirtualCamera() {
            if (_editVirtualCamera == null) {
                _editVirtualCamera = GameObject.Find("EditVirtualCamera").GetComponent<CinemachineVirtualCamera>();
            }

            return _editVirtualCamera;
        }

        public CinemachineVirtualCamera GetCustomVirtualCamera()
        {
            if (_customVirtualCamera == null)
            {
                _customVirtualCamera = GameObject.Find("CustomVirtualCamera").GetComponent<CinemachineVirtualCamera>();
                cinemachineTouchController = _customVirtualCamera.GetComponent<VirtualCameraTouchController>();
            }

            return _customVirtualCamera;
        }

        public void SetPlayVirtualDamping(Vector3 damping)
        {
            var playVirtualCamera = GetPlayVirtualCamera();
            if (playVirtualCamera != null)
            {
                var transposer = playVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();
                if (transposer != null)
                {
                    transposer.m_XDamping = damping.x;
                    transposer.m_YDamping = damping.y;
                    transposer.m_ZDamping = damping.z;
                }
            }
        }

        public override void Release() {
            base.Release();
            if (_colliderListenerSetup) {
                MessageHelper.RemoveListener<bool>(MessageName.LinkEmoteStateChange, OnLinkEmoteColliderChange);
                MessageHelper.RemoveListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnLinkEmoteColliderChange);
                _colliderListenerSetup = false;
            }
            Clear();
        }

        public void Clear() {
            SetMainCameraCinemachineBrain(false);
            _UICameraGo = null;
            _UICamera = null;
            _mainCameraGo = null;
            _mainCamera = null;
            _mainCinemachineBrain = null;
        }

        public void SetMainCameraCinemachineBrain(bool isEnable) {
            if (MainCinemachineBrain != null) {
                MainCinemachineBrain.enabled = isEnable;
            }
            if (isEnable) {
                SetupLinkEmoteColliderListener();
            }
        }

        private bool _colliderListenerSetup = false;
        private void SetupLinkEmoteColliderListener()
        {
            if (_colliderListenerSetup) return;
            _colliderListenerSetup = true;
            MessageHelper.AddListener<bool>(MessageName.LinkEmoteStateChange, OnLinkEmoteColliderChange);
            MessageHelper.AddListener<bool>(MessageName.BuddyLinkEmoteStateChange, OnLinkEmoteColliderChange);
        }

        private void OnLinkEmoteColliderChange(bool isLink)
        {
            var cam = GetPlayVirtualCamera();
            if (cam == null) return;
            var cinemachineCollider = cam.GetComponent<CinemachineCollider>();
            if (cinemachineCollider != null)
                cinemachineCollider.enabled = !isLink;
        }

        public Vector2 ConvertPlayerPosToUI(RectTransform parentRect) {
            Vector2 result = Vector2.zero;
            var mainCamera = GetMainCamera();
            var uiCamera = GetUICamera();
            if (mainCamera != null && AvatarController.Inst.SelfController != null && uiCamera != null) {
                var selfPlayer = AvatarController.Inst.SelfController.transform;
                Vector3 camPos = mainCamera.WorldToScreenPoint(selfPlayer.position);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, camPos, uiCamera, out result);
            }
            return result;
        }


        private void OnCameraCutd(CinemachineBrain newCamera)
        {
            Debug.Log($"切换！新相机 '{newCamera.name}'。");
        }
        private void OnCameraActivated(ICinemachineCamera newCamera, ICinemachineCamera oldCamera)
        {
            Debug.Log($"运镜结束！新相机 '{newCamera.Name}' 已激活。");

            if (newCamera.Name == "CustomVirtualCamera")
            {
                customVirtualCameraEndAction?.Invoke();
                customVirtualCameraEndAction = null;
            }
        }

        public void SetCustomVirtualCameraEndAction(Action _ac) {
            customVirtualCameraEndAction = _ac;
        }

        public void SetCinemachineTouchController(bool bo, CinemachineTouchParam param)
        {
            cinemachineTouchController.enabled = bo;
            cinemachineTouchController.SetData(param);
        }
    }
}


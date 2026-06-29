using UnityEngine;
using DG.Tweening;
using GameData.PgcData;
using Es;
using GameData;

namespace Game.Avatar
{

    public enum ViewType
    {
        ZoomUpperBody,
        ZoomUnderBody,
        ZoomWholeBody,
        ZoomWholeBack,
        ZoomEmote,
    }

    /// <summary>
    /// Description:人物形象编辑界面，对人物进行镜头拉近拉远
    /// </summary>
    public class AvatarCameraController : CommonAvatarGestureHandler
    {
        [Header("预设相机位置")]
        public ViewType viewType;
        [SerializeField] public float ZoomUpperPosY;
        [SerializeField] public float ZoomUppereCameraSize;
        [SerializeField] public float ZoomWholePosY;
        [SerializeField] public float ZoomWholeCameraSize;
        [SerializeField] public float ZoomFootPosY;
        [SerializeField] public float ZoomFootCameraSize;

        private Tweener cameraOrthoSizeTw;
        /// <summary>MoveRoot 世界旋转 tween，统一管理防止目标销毁后 DOTween 报空引用</summary>
        private Tweener _moveRootRotTw;
        /// <summary>RotateTarget 世界旋转 tween，统一管理防止目标销毁后 DOTween 报空引用</summary>
        private Tweener _rotateTargetRotTw;
        private float oriMinScale;
        /// <summary>
        /// 重置角色朝向到正面（Y=-180），RotateTarget 为空时跳过。
        /// </summary>
        private float oriOrthoSize;

        private void ReRoleRote()
        {
            if (RotateTarget == null)
                return;

            Vector3 roleCurRote = new Vector3(0, -180, 0);
            _rotateTargetRotTw?.Kill();
            _rotateTargetRotTw = RotateTarget.DORotate(roleCurRote, 0.5f);
        }

        protected override void Awake()
        {
            base.Awake();
            oriMinScale = minScale;
            oriOrthoSize = roleCamera.orthographicSize;
        }

        private void CameraZoom()
        {
            switch (viewType)
            {
                case ViewType.ZoomUpperBody:
                    ZoomUpperBody();
                    break;
                case ViewType.ZoomWholeBody:
                    ZoomWholeBody();
                    break;
                case ViewType.ZoomUnderBody:
                    ZoomUnderBody();
                    break;
                case ViewType.ZoomWholeBack:
                    ZoomWholeBack();
                    break;
                case ViewType.ZoomEmote:
                    ZoomEmote();
                    break;
                default:
                    break;
            }
        }

        //全身
        private void ZoomWholeBody()
        {
            Vector3 enlargeV3 = new Vector3(MoveRoot.localPosition.x, ZoomWholePosY, MoveRoot.localPosition.z);
            Vector3 roation = new Vector3(0, 0, 0);
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomWholeCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            MoveRoot.DOLocalMove(enlargeV3, 0.5f);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(roation, 0.5f);
        }
        //半身
        private void ZoomUpperBody()
        {
            Vector3 zoomoutV3 = new Vector3(MoveRoot.localPosition.x, ZoomUpperPosY, MoveRoot.localPosition.z);
            Vector3 roation = new Vector3(0, 0, 0);
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomUppereCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            MoveRoot.DOLocalMove(zoomoutV3, 0.5f);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(roation, 0.5f);
        }
        private void ZoomUnderBody()
        {
            Vector3 roleCurRote = new Vector3(0, -138, 0);
            _rotateTargetRotTw?.Kill();
            _rotateTargetRotTw = RotateTarget.DORotate(roleCurRote, 0.5f);
            Vector3 v = new Vector3(MoveRoot.localPosition.x, ZoomFootPosY, MoveRoot.localPosition.z);
            Vector3 r = new Vector3(10, MoveRoot.rotation.y, MoveRoot.rotation.z);
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomFootCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            MoveRoot.DOLocalMove(v, 0.5f);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(r, 0.5f);
        }

        private void ZoomWholeBack()
        {
            Vector3 roleCurRote = new Vector3(0, 360, 0);
            _rotateTargetRotTw?.Kill();
            _rotateTargetRotTw = RotateTarget.DORotate(roleCurRote, 0.5f);
            Vector3 enlargeV3 = new Vector3(MoveRoot.localPosition.x, ZoomWholePosY, MoveRoot.localPosition.z);
            Vector3 roation = new Vector3(0, 0, 0);
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomWholeCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            MoveRoot.DOLocalMove(enlargeV3, 0.5f);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(roation, 0.5f);
        }

        private void ZoomEmote()
        {
            Vector3 enlargeV3 = new Vector3(MoveRoot.localPosition.x, ZoomWholePosY, MoveRoot.localPosition.z);
            Vector3 roation = new Vector3(0, 0, 0);
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomWholeCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            MoveRoot.DOLocalMove(enlargeV3, 0.5f);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(roation, 0.5f);
        }

        public void ZoomCustom(Vector3 roation, float cameraSize)
        {
            Vector3 enlargeV3 = new Vector3(MoveRoot.localPosition.x, ZoomWholePosY, MoveRoot.localPosition.z);
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, cameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            MoveRoot.DOLocalMove(enlargeV3, 0.5f);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(roation, 0.5f);
        }

        [HideInInspector]
        public float customEmoteCameraScale = 1.0f;

        public string curEmoteId { get; private set; }
        public void SetEmoteView(string emoteId)
        {
            curEmoteId = emoteId;
            var uiConfig = Es.DataTables.GetEmoUIConfig(emoteId);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(new Vector3(5, uiConfig.cameraRot, 0), 0.5f);
            if (uiConfig.cameraScale > 0)
            {
                minScale = uiConfig.cameraScale;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, uiConfig.cameraScale * ResolutionAutoFit.CameraScale * customEmoteCameraScale, 0.5f);
            }
            else
            {
                minScale = oriMinScale;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomWholeCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            }
            roleCamera.DOKill();
            roleCamera.transform.localPosition = new Vector3(uiConfig.cameraPos.x, uiConfig.cameraPos.y, roleCamera.transform.localPosition.z);
            ReRoleRote();
        }

        // 个人主页等场景：对特定动作直接按给定尺寸缩放相机(orthographicSize)，不改变相机位置/旋转。
        // size 越大角色显示越小；会同步更新 minScale 以保证手势缩放范围正确。
        public void SetCameraScale(float size)
        {
            minScale = size;
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, size * ResolutionAutoFit.CameraScale, 0.5f);
        }

        // 还原 SetCameraScale 的特殊缩放：相机大小(orthographicSize)与 minScale 回到打开界面时的默认值。
        public void ResetCameraScale()
        {
            minScale = oriMinScale;
            cameraOrthoSizeTw?.Kill();
            cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, oriOrthoSize, 0.5f);
        }



        public void SetEmoteView(UgcAnimSubType subType)
        {
            var uiConfig = DataTables.GetPoseModeConfig((int)subType);
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(new Vector3(uiConfig.EditCamRot.x, 0, 0), 0.5f);
            var cameraScale = (subType == UgcAnimSubType.Single || subType == UgcAnimSubType.PetSingle) ? 1.4f : 2f;
            if (cameraScale > 0)
            {
                minScale = cameraScale * 1.8f;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, cameraScale * ResolutionAutoFit.CameraScale * customEmoteCameraScale, 0.5f);
            }
            else
            {
                minScale = oriMinScale * 1.8f;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomWholeCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            }
            roleCamera.DOKill();
            roleCamera.transform.localPosition = new Vector3(-uiConfig.EditCamPos.x, uiConfig.EditCamPos.y, roleCamera.transform.localPosition.z);
            ReRoleRote();
        }

        public void ResetEmoteView()
        {
            curEmoteId = "Error";
            minScale = oriMinScale;
            roleCamera.DOKill();
            roleCamera.transform.localPosition = new Vector3(0, 0, roleCamera.transform.localPosition.z);
        }

        public void SetVehicleView(VehicleSubType subType)
        {
            
            var cameraScale = 2.5f;
            var cameraPos = new Vector3(0, 0, 0);

            switch (subType)
            {
                case VehicleSubType.SingleVehicle:
                    cameraPos = new Vector3(0, 0, 0);
                    break;
                case VehicleSubType.DoubleVehicle:
                    cameraPos = new Vector3(0, 0, 0);
                    break;
            }

            MoveRoot.DOLocalMove(cameraPos, 0.5f);
            if (cameraScale > 0)
            {
                minScale = 8f;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, cameraScale * ResolutionAutoFit.CameraScale * customEmoteCameraScale, 0.5f);
            }
            else
            {
                minScale = oriMinScale;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomWholeCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            }
            roleCamera.DOKill();
            roleCamera.transform.localPosition = new Vector3(0, 0, roleCamera.transform.localPosition.z);
            ReRoleRote();
        }

        public void SetVehicleViewByConfig(PgcVehicleConfig config)
        {
            SetSpecialSkinView(config.fittingRoomConfig);
        }

        public void ResetVehicleView()
        {
            minScale = oriMinScale;
            roleCamera.DOKill();
            roleCamera.transform.localPosition = new Vector3(0, 0, roleCamera.transform.localPosition.z);
        }

        public void SetCameraZoom(ViewType type)
        {
            viewType = type;
            ReRoleRote();
            CameraZoom();
        }


        public void SetSpecialSkinView(SpecialAnimCameraInfo cameraInfo) {
            _moveRootRotTw?.Kill();
            _moveRootRotTw = MoveRoot.DORotate(cameraInfo.rot, 0.5f);
            if (cameraInfo.scale > 0)
            {
                minScale = cameraInfo.scale;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, cameraInfo.scale * ResolutionAutoFit.CameraScale * customEmoteCameraScale, 0.5f);
            }
            else
            {
                minScale = oriMinScale;
                cameraOrthoSizeTw?.Kill();
                cameraOrthoSizeTw = DOTween.To(() => roleCamera.orthographicSize, x => roleCamera.orthographicSize = x, ZoomWholeCameraSize * ResolutionAutoFit.CameraScale, 0.5f);
            }
            roleCamera.DOKill();
            roleCamera.transform.DOLocalMove(new Vector3(cameraInfo.pos.x, cameraInfo.pos.y, roleCamera.transform.localPosition.z), 0.5f);
            ReRoleRote();
        }



        public void SetCameraZoom(int classType)
        {
            var resourcesType = UniqueType.ResourceType(classType);
            if (resourcesType == ResourceType.ErrResourceType)
            {
                SetCameraZoom(ViewType.ZoomWholeBody);
                return;
            }

            if (resourcesType == ResourceType.Emote)
            {
                SetCameraZoom(ViewType.ZoomEmote);
                return;
            }

            if(resourcesType == ResourceType.UgcVehicle)
            {
                //SetCameraZoom(ViewType.ZoomWholeBody);
                ReRoleRote();
                ZoomCustom(Vector3.zero, 2.5f);
                minScale = 8f;
                return;
            }

            var avatarSubType = UniqueType.AvatarSubType(classType);
            switch (avatarSubType)
            {
                case AvatarSubType.Shoe:
                    SetCameraZoom(ViewType.ZoomUnderBody);
                    break;
                case AvatarSubType.Backpack:
                case AvatarSubType.Tail:
                case AvatarSubType.Cape:
                    SetCameraZoom(ViewType.ZoomWholeBack);
                    break;
                case AvatarSubType.Eyes:
                case AvatarSubType.Earring:
                case AvatarSubType.Hats:
                case AvatarSubType.FacePaint:
                case AvatarSubType.Blush:
                case AvatarSubType.Brow:
                case AvatarSubType.Glasses:
                case AvatarSubType.Nose:
                case AvatarSubType.Mouth:
                case AvatarSubType.Head:
                case AvatarSubType.Ear:
                    // 宠物因为较小，直接看全身
                    if (resourcesType == ResourceType.PGCPetAvatar || resourcesType == ResourceType.UGCPetAvatar)
                    {
                        SetCameraZoom(ViewType.ZoomWholeBody);
                    }
                    else
                    {
                        SetCameraZoom(ViewType.ZoomUpperBody);
                    }

                    break;
                default:
                    SetCameraZoom(ViewType.ZoomWholeBody);
                    break;
            }
        }

        /// <summary>
        /// 销毁时清理所有旋转 tween，防止目标 GameObject 被销毁后 DOTween 仍尝试写入而报空引用。
        /// </summary>
        private void OnDestroy()
        {
            _moveRootRotTw?.Kill();
            _rotateTargetRotTw?.Kill();
            cameraOrthoSizeTw?.Kill();
        }
    }
}

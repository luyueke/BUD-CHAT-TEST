using System;
using Es;
using Game.Avatar;
using GameData.PgcData;
using UI.Preview3D.Bean;
using UnityEngine;

namespace UI.Preview3D.PreviewHandle
{
    public class ClothPreviewHandler : PlayerBasicPreviewHandler
    {
        public override void HandlePreview(Preview3DData data, PreviewWrap wrap)
        {
            base.HandlePreview(data, wrap);
            ChangeUIAvatarCloth(data);
        }

        public override void CancelPreview(Preview3DData data, PreviewWrap wrap)
        {
            ResetAvatarCloth();
            base.CancelPreview(data, wrap);
        }

        private void ChangeUIAvatarCloth(Preview3DData data)
        {
            _characterWrap?.ChangePart(data.ResType, data.PgcIdStr);
        }

        private void ResetAvatarCloth()
        {
            CharacterData avatarInfo = AccountDataManager.Inst.UserInfo.avatarInfo;
            _characterWrap?.RefreshAvatar(avatarInfo);
        }

        protected override PreviewParams GetPreviewPrams(Preview3DData data)
        {
            return GetPreviewParamsConfigByResType(data.ResType);
        }

        #region 不同部件对应不同角度

        private PreviewParams GetPreviewParamsConfigByResType(int resType)
        {
            switch ((AvatarSubType)resType)
            {
                case AvatarSubType.Eyes:
                case AvatarSubType.Brow:
                case AvatarSubType.Nose:
                case AvatarSubType.Mouth:
                case AvatarSubType.Blush:
                case AvatarSubType.Hats:
                case AvatarSubType.Glasses:
                case AvatarSubType.Visor:
                case AvatarSubType.Earring:
                case AvatarSubType.Scarf:
                case AvatarSubType.Hair:
                case AvatarSubType.FacePaint:
                    return NewUpperBodyPreviewParams();

                case AvatarSubType.Belt:
                case AvatarSubType.Clothes:
                case AvatarSubType.Effect:
                case AvatarSubType.Hand:
                case AvatarSubType.Glove:
                case AvatarSubType.Crossbody:
                    return NewWholeBodyPreviewParams();

                case AvatarSubType.Backpack:
                case AvatarSubType.Cape:
                    return NewBackBodyPreviewParams();

                case AvatarSubType.Shoe:
                    return NewFootPreviewParams();

                default:
                    return NewWholeBodyPreviewParams();
            }
        }

        //全身
        private PreviewParams NewWholeBodyPreviewParams()
        {
            return new PreviewParams()
            {
                ModelLocalEulerAngles = new Vector3(0, -180, 0),
                CameraLocalPos = new Vector3(0, 0.6f, -2.5f),
                CameraLocalEulerAngles = Vector3.zero,
            };
        }

        //背后
        private PreviewParams NewBackBodyPreviewParams()
        {
            return new PreviewParams()
            {
                ModelLocalEulerAngles = new Vector3(0, 0, 0),
                CameraLocalPos = new Vector3(0, 0.6f, -2.5f),
                CameraLocalEulerAngles = Vector3.zero,
            };
        }

        //上半身
        private PreviewParams NewUpperBodyPreviewParams()
        {
            return new PreviewParams()
            {
                ModelLocalEulerAngles = new Vector3(0, -180, 0),
                CameraLocalPos = new Vector3(0, 0.78f, -1.5f),
                CameraLocalEulerAngles = Vector3.zero,
            };
        }

        //双脚
        private PreviewParams NewFootPreviewParams()
        {
            var curCamRot = CurPreviewWrap.PreviewModelRoot.previewCamera.transform.rotation;
            return new PreviewParams()
            {
                ModelLocalEulerAngles = new Vector3(0, -138, 0),
                CameraLocalPos = new Vector3(0, 0.29f, -0.99f),
                CameraLocalEulerAngles = new Vector3(10, curCamRot.y, curCamRot.z),
            };
        }

        #endregion
    }
}
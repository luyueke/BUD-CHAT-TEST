using GameData.BaseInfo;
using UnityEngine;

namespace Es {
    public partial class AvatarCommonData {





        public void SetScaleLimit(float maxScale) {
            _scaLimit[0] = Vector3.zero;
            _scaLimit[1] = Vector3.one * maxScale;
        }


        public void ResetDef(SkinInfo skinInfo) {
            var oriConfigData = Es.DataTables.GetAvatarCommonData(skinInfo.templateId);
            if (oriConfigData == null) {
                return;
            }

            var configData = oriConfigData.MemberwiseClone() as AvatarCommonData;
            if (configData == null) {
                return;
            }
            if (skinInfo.skinDetailInfo != null) {
                // 因为角色缩放了 1.7倍, 因此值需要缩小 1.7倍率
                var fixScale = 1 / 1.7f;
                float maxValue = Mathf.Max(skinInfo.skinDetailInfo.size.x, skinInfo.skinDetailInfo.size.y,
                    skinInfo.skinDetailInfo.size.z);
                if (maxValue <= float.Epsilon) {
                    SetScaleLimit(2);
                } else if (maxValue < 2) {
                    SetScaleLimit(2 / maxValue);
                    _sDef = Vector3.one * fixScale;
                } else {
                    SetScaleLimit(2 / maxValue);
                    _sDef = configData.sDef * (1 / maxValue);
                }
            } else {
                _sDef = configData.sDef;
            }
            _pDef = configData.pDef;
            _rDef = configData.rDef;
            _anchor = configData.anchor;
        }

        public static AvatarCommonData From(SkinInfo skinInfo)
        {
            AvatarCommonData oriConfigData = null;

            switch ((SkinType)skinInfo.skinType)
            {
                case SkinType.Pet:
                    oriConfigData = Es.DataTables.GetPetAvatarCommonData(skinInfo.templateId);
                    break;

                default:
                case SkinType.Avatar:
                    oriConfigData  = Es.DataTables.GetAvatarCommonData(skinInfo.templateId);
                    break;
            }

            if (oriConfigData == null) {
                return null;
            }

            var configData = oriConfigData.MemberwiseClone() as AvatarCommonData;
            if (configData == null) {
                return null;
            }

            if (skinInfo.migrateData == 1 && skinInfo.skinDetailInfo != null) {
                configData._sDef = skinInfo.skinDetailInfo.sDef;
                configData._anchor = skinInfo.skinDetailInfo.anchor;
                configData._pDef = skinInfo.skinDetailInfo.pDef;
                configData._rDef = skinInfo.skinDetailInfo.rDef;
                configData.SetScaleLimit(2 / skinInfo.skinDetailInfo.sDef.magnitude);
                return configData;
            }


            if (skinInfo.skinDetailInfo != null && skinInfo.isProp) {
                // 因为角色缩放了 1.7倍, 因此值需要缩小 1.7倍率
                var fixScale = 1 / 1.7f;
                float maxValue = Mathf.Max(skinInfo.skinDetailInfo.size.x, skinInfo.skinDetailInfo.size.y,
                    skinInfo.skinDetailInfo.size.z);
                if (maxValue <= float.Epsilon) {
                    configData.SetScaleLimit(2);
                } else if (maxValue < 2) {
                    configData.SetScaleLimit(2 / maxValue);
                    configData._sDef = Vector3.one * fixScale;
                } else {
                    configData.SetScaleLimit(2 / maxValue);
                    configData._sDef *= (1 / maxValue);
                }

                if (skinInfo.skinDetailInfo.sDef.magnitude > configData._scaLimit[1].magnitude) {
                    skinInfo.skinDetailInfo.sDef = configData._scaLimit[1];
                }

                if (skinInfo.skinDetailInfo.anchor != Vector3.zero) {
                    configData._anchor = skinInfo.skinDetailInfo.anchor;
                }
            }

            if (skinInfo.skinDetailInfo != null) {

                if (skinInfo.skinDetailInfo.sDef != Vector3.one) {
                    configData._sDef = skinInfo.skinDetailInfo.sDef;
                }

                if (skinInfo.skinDetailInfo.pDef != Vector3.zero) {
                    configData._pDef = skinInfo.skinDetailInfo.pDef;
                }

                if (skinInfo.skinDetailInfo.rDef != Vector3.zero) {
                    configData._rDef = skinInfo.skinDetailInfo.rDef;
                }
            }

            return configData;

        }


        public static AvatarCommonData SkinFrom(SkinInfo skinInfo) {
            AvatarCommonData oriConfigData = null;

            switch ((SkinType)skinInfo.skinType)
            {
                case SkinType.Pet:
                    oriConfigData = Es.DataTables.GetPetAvatarCommonData(skinInfo.templateId);
                    break;

                default:
                case SkinType.Avatar:
                    oriConfigData  = Es.DataTables.GetAvatarCommonData(skinInfo.templateId);
                    break;
            }

            if (oriConfigData == null) {
                return null;
            }

            var configData = oriConfigData.MemberwiseClone() as AvatarCommonData;
            if (configData == null) {
                return null;
            }

            if (skinInfo.skinDetailInfo != null) {

                if (skinInfo.skinDetailInfo.sDef != Vector3.one) {
                    configData._sDef = skinInfo.skinDetailInfo.sDef;
                }

                if (skinInfo.skinDetailInfo.pDef != Vector3.zero) {
                    configData._pDef = skinInfo.skinDetailInfo.pDef;
                }

                if (skinInfo.skinDetailInfo.rDef != Vector3.zero) {
                    configData._rDef = skinInfo.skinDetailInfo.rDef;
                }
            }
            return configData;
        }
    }
}

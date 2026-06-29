using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using GameData.UGCData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI {
    public class BaseDetailView : UGCBaseStateView {
        [SerializeField] protected RemoteImageBehaviour cover;

        [SerializeField] protected CButton editCoverBtn;

        [SerializeField] protected CButton nameEditBtn;
        [SerializeField] protected SuperTextMesh nameText;
        [SerializeField] protected GameObject emptyNameObj;
        [SerializeField] protected Text nameLimitText;
        private KeyBoardInfo nameKeyBoardInfo;
        protected virtual int NameLimitCount => 30;

        [SerializeField] protected CButton descriptionEditBtn;
        [SerializeField] protected SuperTextMesh descriptionText;
        [SerializeField] protected GameObject emptyDescriptionObj;
        [SerializeField] protected Text descLimitText;
        [Header("设置背景风格")]
        [SerializeField] private bool CanPublishAnime;

        private KeyBoardInfo descriptionKeyBoardInfo;
        protected virtual int DescLimitCount => 250;

        
        protected enum PublishResType
        {
            Mat = 1,
            Prop = 2,
            Instrument = 3,
            MusicScore = 4,
            Tone = 5
        }


        public override void Awake() {
            base.Awake();
            editCoverBtn.onClick.AddListener(OnEditCoverBtnClick);
            nameEditBtn.onClick.AddListener(OnNameEditBtnClick);
            descriptionEditBtn.onClick.AddListener(OnDescriptionEditBtnClick);


            nameKeyBoardInfo = new KeyBoardInfo {
                type = 0,
                placeHolder = "",
                inputMode = 0,
                maxLength = NameLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };

            descriptionKeyBoardInfo = new KeyBoardInfo {
                type = 0,
                placeHolder = "",
                inputMode = 0,
                maxLength = DescLimitCount,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("字数超出限制"),
                returnKeyType = (int)ReturnType.Return
            };
        }

        public override void Show()
        {
            base.Show();
            ChangeUgcStyle();
        }

        private void ChangeUgcStyle()
        {
            switch (editData)
            {
                case InstrumentEditData data:
                    var skinInfo = data.GetSkinInfo();
                    SetUgcStyle(skinInfo.ugcStyle);
                    break;
                case SkinEditData data:
                    skinInfo = data.GetSkinInfo();
                    SetUgcStyle(skinInfo.ugcStyle);
                    break;
                case PropEditData data:
                    var propInfo = data.GetPropInfo();
                    SetUgcStyle(propInfo.ugcStyle);
                    break;
                case MaterialEditData data:
                    var matInfo = data.GetMaterialInfo();
                    SetUgcStyle(matInfo.ugcStyle);
                    break;
                case UgcBundleEditData data:
                    var bundleInfo = data.GetUgcBundleInfo();
                    SetUgcStyle(bundleInfo.ugcStyle);
                    break;
                case VehicleEditData data:
                    var vehicleInfo = data.GetVehicleInfo();
                    SetUgcStyle(vehicleInfo.ugcStyle);
                    break;
                default:
                    SetUgcStyle((int)UgcShaderStyle.Normal);
                    break;
            }
        }

        private void SetActivityBG(bool isNormal)
        {
            string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
            var ActivityCenterBg = this.transform.Find("ActivityCenterBg");
            if (ActivityCenterBg != null) {
                var bgItems = this.transform.Find("ActivityCenterBg").GetComponent<ActivityCenterBgItem>();
                bgItems.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
            {
                "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
            });
                bgItems.gameObject.SetActive(true);
                var bgPanel = bgItems.GetComponent<ColorBgPanel>();
                var bgIconColor = isNormal
                    ? DataUtil.DeSerializeColorCheckHash("EFE7FF")
                    : DataUtil.DeSerializeColorCheckHash("4CDDBC");
                bgPanel.SetImagesColor(bgIconColor);
            }
        }

        protected virtual void SetUgcShaderStyle(UgcShaderStyle ugcStyle)
        {
        }

        private void SetUgcStyle(int ugcStyle)
        {
            var shaderStyle = (UgcShaderStyle) ugcStyle;
            bool isNormal = shaderStyle == UgcShaderStyle.Normal;
            SetActivityBG(isNormal);
            SetUgcShaderStyle(shaderStyle);
            if (CanPublishAnime)
            {
                var animeTag = this.transform.Find("AnimeTag");
                var PanelBg = this.transform.Find("Main/Content").GetComponent<Image>();
                var priceParent = this.transform.Find("Main/Content/DetailsInfo/Price/PriceContent");
                var coverBg = this.transform.Find("Main/LeftPanelImageBg/Bg").GetComponent<Image>();
                
                if (animeTag != null)
                {
                    animeTag.gameObject.SetActive(!isNormal);
                }
                
                var bgColor = isNormal
                    ? DataUtil.DeSerializeColorCheckHash("DAD0FF")
                    : DataUtil.DeSerializeColorCheckHash("93E4D2");
                PanelBg.color = bgColor;
                coverBg.color = bgColor;
                
                var toggleColor = isNormal
                    ? DataUtil.DeSerializeColorCheckHash("D4C0FF")
                    : DataUtil.DeSerializeColorCheckHash("4CDDBC");
            
                var toggles = priceParent.GetComponentsInChildren<Toggle>();
                if (toggles != null)
                {
                    foreach (var toggle in toggles)
                    {
                        toggle.GetComponent<Image>().color = toggleColor;
                        var child = toggle.transform.GetChild(0).GetComponent<Image>();
                        child.color = toggleColor;
                    }
                }
                var customPriceTransform = priceParent.Find("PriceChangeBtn");
                if (customPriceTransform != null)
                {
                    var customPriceImage = customPriceTransform.GetComponent<Image>();
                    customPriceImage.color = toggleColor;
                    var child0 = customPriceImage.transform.GetChild(0).GetComponent<Image>();
                    child0.color = toggleColor;
                    var child1 = customPriceImage.transform.GetChild(1).GetComponent<Text>();
                    child1.color = toggleColor;
                }
            }
        }


        private void OnDescriptionEditBtnClick() {
            descriptionKeyBoardInfo.defaultText = editData.GetInfo().desc;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetDescFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(descriptionKeyBoardInfo));
        }

        private void OnGetDescFromNative(string desc) {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            editData.GetInfo().desc = desc;
            SyncEditData();
        }

        private void OnNameEditBtnClick() {
            nameKeyBoardInfo.defaultText = editData.GetInfo().name;
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetNameFormNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(nameKeyBoardInfo));
        }

        private void OnGetNameFormNative(string value) {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            editData.GetInfo().name = value;
            SyncEditData();
        }


        protected virtual void OnEditCoverBtnClick() {
        }

        protected override void SyncEditData() {
            base.SyncEditData();
            if (string.IsNullOrEmpty(editData.GetInfo().name)) {
                emptyNameObj.SetActive(true);
                nameText.gameObject.SetActive(false);
                nameLimitText.text = $"0/{NameLimitCount}";
            } else {
                nameText.text = editData.GetInfo().name;
                emptyNameObj.SetActive(false);
                nameText.gameObject.SetActive(true);
                nameLimitText.text = $"{editData.GetInfo().name.Length}/{NameLimitCount}";
            }

            if (string.IsNullOrEmpty(editData.GetInfo().desc)) {
                emptyDescriptionObj.SetActive(true);
                descriptionText.gameObject.SetActive(false);
                descLimitText.text = $"0/{DescLimitCount}";
            } else {
                descriptionText.text = editData.GetInfo().desc;
                emptyDescriptionObj.SetActive(false);
                descriptionText.gameObject.SetActive(true);
                descLimitText.text = $"{editData.GetInfo().desc.Length}/{DescLimitCount}";
            }


            SyncCover();

            CheckNextEnable();
        }

        protected virtual void SyncCover() {
            if (!string.IsNullOrEmpty(editData.GetInfo().cover)) {
                cover.Load(editData.GetInfo().cover);
            }
        }

        protected virtual void CheckNextEnable() {
            bool isEnable = !string.IsNullOrEmpty(editData.GetInfo().name);
            isEnable &= !string.IsNullOrEmpty(editData.GetInfo().desc);
            SetNextEnabled(isEnable);
        }
    }
}

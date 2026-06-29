using System;
using System.Collections.Generic;
using Es;
using GameData.BaseInfo;
using GameData.MapData;
using GameData.UGCData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class PropDetailView : BaseDetailView
    {
        [SerializeField] private Toggle[] priceToggles;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;

        [SerializeField] private GameObject[] gemObjects;
        [SerializeField] private GameObject[] pinkCoinObjects;

        private Dictionary<int, Toggle> priceToggleDic = new Dictionary<int, Toggle>();
        private KeyBoardInfo priceKeyBoardInfo;
        private PropEditData propEditData;
        private int MinPrice = 0;
        public override void Awake()
        {
            base.Awake();

            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
       
            priceKeyBoardInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格"),
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice, 9999),
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
        }

        public override void Show()
        {
            base.Show();
            priceToggles[0].isOn = true;
        }
        
        protected override void OnEditCoverBtnClick()
        {
            base.OnEditCoverBtnClick();
            gameObject.SetActive(false);
            UGCPublishStateBase coverState = null;
            if (editData.GetInfo() is PropInfo)
            {
                coverState = new PropCoverState();
            }

            if (coverState == null)
            {
                return;
            }

            coverState.InitData(editData);
            coverState.OnEnter();
            coverState.SetNextCallback(() =>
            {
                coverState.OnExit();
                gameObject.SetActive(true);
                SyncEditData();
            });
            coverState.SetPreCallback(() =>
            {
                coverState.OnExit();
                gameObject.SetActive(true);
                SyncEditData();
            });
        }

        private void OnPriceEditBtnClick()
        {
            priceKeyBoardInfo.defaultText = "";
            priceKeyBoardInfo.placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格");
            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.showKeyboard, OnGetPriceFromNative);
            MobileInterface.Instance.ShowKeyboard(JsonConvert.SerializeObject(priceKeyBoardInfo));
        }


        private void OnGetPriceFromNative(string price)
        {
            MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.showKeyboard);
            if (int.TryParse(price, out var value))
            {
                OnPriceToggleClick(value, true);
            }
        }


        protected override void OnNextBtnClick()
        {
            bool isEmpty = CheckEmpty();
            if (isEmpty) {
                TipPanel.ShowToast("当前的作品为空素材，请继续编辑再来发布哦");
                return;
            }
            ((LoadingButton)nextBtn).ShowLoading();

            // 先上传本地草稿
            // 成功后，发布草稿
            propEditData.draftInfo.Upload((info, isSuccess) =>
            {
                if (gameObject == null)
                {
                    return;
                }

                if (isSuccess)
                {
                    propEditData.draftInfo.PublishDraftToServer((newInfo, newSuccess) =>
                    {
                        if (newSuccess)
                        {
                            base.OnNextBtnClick();
                        }
                        else
                        {
                            ((LoadingButton)nextBtn).HideLoading();
                        }
                    });
                }
                else
                {
                    ((LoadingButton)nextBtn).HideLoading();
                    TipPanel.ShowToast($"{propEditData.draftInfo.GetUploadMessage()}");
                }
            });
        }


        private bool CheckEmpty() {
            if (propEditData.metaDataBytes == null && !string.IsNullOrEmpty(editData.GetInfo().metaDataUrl)) {
                propEditData.metaDataBytes = Asset.LoadRemoteAssetSync(editData.GetInfo().metaDataUrl);
            }
            if (propEditData.metaDataBytes != null) {
                var itemPb = MapPbDataTool.ParsePropPb(propEditData.metaDataBytes);
                if (itemPb != null && itemPb.NodeData.Prims.Count > 0) {
                    return false;
                } else {
                    return true;
                }
            } else {
                return true;
            }
        }

        private void OnPriceToggleClick(int value,bool isCustom = false )
        {
            if (isCustom && (value < MinPrice || value > 9999))
            {
                var lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice, 9999);
                TipPanel.ShowToast(lengthTips);
                return;
            }

            if (propEditData.GetPropInfo().paymentInfo == null)
            {
                propEditData.GetPropInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = propEditData.currencyType
                };
            }
            else
            {
                propEditData.GetPropInfo().paymentInfo.currencyType = propEditData.currencyType;
                propEditData.GetPropInfo().paymentInfo.price = value;
            }

            SyncEditData();
        }
        
        protected override void SetUgcShaderStyle(UgcShaderStyle ugcStyle)
        {
            base.SetUgcShaderStyle(ugcStyle);
            string freeText = LocalizationManager.Inst.GetLocalizedText("免费");
            var priceData = DataTables.GetOtherPublishPrice((int)PublishResType.Prop);
            MinPrice = ugcStyle == UgcShaderStyle.Normal ? priceData.normalLowPrice : priceData.animeLowPrice;
#if PACKAGE_TYPE_US
            MinPrice = priceData.globalLowPrice;
#endif
            priceToggleDic.Clear();
            for (var i = 0; i < priceToggles.Length; i++)
            {
                priceToggles[i].isOn = false;
                var priceText = priceToggles[i].transform.Find("Label").GetComponent<Text>();
                int price = ugcStyle == UgcShaderStyle.Normal ? priceData.prices[i] : priceData.animePrices[i];
#if PACKAGE_TYPE_US
                price = priceData.globalPrices[i];
#endif
                priceText.text = price == 0 ? freeText : price.ToString();
                priceToggleDic.Add(price, priceToggles[i]);
                priceToggles[i].onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        OnPriceToggleClick(price);
                    }
                });
            }
        }

        protected override void SyncEditData()
        {
            propEditData = editData as PropEditData;

            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();
            
            var paymentInfo = propEditData.GetPropInfo().paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                // priceToggles[0].SetIsOnWithoutNotify(true);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                selfPriceText.text = "";
            }
            else
            {
                bool isCustom = false;
                Toggle findToggle = null;
                if (propEditData.GetPropInfo().paymentInfo.currencyType == CurrencyType.PinkCoin)
                {
                    priceToggleDic.TryGetValue(propEditData.GetPropInfo().paymentInfo.price,
                        out findToggle);
                }
                if (findToggle == null)
                {
                    priceToggles[0].group.SetAllTogglesOff(false);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text =
                        paymentInfo.price.ToString();
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(propEditData.currencyType, gameObject);
                }
                else
                {
                    findToggle.SetIsOnWithoutNotify(true);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                }
                
                // coinText.SetActive(true);
                double price = paymentInfo.price / 2.0;
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",price,"<q=icn_common_green_big>");
            }
        }
        

        protected override void SyncCover()
        {
            base.SyncCover();
            var tmpCover = propEditData.draftInfo.GetCoverUrl();
            if (!string.IsNullOrEmpty(tmpCover))
            {
                cover.Load(tmpCover);
            }
        }
    }
}

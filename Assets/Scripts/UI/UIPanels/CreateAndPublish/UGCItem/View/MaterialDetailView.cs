using System.Collections.Generic;
using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using GameData.BaseInfo;
using GameData.Gashapon;
using GameData.MapData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class MaterialDetailView :  BaseDetailView
    {
        [SerializeField] private Toggle[] priceToggles;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;

        [SerializeField] private GameObject[] gemObjects;
        [SerializeField] private GameObject[] pinkCoinObjects;

        private Dictionary<int, Toggle> priceToggleDic = new Dictionary<int, Toggle>();

        private KeyBoardInfo priceKeyBoardInfo;

        private MaterialEditData skinEditData;
        [SerializeField] private GameObject coinText;
        private int MinPrice = 0;
        public override void Awake()
        {
            base.Awake();
            editCoverBtn.gameObject.SetActive(false);
            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);

            priceKeyBoardInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = LocalizationManager.Inst.GetLocalizedText("请输入自定义价格"),
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", 0, 9999),
                defaultText = "",
                returnKeyType = (int)ReturnType.Return
            };
        }


        public override void Show()
        {
            base.Show();
            priceToggles[0].isOn = false;
            priceToggles[0].isOn = true;
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
            if (editData is not MaterialEditData skinEditData) {
                return;
            }
            ((LoadingButton)nextBtn).ShowLoading();

            skinEditData.draftInfo.Upload((info, isSuccess) =>
            {
                if (gameObject == null) return;
                if (isSuccess)
                {
                    skinEditData.draftInfo.PublishDraftToServer((newInfo, isSuccess) => {
                        if (isSuccess)
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
                    TipPanel.ShowToast($"{skinEditData.draftInfo.GetUploadMessage()}");
                }
            });
        }

        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            var lengthTips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice, 9999);
            if (isCustom && (value < MinPrice || value > 9999))
            {
                TipPanel.ShowToast(lengthTips);
                return;
            }

            if (skinEditData.GetMaterialInfo().paymentInfo == null)
            {
                skinEditData.GetMaterialInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = skinEditData.currencyType
                };
            }
            else
            {
                skinEditData.GetMaterialInfo().paymentInfo.currencyType = skinEditData.currencyType;
                skinEditData.GetMaterialInfo().paymentInfo.price = value;
            }

            SyncEditData();
        }


        protected override void SetUgcShaderStyle(UgcShaderStyle ugcStyle)
        {
            base.SetUgcShaderStyle(ugcStyle);
            string freeText = LocalizationManager.Inst.GetLocalizedText("免费");
            var priceData = DataTables.GetOtherPublishPrice((int)PublishResType.Mat);
            priceToggleDic.Clear();
            MinPrice = ugcStyle == UgcShaderStyle.Normal ? priceData.normalLowPrice : priceData.animeLowPrice;
#if PACKAGE_TYPE_US
            MinPrice = priceData.globalLowPrice;
#endif
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
            skinEditData = editData as MaterialEditData;

            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();
            var paymentInfo = skinEditData.GetMaterialInfo().paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                // priceToggles[0].SetIsOnWithoutNotify(true);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                selfPriceText.text = "";
                coinText.SetActive(false);
            }
            else
            {
                Toggle findToggle = null;
                if(skinEditData.GetMaterialInfo().paymentInfo.currencyType == CurrencyType.PinkCoin)
                {
                    priceToggleDic.TryGetValue(skinEditData.GetMaterialInfo().paymentInfo.price, out findToggle);
                }
                if (findToggle == null)
                {
                    priceToggles[0].group.SetAllTogglesOff(false);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text =
                        paymentInfo.price.ToString();
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(skinEditData.currencyType, gameObject);
                }
                else
                {
                    findToggle.SetIsOnWithoutNotify(true);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                }
                double price = paymentInfo.price / 2.0;
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",price,"<q=icn_common_green_big>");
                // coinText.SetActive(true);
            }
        }
    }
}

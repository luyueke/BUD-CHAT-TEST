using System;
using System.Collections.Generic;
using Basic.Extensions;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.MusicalInstrument;
using GameData.Base;
using GameData.MapData;
using GameData.UGCData;
using Message;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using PaymentInfo = GameData.Base.PaymentInfo;

namespace UI
{
    public class InstrumentPublishView :  BaseDetailView
    {
        [SerializeField] private Toggle[] priceToggles;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;

        [SerializeField] private GameObject[] gemObjects;
        [SerializeField] private GameObject[] pinkCoinObjects;

        private Dictionary<int, Toggle> priceToggleDic = new Dictionary<int, Toggle>();

        private KeyBoardInfo priceKeyBoardInfo;

        private InstrumentEditData instrumentEditData;
        
        private int MinPrice = 100;
        private const int MaxPrice = 9999;
        
        public override void Awake()
        {
            base.Awake();
            editCoverBtn.gameObject.SetActive(false);
            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);

            string holderStr = LocalizationManager.Inst.GetLocalizedText("请输入整数");
            string lengthStr = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice,MaxPrice);
            priceKeyBoardInfo = new KeyBoardInfo()
            {
                type = 0,
                placeHolder = holderStr,
                inputMode = 1,
                maxLength = 4,
                inputFlag = 0,
                textSecurity = 1,
                lengthTips = lengthStr,
                returnKeyType = (int)ReturnType.Return
            };
        }

        public override void Show()
        {
            base.Show();
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
            bool isEmpty = CheckEmpty();
            if (isEmpty) {
                TipPanel.ShowToast("当前的作品为空素材，请继续编辑再来发布哦");
                return;
            }

            bool isVip = VipDataManager.Inst.isVip;
            if (!isVip)
            {
                var joinVipType = new List<JoinVipType>();
                var joinVipTitle = "";

                var animId = instrumentEditData.GetInstrumentInfo().moveId;
                var toneInfo = instrumentEditData.GetToneInfo();
                if (toneInfo.IsPgc())
                {
                    var toneConfig =  Es.DataTables.GetInstrumentToneConfig(toneInfo.id);
                    if (toneConfig != null && toneConfig.isVip == 1)
                    {
                        string toneStr = LocalizationManager.Inst.GetLocalizedText(toneConfig.toneName);
                        string titleStr = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：VIP音色-");
                        joinVipTitle = titleStr + toneStr;
                        joinVipType.Add(JoinVipType.VIP_Tone);
                    }
                }
                if (!MusicalInstrumentUtils.FreeMoveIdList.Contains(animId))
                {
                    var animConfigList = Es.DataTables.GetInstrumentAniConfigList();
                    var animConfig = animConfigList.Find(x => x.emoId == animId);
                    string animStr = LocalizationManager.Inst.GetLocalizedText(animConfig?.name?.Replace("DIY", ""));
                    string titleStr = LocalizationManager.Inst.GetLocalizedText("您正在使用的VIP功能：VIP演奏动作-");
                    joinVipTitle = titleStr + animStr;
                    joinVipType.Add(JoinVipType.VIP_InstrumentAnim);
                }

                if (joinVipType.Count > 0)
                {
                    UIManager.Inst.OpenPanel<JoinVipPanel>(PanelId.JoinVipPanel, joinVipTitle, joinVipType);
                    return;
                }
            }

            ((LoadingButton)nextBtn).ShowLoading();

            instrumentEditData.skinActionDraftInfo.Upload((info, isSuccess) => {
                if (gameObject == null) {
                    return;
                }
                if (isSuccess) {
                    instrumentEditData.skinActionDraftInfo.PublishDraftToServer((skinInfo, skinActionInfo, newSuccess) => {
                        if (newSuccess) {
                            base.OnNextBtnClick();
                            MessageHelper.Broadcast<UgcBaseInfo>(MessageName.UgcInstrumentDidPublishedNew, skinInfo);
                        } else {
                            ((LoadingButton)nextBtn).HideLoading();
                        }
                    });
                } else {
                    ((LoadingButton)nextBtn).HideLoading();
                    TipPanel.ShowToast($"{instrumentEditData.skinActionDraftInfo.GetUploadMessage()}");
                }
            });


        }

        private bool CheckEmpty() {
            if (instrumentEditData.metaDataBytes == null && !string.IsNullOrEmpty(editData.GetInfo().metaDataUrl)) {
                instrumentEditData.metaDataBytes = Asset.LoadRemoteAssetSync(editData.GetInfo().metaDataUrl);
            }
            if (instrumentEditData.metaDataBytes != null) {
                var itemPb = MapPbDataTool.ParsePropPb(instrumentEditData.metaDataBytes);
                if (itemPb != null && itemPb.NodeData.Prims.Count > 0) {
                    return false;
                } else {
                    return true;
                }
            } else {
                return true;
            }
        }

        private void OnPriceToggleClick(int value, bool isCustom = false)
        {
            if (isCustom && (value < MinPrice || value > MaxPrice))
            {
                string tipsStr = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice,MaxPrice);
                TipPanel.ShowToast(tipsStr);
                return;
            }

            if (instrumentEditData.GetSkinInfo().paymentInfo == null)
            {
                instrumentEditData.GetSkinInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = instrumentEditData.currencyType
                };
            }
            else
            {
                instrumentEditData.GetSkinInfo().paymentInfo.price = value;
                instrumentEditData.GetSkinInfo().paymentInfo.currencyType = instrumentEditData.currencyType;
            }

            SyncEditData();
        }

        protected override void SetUgcShaderStyle(UgcShaderStyle ugcStyle)
        {
            base.SetUgcShaderStyle(ugcStyle);
            string freeText = LocalizationManager.Inst.GetLocalizedText("免费");
            var priceData = DataTables.GetOtherPublishPrice(3);
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
            instrumentEditData = editData as InstrumentEditData;

            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();

            var paymentInfo = instrumentEditData.GetSkinInfo().paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                // priceToggles[0].SetIsOnWithoutNotify(true);
                OnPriceToggleClick(100);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",50,"<q=icn_common_green_big>");
            }
            else
            {
                Toggle findToggle = null;
                if (instrumentEditData.GetSkinInfo().paymentInfo.currencyType == CurrencyType.PinkCoin)
                {
                    priceToggleDic.TryGetValue(instrumentEditData.GetSkinInfo().paymentInfo.price,
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
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(instrumentEditData.currencyType, gameObject);
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

            }
        }

        protected override void SyncCover() {
            var coverUrl = instrumentEditData.skinActionDraftInfo.GetCoverUrl();
            cover.Load(coverUrl);
        }
    }
}

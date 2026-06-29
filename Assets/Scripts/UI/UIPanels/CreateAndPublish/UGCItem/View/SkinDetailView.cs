using System.Collections.Generic;
using Basic.Extensions;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using GameData.Base;
using GameData.BaseInfo;
using GameData.Gashapon;
using GameData.MapData;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using xasset;
using PaymentInfo = GameData.Base.PaymentInfo;
using Text = UnityEngine.UI.Text;

namespace UI
{
    public class SkinDetailView :  BaseDetailView
    {
        [SerializeField] private Toggle[] priceToggles;
        [SerializeField] private Toggle gem20;
        [SerializeField] private Toggle gem30;
        [SerializeField] private Toggle gem50;
        [SerializeField] private Toggle gem55;
        [SerializeField] private Toggle gem75;
        [SerializeField] private CButton priceEditBtn;
        [SerializeField] private SuperTextMesh selfPriceText;

        [SerializeField] private GameObject[] gemObjects;
        [SerializeField] private GameObject[] pinkCoinObjects;
        [Header("私单")]
        [SerializeField] private GameObject Go_PrivateOrderContent;
        [SerializeField] private CButton Btn_PrivateOrder;
        [SerializeField] private GameObject Go_Private_Open;
        [SerializeField] private GameObject Go_Private_Close;
        [SerializeField] private CButton Btn_PrivatePublish;
        
        private int MinPrice = 10;
        private const int MaxPrice = 9999;
        private Dictionary<int, Toggle> priceToggleDic = new Dictionary<int, Toggle>();
        private Dictionary<int, Toggle> gemToggleDic;

        private KeyBoardInfo priceKeyBoardInfo;

        private SkinEditData skinEditData;
        private bool _isPrivate = false;


        public override void Awake()
        {
            base.Awake();
            Btn_PrivatePublish.onClick.AddListener(OnNextBtnClick);
            editCoverBtn.gameObject.SetActive(false);
            priceEditBtn.onClick.AddListener(OnPriceEditBtnClick);
            Btn_PrivateOrder.onClick.AddListener(OnBtnPrivateOrderClick);
        }
        
        public override void Show()
        {
            base.Show();
            priceToggles[0].isOn = true;
        }

        private void InitPriceToggle()
        {
            Go_Private_Open.SetActive(_isPrivate);
            Go_Private_Close.SetActive(!_isPrivate);
            // priceToggle20.onValueChanged.RemoveAllListeners();
            // priceToggle20.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(20);
            //     }
            // });
            //
            // priceToggle25.onValueChanged.RemoveAllListeners();
            // priceToggle25.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(25);
            //     }
            // });
            //
            // priceToggle30.onValueChanged.RemoveAllListeners();
            // priceToggle30.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(30);
            //     }
            // });
            //
            // priceToggle35.onValueChanged.RemoveAllListeners();
            // priceToggle35.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(35);
            //     }
            // });
            //
            // priceToggle40.onValueChanged.RemoveAllListeners();
            // priceToggle40.onValueChanged.AddListener((isOn) =>
            // {
            //     if (isOn)
            //     {
            //         OnPriceToggleClick(40);
            //     }
            // });

            #region Gem
            gem20.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(20);
                }
            });
            gem30.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(30);
                }
            });
            gem50.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(50);
                }
            });
            gem55.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(55);
                }
            });
            gem75.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    OnPriceToggleClick(75);
                }
            });
            gemToggleDic = new Dictionary<int, Toggle>()
            {
                { 20, gem20 },
                { 30, gem30 },
                { 50, gem50 },
                { 55, gem55 },
                { 75, gem75 }
            };
            #endregion


            // var normalPriceConfig = new Dictionary<int, Toggle>()
            // {
            //     { 20, priceToggle20 },
            //     { 25, priceToggle25 },
            //     { 30, priceToggle30 },
            //     { 35, priceToggle35 },
            //     { 40, priceToggle40 }
            // };
            //
            //
            // priceToggleDic = normalPriceConfig;
            
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
            Btn_PrivatePublish.gameObject.SetActive(_isPrivate);
            nextBtn.gameObject.SetActive(!_isPrivate);
        }

        protected override void SetUgcShaderStyle(UgcShaderStyle ugcStyle)
        {
            base.SetUgcShaderStyle(ugcStyle);
            var priceList = DataTables.GetSkinPublishPriceList();
            SkinPublishPrice priceData = priceList[0];
            if (skinEditData != null)
            {
                var skinType = skinEditData.GetSkinInfo().skinType;
                var subType = skinEditData.GetSkinInfo().subType;

                var tempData = priceList.Find(x => x.skinType == skinType && x.subType == subType);
                if (tempData != null)
                {
                    priceData = tempData;
                }
            }
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
                priceText.text = price.ToString();
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


        private void OnBtnPrivateOrderClick()
        {
            _isPrivate = !_isPrivate;
            skinEditData.GetSkinInfo().isPrivateOrder = _isPrivate ? 1 : 0;
            OnPriceToggleClick(MinPrice);

            InitPriceToggle();
            InitPaymentInfo();
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

            CButton curPublishBtn = _isPrivate ? Btn_PrivatePublish : nextBtn;

            if (_isPrivate)
            {
                var costNum = 10;
                var currentGem = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
                if (currentGem < costNum)
                {
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, costNum - currentGem);
                    return;
                }
            }

            ((LoadingButton)curPublishBtn).ShowLoading();

            skinEditData.draftInfo.Upload((info, isSuccess) => {
                if (gameObject == null) {
                    return;
                }
                if (isSuccess) {
                    skinEditData.draftInfo.PublishDraftToServer((newInfo, newSuccess) =>
                    {
                        if (newSuccess) {
                            base.OnNextBtnClick();

                            /// 申诉中不参加活动
                            bool isAppealing = newInfo?.auditInfo?.auditResult == (int)AuditResult.Appealing;
                            //私单不参加活动
                            if (!isAppealing && !_isPrivate)
                            {
                                ContestDataManager.Inst.JoinSkinContest(newInfo.templateId, newInfo.id, newInfo.skinType == 1,newInfo.ugcStyle);
                            }

                            if (_isPrivate)
                            {
                                AccountDataManager.Inst.BalanceInfo.Refresh();
                            }
                            if (_isPrivate)
                            {
                                AccountDataManager.Inst.BalanceInfo.Refresh();
                            }
                        } else {
                            ((LoadingButton)curPublishBtn).HideLoading();
                        }
                    });
                } else {
                    ((LoadingButton)curPublishBtn).HideLoading();
                    TipPanel.ShowToast($"{skinEditData.draftInfo.GetUploadMessage()}");
                }
            });
        }


        private bool CheckEmpty() {

            // 非素材类皮肤 不做空素材校验
            if (!skinEditData.GetSkinInfo().isProp) {
                return false;
            }

            if (skinEditData.metaDataBytes == null && !string.IsNullOrEmpty(editData.GetInfo().metaDataUrl)) {
                skinEditData.metaDataBytes = Asset.LoadRemoteAssetSync(editData.GetInfo().metaDataUrl);
            }
            if (skinEditData.metaDataBytes != null) {
                var itemPb = MapPbDataTool.ParsePropPb(skinEditData.metaDataBytes);
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
                string tips = LocalizationManager.Inst.GetLocalizedText("请输入一个介于{0}和{1}之间的整数。", MinPrice, MaxPrice);
                TipPanel.ShowToast(tips);
                return;
            }

            if (skinEditData.GetSkinInfo().paymentInfo == null)
            {
                skinEditData.GetSkinInfo().paymentInfo = new PaymentInfo()
                {
                    price = value,
                    currencyType = skinEditData.currencyType
                };
            }
            else
            {
                skinEditData.GetSkinInfo().paymentInfo.price = value;
                skinEditData.GetSkinInfo().paymentInfo.currencyType = skinEditData.currencyType;
            }

            SyncEditData();
        }

        protected override void SyncEditData()
        {
            skinEditData = editData as SkinEditData;

            base.SyncEditData();
            if (editData == null || editData.GetInfo() == null)
            {
                return;
            }

            ((LoadingButton)nextBtn).HideLoading();

            var curSkinType = skinEditData.GetSkinInfo().skinType;
            Go_PrivateOrderContent.SetActive(LobbyInfoManager.Inst.LobbyInfo.enablePrivateOrder == 1 && ((SkinType)curSkinType !=SkinType.Pet));
            InitPriceToggle();
            InitPaymentInfo();
            // SetCurrencyView(skinEditData.currencyType);
            // SetTipIconAndText(skinEditData.currencyType);
        }

        private void InitPaymentInfo()
        {
            var paymentInfo = skinEditData.GetSkinInfo().paymentInfo;
            if (paymentInfo == null || paymentInfo.price == 0)
            {
                // priceToggles[0].SetIsOnWithoutNotify(true);
                OnPriceToggleClick(MinPrice);
                priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                // selfPriceText.text = "每个商品你会获得等值的" + minValue / 2.0 + "个";
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",MinPrice / 2.0,"<q=icn_common_green_big>");
            }
            else
            {
                bool isCustom = false;
                Toggle findToggle = null;
                if (skinEditData.GetSkinInfo().paymentInfo.currencyType == CurrencyType.PinkCoin)
                {
                    isCustom = !priceToggleDic.TryGetValue(skinEditData.GetSkinInfo().paymentInfo.price, out var toggle);
                    findToggle = toggle;
                }
                if (isCustom)
                {
                    priceToggles[0].group.SetAllTogglesOff(false);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup/Label").GetComponent<CText>().text = paymentInfo.price.ToString();
                    priceEditBtn.transform.Find("HasPrizeGroup/Image").GetComponent<Image>().sprite = PgcUtils.LoadCurrencyIcon(skinEditData.currencyType, gameObject);
                }
                else
                {
                    findToggle?.SetIsOnWithoutNotify(true);
                    priceEditBtn.transform.Find("PriceCheckmark").gameObject.SetActive(false);
                    priceEditBtn.transform.Find("PriceNumEdit").gameObject.SetActive(true);
                    priceEditBtn.transform.Find("HasPrizeGroup").gameObject.SetActive(false);
                }
                // selfPriceText.text = skinEditData.currencyType == CurrencyType.Gem ? $"每个商品你会获得等值的{paymentInfo.price}个" : $"每个商品你会获得等值的{paymentInfo.price / 2.0}个";
                double price = paymentInfo.price / 2.0;
                selfPriceText.SetLocalText("每个商品你会获得等值的{0}个{1}创作者币",price,"<q=icn_common_green_big>");
            }
        }


        protected override void SyncCover() {

            if (skinEditData.GetSkinInfo().isProp) {
                var coverUrl = skinEditData.draftInfo.GetCoverUrl();
                cover.Load(coverUrl);
            } else {
                base.SyncCover();
            }


        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using EventTracking;
using GameData.Gashapon;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class LimitPackageItem : MonoBehaviour
{
    public GameObject Go_FreeTag;
    public CButton Btn_Buy;
    public CButton Btn_Search;
    public Image Img_BadgeIcon;
    public Image Img_Bg;
    public Text Txt_Title;
    public GameObject Go_TimeCount;
    public Text Txt_TimeCount;
    public Text Txt_Price;
    public GameObject Go_Discount;
    public Text Txt_Discount;
    public GameObject Go_Owned;
    public Text Txt_OwnedTips;
    public Text Txt_limitPurchase;

    private BaseLimitPackageData _curData;
    private string _curBudOrderId;
    private ProductInfo _curProductInfo = new ProductInfo();
    private IapTrackData _iapTrackData = new IapTrackData();
    private string spriteatlasPath = RechargePanel.RechargePanelAtlas;


    private void Awake()
    {
        Btn_Buy.onClick.AddListener(OnBuyBtnClick);
        Btn_Search?.onClick.AddListener(OnSearchBtnClick);
    }

    public void InitData(BaseLimitPackageData data)
    {
        this._curData = data;

        this.Txt_Title.text = data.name;
        this.Txt_TimeCount.text = data.refreshTime;

        if (data.isPurchase == 1)
        {
            Go_TimeCount.SetActive(false);
            Go_Owned.SetActive(true);
            switch ((FrequencyType)data.frequencyType)
            {
                case FrequencyType.DailyLimit:
                    Txt_OwnedTips.text = "明天再来";
                    break;
                case FrequencyType.WeeklyLimit:
                    Txt_OwnedTips.text = "下周再来";
                    break;
                case FrequencyType.MonthlyLimit:
                    Txt_OwnedTips.text = "下月再来";
                    break;
                case FrequencyType.SeasonLimit:
                    Txt_OwnedTips.text = "已购买";
                    break;
                case FrequencyType.DailyUnlockLimit:
                    Txt_OwnedTips.text = "明天再来";
                    break;
                case FrequencyType.OnceOnlyLimit:
                    Txt_OwnedTips.text = "已购买";
                    break;
            }
        }

        if (!string.IsNullOrEmpty(data.discount))
        {
            Txt_Discount.text = data.discount;
            Go_Discount.SetActive(true);
        }
        else
        {
            Go_Discount.SetActive(false);
        }

        if (data.limitPackageCurrencyType == 1)
        {
            this.Txt_Price.text = data.price.ToString();
        }
        else if (data.limitPackageCurrencyType == 2)
        {
            Img_BadgeIcon.gameObject.SetActive(true);
            this.Txt_Price.text = data.price.ToString();
        } else if (data.limitPackageCurrencyType == 3)
        {
            Img_BadgeIcon.gameObject.SetActive(true);
            this.Txt_Price.text = data.price.ToString();
            if (Txt_limitPurchase)
            {
                Txt_limitPurchase.text = "累计可购买 " + data.leftPurchaseTimes  + "/" + data.totalPurchaseTimes;
            }
        }
        
        Go_FreeTag.SetActive(data.price == 0);
    }

    public void SetTimeOutContent(BaseLimitPackageData data)
    {
        this.Txt_TimeCount.text = "剩余时间"+ data.refreshTime;
    }
    public BaseLimitPackageData GetBindData()
    {
        return _curData;
    }

    private void OnSearchBtnClick()
    {

        if (_curData == null || _curData.isPurchase == 1)
            return;

        if (_curData.rewardList?.Count > 0)
        {
            for(int i=0;i<_curData.rewardList.Count;i++)
            {
                if (_curData.rewardList[i].rewardType == (int)BUDRewardType.RewardHomepageSkin)
                {
                    UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, _curData.rewardList[i].homepageSkinType);
                    break;
                }
                else if (_curData.rewardList[i].rewardType == (int)BUDRewardType.RewardAvatarFrame)
                {
                    HeadCycleData headData = UserUIWidgetManager.Inst.GetHeadCycleDataById(_curData.rewardList[i].avatarFrameType);
                    if(headData != null)
                    {
                        PreviewManager.Inst.ShowAvatarFramePreview(headData.Id);
                        break;
                    }
     
                }
                else if (_curData.rewardList[i].rewardType == (int)BUDRewardType.RewardChatBubbles)
                {
                    GameChatBubbleData chatData = UserUIWidgetManager.Inst.GetChatDataByID(_curData.rewardList[i].chatBubblesType);
                    if(chatData != null)
                    {
                        var tem = new RewardPreviewInfo(BUDRewardType.RewardChatBubbles, CurrencyType.None, chatData.PgcId, chatData.Name, "");
                        tem.SetTitleAndDes(chatData.Name,chatData.Desc);
                        PreviewManager.Inst.ShowPreview(tem);
                        break;
                    }       
               
                }
            }
    
        }
    }
    public void OnBuyBtnClick()
    {
        if (_curData.isPurchase == 1)
        {
            return;
        }
        
        if (_curData.orderType == 1)
        {
            if (_curData.productId.Contains("budlovepack7"))
            {
                ValentinePackDetailPanel panel = UIManager.Inst.OpenPanel<ValentinePackDetailPanel>(PanelId.ValentinePackDetailPanel);
                panel.buyAction = () =>
                {
                    _curProductInfo.productId = _curData.productId;
                    _curProductInfo.price = _curData.price.ToString();
                    _curProductInfo.productName = _curData.name;
            
                    if (IAPDataManager.Inst.IsOfficialChannel())
                    {
                        ConfirmPaymentPanel panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, _curProductInfo.price);
                        panel.SetCallback(paymentType =>
                        {
                            PurchaseIAPOrder(paymentType);
                        });
                        return;
                    }
                    
                    PurchaseIAPOrder(ConfirmPaymentPanel.PaymentType.Default);
                };
                return;
            }
            _curProductInfo.productId = _curData.productId;
            _curProductInfo.price = _curData.price.ToString();
            _curProductInfo.productName = _curData.name;
            
            if (IAPDataManager.Inst.IsOfficialChannel())
            {
                ConfirmPaymentPanel panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, _curProductInfo.price);
                panel.SetCallback(paymentType =>
                {
                    PurchaseIAPOrder(paymentType);
                });
                return;
            }
            
            PurchaseIAPOrder(ConfirmPaymentPanel.PaymentType.Default);
        }
        else if (_curData.orderType == 4)
        {
            if (_curData.packageType == (int)LimitPackType.YouYouCoinPackage)
            {
                YouyouCoinPackPanel panel = UIManager.Inst.OpenPanel<YouyouCoinPackPanel>(PanelId.YouyouCoinPackPanel,_curData.leftPurchaseTimes, _curData);
                return;
            }
            PurchaseOrder(_curData.limitPackageCurrencyType);
        }
    }

    private void PurchaseIAPOrder(ConfirmPaymentPanel.PaymentType paymentType)
    {
        _iapTrackData.item = this._curData.name;
        _iapTrackData.price = this._curData.price.ToString();
        _iapTrackData.channel = "";
        
        ShowPurchaseLoading();
        IAPDataManager.Inst.GetProductOrderId(_curData.productId, null,(b, info) =>
        {
            _curBudOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(_curBudOrderId))
            {
                HidePurchaseLoading();
                return;
            }
            
            ChannelProductInfo channelProductInfo = _curProductInfo.toU8Info();
            channelProductInfo.extension = JsonConvert.SerializeObject(info);
            channelProductInfo.cpOrderId = _curBudOrderId;
            channelProductInfo.paymentType = (int)paymentType;
            channelProductInfo.productDesc = _curData.name;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow, JsonConvert.SerializeObject(channelProductInfo));
        });
    }
    
    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel = UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }
    
    private void StartBillingFlow(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        BillingResultResponse billingResultResponse = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (billingResultResponse.resultType == (int)BillingResultType.UserPaySuccess)
        {
            StartLooping();
            TrackEvent(billingResultResponse);
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }
    
    private void StartLooping()
    {
        if (string.IsNullOrEmpty(_curBudOrderId))
        {
            return;
        }

        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            panel.StartTimer(60, () =>
            {
                PurchaseStatusManager.Inst.StopLoop(); 
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }

        PurchaseStatusManager.Inst.StartLoop(_curBudOrderId, (orderResult, productGemInfo) =>
        {
            if (this == null)
            {
                return;
            }

            if (!orderResult)
            {
                if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
                {
                    //支付失败埋点
                    LoadEvent.ReportPopupStatus("Paymentfailed", "HotSalePayment");
                    
                }
                //新用户行为埋点
                if (SignInPanel.isNewPlayer)
                {
                    LoadEvent.ReportPopupStatus("fail", "pay_fail");
                }
                return;
            }

           
            MessageHelper.Broadcast(MessageName.BuyStartPack);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            if (productGemInfo != null && productGemInfo.limitPackage != null)
            {
                var luckAmount = productGemInfo.limitPackage.luckyCoinNum;
                ShowPackReward(luckAmount, 0);
            }
            else
            { 
                ShowPackReward();       
            }
            //UGC商城埋点
            if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
            {
                //支付成功埋点
                LoadEvent.ReportPopupStatus("PaymentSuccessful", "HotSalePayment");
            }
            //新用户行为埋点
            if (SignInPanel.isNewPlayer)
            {
                LoadEvent.ReportPopupStatus("Price_" + productGemInfo.price, "pay_success");
            }
        });
    }
    
    private void TrackEvent(BillingResultResponse billingResultResponse)
    {
        Dictionary<string, object> trackData = new Dictionary<string, object>();
        float result;
        if (float.TryParse(_iapTrackData.price, NumberStyles.Float, CultureInfo.InvariantCulture,out result))
        {
            trackData.Add("priceNum", result);
        }
        
        trackData.Add("item", _iapTrackData.item);
        bool isUploadData = !AnalyticsManager.Inst.ContainOrderID(_curBudOrderId);
        if (isUploadData)
        {
            trackData.Add("budOrderId", _curBudOrderId ?? "");
            trackData.Add("method", "LimitPackageItem");
            AnalyticsManager.Inst.Track(AnalyticsEventName.TOP_UP_SUCCESS, trackData);
        }
    }
    
    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    private void ShowPackReward(int randomLuckyAmount = 0, int randomYouYouAmount = 0)
    {
        if (_curData.productId.Contains("budlovepack7"))
        {
            ShowValentineReward();
            return;
        }
        var rewardItemDatas = new List<CommonRewardItemData>();
        foreach (var rewardData in _curData.rewardList)
        {
           
            var itemData = new CommonRewardItemData()
            {
                IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardData.rewardType, gameObject),
                RewardAmount = rewardData.amount,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardData.rewardType)
            };
            if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardYouYouCoin)
            {
                if (randomYouYouAmount != 0)
                {
                    itemData.RewardAmount = randomYouYouAmount;
                }
            }
            if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardLuckyCoin)
            {
                if (randomLuckyAmount != 0)
                {
                    itemData.RewardAmount = randomLuckyAmount;
                }
            }
            else if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardGem)
            {
                var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.RewardAtlas);
                var sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "ic_rewards_big_3", gameObject);
                itemData.IconSp = sprite;
            }  
            else if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardHomepageSkin)
            {
                var sprite = ProfileThemeManager.Inst.LoadThemeIcon( rewardData.homepageSkinType, gameObject);
                itemData.IconSp = sprite;
            }
            else if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardAvatarFrame)
            {
                var sprite =  UserUIWidgetManager.Inst.GetHeadCycleSp(rewardData.avatarFrameType, gameObject);
                itemData.IconSp = sprite;
            }
            else if ((BUDRewardType)rewardData.rewardType == BUDRewardType.RewardChatBubbles)
            {
                var sprite = UserUIWidgetManager.Inst.GetChatBubbleIconByType((ChatBubblesType)rewardData.chatBubblesType, this.gameObject);
                itemData.IconSp = sprite;
            }

            rewardItemDatas.Add(itemData);
        }
        
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
        
        ReddotManagerUtils.Inst.RefreshRedDot();
        BoardcastOnBuySuccess();
    }

    private void ShowValentineReward()
    {
        var rewardItemDatas = new List<CommonRewardItemData>();
    
        for (int i = 0; i < 4; i++)
        {
            int amount = 0;
            Sprite IconSp = null;
            if (i == 0)
            {
                amount = 30;
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "spring_purple_coin", gameObject);
            }
            else
            {
                amount = 0;
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "valentine_reward" + (i + 1),
                    gameObject);
            }

            Sprite sprite = null;
           
            var itemData = new CommonRewardItemData()
            {
                IconSp = IconSp,
                RewardAmount = amount,
                rewardName = ""
            };
            rewardItemDatas.Add(itemData);
        }
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardItemDatas);
        
        ReddotManagerUtils.Inst.RefreshRedDot();
        BoardcastOnBuySuccess();
    }
    
    private void PurchaseOrder(int limitPackageCurrencyType)
    {
        switch (limitPackageCurrencyType)
        {
            case 2:
            {
                int needNum = _curData.price - AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Badge);
                if (needNum > 0)
                {
                    ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
                    badgePanel.SetData(CurrencyType.Badge, CurrencyType.Gem, needNum);
                    return;
                }

                break;
            }
            case 3:
            {
                int needNum = _curData.price - AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
                if (needNum > 0)
                {
                    UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, needNum);
                    return;
                }

                break;
            }
        }
        
        JObject req = new JObject()
        {
            ["productType"] = 7,
            ["productId"] = _curData.productId,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (response) =>
        {
            BuyProductResult rsp = JsonConvert.DeserializeObject<BuyProductResult>(response);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            if (rsp != null && rsp.rewardList != null && rsp.rewardList.Count > 0)
            {
                ShowPackReward(0,rsp.rewardList[0].amount);
            }
            else
            {
                ShowPackReward();
            }
        }, (_) =>
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
        });
    }
    

    
    #region 支付相关流程

    private void BoardcastOnBuySuccess()
    {
        MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);   
    }
    #endregion
}

using System.Collections.Generic;
using System.Globalization;
using Message;
using Newtonsoft.Json;
using Sirenix.Utilities;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;
using EventTracking;
using UI.UIPanels.RechargePanel;

public class VipMonthPack : MonoBehaviour
{
    public Text savedTxt;
    public Text youyouCoinNum;
    public Text expNum;
    public Button receiveBtn;
    public Button buyBtn;
    public GameObject confirmObj;
    public Button confirmBuyBtn;
    public Text expireTxt;
    public Text renewalTxt;
    public Button continueBtn;
    public GameObject receiveMaskObj;
    public GameObject buyMaskObj;
    public Button confirmBgObjBtn;

    private SubscribeStatusResponse _subscribeStatusResponse;
    private SubscribeVipType currentSubscribeVipType;
    private IapTrackData _iapTrackData = new IapTrackData();
    private string budOrderId = "";
    private int costNum = 60;
    private int youyouRewardNum = 1;
    private int expRewardNum = 10;
    private BaseLimitPackageData weeklyLimitPackageData;
    

    private void Start()
    {   
        continueBtn.onClick.AddListener(() => { ContinueVip(SubscribeVipType.MonthCard); });
        buyBtn.onClick.AddListener(() => { confirmObj.gameObject.SetActive(true); });

        confirmBuyBtn.onClick.AddListener(() =>
        {
            confirmObj.gameObject.SetActive(false);
            PurchaseByGem();
        });

        receiveBtn.onClick.AddListener(() =>
        {
            GetSubcribeReward();
        });

        confirmBgObjBtn.onClick.AddListener(() =>
        {
            confirmObj.gameObject.SetActive(false);
        });
        
        RefreshVipStatus();

        //埋点
        EventTracking.LoadEvent.ReportPopupStatus(RechargeId.VipMonthPack.ToString());
        


    }

    private void GetSubcribeReward()
    {
        IAPDataManager.Inst.GetSubScribeReward(isSuccess =>
        {
            if (isSuccess)
            {
                
                string spriteatlasPath = string.Format(PackCenterPanel.ViewBasePath + "{0}/{1}.spriteatlas","VipMonthPack", "VipMonthPack");
        
                Sprite youyouSp =
                    XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_month_youyou", gameObject);
                
                Sprite expSp =
                    XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_month_exp", gameObject);
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new()
                    {
                        IconSp = youyouSp,
                        RewardAmount = youyouRewardNum,
                        rewardName = "优优币"
                    }
                    //,
                    //new()
                    //{
                    //    IconSp = expSp,
                    //    RewardAmount = expRewardNum,
                    //    rewardName = "经验值"
                    //}
                });
        
        
                AccountDataManager.Inst.BalanceInfo.Refresh();
                Refresh();
            }
        });
    }

    private void PurchaseByGem()
    {
        var currentGem = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        if (currentGem < costNum)
        {
            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, costNum - currentGem);
            return;
        }
        
        IAPDataManager.Inst.PayByGem(BUDProductType.VipMonthPackage, weeklyLimitPackageData.productId, resultHandler:
            result =>
            {
                if (this != null)
                {
                }
        
                if (result)
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    Refresh();
                    string spriteatlasPath = string.Format(PackCenterPanel.ViewBasePath + "{0}/{1}.spriteatlas","VipMonthPack", "VipMonthPack");
        
                    Sprite youyouSp =
                        XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_month_youyou", gameObject);
                
                    Sprite expSp =
                        XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_month_exp", gameObject);
                    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    panel.ShowRewards(new List<CommonRewardItemData>()
                    {
                        new()
                        {
                            IconSp = youyouSp,
                            RewardAmount = 15,
                            rewardName = "优优币"
                        }
                    });
                }
            });
    }
    
    private void Refresh()
    {
        RefreshVipStatus();
        
        ReddotManagerUtils.Inst.RefreshRedDot();
    }

    private void RefreshVipStatus()
    {
        IAPDataManager.Inst.GetSubscribeStatus((b, subscribeStatusResponse) =>
        {
            if (subscribeStatusResponse == null)
            {
                return;
            }

            this._subscribeStatusResponse = subscribeStatusResponse;
            this.weeklyLimitPackageData = subscribeStatusResponse.weeklyLimitPackage;
            SetupUI();
        });
    }
    

    private void SetupUI()
    {
        if (_subscribeStatusResponse == null)
        {
            return;
        }

        var isVip = _subscribeStatusResponse.vipType is (int)SubscribeVipType.MonthCard
            or (int)SubscribeVipType.YearCard;
        renewalTxt.SetLocalText(isVip ? "续订" : "开通");

        if (isVip)
        {
            expireTxt.gameObject.SetActive(true);
            receiveBtn.gameObject.SetActive(_subscribeStatusResponse.dailyRewardStatus != 1);
            var storeDays = _subscribeStatusResponse.storageDays;
            savedTxt.text = storeDays > 1 &&  _subscribeStatusResponse.dailyRewardStatus != 1
                ? $"最多可存7天奖励（已存{storeDays}天奖励)" 
                : "最多可存7天奖励";
            savedTxt.gameObject.SetActive(true);
            receiveMaskObj.SetActive(_subscribeStatusResponse.dailyRewardStatus == 1);
            buyBtn.gameObject.SetActive(true);
            
            var isPurchased = weeklyLimitPackageData.isPurchase == 1;
            if (isPurchased)
            {
                buyBtn.gameObject.SetActive(false);
                buyMaskObj.gameObject.SetActive(true);
            }
            else
            {
                buyBtn.gameObject.SetActive(true);
                buyMaskObj.gameObject.SetActive(false);
            }
        }
        else
        {
            expireTxt.gameObject.SetActive(false);
            receiveBtn.gameObject.SetActive(false);
            receiveMaskObj.gameObject.SetActive(false);
            buyBtn.gameObject.SetActive(false);
        }

        // var storeDays = _subscribeStatusResponse.storageDays;
        // savedTxt.text = $"最多可存7天奖励（已存{storeDays}天奖励)";

        var vipDailyRewards = _subscribeStatusResponse.vipDailyReward;
        if (!vipDailyRewards.IsNullOrEmpty())
        {
            foreach (var dailyReward in vipDailyRewards)
            {
                switch (dailyReward.rewardType)
                {
                    case 40:
                    {
                        var amount = dailyReward.amount;
                        youyouCoinNum.text = "x" + amount;
                        youyouRewardNum = amount;
                        break;
                    }
                    case 38:
                    {
                        var amount = dailyReward.amount;
                        expNum.text = "x" + amount;
                        expRewardNum = amount;
                        break;
                    }
                }
            }
        }

        expireTxt.text = "VIP会员有效倒计时：" + _subscribeStatusResponse.remainTime;



    }

    private void ContinueVip(SubscribeVipType subscribeVipType)
    {
        if (_subscribeStatusResponse == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = IAPDataManager.GetVipProductInfo(subscribeVipType, _subscribeStatusResponse);
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { BuyVip(subscribeVipType, paymentType); });
            return;
        }

        BuyVip(subscribeVipType, ConfirmPaymentPanel.PaymentType.Default);
    }

    private void BuyVip(SubscribeVipType subscribeVipType, ConfirmPaymentPanel.PaymentType paymentType)
    {
        this.currentSubscribeVipType = subscribeVipType;
        ProductInfo productInfo = IAPDataManager.GetVipProductInfo(subscribeVipType, _subscribeStatusResponse);
        if (productInfo == null)
        {
            return;
        }

        ShowPurchaseLoading();
        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        var productId = channelProductInfo.productId;
        if (string.IsNullOrEmpty(productId))
        {
            return;
        }

        _iapTrackData.price = channelProductInfo.price;
        _iapTrackData.item = subscribeVipType == SubscribeVipType.MonthCard ? "VIP" : "SVIP";
        _iapTrackData.channel = "";

        IAPDataManager.Inst.GetProductOrderId(productId, null, (b, info) =>
        {
            budOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(budOrderId))
            {
                HidePurchaseLoading();
                return;
            }

            channelProductInfo.extension = JsonConvert.SerializeObject(info);
            channelProductInfo.paymentType = (int)paymentType;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
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

    private void TrackEvent(BillingResultResponse billingResultResponse)
    {
        Dictionary<string, object> trackData = new Dictionary<string, object>();
        float result;
        if (float.TryParse(_iapTrackData.price, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            trackData.Add("priceNum", result);
        }

        trackData.Add("item", _iapTrackData.item);
        bool isUploadData = !AnalyticsManager.Inst.ContainOrderID(budOrderId);
        if (isUploadData)
        {
            trackData.Add("budOrderId", budOrderId ?? "");
            trackData.Add("method", "VipMonth");
            AnalyticsManager.Inst.Track(AnalyticsEventName.TOP_UP_SUCCESS, trackData);
        }
    }

    private void StartLooping()
    {
        if (string.IsNullOrEmpty(budOrderId))
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

        PurchaseStatusManager.Inst.StartLoop(budOrderId, (orderResult, productGemInfo) =>
        {
            if (this == null)
            {
                return;
            }

            if (!orderResult)
            {
                return;
            }

            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            ShowVipRewards();
            Refresh();
            VipDataManager.Inst.UpdateVipStatus();
            MessageHelper.Broadcast(MessageName.BuyVipSuccess);
        });
    }

    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    private void ShowVipRewards()
    {
        var spriteatlasPath = RechargePanel.RechargePanelAtlas;
        SubscribeStatusReward subscribeStatusReward = _subscribeStatusResponse.rewards;
        if (currentSubscribeVipType == SubscribeVipType.MonthCard)
        {
            if (subscribeStatusReward != null)
            {
                Message.MessageHelper.Broadcast(Message.MessageName.AvaterDatabaseCheck);
                Sprite monthSp =
                    XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vip_reward_vip", gameObject);
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                panel.ShowRewards(new List<CommonRewardItemData>()
                {
                    new CommonRewardItemData()
                    {
                        IconSp = monthSp,
                        RewardAmount = 1,
                        rewardName = "VIP月卡"
                    }
                });

                List<int> monthReward = subscribeStatusReward.month;
                if (monthReward != null && monthReward.Count > 0)
                {
                    Sprite reward1 =
                        XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "vipland_gem", gameObject);
                    var rewardpanel1 = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                    rewardpanel1.ShowRewards(new List<CommonRewardItemData>()
                    {
                        new CommonRewardItemData()
                        {
                            IconSp = reward1,
                            RewardAmount = 210,
                            rewardName = "BUD钻"
                        },
                    });
                }
            }
        }
    }

    private void OnDestroy()
    {
    }
}
using Basic.Utils;
using EventTracking;
using Game.Event;
using GameData.Manager;
using GameUI;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using Game.Store;


public class AnniversaryMonthCardMgr : IActivity
{
    public enum MonthCardType
    {
        Silver,
        Gold
    }
    private static AnniversaryMonthCardMgr _instance;
    public static AnniversaryMonthCardMgr Inst
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AnniversaryMonthCardMgr();
                ActivityManager.Inst.AddActivity(ActivityId.AnniversaryCelebrationMonth, _instance);
                RedDotSystemNew.Inst.AddReddotType(ReddotType.AnniversaryCelebrationMonth, _instance.IsEntryRedDot);
            }
            return _instance;
        }
    }



    public AnniversaryMonthCardView view;

    public static readonly DateTime ACTIVITY_START_TIME = new DateTime(2025, 7, 1, 11, 0, 0); //活动开始时间 todo 需要修改为8/1
    public static readonly DateTime ACTIVITY_END_TIME = new DateTime(2025, 8, 31, 23, 59, 59);

    public SubscribeStatusResponse subscribeStatusRsp;
    //(day,index)
    public Dictionary<MonthCardType, RewardPreviewInfo[]> RewardTypeList;
    public Dictionary<PaidPackageType, PaidPackageListItem> PaidPackage2ItemDict = new();
    public Dictionary<TASK_ID, TaskInfoData> TaskId2InfoDict = new();

    private string budOrderId = "";
    private IapTrackData _iapTrackData = new IapTrackData();
    private SubscribeVipType currentSubscribeVipType;
    private MonthCardType curBuyMonthCardType;

    AnniversaryMonthCardMgr()
    {
        RewardTypeList ??= new();
        RewardTypeList.Clear();
        RewardTypeList.Add(MonthCardType.Silver, new RewardPreviewInfo[2] { new RewardPreviewInfo(BUDRewardType.RewardPinkCoin, CurrencyType.PinkCoin, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", "") });
        RewardTypeList.Add(MonthCardType.Gold, new RewardPreviewInfo[3] { new RewardPreviewInfo(BUDRewardType.RewardPinkCoin, CurrencyType.PinkCoin, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardBadge, CurrencyType.Badge, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", "") });

        AssetsDataManager.IsMonthCardActive = IsAnyMonthCardActive;
        AssetsDataManager.GetDiscountRate = GetDiscountRate;
    }

    /// <summary>
    /// 购买或续费月卡 展示奖励及更新奖励
    /// </summary>
    /// <param name="monthCardType"></param>
    public void ShowBuyMonthCardReward(MonthCardType monthCardType)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        if (monthCardType == MonthCardType.Silver)
        {
            CommonRewardItemData item;
            item = new CommonRewardItemData()
            {
                RewardAmount = 300,
                rewardType = (int)BUDRewardType.RewardPinkCoin,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardPinkCoin),
            };
            items.Add(item);
        }
        else
        {
            CommonRewardItemData item;
            item = new CommonRewardItemData()
            {
                RewardAmount = 600,
                rewardType = (int)BUDRewardType.RewardPinkCoin,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardPinkCoin),
            };
            items.Add(item);

            item = new CommonRewardItemData()
            {
                RewardAmount = 30000,
                rewardType = (int)BUDRewardType.RewardCoin,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardCoin),
            };
            items.Add(item);
        }
        panel.ShowRewards(items);
        panel.ShowCommonBtn("确定", null);
        TokenDataManager.Inst.GetTokenData();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }


    /// <summary>
    /// 领取月卡奖励
    /// </summary>
    /// <param name="subscribeRewardReqType">领取类型</param>
    /// <param name="resultAction">结果回调</param>
    public void ClaimMonthCardReward(SubscribeRewardReqType subscribeRewardReqType, Action<bool> resultAction = null)
    {
        SubscribeRewardReq subscribeRewardReq = new SubscribeRewardReq()
        {
            type = (int)subscribeRewardReqType
        };
        Debug.Log("AnniversaryMonthCardMgr GetMonthCardReward: " + JsonConvert.SerializeObject(subscribeRewardReq));
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SubScribeReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(subscribeRewardReq),
            onReceive: arg0 =>
            {
                resultAction?.Invoke(true);
                ShowRewards(subscribeRewardReqType);
                TokenDataManager.Inst.GetTokenData();
                AccountDataManager.Inst.BalanceInfo.Refresh();
                GetMonthCardInfo();
                LoggerUtils.Log("AnniversaryMonthCardMgr GetMonthCardReward success: " + JsonConvert.SerializeObject(subscribeRewardReq));
            }, onFail: arg0 =>
            {
                resultAction?.Invoke(false);
            }, retryCount: 3);
    }

    private void ShowRewards(SubscribeRewardReqType subscribeRewardReqType)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();

        if (subscribeRewardReqType == SubscribeRewardReqType.silverMonthCardReward)
        {
            var days = int.Parse(subscribeStatusRsp?.monthlyCard?.basicCard?.accumulationDays ?? "0");
            items.Add(new CommonRewardItemData()
            {
                RewardAmount = 10 * days,
                rewardType = (int)BUDRewardType.RewardPinkCoin,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardPinkCoin),
            });
            items.Add(new CommonRewardItemData()
            {
                RewardAmount = 1000 * days,
                rewardType = (int)BUDRewardType.RewardCoin,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardCoin),
            });
        }
        else if (subscribeRewardReqType == SubscribeRewardReqType.goldMonthCardReward)
        {
            var days = int.Parse(subscribeStatusRsp?.monthlyCard?.premiumCard?.accumulationDays ?? "0");
            items.Add(new CommonRewardItemData()
            {
                RewardAmount = 20 * days,
                rewardType = (int)BUDRewardType.RewardPinkCoin,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardPinkCoin),
            });
            items.Add(new CommonRewardItemData()
            {
                RewardAmount = 20 * days,
                rewardType = (int)BUDRewardType.RewardBadge,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardBadge),
            });
            items.Add(new CommonRewardItemData()
            {
                RewardAmount = 1000 * days,
                rewardType = (int)BUDRewardType.RewardCoin,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardCoin),
            });
        }
        panel.ShowRewards(items);
        panel.ShowCommonBtn("确定", null);
    }

    /// <summary>
    /// 拉月卡信息
    /// </summary> <summary>
    /// 
    /// </summary>
    public void GetMonthCardInfo(Action action = null )
    {
        Debug.Log("AnniversaryMonthCardMgr GetMonthCardInfo");
        IAPDataManager.Inst.GetSubscribeStatus((result, res) =>
        {
            Debug.Log("AnniversaryMonthCardMgr GetMonthCardInfo success: " + JsonConvert.SerializeObject(res));
            subscribeStatusRsp = res;
            action?.Invoke();
            if (this.view != null)
            {
                this.view.RefreshUI();
            }
        });
    }

    /// <summary>
    /// 月卡银卡是否激活
    /// </summary>
    /// <returns></returns>
    public bool IsMonthCardActive(MonthCardType monthCardType)
    {
        if (monthCardType == MonthCardType.Silver)
        {
            var remainTime = subscribeStatusRsp?.monthlyCard?.basicCard?.remainTime;
            return !string.IsNullOrEmpty(remainTime);
        }
        else
        {
            var remainTime = subscribeStatusRsp?.monthlyCard?.premiumCard?.remainTime;
            return !string.IsNullOrEmpty(remainTime);
        }
    }

    public bool IsAnyMonthCardActive()
    {
        return IsMonthCardActive(MonthCardType.Silver) || IsMonthCardActive(MonthCardType.Gold);
    }
    public bool IsAnyMonthGoldCardActive()
    {
        return IsMonthCardActive(MonthCardType.Gold);
    }
    public bool IsAnyMonthSilverCardActive()
    {
        return IsMonthCardActive(MonthCardType.Silver);
    }

    public float GetDiscountRate()
    {
        float discountRate = 1f;
        if (IsMonthCardActive(MonthCardType.Gold))
        {
            discountRate = (float)subscribeStatusRsp.monthlyCard.premiumCard.marketDiscount / 100f;
        }
        else if (IsMonthCardActive(MonthCardType.Silver))
        {
            discountRate = (float)subscribeStatusRsp.monthlyCard.basicCard.marketDiscount / 100f;
        }
        return Mathf.Round(discountRate * 100f) / 100f;
    }



    public bool CheckHasReward(MonthCardType monthCardType)
    {
        if (subscribeStatusRsp == null)
        {
            return false;
        }
        if (monthCardType == MonthCardType.Silver)
        {

            string days = subscribeStatusRsp?.monthlyCard?.basicCard?.accumulationDays;
            return int.Parse(days) > 0;
        }
        else
        {
            string days = subscribeStatusRsp?.monthlyCard?.premiumCard?.accumulationDays;
            return int.Parse(days) > 0;
        }
    }


    public bool IsDuringActivity()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        return now >= ACTIVITY_START_TIME && now <= ACTIVITY_END_TIME;
    }

    private Action buyCallback;

    #region 购买月卡
    public void ContinueVip(SubscribeVipType subscribeVipType, MonthCardType monthCardType, Action callback = null)
    {
        if (subscribeStatusRsp == null)
        {
            return;
        }

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = null;
            if (monthCardType == MonthCardType.Silver)
            {
                productInfo = subscribeStatusRsp.monthlyCard.basicCard.productInfo;
            }
            else
            {
                productInfo = subscribeStatusRsp.monthlyCard.premiumCard.productInfo;
            }
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { BuyVip(subscribeVipType, paymentType, monthCardType,callback); });
            return;
        }

        BuyVip(subscribeVipType, ConfirmPaymentPanel.PaymentType.Default, monthCardType,callback);
    }
    private void BuyVip(SubscribeVipType subscribeVipType, ConfirmPaymentPanel.PaymentType paymentType, MonthCardType monthCardType , Action callback = null)
    {
        this.currentSubscribeVipType = subscribeVipType;
        this.curBuyMonthCardType = monthCardType;
        buyCallback = callback;
        ProductInfo productInfo = null;
        if (monthCardType == MonthCardType.Silver)
        {
            productInfo = subscribeStatusRsp.monthlyCard.basicCard.productInfo;
        }
        else
        {
            productInfo = subscribeStatusRsp.monthlyCard.premiumCard.productInfo;
        }

        ShowPurchaseLoading();
        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        var productId = channelProductInfo.productId;

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
            channelProductInfo.cpOrderId = budOrderId;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
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
            // TrackEvent(billingResultResponse);
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
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
            TokenDataManager.Inst.GetTokenData();
            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            ShowBuyMonthCardReward(curBuyMonthCardType);
            GetMonthCardInfo(buyCallback);
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    #endregion

    public bool IsEntryOpen()
    {
        if (!IsDuringActivity())
        {
            return false;
        }
        return true;
    }

    public bool IsEntryRedDot(string param)
    {
        if (IsEntryOpen())
        {
            // return false;
        }

        return CheckHasReward(MonthCardType.Silver) || CheckHasReward(MonthCardType.Gold);
    }


    public bool IsAllRewardClaimed()
    {
        return true;
    }

    public bool IsDuringPackEndDate()
    {
        return false;
    }

    public bool IsOpen()
    {
        return IsDuringActivity();
    }

    public void ShowPanel()
    {

    }

    public List<ReddotType> GetReddotTypes()
    {
        return new List<ReddotType>() { ReddotType.AnniversaryCelebrationMonth };
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }
}

using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;
using System;
using Game.Event;
using UI.Manager;
using Message;
using GameData.Manager;


public class AnniversaryLimitPackMgr
{
    public enum MonthCardType
    {
        Silver,
        Gold
    }
    private static AnniversaryLimitPackMgr _instance;
    public static AnniversaryLimitPackMgr Inst
    {
        get
        {
            return _instance ?? (_instance = new AnniversaryLimitPackMgr());
        }
    }



    public AnniversaryLimitPackView view;

    public static readonly DateTime DISCOUNT_START_TIME = new DateTime(2025, 7, 31, 0, 0, 0); //活动打折开始时间
    public static readonly DateTime DISCOUNT_END_TIME = new DateTime(2025, 8, 7, 23, 59, 59);

    private string productId_discount_android = "android_limitertimepac6";
    private string productId_normal_android = "android_limitertimepac18";
    private string productId_discount_ios = "ios_limitertimepac6";
    private string productId_normal_ios = "ios_limitertimepac18";

    private List<PopupPackageList> popupPackageList;

    private string budOrderId = "";


    AnniversaryLimitPackMgr()
    {
    }

    public string GetProductId()
    {
        if (IsDuringDiscount())
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return productId_discount_android;
            }
            return productId_discount_ios;
        }
        if (Application.platform == RuntimePlatform.Android)
        {
            return productId_normal_android;
        }
        return productId_normal_ios;
    }

    public string[] GetProductIds()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            return new string[] { productId_discount_android, productId_normal_android };
        }
        return new string[] { productId_discount_ios, productId_normal_ios };
    }

    public bool IsDuringDiscount()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        return now >= DISCOUNT_START_TIME && now <= DISCOUNT_END_TIME;
    }
    #region
    public void BuyPackage()
    {
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ProductInfo productInfo = GetProductInfo();
            if (productInfo == null)
            {
                return;
            }
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => { _BuyPack(paymentType); });
            return;
        }

        _BuyPack(ConfirmPaymentPanel.PaymentType.Default);
    }
    void _BuyPack(ConfirmPaymentPanel.PaymentType paymentType)
    {
        ProductInfo productInfo = GetProductInfo();
        if (productInfo == null)
        {
            return;
        }

        ShowPurchaseLoading();
        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        var productId = channelProductInfo.productId;


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
            TrackAnalyticsData_Buy();
            HidePurchaseLoading();
            ShowBuyReward();
            GetActivityInfo();
        });
    }

    public void ShowBuyReward()
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        {
            //             俏皮双马尾	 10800119	
            // 卡哇伊眼睛	 10600127
            // 可爱牛仔裙         10400493
            // 小恶魔发卡	12100142
            CommonRewardItemData item;
            item = new CommonRewardItemData()
            {
                RewardAmount = 1,
                rewardType = (int)BUDRewardType.RewardPgcResource,
                rewardName = "俏皮双马尾",
                pgcId = "10800119",
            };
            items.Add(item);

            item = new CommonRewardItemData()
            {
                RewardAmount = 1,
                rewardType = (int)BUDRewardType.RewardPgcResource,
                rewardName = "卡哇伊眼睛",
                pgcId = "10600127",
            };
            items.Add(item);

            item = new CommonRewardItemData()
            {
                RewardAmount = 1,
                rewardType = (int)BUDRewardType.RewardPgcResource,
                rewardName = "可爱牛仔裙",
                pgcId = "10400493",
            };
            items.Add(item);

            item = new CommonRewardItemData()
            {
                RewardAmount = 1,
                rewardType = (int)BUDRewardType.RewardPgcResource,
                rewardName = "小恶魔发卡",
                pgcId = "12100142",
            };
            items.Add(item);
        }
        panel.ShowRewards(items);
        panel.ShowCommonBtn("确定", null);
        TokenDataManager.Inst.GetTokenData();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    #endregion

    ProductInfo GetProductInfo()
    {
        // if (Application.platform == RuntimePlatform.Android)
        // {
        //     ProductInfo productInfo = new()
        //     {
        //         productId = "android_limitertimepac18",
        //         productName = "周年庆限时秒杀礼包",
        //         productDesc = "周年庆限时秒杀礼包",
        //         price = "18",
        //         type = 0
        //     };
        //     return productInfo;
        // }
        // else
        // {
        //     ProductInfo productInfo = new()
        //     {
        //         productId = "ios_limitertimepac18",
        //         productName = "周年庆限时秒杀礼包",
        //         productDesc = "周年庆限时秒杀礼包",
        //         price = "18",
        //         type = 0
        //     };
        //     return productInfo;
        // }

        var productId = GetProductId();
        var popupPackage = popupPackageList.FirstOrDefault(x => x.productInfo.productId == productId);
        if (popupPackage == null)
        {
            GetActivityInfo();
            return null;
        }
        return popupPackage.productInfo;
    }

    public void GetActivityInfo()
    {
        Debug.Log(" AnniversaryLimitPackMgr GetActivityInfo");
        IAPDataManager.Inst.GetProductInfo(response =>
        {
            if (response != null)
            {
                popupPackageList = response.popupPackageList;
                Debug.Log(" AnniversaryLimitPackMgr GetActivityInfo popupPackageList=" + JsonConvert.SerializeObject(popupPackageList));
                MessageHelper.Broadcast(MessageName.UpdateAnniversaryLimitPack);
            }
        });
    }

    public bool CheckHasBuy()
    {
        if (popupPackageList == null || popupPackageList.Count == 0)
        {
            return false;
        }
        var productIds = GetProductIds();
        foreach (var productId in productIds)
        {
            foreach (var popupPackage in popupPackageList)
            {
                if (popupPackage.productInfo.productId == productId)
                {
                    if (popupPackage.isPaid == 1)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public bool IsEntryOpen()
    {
        if (popupPackageList == null || popupPackageList.Count == 0)
        {
            return false;
        }
        var productIds = GetProductIds();
        foreach (var productId in productIds)
        {
            foreach (var popupPackage in popupPackageList)
            {
                if (popupPackage.productInfo.productId == productId)
                {
                    if (popupPackage.isPaid == 1)
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }

    public void OpenLimitPackWin()
    {
        UIManager.Inst.OpenPanel<AnniversaryLimitPackView>(PanelId.AnniversaryLimitPackView);
    }

    public void TrackAnalyticsData_Click()
    {
        AnalyticsManager.Inst.Track(AnalyticsEventName.LimitedTimePackage_Click);

    }

    public void TrackAnalyticsData_Show()
    {
        AnalyticsManager.Inst.Track(AnalyticsEventName.LimitedTimePackage_Show);
    }

    public void TrackAnalyticsData_Buy()
    {
        AnalyticsManager.Inst.Track(AnalyticsEventName.LimitedTimePackage_Buy);
    }
}

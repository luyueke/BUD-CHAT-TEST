using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Message;
using System;
using System.Linq;
using GameData.Manager;
using Game.Database;

public class HalloweenLimitPackMgr
{
    private string budOrderId = "";

    public static string ViewBasePath = "Assets/Loadable/UI/UIPanel/HalloweenLimitPackView/Rules/HalloweenRule.json";

    public HalloweenLimitPackView View;
    
    public static readonly DateTime DISCOUNT_START_TIME = new DateTime(2025, 10, 10, 0, 0, 0); //活动打折开始时间
    public static readonly DateTime DISCOUNT_END_TIME = new DateTime(2025, 12, 4, 23, 59, 59);
    
    private string productId_android = "android_helloween12pack";
    private string productId_ios = "ios_helloween12pack";
    
    private static HalloweenLimitPackMgr _instance;
    public static HalloweenLimitPackMgr Inst
    {
        get
        {
            return _instance ?? (_instance = new HalloweenLimitPackMgr());
        }
    }
    
    private List<PopupPackageList> popupPackageList;
    
    public void GetActivityInfo()
    {
        Debug.Log(" HalloweenLimitPackMgr GetActivityInfo");
        IAPDataManager.Inst.GetProductInfo(response =>
        {
            if (response != null)
            {
                popupPackageList = response.popupPackageList;
                Debug.Log(" HalloweenLimitPackMgr GetActivityInfo popupPackageList=" + JsonConvert.SerializeObject(popupPackageList));
                MessageHelper.Broadcast(MessageName.UpadateHalloweenLimitPack);
            }
        });
    }
    
    public void OpenLimitPackWin()
    {
        UIManager.Inst.OpenPanel<HalloweenLimitPackView>(PanelId.HalloweenLimitPackView);
    }
    
    public bool IsEntryOpen()
    {
        if (popupPackageList == null || popupPackageList.Count == 0)
        {
            return false;
        }
        var productId = GetProductId();
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
        return true;
    }
    
    public string GetProductId()
    {
        if (Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WindowsEditor)
        {
            return productId_android;
        }
        return productId_ios;
    }

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
            panel.SetCallback(paymentType => { BuyPack(paymentType); });
            return;
        }

        BuyPack(ConfirmPaymentPanel.PaymentType.Default);
    }
    
    void BuyPack(ConfirmPaymentPanel.PaymentType paymentType)
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
            // TrackEvent(billingResultResponse);
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }
    
    public void StartLooping()
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
            UIManager.Inst.ClosePanel(PanelId.HalloweenLimitPackView);
        });
    }
    
    public void ShowBuyReward()
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        int goldCount = 0;//这个是奖励货币数，服务器不好下发自己计算
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        {
            //             俏皮双马尾	 10800119	
            // 卡哇伊眼睛	 10600127
            // 可爱牛仔裙         10400493
            // 小恶魔发卡	12100142
            CommonRewardItemData item;
            if (BagDatabase.Inst.Select("10800093") != null) 
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽头发",
                    pgcId = "10800093",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("10400368") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽裙子",
                    pgcId = "10400368",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("10900367") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽帽子",
                    pgcId = "10900367",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("11000153") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔法药水",
                    pgcId = "11000153",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("12000017") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽手套",
                    pgcId = "12000017",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("11300267") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽靴子",
                    pgcId = "11300267",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("10100076") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽斗篷",
                    pgcId = "10100076",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("10600084") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽眼睛",
                    pgcId = "10600084",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("10200037") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "魔女罗斯玛丽尾巴",
                    pgcId = "10200037",
                };
                items.Add(item);
            }

            if (BagDatabase.Inst.Select("40100071") != null)
            {
                goldCount++;
            }
            else
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = 1,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "喝咖啡",
                    pgcId = "40100071",
                };
                items.Add(item);
            }

            if (goldCount > 0)
            {
                item = new CommonRewardItemData()
                {
                    RewardAmount = goldCount * 10,
                    rewardType = (int)BUDRewardType.RewardPgcResource,
                    rewardName = "徽章",
                    pgcId = "30200001",
                };
                items.Add(item);
            }
        }

        panel.ShowRewards(items);
        panel.ShowCommonBtn("确定", null);
        TokenDataManager.Inst.GetTokenData();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }
    
    public void TrackAnalyticsData_Buy()
    {
        AnalyticsManager.Inst.Track(AnalyticsEventName.LimitedTimePackage_Buy);
    }
    
    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }
    
    ProductInfo GetProductInfo()
    {
        var productId = GetProductId();
        var popupPackage = popupPackageList.FirstOrDefault(x => x.productInfo.productId == productId);
        if (popupPackage == null)
        {
            GetActivityInfo();
            return null;
        }
        return popupPackage.productInfo;
    }
    
}

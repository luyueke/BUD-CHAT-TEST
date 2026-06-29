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
using UI.UIPanels.GashaponPanel;
using GameData.Gashapon;


public class AnniversaryLuckyKoiMgr : IActivity
{
    public enum MonthCardType
    {
        Silver,
        Gold
    }
    private static AnniversaryLuckyKoiMgr _instance;
    public static AnniversaryLuckyKoiMgr Inst
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AnniversaryLuckyKoiMgr();
                ActivityManager.Inst.AddActivity(ActivityId.AnniversaryLuckyKoi, _instance);
                RedDotSystemNew.Inst.AddReddotType(ReddotType.AnniversaryLuckyKoi, _instance.IsEntryRedDot);
            }
            return _instance;
        }
    }



    public AnniversaryLuckyKoiView view;

    public static readonly DateTime ACTIVITY_START_TIME = new DateTime(2025, 8, 12, 11, 0, 0); //活动开始时间 todo 
    public static readonly DateTime ACTIVITY_END_TIME = new DateTime(2025, 8, 31, 23, 59, 59);

    //(day,index)
    public Dictionary<int, RewardPreviewInfo> RewardTypeList;

    //扭蛋信息
    public GashaponInfoRsp gashaponInfoRsp;
    string gashaponId = "lottery.luckyKoi";
    public GashaponData gashaponData;


    GashaponRsp _gashaponRsp;

    private int koiGachaCoin;
    bool _checkKoiGachaCoin = false;

    AnniversaryLuckyKoiMgr()
    {
        RewardTypeList ??= new();
        RewardTypeList.Clear();
        RewardTypeList.Add(1, new RewardPreviewInfo(BUDRewardType.RewardPinkCoin, CurrencyType.PinkCoin, "", "", ""));
        RewardTypeList.Add(2, new RewardPreviewInfo(BUDRewardType.RewardPgcResource, CurrencyType.None, "40100514", "机械熊熊舞", ""));
        RewardTypeList.Add(5, new RewardPreviewInfo(BUDRewardType.RewardCommunitySkinTicket, CurrencyType.CommunitySkinTicket, "", "", ""));
        RewardTypeList.Add(6, new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", ""));
        RewardTypeList.Add(7, new RewardPreviewInfo(BUDRewardType.RewardCommunityInstrumentTicket, CurrencyType.CommunityInstrumentTicket, "", "", ""));
        RewardTypeList.Add(8, new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", ""));
        RewardTypeList.Add(9, new RewardPreviewInfo(BUDRewardType.RewardEnergyCoin, CurrencyType.EnergyCoin, "", "", ""));



        MessageHelper.AddListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
    }

    private void OnPlayerInfoAccountChange(CurrencyType currencyType)
    {
        if (currencyType == CurrencyType.KoiGachaCoin)
        {
            if (view != null)
            {
                view.RefreshUI();
            }
            MessageHelper.Broadcast(MessageName.ReddotNotice);
        }
    }


    public void GetInfoGashaponInfo()
    {
        GashaponStoreHandler dataHandler = AssetsDataManager.GetData<GashaponStoreHandler>();
        gashaponData = dataHandler.GetGashaponData(gashaponId);
        // Debug.Log("AnniversaryLuckyKoiMgr GetGashaponData: " + JsonConvert.SerializeObject(gashaponData));
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGetGashaponInfoSuccess);
    }

    private void OnGetGashaponInfoSuccess(GashaponInfoRsp rsp)
    {
        Debug.Log("AnniversaryLuckyKoiMgr OnGetGashaponInfoSuccess: " + JsonConvert.SerializeObject(rsp));
        gashaponInfoRsp = rsp;

        if (rsp.rewardPool != null)
        {
            foreach (var drawnInfo in rsp.rewardPool)
            {
                // var rewardItem = rewardItems.Find(tmp => tmp.GetRewardData().RewardId == drawnInfo.rewardId);
                // if (rewardItem != null)
                // {
                //     rewardItem.SetDrawnStatus(drawnInfo.everDrawn == 1);
                // }
            }
        }
        if (view != null)
        {
            view.RefreshUI();
        }

        try
        {
            JObject jsonObject = JObject.Parse(gashaponInfoRsp.extraData);
            koiGachaCoin = jsonObject["accumulatedConsumptionGacha"]["unViewedConvertedTimes"].ToObject<int>();
            if (koiGachaCoin > 0)
            {
                var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
                List<CommonRewardItemData> items = new List<CommonRewardItemData>();
                {
                    CommonRewardItemData item;
                    item = new CommonRewardItemData()
                    {
                        RewardAmount = koiGachaCoin,
                        rewardType = (int)BUDRewardType.RewardLuckyKoiTicket,
                        rewardName = PgcUtils.GetRewardName((BUDRewardType)BUDRewardType.RewardLuckyKoiTicket),
                        pgcId = ""
                    };
                    items.Add(item);
                }
                panel.ShowRewards(items);
                panel.ShowCommonBtn("确定", null);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("AnniversaryLuckyKoiMgr OnGetGashaponInfo showRewardError: " + e.Message);
        }

        // koiGachaCoin = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.KoiGachaCoin);
        // _checkKoiGachaCoin = true;
        AccountDataManager.Inst.BalanceInfo.Refresh();
        TokenDataManager.Inst.GetTokenData();

    }

    public bool CheckHadEarnReward(int index)
    {
        if (gashaponInfoRsp == null)
        {
            return false;
        }
        var gashaponData = AnniversaryLuckyKoiMgr.Inst.gashaponData;
        List<int> rewardIds = new List<int>();
        if (index == 2)
        {

            rewardIds.Add(2036);
        }
        else if (index == 3)
        {
            rewardIds.Add(2037);
            rewardIds.Add(2038);
            rewardIds.Add(2039);
        }
        else if (index == 4)
        {
            rewardIds.Add(2040);
            rewardIds.Add(2041);
            rewardIds.Add(2042);
        }
        bool hadEarn = true;
        foreach (var rewardId in rewardIds)
        {
            foreach (var item in gashaponData.RewardList)
            {
                if (item.RewardId == rewardId)
                {
                    if (!GashaponUtils.IsOwnedReward(item))
                    {
                        hadEarn = false;
                        break;
                    }
                }
            }
        }
        return hadEarn;
    }


    public void SendGashaponRequestOnce()
    {
        if (!IsDuringActivity())
        {
            TipPanel.ShowToast("活动已结束");
            return;
        }
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        if (GetRestGachaCount() <= 0)
        {
            TipPanel.ShowToast("今日剩余次数不足");
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.KoiGachaCoin, gashaponInfoRsp.singleDrawDiscountedPrice))
        {
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.KoiGachaCoin, gashaponInfoRsp.singleDrawDiscountedPrice);
            return;
        }
        GashaponDataManager.Inst.RequestGashapon(gashaponId, 1, (gashaponRsp) =>
        {
            OnGashaOnceRsp(gashaponRsp, true);
        });
    }



    //供老的扭蛋使用
    public void SendGashaponRequestTenTimes()
    {
        if (!IsDuringActivity())
        {
            TipPanel.ShowToast("活动已结束");
            return;
        }
        if (gashaponData == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        if (GetRestGachaCount() <= 0)
        {
            TipPanel.ShowToast("今日剩余次数不足");
            return;
        }

        int temPrice = GashaponUtils.GetRealTenPrice(gashaponData);
        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.KoiGachaCoin, temPrice))
        {
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.KoiGachaCoin, temPrice);
            return;
        }

        GashaponDataManager.Inst.RequestGashapon(gashaponId, 10, (gashaponRsp) =>
        {
            OnGashaOnceRsp(gashaponRsp, false);
        });
    }

    void OnGashaOnceRsp(GashaponRsp gashaponRsp, bool isOnce = false)
    {
        if (!view || gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }
        _gashaponRsp = gashaponRsp;

        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
        new GashaponTwistAnimParam()
        {
            gashaponId = gashaponId
        });
        if (isOnce)
        {
            panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () =>
            {
                OnGashaTwistAnimComplete(gashaponRsp);
            });
        }
        else
        {
            panel.PlayTenTwistAnimation(gashaponRsp.rewardList, () =>
            {
                OnGashaTwistAnimComplete(gashaponRsp);
            });
        }
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp)
    {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponId, gashaponRsp);

        GetInfoGashaponInfo();
    }


    public int GetHadLotteryCount()
    {
        if (gashaponInfoRsp == null)
        {
            return 0;
        }
        try
        {
            JObject jsonObject = JObject.Parse(gashaponInfoRsp.extraData);
            return jsonObject["accumulatedConsumptionGacha"]["totalGacha"].ToObject<int>();
        }
        catch (System.Exception)
        {
            return 0;
        }
    }

    public int GetConsumeCount()
    {
        if (gashaponInfoRsp == null)
        {
            return 0;
        }
        try
        {
            // gashaponInfoRsp.extraData="{\"luckyStarNum\":0,\"randomLuckyStarNum\":0,\"accumulatedConsumptionGacha\":{\"totalGacha\":1,\"convertedTimes\":1,\"unViewedConvertedTimes\":0,\"consumptions\":[{\"accountType\":5,\"amount\":70}]}}";
            JObject jsonObject = JObject.Parse(gashaponInfoRsp.extraData);
            var consumptions = jsonObject["accumulatedConsumptionGacha"]["consumptions"];
            if (consumptions == null)
            {
                return 0;
            }
            if (consumptions is JArray array)
            {
                int sum = 0;
                foreach (var item in array)
                {
                    sum += item["amount"].ToObject<int>();
                }
                return sum;
            }
            return 0;
        }
        catch (System.Exception)
        {
            return 0;
        }
    }

    public int GetTicketCount()
    {
        int newKoiGachaCoin = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.KoiGachaCoin);
        return newKoiGachaCoin;
    }

    public int GetRestGachaCount()
    {
        if (gashaponInfoRsp == null)
        {
            return 0;
        }
        return gashaponInfoRsp.restGachaNum;
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






    public bool IsDuringActivity()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        return now >= ACTIVITY_START_TIME && now <= ACTIVITY_END_TIME;
    }

    public bool IsEntryRedDot(string param)
    {
        return IsDuringActivity() && GetTicketCount() > 0;
    }





    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }

    public List<ReddotType> GetReddotTypes()
    {
        return new List<ReddotType>() { ReddotType.AnniversaryLuckyKoi };
    }

    public bool IsOpen()
    {
        return true;
    }

    public void ShowPanel()
    {
    }
}

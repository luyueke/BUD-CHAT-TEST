using System;
using AIGame.Base;
using BUD.MailBox;
using Game.AINPCStudio;
using Game.AIResData;
using Game.AnimationStudio;
using GameUI;
using Message;
using Newtonsoft.Json;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using View.UI.PopupPanelSystem.Data;

public class WebtoolNewsSkipManager : GlobalInstance<WebtoolNewsSkipManager>
{
    public void HandleSkip(HallRemoteBtn mailSkipData)
    {
        if (mailSkipData == null)
        {
            return;
        }

        if (mailSkipData.skipType == 1)
        {
            LotteryDataBtn data = JsonConvert.DeserializeObject<LotteryDataBtn>(mailSkipData.skipData);
            string lotteryId = data.lottery_id;
            GashaponDataManager.Inst.JumpToGashapon(lotteryId);
        }
        else if (mailSkipData.skipType == 2)
        {
            HotSaleData data = JsonConvert.DeserializeObject<HotSaleData>(mailSkipData.skipData);
            int rechargeId = data.rechargeId;
            UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, (RechargeId)rechargeId);
        }
        else if (mailSkipData.skipType == 3)
        {
            ActivityCenterData data = JsonConvert.DeserializeObject<ActivityCenterData>(mailSkipData.skipData);
            if (int.TryParse(data.activityId, out int id))
            {
                ActivityId activityId = (ActivityId)id;
                UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, activityId.ToString());
            }
        }
    }

    public void HandleSkip(MailSkipData mailSkipData)
    {
        if (mailSkipData == null)
        {
            return;
        }

        var webtoolNewsData = new WebtoolNewsData()
        {
            skipType = mailSkipData.skipType,
            skipData = mailSkipData.extraData
        };

        HandleSkip(webtoolNewsData);
    }

    public void HandleSkip(WebtoolNewsData webtoolNewsData)
    {
        if (webtoolNewsData == null)
            return;

        switch ((NewsMainType)webtoolNewsData.skipType)
        {
            case NewsMainType.Activity:
                SkipToActivityCenter(webtoolNewsData);
                break;
            case NewsMainType.LotteryGacha:
                SkipToLottery(webtoolNewsData);
                break;
            case NewsMainType.FittingRoom:
                SkipToFittingRoom(webtoolNewsData);
                break;
            case NewsMainType.OfficialStore:
                UIManager.Inst.OpenPanel(PanelId.StoreMallPanel);
                break;
            case NewsMainType.RechargeCenter:
                UIManager.Inst.OpenPanel(PanelId.RechargePanel);
                break;
            case NewsMainType.OutfitStudio:
                UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Avatar);
                break;
            case NewsMainType.GameStudio:
                UIManager.Inst.OpenPanel(PanelId.AssetStudioCategoryPanel);
                break;
            case NewsMainType.Play:
                UIManager.Inst.OpenPanel(PanelId.CommunityGamesPanel);
                break;
            case NewsMainType.CreativeContest:
                SkipToContest(webtoolNewsData);
                break;
            case NewsMainType.OfficialMail:
                UIManager.Inst.OpenPanel<MailboxPanel>(PanelId.MailboxPanel);
                break;
            case NewsMainType.MusicStudio:
                UIManager.Inst.OpenPanel(PanelId.InstrumentStudioCategoryPanel);
                break;
            case NewsMainType.CreatorCenter:
                //UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel);
                OcCompetitionSystem.Inst.OpenPanel();
                break;
            case NewsMainType.SeasonPass:
                SeasonPassData seasonPassData = JsonConvert.DeserializeObject<SeasonPassData>(webtoolNewsData.skipData);
                var seasonPassType = Convert.ToInt32(seasonPassData.seasonPassType);
                UIManager.Inst.OpenPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel, (SeasonPassType)seasonPassType);
                break;
            case NewsMainType.PaidPackage:
                PaidPackSkipData data = JsonConvert.DeserializeObject<PaidPackSkipData>(webtoolNewsData.skipData);
                data.HandSkip();
                break;
            case NewsMainType.Vip:
                UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.VipMonthPack);
                break;
            case NewsMainType.H5:
                SkipToWebUrl(webtoolNewsData);
                break;
            case NewsMainType.PinkCoinTask:
                UIManager.Inst.OpenPanel(PanelId.PinkCoinPanel);
                break;
            case NewsMainType.PetFittingRoom:
                SkipToPetFittingRoom(webtoolNewsData);
                break;
            case NewsMainType.PetStudio:
                UIManager.Inst.OpenPanel(PanelId.AvatarStudioMainPanel, CharacterStyle.Pet);
                break;
            case NewsMainType.AnimationStudio:
                UIManager.Inst.OpenPanel(PanelId.AnimationStudioMainPanel, AnimationStudioType.Animation);
                break;
            case NewsMainType.LimitPaidPackage:
                UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.LimitedRechargeGiftPack);
                break;

            case NewsMainType.AccumulatedRecharge:
                UIManager.Inst.OpenPanel(PanelId.RechargePanel);
                UIManager.Inst.OpenPanel(PanelId.CumulativeRechargePanel);
                break;
            case NewsMainType.TipsUpdate:
                ForceUpdate();
                break;
            case NewsMainType.ForceUpdate:
                ForceUpdate();
                break;
            case NewsMainType.NpcStudio:
                UIManager.Inst.OpenPanel<AINpcStudioMainPanel>(PanelId.AINpcStudioMainPanel);
                break;
            case NewsMainType.NpcStore:
                UIManager.Inst.OpenPanel<AINpcStorePanel>(PanelId.AINpcStorePanel);
                break;
            case NewsMainType.ApartmentEscape:
                var aiResData = AIResDataManager.Inst.GetAIGameData(AIResType.AIYandere);
                if (aiResData == null)
                {
                    UIManager.Inst.OpenPanel<AIYandereStartPanel>(PanelId.AIYandereStartPanel);
                }
                else
                {
                    if (aiResData.remaining <= 0)
                    {
                        var aiPanel = UIManager.Inst.OpenPanel<AIBuyResourcePanel>(PanelId.AIBuyResourcePanel,(int)AIResType.AIYandere);
                    }
                    else
                    {
                        UIManager.Inst.OpenPanel<AIYandereStartPanel>(PanelId.AIYandereStartPanel);
                    }
                }
                break;
            case NewsMainType.BUDPartner:
                UIManager.Inst.OpenPanel(PanelId.AIBuddyListPanel);
                break;
            case NewsMainType.GiftStore:
                SkipToGiftStore(webtoolNewsData);
                break;
            case NewsMainType.HotSales:
                SkipToHotSale(webtoolNewsData);
                break;
            case NewsMainType.NewUserGift:
                UIManager.Inst.OpenPanel(PanelId.PinkCoinPanel);
                break;
            case NewsMainType.StarterQuest:
                UIManager.Inst.OpenPanel(PanelId.NewBieSevenDayV3TaskPanel);
                break;
            case NewsMainType.HospitalEscape:
                UIManager.Inst.OpenPanel(PanelId.AIHospitalMainEntryPanel);
                break;
            case NewsMainType.AnniversaryEvent:
                
                SkipAnniversary(webtoolNewsData);
                break;

        }
    }
    private void SkipAnniversary(WebtoolNewsData webtoolNewsData)
    {
        TipPanel.ShowToast("周年庆活动已结束");
        return;
        UIManager.Inst.OpenPanel(PanelId.AnniversaryPanel);
        AnniversarySkipData data = JsonConvert.DeserializeObject<AnniversarySkipData>(webtoolNewsData.skipData);
        MessageHelper.Broadcast(MessageName.AnniversaryPanel_TabChange, data.eventId);
    }
    private void SkipToContest(WebtoolNewsData webtoolNewsData)
    {
        ContestData data = JsonConvert.DeserializeObject<ContestData>(webtoolNewsData.skipData);
        string contestId = data.contestId;
        if (string.IsNullOrEmpty(contestId))
        {
            return;
        }

        ContestEventManager.Inst.OpenContestSelectPage(contestId);
    }
    
    private void SkipToGiftStore(WebtoolNewsData webtoolNewsData)
    {
        FittingRoomData data = JsonConvert.DeserializeObject<FittingRoomData>(webtoolNewsData.skipData);
        int tabType = Math.Max(data.tabType - 1, 0); // 确保 tabType 不小于 0
        int classType = data.classType;
        var sendGiftPanel = UIManager.Inst.SwapPanel(PanelId.SendGiftPanel) as SendGiftPanel;
        sendGiftPanel?.JumpTo((SendGiftMainTabs.Tab)tabType, classType);
    }

    private void SkipToFittingRoom(WebtoolNewsData webtoolNewsData)
    {
        FittingRoomData data = JsonConvert.DeserializeObject<FittingRoomData>(webtoolNewsData.skipData);
        int tabType = data.tabType;
        int classType = data.classType;
        var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
        fittingRoom?.JumpTo((MainTabs.Tab)tabType, classType);
    }

    private void SkipToPetFittingRoom(WebtoolNewsData webtoolNewsData)
    {
        FittingRoomData data = JsonConvert.DeserializeObject<FittingRoomData>(webtoolNewsData.skipData);
        int tabType = data.tabType;
        int classType = data.classType;
        var fittingRoom = UIManager.Inst.OpenPanel<FittingRoomPanel>(PanelId.FittingRoomPanel, true);
        fittingRoom?.JumpTo((MainTabs.Tab)tabType, classType);
    }


    private void SkipToWebUrl(WebtoolNewsData webtoolNewsData)
    {
        WebUrlData data = JsonConvert.DeserializeObject<WebUrlData>(webtoolNewsData.skipData);
        string webUrl = data.webUrl;
        if (string.IsNullOrEmpty(webUrl))
        {
            return;
        }

        Application.OpenURL(webUrl);
    }
    
    private void SkipToHotSale(WebtoolNewsData webtoolNewsData)
    {
        HotSaleData data = JsonConvert.DeserializeObject<HotSaleData>(webtoolNewsData.skipData);
        int rechargeId = data.rechargeId;
        UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, (RechargeId)rechargeId);
    }

    private void SkipToActivityCenter(WebtoolNewsData webtoolNewsData)
    {
        ActivityCenterData data = JsonConvert.DeserializeObject<ActivityCenterData>(webtoolNewsData.skipData);
        string activityId = data.activityId;
        if (string.IsNullOrEmpty(activityId))
        {
            return;
        }

        UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, activityId);
    }

    private void SkipToLottery(WebtoolNewsData webtoolNewsData)
    {
        LotteryData data = JsonConvert.DeserializeObject<LotteryData>(webtoolNewsData.skipData);
        string lotteryId = data.lotteryId;
        if (string.IsNullOrEmpty(lotteryId))
        {
            return;
        }

        GashaponDataManager.Inst.JumpToGashapon(lotteryId);
    }

    private void ForceUpdate()
    {
#if UNITY_ANDROID
        #if PACKAGE_TYPE_US
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openNativeStore, "");
        #else
                switch ((IAPDataManager.ChannelIdEnum)IAPDataManager.Inst.channelId)
                {
                    case IAPDataManager.ChannelIdEnum.Tencent:    // 应用宝
                        Application.OpenURL("https://a.app.qq.com/o/simple.jsp?pkgname=com.tencent.tmgp.budapp&fromcase=70051&g_f=1182517&scenevia=XQYFX");
                        break;
                    case IAPDataManager.ChannelIdEnum.BiliBili:   // B站
                        Application.OpenURL("https://app.biligame.com/page/detail_share2.html?id=111855&sourceFrom=23006&_1733378618816");
                        break;
                    case IAPDataManager.ChannelIdEnum.GameCenter: // 4399
                        Application.OpenURL("http://a.4399.cn/mobile/221228.html?from=yxh");
                        break;
                    case IAPDataManager.ChannelIdEnum.Douyin:     // 抖音
                        TipPanel.ShowToast("您的账号为抖音渠道服账号，请前往抖音进行包体更新");
                        break;
                    case IAPDataManager.ChannelIdEnum.KuaiShou:   // 快手
                        TipPanel.ShowToast("您的账号为快手渠道服账号，请前往快手进行包体更新");
                        break;
                    case IAPDataManager.ChannelIdEnum.ChongChong: // 虫虫游戏
                        Application.OpenURL("https://wap.ccplay.com/package/226544.html");
                        break;
                    case IAPDataManager.ChannelIdEnum.HaoyouKuaiBao: // 好游快爆
                        Application.OpenURL("https://www.3839.com/a/170822.htm?from=hykb");
                        break;
                    case IAPDataManager.ChannelIdEnum.Vivo:       // Vivo
                    case IAPDataManager.ChannelIdEnum.Oppo:       // Oppo
                    case IAPDataManager.ChannelIdEnum.Honor:      // 荣耀
                    case IAPDataManager.ChannelIdEnum.Huawei:     // 华为
                    case IAPDataManager.ChannelIdEnum.Xiaomi:     // 小米
                    case IAPDataManager.ChannelIdEnum.Official: // 官服
                    case IAPDataManager.ChannelIdEnum.TapTap:   // TapTap
                    case IAPDataManager.ChannelIdEnum.Unknown: // 未知渠道
                    default:
                        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openNativeStore, "");
                        break;
                }
        #endif
               
#elif UNITY_IOS
                var appStore = "itms-apps://itunes.apple.com/app/apple-store/id6450975322";
        #if PACKAGE_TYPE_US
                appStore = "itms-apps://itunes.apple.com/app/apple-store/id1590291415";
        #endif
                Application.OpenURL(appStore);
#endif
    }
}

public class ActivityCenterData
{
    public string activityId;
}

public class LotteryData
{
    public string lotteryId;
}
public class LotteryDataBtn
{
    public string lottery_id;
}

public class WebUrlData
{
    public string webUrl;
}

public class FittingRoomData
{
    public int tabType;
    public int classType;
}

public class ContestData
{
    public string contestId;
}

public class HotSaleData
{
    public int rechargeId;
}
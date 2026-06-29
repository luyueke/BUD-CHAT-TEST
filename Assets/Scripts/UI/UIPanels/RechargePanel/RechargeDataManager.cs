using Es;

namespace UI.UIPanels.RechargePanel {


    public enum RechargeId {
        ErrRechargeId = 0,
        // 兑换码
        RedeemCode = 1,
        //1元福瑞礼
        FurryPack = 2,
        //10元梦幻紫罗兰礼包
        DreamyVioletPack = 3,
        //6元桃桃康蒂礼包
        PeachContiPack = 4,
        //VIP月卡会员
        VipMonthPack = 5,
        //超值礼包
        LimitedRechargeGiftPack = 6,
        //赛季累充
        S10SeasonRecharge = 7,
        //BUD钻充值
        GemPack = 8,
        //首充1元活动
        FirstChargeOneYuan = 9,
        //首充6元活动
        FirstChargeSixYuan = 10,
        // 新年福运签
        NewYearsFortune = 11,
        //春节限定礼包
        SpringLimited = 12,
        //春节12元礼包
        S7PhoenixPackage = 13,
        //瞌睡小锦鲤
        SleepyCoi = 14,
        //S8 限时返场礼包
        S8TwentyYuanPackage = 15,
        //S8限时粉币礼包
        S8CommunityCoinPack = 16,
        //S9每日累计充值6元
        DailyRechargePanel = 17,
        S9LimitedTimeCurrencyPack = 18,
        MiserableNursePromise = 19, // 丧丧护士的约定
        LiuyuanGiftPack= 20, //6元VIP礼包
        ShiyuanGiftPack = 21, //10元礼包 
        MonthCard = 22, //特惠月卡
        CatGiftPack = 23,//喵币礼包
        Y2kSnowboard = 24,//S12 滑雪板
        BuyReducePanel = 25,//满减
        S12SeasonRecharge = 77, // S12 赛季累充活动

        LoverGift = 88, // 情人节礼包
        QianQianWanWanGift = 89, // 千千万万礼包
        SeasonPrePack = 90, // 新赛季预购

        S14SeasonRecharge = 91, // S14 赛季累充活动

        WaCoinPack = 92, // 袜币礼包
        S15SeasonRecharge = 93, // S15 赛季累充活动
        ShovelPack = 94, // 铲子礼包
		BabyShrimpGiftPack = 95, // 虾仔礼包
		zzzGiftPack = 96, // 绒币礼包
    }

    public class RechargeDataManager : GlobalInstance<RechargeDataManager>{


        public RechargeViewConfig GetRechargeViewConfig(RechargeId viewId)
        {
            if (viewId == RechargeId.ErrRechargeId)
            {
                return null;
            }
            return DataTables.GetRechargeViewConfig((int)viewId);
        }

    }
}

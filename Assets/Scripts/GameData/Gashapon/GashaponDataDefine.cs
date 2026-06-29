using System;
using System.Collections.Generic;
using GameData.Rewards;

namespace GameData.Gashapon
{
    #region 扭蛋规则定义

    public class GashaRule
    {
        public int goldGuaranteed;
        public int purpleGuaranteed;
        public float goldProbability;
        public float purpleProbability;
        public float blueProbability;
    }

    //扭蛋Webtool配置数据
    public class GashaponInfo
    {
        public int gashaponId;
        public string name;
        public int singlePrice; //单抽价格
        public int tenDrawsPrice; //十连抽价格
        public int tenDrawsDiscount; //十连抽折扣 10=10%off

        /// <summary>
        /// 是否半价--用于客户端计算价格，服务器下发的singlePrice和tenDrawsPrice是原价，
        /// 1.hasHalfPrice = 1时，实际单抽价格和十连抽价格都是 *50% ,且tenDrawsDiscount失效，优先使用半价计算结果
        /// 2.hasHalfPrice = 0时，实际单抽价格是原价，实际十连抽价格用 tenDrawsPrice*tenDrawsDiscount 计算折扣后价格
        /// </summary>
        public int hasHalfPrice;

        public CurrencyType currencyType; //货币类型： 1：coin、2：badge、3：gem
        public GashaRule gachaRule; //扭蛋规则

        public int CalculateActualSingleTimePrice()
        {
            if (hasHalfPrice == 1)
            {
                return (int)(singlePrice * 0.5f);
            }
            else
            {
                return singlePrice;
            }
        }

        public int CalculateActualTenTimesPrice()
        {
            if (hasHalfPrice == 1)
            {
                return (int)(tenDrawsPrice * 0.5f);
            }
            else
            {
                return (int)(tenDrawsPrice * ((100 - tenDrawsDiscount) / 100f));
            }
        }

        public string CalculateDiscountStr()
        {
            if (hasHalfPrice == 1)
            {
                return "5";
            }
            else if (tenDrawsDiscount > 0)
            {
                var discount = (100 - tenDrawsDiscount) / 100.00f;
                return $"{discount * 10}";
            }

            return string.Empty;
        }
    }

    public class GashaponSerialInfo
    {
        public GashaponInfo gashaponInfo;
        public List<RewardInfo> rewardInfo = new List<RewardInfo>();
    }

    #endregion


    #region 抽奖

    public class GashaponReq
    {
        public string lotteryId;//扭蛋id
        public int times; // 单抽:1 十连抽:10
        public int cardId;
    }

    public class TaskGashaponReq
    {
        public string activityId;//扭蛋id
        public int times; // 单抽:1 十连抽:10
    }
    public class GashaponRsp
    {
        public List<RewardInfo> rewardList = new List<RewardInfo>();
        public int wonFirstPrizeFrequency;//扭到一等奖的次数
        public GashaponInfoRsp lotteryInfo;
        public List<RewardInfo> replaceRewardList;
        public int popupType;
        public int gachaTimes;//提前大奖时的抽取次数

        public GachaReturnInfo gachaReturn; //提前大奖时的返还奖励
    }

    public class GachaReturnInfo
    {
        public int rewardType; //提前大奖时的返还奖励类型
        public int amount; //提前大奖时的返还奖励数量
    }

    #endregion

    #region 扭蛋进度信息

    [Serializable]
    public class GashaponLuckyPregross
    {
        public int start;
        public int end;

        public int round; //第几轮
        public int roundIndex; //轮次中的第几抽
    }

    public class GashaponInfoRsp
    {
        public int restGachaNum = 0;
        public GashaponLuckyPregross luckyProgressInfo;

        public int singleDrawPrice;
        public int singleDrawDiscountedPrice;
        public int tenDrawPrice;//10抽折前价格
        public int tenDrawDiscountedPrice;//10抽折后价格

        public List<GashaponExtraTaskInfo> taskList;
        public List<GashaponRewardDrawnInfo> rewardPool;
        public string extraData;
        public int isHavingPgcOptionalBox;
    }

    public class GashaponExtraTaskInfo
    {
        public int eventId;
        public int rewardStatus;
    }

    public class GashaponRewardDrawnInfo {
        public int cardId;
        public int rewardId;
        public int everDrawn;
        public string pgcId;
        public int level;
    }
    #endregion

    public class CommonRewardData
    {
        public int rewardType;
        public int rewardNum;
        public string pgcId;
    }
}

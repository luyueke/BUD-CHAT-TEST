using System.Collections.Generic;

namespace GameData.Rewards
{
    #region 基本奖励数据定义

    public enum RewardLevel
    {
        Gold = 1, // 金色
        Purple = 2, //紫奖
        Blue = 3, // 蓝奖
    }

    public class RewardInfo
    {
        public int level;
        public int rewardType;
        public int amount;
        public string pgcId;
        public string rewardName;
        public string bundleId;

        /// <summary>
        /// 是否替换奖励
        /// </summary>
        public int isReplaced;
        /// <summary>
        /// 是否暴击
        /// </summary>
        public int isCritical;
    }

    #endregion

    #region 打开GetRewardPanel时传递数据

    /// <summary>
    /// 获奖来源
    /// </summary>
    public enum RewardSource
    {
        Gashapon, //扭蛋奖励-单次/十连抽
    }

    public class OpenRewardPanelParas
    {
        public RewardSource RewardSource;
        public List<RewardInfo> RewardInfos = new List<RewardInfo>();
    }

    public class ServerRewardInfo
    {
        public int rewardType;
        public int amount;
    }

    #endregion
}

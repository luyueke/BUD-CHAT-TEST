using Game.Event;
using GameData;
using GameUI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using View.UI.PopupPanelSystem.Data;

public class ActivityResponse
{
    public List<ActivityInfo> list;
    public string customerServiceUrl;
}

public class ActivityInfo
{
    //后端交互字段
    public string activityId;
    public string leftTime;
    public int activityStatus;//0为未上线，1为上线
    public int currencyAmount;
    public int finishAmount;//已完成进度
    public int targetAmount;//目标完成进度
    public List<ActivityRewardInfo> rewardList;
    public List<ActivityRewardInfo> preRewardList;
    public List<ActivityEventInfo> eventList;
    public List<TaskItemData> taskList;

    //业务字段
    public string activityTital;
    public long removeTime;//活动下架时间
    public string viewPrefabPath;

    public ActivityRewardPanelCfg rewardPanelCfg;

    // 扩展业务字段
    public object extra;

    public string tabCoverName;
    public int currencyType;
    //s4-b-消费狂欢节
    public ConsumeCarnivalInfo consumeCarnivalInfo;
    // S5 折扣卡
    public DiscountCardInfo discountCardInfo;
    //s5活动 圣诞消费必反
    public ChristmasCoinRebatesInfo christmasCoinRebatesInfo;

    //S6活动 组队消费活动
    public GroupConsumeInfo groupConsumeInfo;


    //S6活动 新年登录礼
    public NewYearLoginInfo newYearLoginInfo;

    //仅公寓逃脱模拟器预告活动
    public ApartmentEscapeInfo apartmentEscapeInfo;

    //跳转活动
    public ActivitySkipMsgItem activitySkipMsg;

    public ActivityProductInfo productinfo;

    public PlantUserDayInfo treePlantingDayInfo;

    public TreasureHauntingInfo treasureHauntingInfo;
}

public class TreasureHauntingInfo
{
    public int currentLevel;//当前关卡
    public List<int> showedGrids;//已经翻开的格子id
    public List<ActivityEventInfo> levelEventList;//关卡奖励状态，来自服务器 eventList
}

public class ActivityProductInfo
{
    public int isPurchased;
    public int isPurchased1;
    public int isPurchased2;
}

public class ApartmentEscapeInfo
{
    public int totalAmount;
    public int currentAmount;
}

public class ActivityRewardInfo
{
    //后端交互字段
    public int rewardId;
    public int rewardStatus;
    public int redeemLeftCount;

    //业务字段
    public string rewardType; // 字段类型不对，S2版本开始暂停使用，慢慢替换
    public int spendNum; // 花费数量
    public int budRewardType; // 奖励类型 和 BUDRewardType 枚举一致
    public string rewardName; // 奖励展示的名称
    public string pgcId; //奖励pgcID
    public int rewardNum;
    public string rewardIcon;

    public string previewIcon;
    public string bundleId;
    // 扩展业务字段
    public object extra;
}
public class ActivityEventInfo
{
    //后端交互字段
    public int eventId;
    // ClaimStatus
    public int eventStatus;
    public int finishAmount;

    //业务字段
    public int groupId;
    public string eventName;
    public string rewardName;
    public int rewardNum;
    public int rewardType;
    public int targetAmount;
    public int eventSkipType;
    public string pgcId;
    public string rewardSpecial;
    public string extra; // s2-b新增业务字段

    public string targetAmountList;  //阶段任务使用
    public string rewardNumList;//阶段任务使用
}

public class ActivityEventClaimResponse
{
    public int claimAmount;
    public int currencyAmount;
    public ActivityEventInfo eventInfo;
    public List<ServerRewardInfo> rewardList;
    public List<ServerRewardInfo> replaceRewardList;
}
public class ActivityCenterInfoReq
{
    public List<string> idList;
}
public class ActivityRewardConvertResponse
{
    public int rewardId;
    public int rewardStatus;
    public int currencyAmount;
    public ActivityReplaceReward replaceReward; // 兑换成功后，如果后端检测到有这个奖励，会返回此字段，兑换成badge 等其他货币
    public RewardList[] rewardList;
}

public class ActivityReplaceReward
{
    public int rewardType;
    public int amount;
}

public class RewardList
{
    public int rewardType;
    public int amount;
    public string bundleId;
}


public class ActivityRewardPanelCfg
{
    /// <summary>
    /// 预览页面的背景图
    /// </summary>
    public string rewardBg;


    #region 纯色背景加循环图案
    public string rewardBgColor;
    public List<string> rewardIcons;
    #endregion


    /// <summary>
    /// 预览奖品的 背景颜色
    /// </summary>
    public string rewardItemBgColor;

    public string endTimeTip;


}


public interface EventItemViewProtocol
{
    /// <summary>
    /// 当前的领取状态
    /// </summary>
    public TaskClaimState eventStatus { get; set; }

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="data">本地数据</param>
    /// <param name="clickAction">点击响应事件</param>
    /// <typeparam name="T">初始化类型</typeparam>
    public void Init(ActivityEventInfo data, Action<ActivityEventInfo, TaskClaimState> clickAction);

    /// <summary>
    /// 刷新领取状态信息
    /// </summary>
    /// <param name="data">后端数据</param>
    /// <typeparam name="T1">后端数据类型</typeparam>
    public void Refresh(ActivityEventInfo data);

    public void SyncClaimed();
}

public class ConsumeCarnivalInfo
{
    public string activityId;
    public int gemAmount;
    public int communityCoinAmount;
    public int totalVoucher;
    public int usedVoucher;
    public int currentVoucher;
    public int reddot;
}

public class DiscountCardInfo
{
    public int totalAmount;
    public int savedAmount;
}

public class ChristmasCoinRebatesInfo
{
    public int isPrizeDraw;
    public List<string> koiPrizeList;
    public List<string> firstPrizeList;
    public List<string> secondPrizeList;
}
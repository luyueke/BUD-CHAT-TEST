using System.Collections.Generic;
using System.Linq;
using System.Timers;
using Game.Base;
using GameUI;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.UIPanels.RechargePanel;
using UnityEngine;
public enum ReddotType
{
    ApplyingFriend,
    Mail,
    InteractNotification,
    GiftNotification,
    AuditNotification,
    ActivityCenter,
    Task,
    Charge,
    Chat,
    PinkCoinTask,
    LimitPackage,
    NewbieTask,
    CreatorCenter,
    AIBuddyTask,

    GameEntryPark,

    // 新红点 
    S11GroupConsume,
    AnniversaryCelebrationGift,
    AnniversaryCelebrationMonth,
    AnniversaryCelebrationSummer,
    AnniversaryCelebrationStore,
    AnniversaryCelebrationEvent,
    Gashapon,
    AnniversaryLuckyKoi,
    GashaponTab,
    SeasonCumulative,      //赛季累充
    TreePlantingDayTask,
}
/// <summary>
/// 红点管理工具类
/// </summary>
public class ReddotManagerUtils : GlobalInstance<ReddotManagerUtils>
{

    private Dictionary<ReddotType, int> redDotNumDic = new Dictionary<ReddotType, int>()
    {
        {ReddotType.ApplyingFriend,0},
        {ReddotType.Mail,0},
        {ReddotType.InteractNotification,0},
        {ReddotType.AuditNotification,0},
        {ReddotType.ActivityCenter,0},
        {ReddotType.Task,0},
        {ReddotType.Chat,0},
        {ReddotType.PinkCoinTask,0},
        {ReddotType.LimitPackage,0},
        {ReddotType.GiftNotification,0},
        {ReddotType.NewbieTask,0},
        {ReddotType.CreatorCenter,0},
        {ReddotType.AIBuddyTask,0},

        {ReddotType.GameEntryPark,0},
        {ReddotType.Gashapon,0}
    };

    private int[] chargeReddotIds;

    private BudTimer timer;

    public int GetRedDotCount(ReddotType type)
    {
        if (redDotNumDic.ContainsKey(type))
        {
            return redDotNumDic[type];
        }
        return 0;
    }

    public bool HasRechargeRedDot(int rechargeId) {
        if (rechargeId == (int)RechargeId.MonthCard) {
            return AnniversaryMonthCardMgr.Inst.IsEntryRedDot("");
        }
        if (chargeReddotIds != null && chargeReddotIds.Contains(rechargeId)) {
            return true;
        } else {
            return false;
        }
    }

    public void CleanRedDotNum(ReddotType type)
    {
        if (redDotNumDic.ContainsKey(type))
        {
           redDotNumDic[type] = 0;
        }
        MessageHelper.Broadcast(MessageName.ReddotNotice);
    }

    /// <summary>
    /// 每一分钟刷新一次红点
    /// </summary>
    public void Init()
    {
        // 创建一个Timer对象，设置间隔为一分钟（以毫秒为单位）
        TimerManager.Inst.Stop(timer);
        timer = TimerManager.Inst.Run("reddot", 60, 60, () => { TimerElapsed(); });

        RefreshRedDot();

    }

    public override void Release()
    {
        base.Release();

        // 停止和释放定时器
        TimerManager.Inst.Stop(timer);
    }


    private  void TimerElapsed()
    {
        // 在这里执行你希望每隔一分钟执行一次的操作
        if ( GameController.IsInHallScene())
        {
            RefreshRedDot();
        }

    }

    /// <summary>
    /// 刷新红点数额
    /// </summary>
    public void RefreshRedDot()
    {
        GetRedDotInfo();
    }

    /// <summary>
    /// 获取红点信息
    /// </summary>
    private void GetRedDotInfo()
    {
        if (Application.isPlaying)
        {
            NetworkManager.Inst.SendHttpRequest<GetReddotInfoResponse>(HttpUrlDefine.reddotNotice,
                HttpMethod.GET,
                "",
                GetRedDotSuccess,
                GetRedDotFail);
        }


    }

    private void GetRedDotSuccess(GetReddotInfoResponse reddotInfoResponse)
    {
        if (reddotInfoResponse != null )
        {
            redDotNumDic[ReddotType.ApplyingFriend] = reddotInfoResponse.applyingFriend;
            redDotNumDic[ReddotType.Mail] = reddotInfoResponse.mail;
            redDotNumDic[ReddotType.InteractNotification] = reddotInfoResponse.interactNotification;
            redDotNumDic[ReddotType.AuditNotification] = reddotInfoResponse.auditNotification;
            redDotNumDic[ReddotType.ActivityCenter] = reddotInfoResponse.activityCenter;
            redDotNumDic[ReddotType.Task] = reddotInfoResponse.task;
            redDotNumDic[ReddotType.Charge] = reddotInfoResponse.charge;
            redDotNumDic[ReddotType.Chat] = reddotInfoResponse.chat;
            redDotNumDic[ReddotType.PinkCoinTask] = reddotInfoResponse.newComerCommunityTask;
            redDotNumDic[ReddotType.LimitPackage] = reddotInfoResponse.limitPackage;
            redDotNumDic[ReddotType.GiftNotification] = reddotInfoResponse.giftNotification;
            redDotNumDic[ReddotType.NewbieTask] = reddotInfoResponse.newbieTask;
            redDotNumDic[ReddotType.AIBuddyTask] = reddotInfoResponse.aiBuddyTask;
            redDotNumDic[ReddotType.CreatorCenter] = reddotInfoResponse.creatorCenter;
            redDotNumDic[ReddotType.Gashapon] = reddotInfoResponse.lotteryTask;
            redDotNumDic[ReddotType.TreePlantingDayTask] = reddotInfoResponse.treePlantingDayTask;
            chargeReddotIds = reddotInfoResponse.chargeReddotIds;
        }

        redDotNumDic[ReddotType.GameEntryPark] = GameEntrySystem.Inst.GetParkRed() ? 1 : 0;

        MessageHelper.Broadcast(MessageName.ReddotNotice);
    }

    private void GetRedDotFail(HttpResponseRawData failMessage)
    {
    }

    public void RefreshRed() {
        GetRedDotSuccess(null);
    }
}

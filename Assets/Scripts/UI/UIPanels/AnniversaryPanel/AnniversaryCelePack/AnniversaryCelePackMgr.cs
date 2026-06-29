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
using System.Runtime.InteropServices.WindowsRuntime;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;


public class AnniversaryCelePackMgr: IActivity
{
    // public static readonly string ActivityId = "AnniversaryBenefits";
    public static readonly TASK_ID TaskId_1 = TASK_ID.DawnSurprise; // 曙光惊喜
    public static readonly TASK_ID TaskId_2 = TASK_ID.SummerEnjoyment; // 夏日畅享
    public static readonly TASK_ID TaskId_3 = TASK_ID.CoolSummerEnjoyment; // 酷夏尊享
    public static readonly TASK_ID TaskId_4 = TASK_ID.DeluxeSupplies; // 豪华补给
    public static readonly TASK_ID TaskId_5 = TASK_ID.AnniversaryBenefits; // 周年福利

    private static AnniversaryCelePackMgr _instance;
    public static AnniversaryCelePackMgr Inst
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AnniversaryCelePackMgr();
                ActivityManager.Inst.AddActivity(ActivityId.AnniversaryCelebrationGift, _instance);
                RedDotSystemNew.Inst.AddReddotType(ReddotType.AnniversaryCelebrationGift, _instance.IsEntryRedDot);
            }
            return _instance;
        }
    }


    public ActivityInfo activityInfo;

    public AnniversaryCelePackView view;

    public static readonly DateTime ACTIVITY_START_TIME = new DateTime(2025, 7, 28, 11, 0, 0); //活动开始时间
    public static readonly DateTime ACTIVITY_END_TIME = new DateTime(2025, 8, 31, 23, 59, 59);


    //(day,index)
    public Dictionary<TASK_ID, Dictionary<int, RewardPreviewInfo[]>> RewardTypeList;
    public Dictionary<PaidPackageType, PaidPackageListItem> PaidPackage2ItemDict = new();
    public Dictionary<TASK_ID, TaskInfoData> TaskId2InfoDict = new();

    public long SavePackTaskEndDate = 0;

    public bool AnniversaryGiftClaimed = true; //周年庆礼包是否领取

    AnniversaryCelePackMgr()
    {
        RewardTypeList ??= new();
        RewardTypeList.Clear();
        RewardTypeList.Add(TaskId_1, new Dictionary<int, RewardPreviewInfo[]>());
        RewardTypeList[TaskId_1].Add(1, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardYouYouCoin, CurrencyType.YouYouCoin, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardPurpleDreamCoin, CurrencyType.PurpleDreamCoin, "", "", "") });
        RewardTypeList.Add(TaskId_2, new Dictionary<int, RewardPreviewInfo[]>());
        RewardTypeList[TaskId_2].Add(1, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardPinkCoin, CurrencyType.PinkCoin, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardBadge, CurrencyType.Badge, "", "", "") });
        RewardTypeList[TaskId_2].Add(2, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunityInstrumentTicket, CurrencyType.CommunityInstrumentTicket, "", "", "") });
        RewardTypeList[TaskId_2].Add(3, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", "") });
        RewardTypeList.Add(TaskId_3, new Dictionary<int, RewardPreviewInfo[]>());

        RewardTypeList[TaskId_3].Add(1, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardGem, CurrencyType.Gem, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardPinkCoin, CurrencyType.PinkCoin, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardLuckyCoin, CurrencyType.LuckyCoin, "", "", "") });
        RewardTypeList[TaskId_3].Add(2, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunitySkinTicket, CurrencyType.CommunitySkinTicket, "", "", "") });
        RewardTypeList[TaskId_3].Add(3, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", "") });

        RewardTypeList.Add(TaskId_4, new Dictionary<int, RewardPreviewInfo[]>());
        RewardTypeList[TaskId_4].Add(1, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardPinkCoin, CurrencyType.PinkCoin, "", "", ""),
            new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", "") });
        RewardTypeList[TaskId_4].Add(2, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", "") });
        RewardTypeList[TaskId_4].Add(3, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunityInstrumentTicket, CurrencyType.CommunityInstrumentTicket, "", "", "") });
        RewardTypeList[TaskId_4].Add(4, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunitySkinTicket, CurrencyType.CommunitySkinTicket, "", "", "") });
        RewardTypeList[TaskId_4].Add(5, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCommunityAnimationTicket, CurrencyType.CommunityAnimationTicket, "", "", "") });

        RewardTypeList.Add(TaskId_5, new Dictionary<int, RewardPreviewInfo[]>());
        RewardTypeList[TaskId_5].Add(1, new RewardPreviewInfo[] { new RewardPreviewInfo(BUDRewardType.RewardCoin, CurrencyType.Coin, "", "", "") });


        PaidPackage2ItemDict ??= new();
        PaidPackage2ItemDict.Clear();
        PaidPackage2ItemDict.Add(PaidPackageType.DawnSurprise, null);
        PaidPackage2ItemDict.Add(PaidPackageType.SummerEnjoyment, null);
        PaidPackage2ItemDict.Add(PaidPackageType.CoolSummerEnjoyment, null);
        PaidPackage2ItemDict.Add(PaidPackageType.DeluxeSupplies, null);

        TaskId2InfoDict ??= new();
        TaskId2InfoDict.Clear();
        TaskId2InfoDict.Add(TaskId_1, null);
        TaskId2InfoDict.Add(TaskId_2, null);
        TaskId2InfoDict.Add(TaskId_3, null);
        TaskId2InfoDict.Add(TaskId_4, null);
        TaskId2InfoDict.Add(TaskId_5, null);
    }



    public void GetProductInfo(PaidPackageType paidPackageType, Action<PaidPackageListItem> onSuccess = null)
    {
        Debug.Log("AnniversaryCelePackMgr GetProductInfo: " + paidPackageType);
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            var paidPackageList = res.paidPackageList;
            if (paidPackageList != null && this != null)
            {
                PaidPackageListItem packageListItem =
                    paidPackageList.Find(x => x.packageType == (int)paidPackageType);
                if (packageListItem != null)
                {
                    Debug.Log("AnniversaryCelePackMgr GetProductInfo success: " + JsonConvert.SerializeObject(packageListItem));

                    PaidPackage2ItemDict[paidPackageType] = packageListItem;
                    onSuccess?.Invoke(packageListItem);
                }
            }
        });
    }

    public void CheckSavePackTaskEndDate()
    {
        foreach (var item in PaidPackage2ItemDict)
        {
            if (item.Value != null && item.Value.isPaid == 1)
            {
                // 解析剩余时间格式（如"5天5小时"）并转换为结束时间戳
                SavePackTaskEndDate = ParseRemainingTimeToEndTimestamp(item.Value.endDate);
            }
        }
    }

    /// <summary>
    /// 解析剩余时间格式并转换为结束时间戳
    /// </summary>
    /// <param name="remainingTimeStr">剩余时间字符串，格式如"5天5小时"</param>
    /// <returns>结束时间戳（秒）</returns>
    public long ParseRemainingTimeToEndTimestamp(string remainingTimeStr)
    {
        if (string.IsNullOrEmpty(remainingTimeStr))
        {
            return 0;
        }

        try
        {
            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;

            // 解析天数
            var dayMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)天");
            if (dayMatch.Success)
            {
                days = int.Parse(dayMatch.Groups[1].Value);
            }

            // 解析小时
            var hourMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)小时");
            if (hourMatch.Success)
            {
                hours = int.Parse(hourMatch.Groups[1].Value);
            }

            // 解析分钟
            var minuteMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)分钟");
            if (minuteMatch.Success)
            {
                minutes = int.Parse(minuteMatch.Groups[1].Value);
            }

            // 解析秒数
            var secondMatch = System.Text.RegularExpressions.Regex.Match(remainingTimeStr, @"(\d+)秒");
            if (secondMatch.Success)
            {
                seconds = int.Parse(secondMatch.Groups[1].Value);
            }

            // 计算结束时间
            DateTime endTime = TcpTimeSystem.Inst.ServerDataTime.AddDays(days).AddHours(hours).AddMinutes(minutes).AddSeconds(seconds);

            // 转换为时间戳（使用UTC时间）
            return ((DateTimeOffset)endTime.ToUniversalTime()).ToUnixTimeSeconds();
        }
        catch (Exception ex)
        {
            Debug.LogError($"解析剩余时间失败: {remainingTimeStr}, 错误: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// 将时间戳转换为DateTime（本地时间）
    /// </summary>
    /// <param name="timestamp">Unix时间戳（秒）</param>
    /// <returns>DateTime对象（本地时间）</returns>
    public static DateTime TimestampToDateTime(long timestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
    }

    /// <summary>
    /// 将DateTime转换为时间戳
    /// </summary>
    /// <param name="dateTime">DateTime对象</param>
    /// <returns>Unix时间戳（秒）</returns>
    public static long DateTimeToTimestamp(DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime.ToUniversalTime()).ToUnixTimeSeconds();
    }

    /// <summary>
    /// 拉几个活动的礼包信息 是否已经购买
    /// </summary> <summary>
    /// 
    /// </summary>
    public void PreGetProductInfo()
    {
        foreach (var paidPackageType in PaidPackage2ItemDict.Keys)
        {
            GetProductInfo(paidPackageType, null);
        }
    }
    /// <summary>
    /// 拉几个活动的任务信息
    /// </summary>
    public void PreGetTaskData()
    {
        foreach (var taskId in new List<TASK_ID> { TaskId_1, TaskId_2, TaskId_3, TaskId_4, TaskId_5 })
        {
            GetTaskData(taskId);
        }
    }

    public void GetTaskData(TASK_ID taskId)
    {
        var reqParam = new GetTaskListReq()
        {
            idList = new List<string>() { taskId.ToString() }
        };
        Debug.Log("AnniversaryCelePackMgr请求活动信息: " + JsonConvert.SerializeObject(reqParam));
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, JsonConvert.SerializeObject(reqParam), (content) =>
        {
            Debug.Log("AnniversaryCelePackMgr请求活动信息成功 success: " + content);
            OnGetTaskDataSuccess(taskId, JsonConvert.DeserializeObject<TaskListRsp>(content));
            if (this.view != null)
            {
                this.view.RefreshUI(taskId);
            }
            MessageHelper.Broadcast(MessageName.AnniversaryPanel_PackRedDot, ActivityId.AnniversaryCelebrationGift);
        }, (error) =>
        {
            LoggerUtils.Log("AnniversaryCelePackMgr GetTaskData failed: " + error);
        });
    }

    private void OnGetTaskDataSuccess(TASK_ID taskId, TaskListRsp taskListRsp)
    {
        if (taskListRsp.list.Count == 0)
        {
            return;
        }
        TaskId2InfoDict[taskId] = taskListRsp.list[0];
        LoggerUtils.Log("AnniversaryCelePackMgr OnGetTaskDataSuccess: " + taskId);

        // for (int i = taskListRsp.list[0].eventList.Count - 1; i >= 0; i--)
        // {
        //     var eventInfo = taskListRsp.list[0].eventList[i];

        // }
    }

    public void RefreshTaskData(TASK_ID taskId, TaskItemData taskItemData)
    {
        var taskInfo = TaskId2InfoDict[taskId];
        if (taskInfo == null)
        {
            return;
        }
        for (int i = taskInfo.eventList.Count - 1; i >= 0; i--)
        {
            var eventInfo = taskInfo.eventList[i];
            if (eventInfo.eventId == taskItemData.eventId)
            {
                eventInfo.eventStatus = taskItemData.eventStatus;
                break;
            }
        }
    }

    public string GetPgcIdBy(TASK_ID taskId, int day, int rewardType)
    {
        string pgcId = "";
        if (RewardTypeList.ContainsKey(taskId) && RewardTypeList[taskId].ContainsKey(day))
        {
            var rewardList = RewardTypeList[taskId][day];
            foreach (var reward in rewardList)
            {
                if ((int)reward.rewardType == rewardType)
                {
                    pgcId = reward.pgcId;
                    break;
                }
            }
        }
        return pgcId;
    }

    public void Testt()
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        {
            CommonRewardItemData item;
            item = new CommonRewardItemData()
            {
                RewardAmount = 1000,
                rewardType = 1,
                rewardName = PgcUtils.GetRewardName((BUDRewardType)1),
                pgcId = GetPgcIdBy(TaskId_5, 1, 1)
            };
            items.Add(item);
        }
        panel.ShowRewards(items);
        panel.ShowCommonBtn("确定", null);
    }

    public void ClaimTaskReward(TASK_ID taskId, int eventId, Action<TaskClaimRsp> success, Action<string> fail)
    {
        Debug.Log("AnniversaryCelePackMgr 请求当天奖励领取:" + eventId);
        EventCenterDataManager.Inst.CliamReward(taskId.ToString(), eventId, 1, 0, (claimRspData) =>
        {
            Debug.Log("AnniversaryCelePackMgr 请求当天奖励领取成功:" + eventId + "  " + JsonConvert.SerializeObject(claimRspData));
            List<TaskClaimRewardData> rewardList = claimRspData.rewardList;
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            List<CommonRewardItemData> items = new List<CommonRewardItemData>();
            foreach (var reward in rewardList)
            {
                CommonRewardItemData item;
                item = new CommonRewardItemData()
                {
                    RewardAmount = reward.amount,
                    rewardType = reward.rewardType,
                    rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType),
                    pgcId = GetPgcIdBy(taskId, eventId, reward.rewardType)
                };
                items.Add(item);
            }
            panel.ShowRewards(items);
            panel.ShowCommonBtn("确定", null);

            // 更新任务信息
            GetTaskData(taskId);
            claimRspData.eventList.ForEach(eventInfo =>
            {
                RefreshTaskData(taskId, eventInfo);
            });
            success?.Invoke(claimRspData);
            if (this.view != null)
            {
                this.view.RefreshUI(taskId);
            }
            TokenDataManager.Inst.GetTokenData();
            AccountDataManager.Inst.BalanceInfo.Refresh();
            // VipDataManager.Inst.UpdateVipStatus();
            // ReddotManagerUtils.Inst.RefreshRedDot();
        });
    }

    public bool IsDuringActivity()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        return now >= ACTIVITY_START_TIME && now <= ACTIVITY_END_TIME;
    }

    //10天（活动下线时如果倒计时没结束且奖励未全部领取，周年庆典入口不消失，右上角显示活动已结束）
    //活动入口是否消失
    public bool IsEntryOpen()
    {
        if (!IsDuringActivity())
        {
            return false;
        }
        if (IsAllRewardClaimed())
        {
            //提前领完 关闭入口
            return false;
        }
        if (!IsDuringPackEndDate())
        {
            //有未领取奖励 但超过倒计时时间 关闭入口
            return false;
        }
        return true;
    }

    public bool IsEntryRedDot(string param)
    {
        if (IsEntryOpen())
        {
            return false;
        }
        foreach (var taskId in new List<TASK_ID> { TaskId_1, TaskId_2, TaskId_3, TaskId_4, TaskId_5 })
        {
            var taskInfo = TaskId2InfoDict[taskId];
            if (taskInfo == null)
            {
                return false;
            }
        }
        return CheckHasReward();
    }

    public bool IsFuliRedDot()
    {
        if (IsEntryOpen())
        {
            return false;
        }
        var taskInfo = TaskId2InfoDict[TaskId_5];
        if (taskInfo == null)
        {
            return false;
        }
        return taskInfo.eventList.Count > 0 && taskInfo.eventList[0].eventStatus == 2;
    }

    public bool IsAllRewardClaimed()
    {
        var taskIds = new List<TASK_ID> { TaskId_1, TaskId_2, TaskId_3, TaskId_4, TaskId_5 };
        var paidPackageIds = new List<PaidPackageType> { PaidPackageType.DawnSurprise, PaidPackageType.SummerEnjoyment, PaidPackageType.CoolSummerEnjoyment, PaidPackageType.DeluxeSupplies };
        for (int i = 0; i < taskIds.Count; i++)
        {
            var taskInfo = TaskId2InfoDict[taskIds[i]];
            if (taskInfo == null)
            {
                continue;
            }
            if (i < paidPackageIds.Count)
            {
                var paidPackageInfo = PaidPackage2ItemDict[paidPackageIds[i]];
                if (paidPackageInfo == null)
                {
                    continue;
                }
                if (paidPackageInfo.isPaid == 0)
                {
                    continue;
                }
            }
            foreach (var eventInfo in taskInfo.eventList)
            {
                if (eventInfo.eventStatus != 3)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public bool CheckHasReward()
    {
        var taskIds = new List<TASK_ID> { TaskId_1, TaskId_2, TaskId_3, TaskId_4, TaskId_5 };
        var paidPackageIds = new List<PaidPackageType> { PaidPackageType.DawnSurprise, PaidPackageType.SummerEnjoyment, PaidPackageType.CoolSummerEnjoyment, PaidPackageType.DeluxeSupplies };
        for (int i = 0; i < taskIds.Count; i++)
        {
            var taskInfo = TaskId2InfoDict[taskIds[i]];
            if (taskInfo == null)
            {
                continue;
            }
            if (i < paidPackageIds.Count)
            {
                var paidPackageInfo = PaidPackage2ItemDict[paidPackageIds[i]];
                if (paidPackageInfo == null)
                {
                    continue;
                }
                if (paidPackageInfo.isPaid == 0)
                {
                    continue;
                }
            }
            foreach (var eventInfo in taskInfo.eventList)
            {
                if (eventInfo.eventStatus == 2)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public bool IsDuringPackEndDate()
    {
        // 将时间戳转换为DateTime进行比较
        if (SavePackTaskEndDate > 0)
        {
            DateTime endTime = TimestampToDateTime(SavePackTaskEndDate);
            return TcpTimeSystem.Inst.ServerDataTime < endTime;
        }
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
        return new List<ReddotType>() { ReddotType.AnniversaryCelebrationGift };
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }
}

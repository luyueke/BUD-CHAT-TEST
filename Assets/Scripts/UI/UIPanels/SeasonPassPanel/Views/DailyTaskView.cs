using System;
using System.Collections.Generic;
using Game.Event;
using Message;
using Newtonsoft.Json;
using SeasonPass;
using UnityEngine;
using UnityEngine.UI;

public class DailyTaskView : BaseTaskSubView {


    [SerializeField]
    private Text onlineTimeText;
    [SerializeField] private DailyTaskItem dailyTaskItemPrefab;
    [SerializeField] private DailyTaskRewardItem dailyRewardItemPrefab;
    [SerializeField] private DailyTaskRewardItem weeklyRewardItemPrefab;

    /// <summary>
    /// 日活跃度
    /// </summary>
    [SerializeField] private Text dailyActiveText;

    /// <summary>
    /// 周活跃度
    /// </summary>
    [SerializeField] private Text weeklyActiveText;
    private TASK_ID CurTaskId = TASK_ID.S15SeasonDaily;
    Dictionary<int, DailyTaskItem> dailyTaskItems = new Dictionary<int, DailyTaskItem>();
    Dictionary<int, DailyTaskRewardItem> dailyRewardItems = new Dictionary<int, DailyTaskRewardItem>();
    Dictionary<int, DailyTaskRewardItem> weeklyRewardItems = new Dictionary<int, DailyTaskRewardItem>(); 


    private List<int> dailyTaskEventIds = new List<int>() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 , 11 };
    private List<int> dailyRewardEventIds = new List<int>() { 12, 13, 14, 15, 16 };
    private List<int> weeklyRewardEventIds = new List<int>() { 17, 18 };

    private Dictionary<int, SeasonTaskEventInfo> seasonTaskEventInfos = new Dictionary<int, SeasonTaskEventInfo>();

    public RectTransform BGLight;
    public RectTransform BGLight2;

    public override void InitData(SeasonPassConfig config)
    {
        base.InitData(config);
        var configAsset = Loader.Load<TextAsset>(config.DailyTaskConfigPath, gameObject);
        var tmpEventInfos = JsonConvert.DeserializeObject<List<SeasonTaskEventInfo>>(configAsset.text);
        foreach (var eventInfo in tmpEventInfos) {
            seasonTaskEventInfos.Add(eventInfo.eventId, eventInfo);
        }
    }

    public override void RefreshData(SeasonPassListRsp rsp) {
        base.RefreshData(rsp);
        if (rsp != null) {
            onlineTimeText.SetLocalText("每日在线时长上限为60分钟（今日已在线{0}分钟）", rsp.todaySyncTime);
        }

    }


    public override void RefreshData(TaskListRsp rsp) {
        base.RefreshData(rsp);
        if (rsp == null || rsp.list == null) {
            return;
        }
        Vector2 offsetMax = BGLight.offsetMax;
        offsetMax.x = -650; 
        BGLight.offsetMax = offsetMax;
        offsetMax = BGLight.offsetMax;
        offsetMax.x = -500;
        BGLight2.offsetMax = offsetMax;
        var taskInfoData = rsp.list.Find(x => x.taskId == CurTaskId.ToString());
        foreach (var taskItemData in taskInfoData.eventList) {
            if (dailyTaskEventIds.Contains(taskItemData.eventId)) {
                if (!dailyTaskItems.TryGetValue(taskItemData.eventId, out var dailyTaskItem)) {
                    dailyTaskItem = Instantiate(dailyTaskItemPrefab, dailyTaskItemPrefab.transform.parent);
                    if (seasonTaskEventInfos.TryGetValue(taskItemData.eventId, out var eventInfo)) {
                        dailyTaskItem.gameObject.SetActive(true);
                        dailyTaskItem.Init(eventInfo, OnClaimed);
                    } else {
                        dailyTaskItem.gameObject.SetActive(true);
                    }
                    dailyTaskItems.Add(taskItemData.eventId, dailyTaskItem);
                }

                dailyTaskItem.SetData(taskItemData);
            }

            if (dailyRewardEventIds.Contains(taskItemData.eventId)) {
                if (!dailyRewardItems.TryGetValue(taskItemData.eventId, out var dailyRewardItem)) {
                    dailyRewardItem = Instantiate(dailyRewardItemPrefab, dailyRewardItemPrefab.transform.parent);
                    if (seasonTaskEventInfos.TryGetValue(taskItemData.eventId, out var eventInfo)) {
                        dailyRewardItem.Init(eventInfo.eventId, new CommonRewardItemData() {
                            rewardName = eventInfo.rewardList[0].rewardName,
                            RewardAmount = eventInfo.rewardList[0].rewardAmount,
                            rewardType = (int)eventInfo.rewardList[0].rewardType,
                            pgcId = eventInfo.rewardList[0].pgcId,
                        }, OnClaimed);
                        dailyRewardItem.SetProcess(eventInfo.spendNum);
                    }

                    dailyRewardItem.gameObject.SetActive(true);
                    dailyRewardItems.Add(taskItemData.eventId, dailyRewardItem);
                }

                dailyRewardItem.SetStatus((ClaimStatus)taskItemData.eventStatus);
                if ((ClaimStatus)taskItemData.eventStatus == ClaimStatus.Unlocked || (ClaimStatus)taskItemData.eventStatus == ClaimStatus.Claimed)
                {
                    offsetMax = BGLight.offsetMax;
                    offsetMax.x += 125;
                    BGLight.offsetMax = offsetMax;
                }
            }

            // 和后端协定 eventId 12 的 finishAmount 是日活跃度
            if (taskItemData.eventId == 12) {
                dailyActiveText.SetText(taskItemData.finishAmount.ToString());
                dailyActiveText.SetPreferredSize();
            }
        }

        // 周活跃奖励数据来自 S15SeasonWeeklyActive
        var weeklyActiveTaskData = rsp.list.Find(x => x.taskId == TASK_ID.S15SeasonWeeklyActive.ToString());
        if (weeklyActiveTaskData != null) {
            foreach (var taskItemData in weeklyActiveTaskData.eventList) {
                if (weeklyRewardEventIds.Contains(taskItemData.eventId)) {
                    if (!weeklyRewardItems.TryGetValue(taskItemData.eventId, out var weeklyRewardItem)) {
                        weeklyRewardItem = Instantiate(weeklyRewardItemPrefab, weeklyRewardItemPrefab.transform.parent);
                        if (seasonTaskEventInfos.TryGetValue(taskItemData.eventId, out var eventInfo)) {
                            weeklyRewardItem.Init(eventInfo.eventId, new CommonRewardItemData() {
                                rewardName = eventInfo.rewardList[0].rewardName,
                                RewardAmount = eventInfo.rewardList[0].rewardAmount,
                                rewardType = (int)eventInfo.rewardList[0].rewardType,
                                pgcId = eventInfo.rewardList[0].pgcId,
                            }, OnClaimed);
                            weeklyRewardItem.SetProcess(eventInfo.spendNum);
                        }

                        weeklyRewardItem.gameObject.SetActive(true);
                        weeklyRewardItems.Add(taskItemData.eventId, weeklyRewardItem);
                    }

                    weeklyRewardItem.SetStatus((ClaimStatus)taskItemData.eventStatus);
                    if ((ClaimStatus)taskItemData.eventStatus == ClaimStatus.Unlocked || (ClaimStatus)taskItemData.eventStatus == ClaimStatus.Claimed)
                    {
                        offsetMax = BGLight2.offsetMax;
                        offsetMax.x += 225;
                        BGLight2.offsetMax = offsetMax;
                    }
                }

                // 和后端协定 eventId 17 的 finishAmount 是周活跃度
                if (taskItemData.eventId == 17) {
                    weeklyActiveText.SetText(taskItemData.finishAmount.ToString());
                    weeklyActiveText.SetPreferredSize();
                }
            }
        }

        dailyRewardItemPrefab.gameObject.SetActive(false);
        weeklyRewardItemPrefab.gameObject.SetActive(false);
        dailyTaskItemPrefab.gameObject.SetActive(false);
    }

    private void OnClaimed(CommonRewardItem data) {
        OnClaimed(data.itemId);
    }

    private void OnClaimed(int eventId) {
        EventCenterDataManager.Inst.CliamReward(CurTaskId.ToString(), eventId, 1, 0,(rsp) => {
            OnClaimedSuccess(rsp, eventId);
        });
    }

    private void OnClaimedSuccess(TaskClaimRsp rsp, int eventId) {

        if (seasonTaskEventInfos.TryGetValue(eventId, out var eventInfo)) {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            var rewardList = new List<CommonRewardItemData>() { };
            foreach (var rewardInfo in eventInfo.rewardList) {
                var rewardItemData = new CommonRewardItemData();
                rewardItemData.rewardType = (int)rewardInfo.rewardType;
                rewardItemData.RewardAmount = rewardInfo.rewardAmount;
                rewardItemData.rewardName = rewardInfo.rewardName;
                rewardItemData.pgcId = rewardInfo.pgcId;
                rewardList.Add(rewardItemData);
            }
            panel.ShowRewards(rewardList);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            MessageHelper.Broadcast(MessageName.RefreshSeasonPass);
            MessageHelper.Broadcast(MessageName.RefreshSeasonPassTask);
        }
    }
}

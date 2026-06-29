using System;
using System.Collections.Generic;
using Game.Event;
using Message;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

public class SeasonTaskSubView : BaseTaskSubView
{


    [SerializeField]
    private Text onlineTimeText;


    [SerializeField]
    private SeasonTaskItem taskItemPrefab = null;

    private TASK_ID CurTaskId = TASK_ID.S15SeasonChallenge;

    private Dictionary<int, SeasonTaskItem> taskItems = new Dictionary<int, SeasonTaskItem>();
    private Dictionary<int, SeasonTaskEventInfo> seasonTaskEventInfos = new Dictionary<int, SeasonTaskEventInfo>();


    public override void InitData(SeasonPassConfig config)
    {
        base.InitData(config);
        var configAsset = Loader.Load<TextAsset>(config.SeasonTaskConfigPath, gameObject);
        var tmpEventInfos = JsonConvert.DeserializeObject<List<SeasonTaskEventInfo>>(configAsset.text);
        foreach (var eventInfo in tmpEventInfos)
        {
            seasonTaskEventInfos.Add(eventInfo.eventId, eventInfo);
        }
    }

    public override void RefreshData(SeasonPassListRsp rsp)
    {
        base.RefreshData(rsp);
        if (rsp == null)
        {
            return;
        }

        if(CurTaskId == TASK_ID.S14SeasonChallenge || CurTaskId == TASK_ID.S15SeasonChallenge)
        {
            onlineTimeText.SetLocalText("每日在线时长上限为120分钟（今日已在线{0}分钟）", rsp.todaySyncTime);
        }
        else if(CurTaskId == TASK_ID.S9AiGameSeasonChallenge) 
        {
            onlineTimeText.SetLocalText("今日已经游玩时长{0},每天最多累计120分钟", rsp.todaySyncTime);
        }
    }

    public override void RefreshData(TaskListRsp rsp)
    {
        base.RefreshData(rsp);
        if (rsp == null || rsp.list == null)
        {
            return;
        }

        var taskInfoData = rsp.list.Find(x => x.taskId == CurTaskId.ToString());
        foreach (var taskItemData in taskInfoData.eventList)
        {
            if (!taskItems.TryGetValue(taskItemData.eventId, out var dailyTaskItem))
            {
                dailyTaskItem = Instantiate(taskItemPrefab, taskItemPrefab.transform.parent);
                if (seasonTaskEventInfos.TryGetValue(taskItemData.eventId, out var eventInfo))
                {
                    dailyTaskItem.Init(eventInfo, OnClaimed);
                }
                dailyTaskItem.gameObject.SetActive(true);
                taskItems.Add(taskItemData.eventId, dailyTaskItem);
            }
            dailyTaskItem.SetData(taskItemData);

        }
        taskItemPrefab.gameObject.SetActive(false);

    }

    private void OnClaimed(int eventId)
    {
        EventCenterDataManager.Inst.CliamReward(CurTaskId.ToString(), eventId, 1, 0, (rsp) => {
            OnClaimedSuccess(rsp, eventId);
        });
    }

    private void OnClaimedSuccess(TaskClaimRsp rsp, int eventId)
    {

        if (seasonTaskEventInfos.TryGetValue(eventId, out var eventInfo))
        {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            var rewardList = new List<CommonRewardItemData>() { };
            foreach (var rewardInfo in eventInfo.rewardList)
            {
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

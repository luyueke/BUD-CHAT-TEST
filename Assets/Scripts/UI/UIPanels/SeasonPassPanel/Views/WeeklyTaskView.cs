using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Extensions;
using Game.Audio;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WeeklyTaskView : BaseTaskSubView
{
    [SerializeField] private WeeklyTaskItem weeklyTaskItemPrefab;

    [SerializeField] private WeeklyTaskRewardItem weeklyTaskRewardItem;

    private int selectWeeklyIndex = 0;
    private int currentUnlockWeeklyIndex = 0;
    private Dictionary<int, Toggle> toggles = new Dictionary<int, Toggle>();
    private SeasonPassListRsp seasonPassRsp;
    private TaskListRsp taskListRsp;

    [Header("每周任务数量 以及 编号")]
    public List<int> weeklyTaskIds = new List<int>() {
        1, 2, 3, 4, 5
    };



    private int weeklyRewardId = 6;

    public TASK_ID curTaskID = TASK_ID.S15SeasonWeekTask;

    private Dictionary<int, SeasonTaskEventInfo> taskInfos = new Dictionary<int, SeasonTaskEventInfo>();
    private Dictionary<int, WeeklyTaskItem> taskItems = new Dictionary<int, WeeklyTaskItem>();
    public RectTransform layoutRoot;
    
    public override void InitData(SeasonPassConfig config)
    {
        base.InitData(config);

        // if (curTaskID == TASK_ID.S15SeasonWeekTask)
        // {
        //     weeklyTaskIds = new List<int>() { 1, 2, 3, 4, 5, 6 };
        //     weeklyRewardId = 7;
        // }

        var configAsset = Loader.Load<TextAsset>(config.WeeklyTaskConfigPath, gameObject);
        JObject jObject = JObject.Parse(configAsset.text);
        if (jObject.TryGetValue(curTaskID.ToString(), out var taskInfoObj))
        {
            var taskEventInfos = taskInfoObj.ToObject<List<SeasonTaskEventInfo>>();
            foreach (var eventInfo in taskEventInfos)
            {
                taskInfos.Add(eventInfo.eventId, eventInfo);
            }
        }

        var tmpToggles = transform.Find("Scroll View/Viewport/WeeklyToggleGroup").GetComponentsInChildren<Toggle>();
        for (int i = 0; i < tmpToggles.Length; i++)
        {
            int weeklyIndex = i + 1;
            toggles.Add(weeklyIndex, tmpToggles[i]);
            tmpToggles[i].onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
                    OnSelectWeekly(weeklyIndex);
                }
            });
        }

    }


    public override void RefreshData(SeasonPassListRsp rsp)
    {
        base.RefreshData(rsp);
        if (rsp == null)
        {
            return;
        }

        seasonPassRsp = rsp;
        rsp.currentWeek = Math.Min(8, rsp.currentWeek);
        int weeklyIndex = 1;
        for (; weeklyIndex <= rsp.currentWeek; weeklyIndex++)
        {

            GameObjectEx.FindChildByName(toggles[weeklyIndex].transform, "Lock").gameObject.SetActive(false);
        }

        for (; weeklyIndex <= 8; weeklyIndex++)
        {
            GameObjectEx.FindChildByName(toggles[weeklyIndex].transform, "Lock").gameObject.SetActive(true);
        }

        currentUnlockWeeklyIndex = rsp.currentWeek;

        RefreshUI();
    }


    public override void RefreshData(TaskListRsp rsp)
    {
        base.RefreshData(rsp);
        if (rsp == null)
        {
            return;
        }

        taskListRsp = rsp;

        var taskInfoData = taskListRsp?.list?.Find(tmp => tmp.taskId == curTaskID.ToString());
        if (taskInfoData != null)
        {
            for (int i = 0, C = toggles.Count; i < C; i++)
            {
                var eventInfo = taskInfoData.eventList.FirstOrDefault(tmp => tmp.eventId == weeklyRewardId * (i + 1));
                if (eventInfo != null)
                {
                    GameObjectEx.FindComponentByName<Text>(toggles[i + 1].transform, "Value").SetText($"{eventInfo.finishAmount}/{weeklyTaskIds.Count}");

                }
            }


        }

        RefreshUI();
    }


    public override void OnShow()
    {
        base.OnShow();

        this.SetFrameCallBack(1, () =>
        {
            toggles[1].SetIsOnWithoutNotify(true);
            OnSelectWeekly(1);
            
        });
        LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
    }


    private void OnSelectWeekly(int weeklyIndex)
    {
        if (weeklyIndex > currentUnlockWeeklyIndex)
        {
            if (toggles.ContainsKey(selectWeeklyIndex))
            {
                toggles[selectWeeklyIndex].SetIsOnWithoutNotify(true);
            }
            TipPanel.ShowToast("任务未开放，请下周再来");
            return;
        }

        selectWeeklyIndex = weeklyIndex;

        RefreshUI();
    }
    void RefreshRedDot()
    {
        if (taskListRsp == null || taskListRsp.list == null)
        {
            return;
        }

        var taskInfoData = taskListRsp.list.Find(x => x.taskId == curTaskID.ToString());

        if (taskInfoData == null)
        {
            return;
        }

        int[] redDotHash = new int[10];
       foreach(var taskInfo in taskInfoData.eventList)
       {
            if(taskInfo.eventStatus == 2)
            {
                int weekIndex = (taskInfo.eventId - 1) / (weeklyTaskIds.Count + 1) + 1;
                redDotHash[weekIndex] += 1;
            }
       }
       for(int i = 1; i < 10; i++)
       {
            if (redDotHash[i] > 0) {
                if(toggles[i]!=null)
                GameObjectEx.FindComponentByName<Image>(toggles[i].transform, "RedDot").gameObject.SetActive(true);
            }
        }

    }
    private void RefreshUI()
    {
        if (taskListRsp == null || taskListRsp.list == null)
        {
            return;
        }

        var taskInfoData = taskListRsp.list.Find(x => x.taskId == curTaskID.ToString());
        if (taskInfoData == null)
        {
            return;
        }

        foreach (var itemKeyValue in taskItems)
        {
            itemKeyValue.Value.gameObject.SetActive(false);
        }

        foreach (var taskItemData in taskInfoData.eventList)
        {

            if ((taskItemData.eventId - 1) / (weeklyTaskIds.Count + 1) != selectWeeklyIndex - 1) continue;

            var index = (taskItemData.eventId - 1) % (weeklyTaskIds.Count + 1) + 1;
            if (weeklyTaskIds.Contains(index))
            {
                if (!taskItems.TryGetValue(taskItemData.eventId, out var dailyTaskItem))
                {
                    dailyTaskItem = Instantiate(weeklyTaskItemPrefab, weeklyTaskItemPrefab.transform.parent);
                    if (taskInfos.TryGetValue(taskItemData.eventId, out var eventInfo))
                    {
                        dailyTaskItem.Init(eventInfo, OnClaimed);
                    }
                    taskItems.Add(taskItemData.eventId, dailyTaskItem);
                }
                dailyTaskItem.gameObject.SetActive(true);
                dailyTaskItem.SetData(taskItemData);
                dailyTaskItem.SetUnlockUpgrade(seasonPassRsp != null && seasonPassRsp.paidType == 1);
            }

            if (index == weeklyRewardId &&
                taskInfos.TryGetValue(taskItemData.eventId, out var rewardEventInfo))
            {
                weeklyTaskRewardItem.Init(rewardEventInfo, OnClaimed);
                weeklyTaskRewardItem.SetData(taskItemData);
            }
        }
        weeklyTaskItemPrefab.gameObject.SetActive(false);
        RefreshRedDot();//更新红点显示
    }


    private void OnClaimed(int eventId)
    {
        EventCenterDataManager.Inst.CliamReward(curTaskID.ToString(), eventId, 1, 0, (rsp) =>
        {
            OnClaimedSuccess(rsp, curTaskID, eventId);

        });
    }

    private void OnClaimedSuccess(TaskClaimRsp rsp, TASK_ID taskID, int eventId)
    {
        if (taskID != curTaskID) return;
        if (taskInfos.TryGetValue(eventId, out var eventInfo))
        {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            var rewardList = new List<CommonRewardItemData>() { };
            var index = 0;
            foreach (var rewardInfo in eventInfo.rewardList)
            {
                // 判断是否购买，没购买只显示第一个奖励。
                if (!SeasonPassDataManager.Inst.GetSeasonPassIsPaid((SeasonPassType)_config.SeasonPassType) && index > 0) continue;
                var rewardItemData = new CommonRewardItemData();
                rewardItemData.rewardType = (int)rewardInfo.rewardType;
                rewardItemData.RewardAmount = rewardInfo.rewardAmount;
                rewardItemData.rewardName = rewardInfo.rewardName;
                rewardItemData.pgcId = rewardInfo.pgcId;
                rewardList.Add(rewardItemData);
                index++;
            }
            panel.ShowRewards(rewardList);
            AccountDataManager.Inst.BalanceInfo.Refresh();
            MessageHelper.Broadcast(MessageName.RefreshSeasonPass);
            MessageHelper.Broadcast(MessageName.RefreshSeasonPassTask);
        }


        //领取完后重新拉任务状态，更新红点
        var reqParam = new GetTaskListReq()
        {
            idList = new List<string>() {
                curTaskID.ToString()
                }
         };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST,
            JsonConvert.SerializeObject(reqParam), (content) => {
                taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);

                if (taskListRsp == null || taskListRsp.list == null)
                    return;

                RefreshData(taskListRsp);

            }, (error) => {

            });

        
    }
}

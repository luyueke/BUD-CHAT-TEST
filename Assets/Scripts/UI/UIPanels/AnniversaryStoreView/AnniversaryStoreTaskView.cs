using Es;
using Game.Event;
using Message;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnniversaryStoreTaskView : MonoBehaviour
{
    
    public Transform content;
    public AnniversaryStoreTaskItem taskItem;
    private List<AnniversaryStoreTaskItem> _generatedItems = new List<AnniversaryStoreTaskItem>();
    private List<TaskConfig> _taskConfigs; // 本地任务配置，在Start中加载
    //private Coroutine _refreshCoroutine;
    Action<int> _reddot;
    public void Init(Action<int>reddot)
    {
        _reddot = reddot;
        // 监听刷新消息
        MessageHelper.AddListener(MessageName.UpdateHallTask, ReferenceTask);
        // 首次获取数据
        ReferenceTask();
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.UpdateHallTask, ReferenceTask);
        //if (_refreshCoroutine != null)
        //{
        //    StopCoroutine(_refreshCoroutine);
        //}
    }

    /// <summary>
    // 刷新任务列表的统一入口
    /// </summary>
    public void ReferenceTask()
    {
        MessageHelper.Broadcast(MessageName.ReddotNotice);
        FetchAndRenderTasks();
    }

    /// <summary>
    /// 核心流程：异步获取网络数据，然后结合本地配置进行渲染
    /// </summary>
    private void FetchAndRenderTasks()
    {
        AnniversaryStoreMgr.Inst.GetStoreTaskData((daily, week,config) =>
        {
            _taskConfigs = config;
            GenderTaskItem(daily, week);
        });
    }

    /// <summary>
    /// 根据最新的数据和排序规则，生成或更新任务项UI
    /// </summary>
    void GenderTaskItem(List<TaskItemData> dailyList, List<TaskItemData> weeklyList)
    {
        Debug.Log($"dailyListCont:{dailyList.Count},weeklyListCont:{weeklyList.Count}");
        // 1. 清理所有旧的UI项
        foreach (var item in _generatedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        _generatedItems.Clear();

        // 如果本地配置为空，则不执行后续操作
        if (_taskConfigs == null || _taskConfigs.Count == 0)
        {
            Debug.LogWarning("[AnniversaryStoreTaskView] 本地任务配置为空，无法生成任何任务项。");
            return;
        }

        // 2. 定义多维度排序规则
        var sortedTaskConfigs = _taskConfigs.OrderBy(task => {
            TaskItemData serverData = (task.taskId == AnniversaryStoreMgr.Inst.dailyTaskId)
                ? dailyList.Find(x => x.eventId == task.eventId)
                : weeklyList.Find(x => x.eventId == task.eventId);

            var status = serverData != null ? (EventStatus)serverData.eventStatus : EventStatus.UnClaim;

            if (status == EventStatus.Claim) return 1;
            if (status == EventStatus.Finish) return 4;
            if (task.taskId == AnniversaryStoreMgr.Inst.dailyTaskId) return 2;
            if (task.taskId == AnniversaryStoreMgr.Inst.weekTaskId) return 3;
            return 99;

        }).ToList();
        int reddotNum = 0;
        // 3. 遍历排序后的列表来创建UI
        foreach (var task in sortedTaskConfigs)
        {
            TaskItemData serverData = (task.taskId == AnniversaryStoreMgr.Inst.dailyTaskId)
                ? dailyList.Find(x => x.eventId == task.eventId)
                : weeklyList.Find(x => x.eventId == task.eventId);

            var item = Instantiate(taskItem, content);
            if (item != null)
            {
                _generatedItems.Add(item);
                item.SetData(task, serverData);
            }
            if (serverData.eventStatus == 2)
            {
                reddotNum++;
            }
        }

        MessageHelper.Broadcast(MessageName.ReddotNotice);
        _reddot?.Invoke(reddotNum);
    }
}
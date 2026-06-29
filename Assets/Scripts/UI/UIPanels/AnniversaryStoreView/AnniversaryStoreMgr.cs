using Es;
using Game.Event;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnniversaryStoreMgr : GlobalInstance<AnniversaryStoreMgr>, IActivity
{
    public readonly string dailyTaskId = "S11CelebrationStoreDailyTask";
    public readonly string weekTaskId = "S11CelebrationStoreWeeklyTask";

    List<TaskItemData> _daylyServerEventList = new List<TaskItemData>();
    List<TaskItemData> _weekServerEventList = new List<TaskItemData>();

    public ActivityInfo _exchangeActivityInfo;
    public List<int> reddotNums = new List<int> {0,0,0};
    List<BaseLimitPackageData> _packgeData;
    public void InitData() {
        GetStoreTaskData(null);
        GetStoreExchangeData(null);
        GetStoreGiftData();
        RedDotSystemNew.Inst.AddReddotType(ReddotType.AnniversaryCelebrationStore , RedDot);
        ActivityManager.Inst.AddActivity(ActivityId.S11CelebrationStore, this);
    }
    public List<ReddotType> GetReddotTypes()
    {
        return new List<ReddotType> { ReddotType.AnniversaryCelebrationStore };
    }
    public void GetStoreTaskData(Action<List<TaskItemData> , List<TaskItemData> , List<TaskConfig>> callBack)
    {
        // --- 恢复到原始逻辑：在Start中加载本地配置 ---
        var configs = Es.DataTables.GetTaskConfigList();
        List<TaskConfig> _taskConfigs;
        if (configs == null || configs.Count == 0)
        {
            Debug.LogError("[AnniversaryStoreTaskView] 严重错误: 在Start时未能从Es.DataTables获取到任何任务配置！");
            _taskConfigs = new List<TaskConfig>(); // 初始化为空列表，防止后续出错
        }
        else
        {
            _taskConfigs = configs.Where(config => config.taskId == dailyTaskId || config.taskId == weekTaskId).ToList();
        }
        List<TaskItemData> daylyServerEventList = new List<TaskItemData>();
        List<TaskItemData> weekServerEventList = new List<TaskItemData>();
        IAPDataManager.Inst.GetTaskList(dailyTaskId, (b, taskInfoResponse) =>
        {
            if (b && taskInfoResponse?.list?.Count > 0 && taskInfoResponse.list[0]?.eventList != null)
            {
                daylyServerEventList = taskInfoResponse.list[0].eventList;
                _daylyServerEventList = daylyServerEventList;
            }

            IAPDataManager.Inst.GetTaskList(weekTaskId, (b, taskInfoResponse) =>
            {
                if (b && taskInfoResponse?.list?.Count > 0 && taskInfoResponse.list[0]?.eventList != null)
                {
                    weekServerEventList = taskInfoResponse.list[0].eventList;
                    _weekServerEventList = weekServerEventList;
                }

                // --- 步骤2: 统一渲染UI ---
                callBack?.Invoke(daylyServerEventList, weekServerEventList , _taskConfigs);
            });
        });
        reddotNums[0] = 0;
        foreach (var task in daylyServerEventList)
        {
            if(task.eventStatus == (int)EventStatus.Claim)
            {
                reddotNums[0] += 1;
            }
        }
        foreach (var task in weekServerEventList)
        {
            if (task.eventStatus == (int)EventStatus.Claim)
            {
                reddotNums[0] += 1;
            }
        }
    }
    public bool RedDot(string param)

    {
        // 在访问任何对象之前添加空值检查
        if (_daylyServerEventList == null || _daylyServerEventList.Count<=0)
        {
            return false;
        }

        // 如果有其他可能为null的对象，也要检查
        if (_weekServerEventList == null || _weekServerEventList.Count <= 0)
        {
            return false;
        }
        foreach (var task in _daylyServerEventList){
            if(task.eventStatus == 2)
            {
                return true;
            }
        }
        foreach (var task in _weekServerEventList)
        {
            if (task.eventStatus == 2)
            {
                return true;
            }
        }
        var freeGiftData = GetStoreGiftData().Find(x => x.price == 0);
        if (freeGiftData == null)
        {
            return false;
        }
        return freeGiftData?.isPurchase != 1;
    }
    public void GetStoreExchangeData(Action<ActivityInfo> callBack)
    {
        ActivityCenterInfoReq req = new ActivityCenterInfoReq() { idList = new List<string> { "S11CelebrationStore" } };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        {
            ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
            if (activityResponse?.list == null || activityResponse.list.Count == 0) { return; }
            _exchangeActivityInfo = activityResponse.list[0];
            callBack?.Invoke(_exchangeActivityInfo);
        },
        (error) => { });
    }
    public List<BaseLimitPackageData> GetStoreGiftData() {

        var packageData = IAPDataManager.Inst.GetLimitedRechargeData();
        if (packageData == null || packageData.celebrationPackage == null)
        {
            return null;
        }
        _packgeData = packageData.celebrationPackage;
        reddotNums[2] = 0;
        foreach (var data in _packgeData)
        {
            // 如果价格不为0，则是普通付费礼包，正常生成Item
            if (data.price == 0&& data.isPurchase != 1)
            {
                reddotNums[2] += 1;
            }
        }
        return _packgeData;
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }

    public bool IsOpen()
    {
        return true;
    }

    public void ShowPanel()
    {

    }
}

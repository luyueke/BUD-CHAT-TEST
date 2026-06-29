using Es;
using Game.Event;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AiScriptKillPanel : ActivityBaseView
{
    //UI组件
    public Text sumTex;
    public Slider sumSlider;
    public Transform taskContent;
    public Transform accountContent;
    public Toggle[] toggles;

    //item预制体
    public AiScriptKillAccountItem accountItem;
    public AiScriptKillTaskItem taskItem;

    //缓存生成item
    private List<AiScriptKillAccountItem> accountItems = new List<AiScriptKillAccountItem>();
    private List<AiScriptKillTaskItem> taskItems = new List<AiScriptKillTaskItem>();
    private string _activytyId;

    //服务器数据
    private ActivityInfo _activityInfo;
    private List<ActivityEventInfo> _localEventList;

    //各个奖励的解锁值，用于设置进度条value的权重
    private readonly List<int> rewardValues = new List<int> { 0, 40, 80, 120, 160, 200 };
    private int claimSum = 0;
    private int currType = 1;

    public override void Init(ActivityInfo info)
    {
        base.Init(info);
        _activityInfo = info;
        _activytyId = info.activityId;
        _localEventList = info.eventList;
        InitToggle();
    }

    private void UpdateRedDots()
    {
        bool hasClaimableTask1 = false;
        bool hasClaimableTask2 = false;

        foreach (var task in _activityInfo.eventList)
        {
            if (task.eventStatus == (int)EventStatus.Claim)
            {
                if (task.eventId <= 3)
                {
                    hasClaimableTask1 = true;
                }
                else if(task.eventId <= 7)
                {
                    hasClaimableTask2 = true;
                }
            }
        }

        toggles[0].transform.Find("RedDot").gameObject.SetActive(hasClaimableTask1);
        toggles[1].transform.Find("RedDot").gameObject.SetActive(hasClaimableTask2);
    }

    void InitToggle()
    {
        // 先移除之前可能存在的监听器，防止重复添加
        for (int i = 0; i < toggles.Length; i++)
        {
            toggles[i].onValueChanged.RemoveAllListeners();
        }

        // 添加新的监听器
        for (int i = 0; i < toggles.Length; i++)
        {
            int index = i + 1;
            toggles[i].onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    GenderTaskItem(index);
                }
            });
        }

        // 先更新红点状态，再选中默认的toggle
        UpdateRedDots();
        toggles[currType - 1].isOn = true;
        GenderTaskItem(currType);
    }

    void GenderTaskItem(int selectType)
    {
        currType = selectType;
        List<int> taskList;
        if (selectType == 1)
        {
            taskList = new List<int> { 1, 2, 3 };
        }
        else
        {
            taskList = new List<int> { 4, 5, 6, 7 };
        }
        taskItems.Clear();

        // 先清空所有子物体
        foreach (Transform child in taskContent)
        {
            Destroy(child.gameObject);
        }

        // 再创建新的物品
        foreach (var task in taskList)
        {
            var localData = _localEventList.Find(x => x.eventId == task);
            var serverdata = _activityInfo.eventList.Find(x => x.eventId == task);
            var item = Instantiate(taskItem, taskContent);
            taskItems.Add(item);
            item.SetData(localData, serverdata, _activytyId, RefrashData);
        }
    }

    void GenderAccountItem()
    {
        List<int> accountList = new List<int> { 8, 9, 10, 11, 12 };
        accountItems.Clear();
        foreach (Transform child in accountContent) Destroy(child.gameObject);
        foreach (var task in accountList)
        {
            var localData = _localEventList.Find(x => x.eventId == task);
            var serverdata = _activityInfo.eventList.Find(x => x.eventId == task);
            var item = Instantiate(accountItem, accountContent);
            accountItems.Add(item);
            item.SetData(localData, serverdata, _activytyId, RefrashData);
        }
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        _activityInfo = info;
        int sum = 0;
        foreach (var even in _activityInfo.eventList)
        {
            if (even.eventStatus == (int)EventStatus.Finish)
            {
                sum += _localEventList.Find(x => x.eventId == even.eventId).rewardNum;
            }
        }
        sumTex.text = info.currencyAmount.ToString();
        sumSlider.value = CalculateProgressValue(info.currencyAmount);
        GenderTaskItem(currType);
        GenderAccountItem();
        UpdateRedDots();  // 更新所有红点状态
    }

    void RefrashData(ActivityEventClaimResponse info)
    {
        claimSum+=info.eventInfo.finishAmount;
        sumSlider.value = CalculateProgressValue(_activityInfo.currencyAmount + claimSum);
        sumTex.text = (_activityInfo.currencyAmount + claimSum).ToString();

        int index = _activityInfo.eventList.FindIndex(x => x.eventId == info.eventInfo.eventId);
        if (index != -1)
        {
            _activityInfo.eventList[index].eventStatus = info.eventInfo.eventStatus;
        }

        UpdateRedDots();  // 更新所有红点状态
    }

    private readonly List<(int value, float progress)> progressPoints = new List<(int, float)>
    {
        (0, 0f),
        (40, 0.121f),
        (80, 0.331f),
        (120, 0.533f),
        (160, 0.738f),
        (200, 0.943f),
        (250, 1f)
    };

    // 根据当前累积值计算进度条位置
    private float CalculateProgressValue(int currentSum)
    {
        if (currentSum <= 0) return 0f;
        if (currentSum >= 250) return 1f;

        // 找到当前累积值所在的区间
        for (int i = 1; i < progressPoints.Count; i++)
        {
            var prevPoint = progressPoints[i - 1];
            var nextPoint = progressPoints[i];

            if (currentSum <= nextPoint.value)
            {
                // 计算在当前区间内的相对进度
                float t = (currentSum - prevPoint.value) / (float)(nextPoint.value - prevPoint.value);
                // 使用线性插值计算实际进度
                return Mathf.Lerp(prevPoint.progress, nextPoint.progress, t);
            }
        }

        return 1f;
    }
}
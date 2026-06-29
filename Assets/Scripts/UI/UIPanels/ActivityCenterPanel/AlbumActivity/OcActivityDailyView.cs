using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class OcActivityDailyView : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject subTask;
    [SerializeField] private OcActivityDailyViewItem _viewItem;
    [SerializeField] private List<OcActivityTaskButton> _dailyViewBtns;
    [SerializeField] private RectTransform _scroContentRect;
    [SerializeField] private RectTransform _viewPort;
    
    private List<OcActivityDailyViewItem> _dailyItems = new List<OcActivityDailyViewItem>();
    private List<OcActivityDailyViewItem> _grandItems = new List<OcActivityDailyViewItem>();
    private bool isSending = false;
    private string activityId;
    private CameraActivityTaskType _currentTabType = CameraActivityTaskType.DailyTask;

    public Action<ActivityEventClaimResponse> ClaimSuccessAction;

    //private Dictionary<CameraActivityTaskType, List<int>> _taskTypeIds = new Dictionary<CameraActivityTaskType, List<int>>()
    //    {
    //        [CameraActivityTaskType.DailyTask] = new List<int>() { 1, 2, 3, 4, 5,6 },
    //        [CameraActivityTaskType.CumTask] = new List<int>() { 7, 8, 9, 10},
    //    //    [CameraActivityTaskType.HumanTask] = new List<int>() { 6, 7, 8, 9, },
    //    //[CameraActivityTaskType.PetTask] = new List<int>() { 10, 11, 12, 13 },
    //    //    [CameraActivityTaskType.PropTask] = new List<int>() { 14, 15, 16, 17 },
    //    };

    //private List<CameraActivityTaskType> _unClaimList = new List<CameraActivityTaskType>();
    //private List<int> _unClaimTaskList = new List<int>();
    
    public void InitListUI(List<ActivityEventInfo> datas, string id)
    {
        activityId = id;
        if (datas == null || datas.Count == 0)
        {
            return;
        }

        InitDailyBtn();
        for (int i = 0; i < 8; i++)
        {
            var obj = Instantiate(_viewItem, content);
            obj.transform.localScale = Vector3.one;
            OcActivityDailyViewItem itemComp = obj.GetComponent<OcActivityDailyViewItem>();
            itemComp.Init(i, datas[i], ClaimReward);
            _dailyItems.Add(itemComp);
        }
        for (int i = 0; i < 15; i++)
        {
            var obj = Instantiate(_viewItem, content);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(false);
            OcActivityDailyViewItem itemComp = obj.GetComponent<OcActivityDailyViewItem>();
            itemComp.Init(i, datas[i + 8], ClaimReward);
            _grandItems.Add(itemComp);
        }

        Sort(_dailyItems);
        OnTaskTypeBtnClick(CameraActivityTaskType.DailyTask);
    }


    private void InitDailyBtn()
    {
        _dailyViewBtns.ForEach(x =>
        {
            x.InitData(OnTaskTypeBtnClick);
        });
    }

    private void OnTaskTypeBtnClick(CameraActivityTaskType type)
    {
        //if (type == CameraActivityTaskType.DailyTask)
        //{
        //    subTask.SetActive(false);
        //    _scroContentRect.anchoredPosition = Vector3.zero;
        //    _viewPort.offsetMin = Vector2.zero;
        //}
        //else if (type == CameraActivityTaskType.CumTask)
        //{
        //    subTask.SetActive(true);
        //    type = CameraActivityTaskType.HumanTask;
        //    _scroContentRect.anchoredPosition = new Vector3(0, -80, 0);
        //    _viewPort.offsetMin = new Vector2(0, 80);
        //}
        _currentTabType = type;
        _dailyViewBtns.ForEach(x=>x.SetSelectState(false));
        var btn = _dailyViewBtns.Find(x => x.CurTaskType == type);
        btn.SetSelectState(true);

        //if (type == CameraActivityTaskType.HumanTask || 
        //    type == CameraActivityTaskType.PetTask ||
        //    type == CameraActivityTaskType.PropTask)
        //{
        //    var dailyTask = _dailyViewBtns.Find(x => x.CurTaskType == CameraActivityTaskType.CumTask);
        //    dailyTask.SetSelectState(true);
        //}

        bool showDaily = type == CameraActivityTaskType.DailyTask;

        _dailyItems.ForEach(x => x.gameObject.SetActive(showDaily));

        if (showDaily)
        {
            _grandItems.ForEach(x => x.gameObject.SetActive(false));
        }
        else
        {
            RefreshGrandGroupVisibility();
        }
    }

    public void RefreshListUI(List<ActivityInfo> datas)
    {
        if (datas == null || datas.Count == 0)
        {
            return;
        }
        _dailyViewBtns.ForEach(x => x.SetReddotEnable(false));

        var data = datas.Find(x => x.activityId == ActivityId.S15TheaterDaily.ToString());
        foreach (var item in _dailyItems)
        {
            var d = data.eventList.Find(x => x.eventId == item.EventId);
            if (d == null)
            {
                continue;
            }
            item.RefrashData(d);
            if(d.eventStatus == (int)TaskClaimState.Enable)
            {
                var btn = _dailyViewBtns.Find(x => x.CurTaskType == CameraActivityTaskType.DailyTask);
                btn.SetReddotEnable(true);
            }
        }
        foreach (var item in _grandItems)
        {
            var d = data.eventList.Find(x => x.eventId == item.EventId);
            if (d == null)
            {
                continue;
            }
            item.RefrashData(d);
            if (d.eventStatus == (int)TaskClaimState.Enable)
            {
                var btn = _dailyViewBtns.Find(x => x.CurTaskType == CameraActivityTaskType.CumTask);
                btn.SetReddotEnable(true);
            }
        }
        // data = datas.Find(x => x.activityId == ActivityId.S15TheaterDailyGrandTotal.ToString());
        //// Debug.LogError($"refeash 000 d=" + JsonConvert.SerializeObject(data));
        // foreach (var item in _grandItems)
        // {
        //     var d = data.eventList.Find(x => x.eventId == item.EventId);
        //   //  Debug.LogError($"refeash 111 name={item.EventName}, id={item.EventId} ,d=" + JsonConvert.SerializeObject(d));
        //     if (d == null)
        //     {
        //         continue;
        //     }
        //     //Debug.LogError($"refeash 222  name={item.EventName}, id={item.EventId} ,d=" + JsonConvert.SerializeObject(d));
        //     item.RefrashData(d);
        //     if (d.eventStatus == (int)TaskClaimState.Enable)
        //     {
        //         var btn = _dailyViewBtns.Find(x => x.CurTaskType == CameraActivityTaskType.CumTask);
        //         btn.SetReddotEnable(true);
        //     }
        // }

        Sort(_dailyItems);
        RefreshGrandGroupVisibility();
        //RefreshReddot(datas);
    }

    private void RefreshGrandGroupVisibility()
    {
        if (_currentTabType == CameraActivityTaskType.DailyTask)
        {
            _grandItems.ForEach(x => x.gameObject.SetActive(false));
            return;
        }

        int groupCount = _grandItems.Count / 3;
        for (int g = 0; g < groupCount; g++)
        {
            var items = new[]
            {
                _grandItems[g * 3],
                _grandItems[g * 3 + 1],
                _grandItems[g * 3 + 2]
            };

            bool allFinished = items[0].EventStatus == (int)TaskClaimState.Finished
                            && items[1].EventStatus == (int)TaskClaimState.Finished
                            && items[2].EventStatus == (int)TaskClaimState.Finished;

            if (allFinished)
            {
                foreach (var item in items)
                    item.gameObject.SetActive(true);
            }
            else
            {
                bool foundCurrent = false;
                foreach (var item in items)
                {
                    bool show = !foundCurrent && item.EventStatus != (int)TaskClaimState.Finished;
                    item.gameObject.SetActive(show);
                    if (show) foundCurrent = true;
                }
            }
        }

        SortGrandGroups();
    }

    private void SortGrandGroups()
    {
        int groupCount = _grandItems.Count / 3;
        for (int g = 0; g < groupCount; g++)
        {
            var items = new[]
            {
                _grandItems[g * 3],
                _grandItems[g * 3 + 1],
                _grandItems[g * 3 + 2]
            };

            bool allFinished = items[0].EventStatus == (int)TaskClaimState.Finished
                            && items[1].EventStatus == (int)TaskClaimState.Finished
                            && items[2].EventStatus == (int)TaskClaimState.Finished;

            if (allFinished)
            {
                // 整组已领取 → 移到列表末尾（正序调用，末尾顺序保持）
                foreach (var item in items)
                    item.transform.SetAsLastSibling();
            }
            else
            {
                var visible = System.Array.Find(items, x => x.EventStatus != (int)TaskClaimState.Finished);
                if (visible != null && visible.EventStatus == (int)TaskClaimState.Enable)
                {
                    // 当前可领取 → 移到列表最前（倒序调用，顺序保持）
                    for (int i = items.Length - 1; i >= 0; i--)
                        items[i].transform.SetAsFirstSibling();
                }
            }
        }
    }

    private void Sort(List<OcActivityDailyViewItem> list)
    {
        foreach (var item in list)
        {
            if(item.EventStatus == (int)TaskClaimState.Enable)
            {
                item.transform.SetAsFirstSibling();
            }
            else if (item.EventStatus == (int)TaskClaimState.Finished)
            {
                item.transform.SetAsLastSibling();
            }
        }
    }

    public void RefreshReddot(List<TaskInfoData> eventInfos)
    {
        //_unClaimList.Clear();   
        //_unClaimTaskList.Clear();
        //_dailyViewBtns.ForEach(x=>x.SetReddotEnable(false));
        

        //foreach (var eventInfo in eventInfos)
        //{
        //    if (eventInfo.eventList[0].eventStatus == (int)ClaimStatus.Unlocked)
        //    {
        //        _unClaimTaskList.Add(eventInfo.eventList[0].eventId);
        //    }
        //}
        
        //_unClaimList = _taskTypeIds
        //    .Where(kvp => kvp.Value.Any(id => _unClaimTaskList.Contains(id)))
        //    .Select(kvp => kvp.Key)
        //    .ToList();
        
        //_dailyViewBtns.ForEach(x =>
        //{
        //    if (_unClaimList.Contains(x.CurTaskType))
        //    {
        //        x.SetReddotEnable(true);
                
        //        //if (x.CurTaskType == CameraActivityTaskType.HumanTask || 
        //        //    x.CurTaskType == CameraActivityTaskType.PetTask ||
        //        //    x.CurTaskType == CameraActivityTaskType.PropTask)
        //        //{
        //        //    var dailyTask = _dailyViewBtns.Find(x => x.CurTaskType == CameraActivityTaskType.CumTask);
        //        //    dailyTask.SetReddotEnable(true);
        //        //}
        //    }
        //});
    }

    private void ShowRewardPanel(ActivityEventClaimResponse data )
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardList = new List<CommonRewardItemData>();
        CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(OcActivityView.atlasPath, "icon", gameObject); ;
        commonRewardItemData.rewardName = "活跃度";
        commonRewardItemData.RewardAmount =  data.claimAmount;
        rewardList.Add(commonRewardItemData);

        panel.ShowRewards(rewardList);
    }
    private void ClaimReward(ActivityEventInfo data)
    {
        //ShowRewardPanel(data);
        //return;
        if (data == null)
        {
            return;
        }

        if (data.eventStatus != (int)EventStatus.Claim)
        {
            return;
        }

        //TASK_ID taskid = (data.groupId == 0) ? TASK_ID.S15TheaterDaily : TASK_ID.S15TheaterDailyGrandTotal;
        //EventCenterDataManager.Inst.CliamReward(taskid.ToString(), data.eventId, 1,0, (claimRspData) =>
        //  {
        //      ShowRewardPanel(data);
        //      MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
        //  }, () => {
        //      Debug.LogError("相机活动，任务领取失败 ");
        //  });

        //return;

        if (string.IsNullOrEmpty(activityId))
        {
            return;
        }

        if (isSending)
        {
            return;
        }
        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = activityId,
            ["eventId"] = data.eventId
        };
        ShowLoading(data.eventId, data.groupId, true);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ShowLoading(data.eventId, data.groupId, false);
                ActivityEventClaimResponse avtivityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(avtivityEventClaimResponse);
            },
            (error) =>
            {
                ShowLoading(data.eventId, data.groupId, false);
                isSending = false;
            });
    }

    private void ShowLoading(int eventId, int groupId, bool isShow)
    {
        var _viewItems = groupId == 0 ? _dailyItems : _grandItems;
        var tmpItemView = _viewItems.Find(x => x.EventId == eventId);
        if (tmpItemView == null)
        {
            return;
        }
        if (isShow)
        {
            tmpItemView.CliamStart();
        }
        else
        {
            tmpItemView.CliamCallBack();
        }
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        if (response?.eventInfo != null)
        {
            var claimedItem = _dailyItems.Find(x => x.EventId == response.eventInfo.eventId)
                           ?? _grandItems.Find(x => x.EventId == response.eventInfo.eventId);
            if (claimedItem?._info != null)
            {
                claimedItem._info.eventStatus = (int)TaskClaimState.Finished;
                claimedItem.InitData(claimedItem._info);
            }
            Sort(_dailyItems);
            RefreshGrandGroupVisibility();
        }
        ShowRewardPanel(response);
        ClaimSuccessAction?.Invoke(response);
    }
}

public enum CameraActivityTaskType
{
    DailyTask = 0,
    CumTask = 1,
    //HumanTask = 2,
    //PetTask = 3,
    //PropTask = 4,
}


using System;
using System.Collections.Generic;
using System.Linq;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.Serialization;

public class CommonDailyView : MonoBehaviour {
    [SerializeField] private CommonDailyViewItem viewItemPrefab;
    [SerializeField] private List<CommonDailyTaskButton> dailyViewBtns;

    private List<CommonDailyViewItem> viewItems = new List<CommonDailyViewItem>();
    private bool isSending = false;
    private string activityId;

    public Action<ActivityEventClaimResponse> ClaimSuccessAction;

    /// <summary>
    /// 继承类自定义
    /// </summary>
    protected virtual Dictionary<CommonTaskType, List<int>> _taskTypeIds => new Dictionary<CommonTaskType, List<int>>() {
        [CommonTaskType.DailyTask] = new List<int>() { 1, 2, 3, },
        [CommonTaskType.OtherTask] = new List<int>() { 4, 5, 6 },
    };

    private List<CommonTaskType> _unClaimList = new List<CommonTaskType>();
    private List<int> _unClaimTaskList = new List<int>();

    public void InitListUI(List<ActivityEventInfo> infos, string id, Action<ActivityEventClaimResponse> onClaimSuccess) {
        activityId = id;
        ClaimSuccessAction = onClaimSuccess;
        if (infos == null || infos.Count == 0) {
            return;
        }

        InitDailyBtn();

        foreach (var keyValue in _taskTypeIds) {
            foreach (var tmpEventId in keyValue.Value) {
                var eventInfo = infos.FirstOrDefault(tmp => tmp.eventId == tmpEventId);
                if (eventInfo != null) {
                    var obj = Instantiate(viewItemPrefab, viewItemPrefab.transform.parent);
                    obj.transform.localScale = Vector3.one;
                    obj.gameObject.SetActive(true);
                    CommonDailyViewItem itemComp = obj.GetComponent<CommonDailyViewItem>();
                    itemComp.Init(eventInfo, ClaimReward);
                    viewItems.Add(itemComp);
                }
            }
        }

        viewItemPrefab.gameObject.SetActive(false);
        OnTaskTypeBtnClick(CommonTaskType.DailyTask);
    }

    private void InitDailyBtn() {
        dailyViewBtns.ForEach(x => {
            x.InitData(OnTaskTypeBtnClick);
        });
    }

    protected virtual void OnTaskTypeBtnClick(CommonTaskType type) {

        dailyViewBtns.ForEach(x => x.SetSelectState(false));
        var btn = dailyViewBtns.Find(x => x.CurTaskType == type);
        btn.SetSelectState(true);


        viewItems.ForEach(x => {
            if (_taskTypeIds[type].Contains(x.EventId)) {
                x.gameObject.SetActive(true);
            } else {
                x.gameObject.SetActive(false);
            }
        });
    }

    public void RefreshListUI(List<ActivityEventInfo> datas) {
        if (datas == null || datas.Count == 0) {
            return;
        }

        foreach (var viewItem in viewItems) {
            var data = datas.Find(x => x.eventId == viewItem.EventId);
            if (data == null) {
                continue;
            }

            viewItem.RefreshData(data);
        }

        RefreshReddot(datas);
    }
    public void UPdateStatus(List<ActivityEventInfo> datas)
    {

        foreach (var viewItem in viewItems)
        {
            var data = datas.Find(x => x.eventId == viewItem.EventId);
            if (data == null)
            {
                continue;
            }

            viewItem.UpdateUI(data);
        }

    }

    public void RefreshReddot(List<ActivityEventInfo> eventInfos) {
        _unClaimList.Clear();
        _unClaimTaskList.Clear();
        dailyViewBtns.ForEach(x => x.SetReddotEnable(false));

        foreach (var eventInfo in eventInfos) {
            if (eventInfo.eventStatus == (int)ClaimStatus.Unlocked) {
                _unClaimTaskList.Add(eventInfo.eventId);
            }
        }

        _unClaimList = _taskTypeIds
            .Where(kvp => kvp.Value.Any(id => _unClaimTaskList.Contains(id)))
            .Select(kvp => kvp.Key)
            .ToList();

        dailyViewBtns.ForEach(x => {
            if (_unClaimList.Contains(x.CurTaskType)) {
                x.SetReddotEnable(true);
            }
        });
    }


    private void ClaimReward(ActivityEventInfo data) {
        if (string.IsNullOrEmpty(activityId)) {
            return;
        }

        if (isSending) {
            return;
        }

        isSending = true;

        JObject jObject = new JObject() {
            ["activityId"] = activityId,
            ["eventId"] = data.eventId
        };
        ShowLoading(data.eventId, true);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) => {
                ShowLoading(data.eventId, false);
                ActivityEventClaimResponse avtivityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                ClaimSuccessAction?.Invoke(avtivityEventClaimResponse);
            },
            (error) => {
                ShowLoading(data.eventId, false);
                isSending = false;
            });
    }

    private void ShowLoading(int eventId, bool isShow) {
        var tmpItemView = viewItems.Find(x => x.EventId == eventId);
        if (tmpItemView == null) {
            return;
        }

        if (isShow) {
            tmpItemView.ClaimStart();
        } else {
            tmpItemView.ClaimCallBack();
        }
    }
#if UNITY_EDITOR
    private void Reset() {
        viewItemPrefab = GameObjectEx.FindComponentByName<CommonDailyViewItem>(transform, "Scroll View/Viewport/EventContent/DailyEventItem");
        dailyViewBtns = transform.GetComponentsInChildren<CommonDailyTaskButton>(true).ToList();
    }
#endif

}

public enum CommonTaskType {
    // 每日任务
    DailyTask = 0,

    // 其他，每周、成就、等
    OtherTask = 1,
}

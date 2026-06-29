using System;
using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;

public class AnimationStudioCumulativeView : MonoBehaviour {

    [SerializeField] private Transform tabViewContent;
    [SerializeField] private AnimationStudioTabItemView tabItemView;

    [SerializeField] private Transform mainViewContent;
    [SerializeField] private AnimationStudioItemView mainItemView;

    private List<AnimationStudioItemView> eventViews = new List<AnimationStudioItemView>();

    private List<string> tabNames = new List<string>() {  "动作发布", "姿势发布", "使用动作", "音效发布", "动作购买" };
    private string activeCategoryKey;
    private string ActivityId;
    private Dictionary<string, AnimationStudioTabItemView> _tabItemInfo = new Dictionary<string, AnimationStudioTabItemView>();

    private Dictionary<AnimationEventGroup, List<ActivityEventInfo>> groupDatas =
        new Dictionary<AnimationEventGroup, List<ActivityEventInfo>>();

    public Action<AnimationEventGroup, ActivityEventClaimResponse> ClaimSuccessAction;
    private bool isSending = false;


    public void InitListUI(Dictionary<AnimationEventGroup, List<ActivityEventInfo>> datas, string activityId)
    {
        ActivityId = activityId;
        this.groupDatas = datas;
        if (groupDatas == null || groupDatas.Count == 0)
        {
            return;
        }

        for (int i = 0; i < tabNames.Count; i++)
        {
            var key = tabNames[i];
            var itemView = Instantiate(tabItemView, tabViewContent);
            itemView.gameObject.SetActive(true);
            itemView.SetData(key, OnCategoryItemClick);
            _tabItemInfo[key] = itemView;
        }

        if (tabNames.Count > 0)
        {
            OnCategoryItemClick(tabNames[0]);
        }

        RefreshRedDot();
    }

    public void RefreshListUI(Dictionary<AnimationEventGroup, List<ActivityEventInfo>> datas)
    {
        if (datas == null || datas.Count == 0)
        {
            return;
        }
        this.groupDatas = datas;
        RefreshRedDot();
        var index = tabNames.FindIndex(x => x == activeCategoryKey) + 1;
        var list = groupDatas[(AnimationEventGroup)index];
        RefreshUI(list);
    }

    private void OnCategoryItemClick(string key)
    {
        if (isSending)
        {
            return;
        }

        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!string.IsNullOrEmpty(activeCategoryKey) && activeCategoryKey == key)
        {
            return;
        }

        activeCategoryKey = key;

        foreach (var element in _tabItemInfo)
        {
            element.Value.UpdateSelected(element.Key == key);
        }

        var index = tabNames.FindIndex(x => x == key) + 1;
        var list = groupDatas[(AnimationEventGroup)index];
        RefreshUI(list);
    }

    private void RefreshUI(List<ActivityEventInfo> datas)
    {
        if (datas == null || datas.Count == 0)
        {
            return;
        }

        foreach (var x in eventViews)
        {
            GameObject.Destroy(x.gameObject);
        }
        eventViews.Clear();

        for (int i = 0; i < datas.Count; i++)
        {
            var obj = Instantiate(mainItemView, mainViewContent);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            AnimationStudioItemView eventItem = obj.GetComponent<AnimationStudioItemView>();
            eventItem.Init(datas[i], ClaimReward);
            eventViews.Add(eventItem);
        }
    }

    private void RefreshRedDot()
    {
        if (groupDatas == null || groupDatas.Count == 0)
        {
            return;
        }

        if (tabNames == null || tabNames.Count == 0)
        {
            return;
        }

        for (int i = 0; i < tabNames.Count; i++)
        {
            var list = groupDatas[(AnimationEventGroup)(i+1)];
            var showRedDot = list.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
            var viewKey = tabNames[i];
            if (_tabItemInfo.Keys.Contains(viewKey))
            {
                _tabItemInfo[viewKey].UpdateRedDot(showRedDot != null);
            }
        }
    }

    private void ClaimReward(ActivityEventInfo data)
    {
        if (string.IsNullOrEmpty(ActivityId))
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
            ["activityId"] = ActivityId,
            ["eventId"] = data.eventId
        };
        ShowLoading(data.eventId, true);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ShowLoading(data.eventId, false);
                ActivityEventClaimResponse avtivityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(avtivityEventClaimResponse);
            },
            (error) =>
            {
                ShowLoading(data.eventId, false);
                isSending = false;
            });
    }

    private void ShowLoading(int eventId, bool isShow)
    {
        var itemView = eventViews.Find(x => x.EventId == eventId);
        if (itemView == null)
        {
            return;
        }
        if (isShow)
        {
            itemView.CliamStart();
        }
        else
        {
            itemView.CliamCallBack();
        }
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        var itemView = eventViews.Find(x => x.EventId == response.eventInfo.eventId);
        var itemInfo = itemView._info;
        if (itemView == null || itemInfo == null)
        {
            return;
        }

        itemInfo.eventStatus = response.eventInfo.eventStatus;
        itemView.RefrashData(itemInfo);

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (itemInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            panel.ShowPgcRewards(new List<string>() {itemInfo.pgcId}, itemInfo.eventName);
        }
        else
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)itemInfo.rewardType, gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)itemInfo.rewardType);
            commonRewardItemData.RewardAmount = response.claimAmount;
            rewardList.Add(commonRewardItemData);

            panel.ShowRewards(rewardList);
        }

        var index = tabNames.FindIndex(x => x == activeCategoryKey) + 1;
        groupDatas[(AnimationEventGroup)index].Find(x => x.eventId == response.eventInfo.eventId).eventStatus = response.eventInfo.eventStatus;
        RefreshRedDot();
        ClaimSuccessAction?.Invoke((AnimationEventGroup)index, response);
    }



}

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

public class PetStudioCumulativeView : MonoBehaviour
{
    [SerializeField] private Transform tabViewContent;
    [SerializeField] private PetStudioTabItemView tabItemView;
    
    [SerializeField] private Transform mainViewContent;
    [SerializeField] private PetStudioDailyItemView mainItemView;
    
    private List<PetStudioDailyItemView> eventViews = new List<PetStudioDailyItemView>();
    
    private List<string> tabNames = new List<string>() {  "编辑形象", "地图游玩", "皮肤创作", "皮肤发布" };
    private string activeCategoryKey;
    private string ActivityId;
    private Dictionary<string, PetStudioTabItemView> _tabItemInfo = new Dictionary<string, PetStudioTabItemView>();

    private Dictionary<PetStudioJumpType, List<ActivityEventInfo>> groupDatas =
        new Dictionary<PetStudioJumpType, List<ActivityEventInfo>>();
    
    public Action<PetStudioJumpType, ActivityEventClaimResponse> ClaimSuccessAction;
    private bool isSending = false;
    
    public void InitListUI(Dictionary<PetStudioJumpType, List<ActivityEventInfo>> groupDatas, string activityId)
    {
        ActivityId = activityId;
        this.groupDatas = groupDatas;
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
    
    public void RefreshListUI(Dictionary<PetStudioJumpType, List<ActivityEventInfo>> groupDatas)
    {
        if (groupDatas == null || groupDatas.Count == 0)
        {
            return;
        }
        this.groupDatas = groupDatas;
        RefreshRedDot();
        var index = tabNames.FindIndex(x => x == activeCategoryKey) + 1;
        var list = groupDatas[(PetStudioJumpType)index];
        RefreshUI(list);
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
            var list = groupDatas[(PetStudioJumpType)(i+1)];
            var showRedDot = list.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
            var viewKey = tabNames[i];
            if (_tabItemInfo.Keys.Contains(viewKey))
            {
                _tabItemInfo[viewKey].UpdateRedDot(showRedDot != null);
            }
        }
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
        var list = groupDatas[(PetStudioJumpType)index];
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
            PetStudioDailyItemView eventItem = obj.GetComponent<PetStudioDailyItemView>();
            eventItem.Init(datas[i], ClaimReward);
            eventViews.Add(eventItem);
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
        groupDatas[(PetStudioJumpType)index].Find(x => x.eventId == response.eventInfo.eventId).eventStatus = response.eventInfo.eventStatus;
        RefreshRedDot();
        ClaimSuccessAction?.Invoke((PetStudioJumpType)index, response);
    }
}

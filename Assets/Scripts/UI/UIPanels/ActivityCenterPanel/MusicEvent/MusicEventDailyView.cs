using System;
using System.Collections;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;

public class MusicEventDailyView : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private MusicEventDailyItemView _itemView;

    private List<MusicEventDailyItemView> eventViews = new List<MusicEventDailyItemView>();
    private bool isSending = false;
    private string ActivityId;

    public Action<NotesJumpGroupType, ActivityEventClaimResponse> ClaimSuccessAction;
    
    public void InitListUI(List<ActivityEventInfo> datas, string activityId)
    {
        ActivityId = activityId;
        if (datas == null || datas.Count == 0)
        {
            return;
        }
        
        for (int i = 0; i < datas.Count; i++)
        {
            var obj = Instantiate(_itemView, content);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            MusicEventDailyItemView eventItem = obj.GetComponent<MusicEventDailyItemView>();
            eventItem.Init(datas[i], ClaimReward);
            eventViews.Add(eventItem);
        }
    }
    
    public void RefreshListUI(List<ActivityEventInfo> datas)
    {
        if (datas == null || datas.Count == 0)
        {
            return;
        }
        
        if (eventViews.Count != datas.Count)
        {
            return;
        }

        foreach (var VARIABLE in eventViews)
        {
            var data = datas.Find(x => x.eventId == VARIABLE.EventId);
            if (data == null)
            {
                continue;
            }
            VARIABLE.RefrashData(data);
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
        
        ClaimSuccessAction?.Invoke(NotesJumpGroupType.Daily, response);
    }
}

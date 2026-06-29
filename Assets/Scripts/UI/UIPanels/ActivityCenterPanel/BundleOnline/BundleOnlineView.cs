using System;
using System.Collections;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

public class BundleOnlineView : ActivityBaseView
{
    [SerializeField] private Transform content;
    [SerializeField] private BundleOnlineItemView _itemView;
    [SerializeField] private CButton rewardBtn;
    [SerializeField] private BundleOnlineCurrencyView currencyView;
    
    private List<BundleOnlineItemView> eventViews = new List<BundleOnlineItemView>();
    private bool isSending = false;

    private ActivityInfo _info;
    public Action<NotesJumpGroupType, ActivityEventClaimResponse> ClaimSuccessAction;
    private bool ReceiveServerData = false;
    
    public override void Init(ActivityInfo info)
    {
        _info = info;
        var bgParent = GameObjectEx.FindChildByName(transform, "BG");
        
        InitBg(bgParent, "#9E00FF",new List<string>()
        {
            "BundleOnline_1",
            "BundleOnline_2",
            "BundleOnline_3"
        });
        InitListUI(info.eventList);
        
        rewardBtn?.onClick.RemoveAllListeners();
        rewardBtn?.onClick.AddListener(ShowExchangePage);
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        _info = info;
        ReceiveServerData = true;
        currencyView.UpdateCurrency(info.currencyAmount);
        RefreshListUI(info.eventList);
    }
    
    private void InitListUI(List<ActivityEventInfo> datas)
    {
        if (datas == null || datas.Count == 0)
        {
            return;
        }
        
        for (int i = 0; i < datas.Count; i++)
        {
            var obj = Instantiate(_itemView, content);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            BundleOnlineItemView eventItem = obj.GetComponent<BundleOnlineItemView>();
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

    private void ShowExchangePage()
    {
        if (!ReceiveServerData)
        {
            return;
        }
        var panel = UIManager.Inst.OpenPanel<BundleOnlineRewardPanel>(PanelId.BundleOnlineRewardPanel);
        panel.SetData(_info, currencyView.balance, i =>
        {
            if (this == null)
            {
                return;
            }
            currencyView.UpdateCurrency(i);
        });
    }
    
    private void ClaimReward(ActivityEventInfo data)
    {
        if (string.IsNullOrEmpty(_info.activityId))
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
            ["activityId"] = _info.activityId,
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
        
        currencyView.UpdateCurrency(response.currencyAmount);

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
        
        UpdateRedDot();
    }
}

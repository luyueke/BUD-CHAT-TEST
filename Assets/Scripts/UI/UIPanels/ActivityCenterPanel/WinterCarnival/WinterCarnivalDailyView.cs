using System;
using System.Collections;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;

public class WinterCarnivalDailyView : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private WinterCarnivalDailyViewItem _viewItem;

    private List<WinterCarnivalDailyViewItem> _viewItems = new List<WinterCarnivalDailyViewItem>();
    private bool isSending = false;
    private string activityId;

    public Action<ActivityEventClaimResponse> ClaimSuccessAction;
    private const int _dailyItemCount = 3;

    public void InitListUI(List<ActivityEventInfo> datas, string id)
    {
        activityId = id;
        if (datas == null || datas.Count == 0)
        {
            return;
        }

        for (int i = 0; i < _dailyItemCount; i++)
        {
            var obj = Instantiate(_viewItem, content);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            WinterCarnivalDailyViewItem itemComp = obj.GetComponent<WinterCarnivalDailyViewItem>();
            itemComp.Init(i, datas[i], ClaimReward);
            _viewItems.Add(itemComp);
        }
    }

    public void RefreshListUI(List<ActivityEventInfo> datas)
    {
        foreach (var VARIABLE in _viewItems)
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

        var tmpItemView = _viewItems.Find(x => x.EventId == response.eventInfo.eventId);
        
        var itemInfo = tmpItemView._info;
        if (tmpItemView == null || itemInfo == null)
        {
            return;
        }

        itemInfo.eventStatus = response.eventInfo.eventStatus;
        tmpItemView.RefrashData(itemInfo);

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

        ClaimSuccessAction?.Invoke(response);
    }
}

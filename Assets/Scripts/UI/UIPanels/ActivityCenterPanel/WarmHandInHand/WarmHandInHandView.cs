using System.Collections.Generic;
using System.Linq;
using GameData.Gashapon;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class WarmHandInHandView : ActivityBaseView {

    [SerializeField]
    private HandDailyView handDailyView;

    [SerializeField]
    private WarmHandInHandRewardItem rewardItemPrefab;



    [SerializeField] private Text currencyAmountText;

    [SerializeField] private CButton previewBtn;

    [SerializeField] private RawImage bgImage;

    private readonly List<int> rewardEventIds = new List<int>() {7, 8, 9, 10, 11};
    private Dictionary<int, WarmHandInHandRewardItem> rewardItems = new Dictionary<int, WarmHandInHandRewardItem>();
    private ActivityInfo activityInfo;
    private bool isSending = false;

    public override void Init(ActivityInfo info) {
        activityInfo = info;
        base.Init(info);
        handDailyView.InitListUI(info.eventList, info.activityId, OnClaimSuccess);

        foreach (var rewardEventId in rewardEventIds) {
            var eventInfo = info.eventList.FirstOrDefault(tmp => rewardEventId == tmp.eventId);
            if (eventInfo == null) {
                continue;
            }

            var rewardData = new CommonRewardItemData() {
                pgcId = eventInfo.pgcId,
                RewardAmount = eventInfo.rewardNum,
                rewardName = eventInfo.eventName,
                rewardType = eventInfo.rewardType,
            };
            var tmpItem = Instantiate(rewardItemPrefab, rewardItemPrefab.transform.parent);
            tmpItem.gameObject.SetActive(true);
            tmpItem.Init(rewardEventId, rewardData, OnClaimCallBack);
            tmpItem.SetProcess(eventInfo.targetAmount);
            rewardItems.Add(rewardEventId, tmpItem);
        }
        rewardItemPrefab.gameObject.SetActive(false);
        previewBtn.onClick.AddListener(OnPreviewClick);
    }

    private void OnPreviewClick() {
        if (activityInfo == null)
        {
            return;
        }
        var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel,activityInfo.activityId);
        panel.SetEventPreview(activityInfo, "参与活动收集缘晶，免费领取蒙眼飞刀动作！", bgImage.texture);
    }

    private void OnClaimCallBack(CommonRewardItem rewardItem) {
        var eventInfo = activityInfo.eventList.Find(tmp => tmp.eventId == rewardItem.itemId);
        if (eventInfo == null) {
            return;
        }

        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked) {
            OnPreviewClick();
            return;
        }

        if (isSending)
        {
            return;
        }
        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse, rewardItem);
            },
            (error) =>
            {
                isSending = false;
            });
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response) {
        if (this == null || gameObject == null) {
            return;
        }
        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource) {
            panel.ShowPgcRewards(new List<string>() { eventInfo.pgcId }, eventInfo.eventName);
        } else {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, panel.gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)eventInfo.rewardType);
            commonRewardItemData.RewardAmount = eventInfo.rewardNum;
            rewardList.Add(commonRewardItemData);
            panel.ShowRewards(rewardList);
        }
        UpdateRedDot();
        RefrashData(activityInfo);
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response, CommonRewardItem rewardItem) {
        if (this == null || gameObject == null) {
            return;
        }
        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }
        AccountDataManager.Inst.BalanceInfo.Refresh();
        activityInfo.currencyAmount = response.currencyAmount;
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        rewardItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        panel.ShowRewards(new List<CommonRewardItemData>() {rewardItem.rewardData});
        UpdateRedDot();
        RefrashData(activityInfo);
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }




    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        activityInfo = info;
        handDailyView.RefreshListUI(info.eventList);

        foreach (var eventInfo in info.eventList) {
            if (rewardItems.TryGetValue(eventInfo.eventId, out var rewardItem)) {
                rewardItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
            }
        }


        UpdateCurrency(info.currencyAmount);
    }

    private void UpdateCurrency(int amount)
    {
        currencyAmountText.SetText(amount.ToString());
        currencyAmountText.SetPreferredSize();
    }
}

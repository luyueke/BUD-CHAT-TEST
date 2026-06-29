using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using GameData.Gashapon;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SockTaskPanel : ActivityBaseView
{

    [SerializeField]
    private SockTaskDailyView dailyView;

    [SerializeField]
    private CommonRewardItem rewardItemPrefab;

    [SerializeField] private Text currencyAmountText;

    [SerializeField] private CButton previewBtn;

    [SerializeField] private RawImage bgImage;
    [SerializeField] private CButton claimAllBtn;

    private readonly List<int> rewardEventIds = new List<int>() { 11, 12, 13, 14, 15, 16 };
    private Dictionary<int, CommonRewardItem> rewardItems = new Dictionary<int, CommonRewardItem>();
    private ActivityInfo activityInfo;
    private bool isSending = false;
    private List<string> pgcIds = new List<string>() { "11000275" };

    public override void Init(ActivityInfo info)
    {
        activityInfo = info;
        base.Init(info);
        dailyView.InitListUI(info.eventList, info.activityId, OnClaimSuccess);

        foreach (var rewardEventId in rewardEventIds)
        {
            var eventInfo = info.eventList.FirstOrDefault(tmp => rewardEventId == tmp.eventId);
            if (eventInfo == null)
            {
                continue;
            }

            var rewardData = new CommonRewardItemData()
            {
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
        previewBtn.onClick.AddListener(() => OnPreviewClick());
        claimAllBtn.onClick.AddListener(OnClaimAllBtnClick);


    }

    private void Awake()
    {
        //监听扭蛋商场是否关闭
        MessageHelper.AddListener(MessageName.OnStoreMallPanelClose, OnStoreMallPanelClose);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnStoreMallPanelClose, OnStoreMallPanelClose);
    }

    private void OnStoreMallPanelClose()
    {
        //刷新数据
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

    private void OnPreviewClick(CommonRewardItemData commonRewardItemData = null)
    {
        if (activityInfo == null)
        {
            return;
        }

        if (commonRewardItemData == null)
        {
            ShowPgcPreview();
        }
        else
        {
            int rewardType = commonRewardItemData.rewardType;
            if (rewardType == (int)BUDRewardType.RewardPgcResource)
            {
                ShowPgcPreview();
                return;
            }

            CurrencyType currencyType = GameUtils.ConvertRewardType(rewardType);
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, currencyType);
        }
    }

    private void ShowPgcPreview()
    {
        var panel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        panel.SetEventPreview(pgcIds, "暖橙晕晕公仔", "", "", "袜袜幼稚园活跃活动，免费领取暖橙晕晕公仔", "#FF9A61", bgImage.texture);
    }

    private void OnClaimAllBtnClick()
    {
        string activityId = activityInfo.activityId;
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
            ["isAll"] = 1
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse avtivityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimAllSuccess(avtivityEventClaimResponse, BUDRewardType.RewardBib);
            },
            (error) => { isSending = false; });
    }

    private void OnClaimCallBack(CommonRewardItem rewardItem)
    {
        var eventInfo = activityInfo.eventList.Find(tmp => tmp.eventId == rewardItem.itemId);
        if (eventInfo == null)
        {
            return;
        }

        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked)
        {
            OnPreviewClick(rewardItem.rewardData);
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

    private void OnClaimAllSuccess(ActivityEventClaimResponse response, BUDRewardType budRewardType)
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        activityInfo.currencyAmount = response.currencyAmount;
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        AccountDataManager.Inst.BalanceInfo.Refresh();
        var rewardList = new List<CommonRewardItemData>();
        CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        commonRewardItemData.IconSp =
            PgcUtils.LoadRewardIcon(budRewardType, panel.gameObject);
        commonRewardItemData.rewardName = PgcUtils.GetRewardName(budRewardType);
        commonRewardItemData.RewardAmount = response.claimAmount;
        rewardList.Add(commonRewardItemData);
        panel.ShowRewards(rewardList);

        UpdateRedDot();
        RefrashData(activityInfo);
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        if (this == null || gameObject == null)
        {
            return;
        }
        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            panel.ShowPgcRewards(new List<string>() { eventInfo.pgcId }, eventInfo.eventName);
        }
        else
        {
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

    private void OnClaimSuccess(ActivityEventClaimResponse response, CommonRewardItem rewardItem)
    {
        if (this == null || gameObject == null)
        {
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

        panel.ShowRewards(new List<CommonRewardItemData>() { rewardItem.rewardData });
        UpdateRedDot();
        RefrashData(activityInfo);
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }




    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        activityInfo = info;
        dailyView.RefreshListUI(info.eventList);

        foreach (var eventInfo in info.eventList)
        {
            if (rewardItems.TryGetValue(eventInfo.eventId, out var rewardItem))
            {
                rewardItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
            }
        }

        UpdateCurrency(info.currencyAmount);
        UpdateClaimAllBtn(info.eventList);

        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }

    private void UpdateCurrency(int amount)
    {
        currencyAmountText.SetText(amount.ToString());
        currencyAmountText.SetPreferredSize();
    }

    private void UpdateClaimAllBtn(List<ActivityEventInfo> datas)
    {
        bool hasUnlockedEvent = datas.Any(data => data.eventId >= 1 && data.eventId < 11 && data.eventStatus == (int)ClaimStatus.Unlocked);
        claimAllBtn.SetClickAble(hasUnlockedEvent);
    }
}

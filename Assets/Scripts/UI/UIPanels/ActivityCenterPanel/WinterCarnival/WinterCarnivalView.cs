using System.Collections;
using System.Collections.Generic;
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

public class WinterCarnivalView : ActivityBaseView
{
    [SerializeField] private Text Txt_CurrencyAmount;
    [SerializeField] private CButton _btn_RewardPreview;
    [Header("下方领奖Item")]
    [SerializeField] private Text Txt_LeftTime;//            endTime.text = "剩余时间：" + leftTime;
    [SerializeField] private Transform rewardItemContent;
    [SerializeField] private WinterCarnivalRewardItem _rewardItem;
    [SerializeField] private Image progressLable;
    
    [Header("右侧任务页面")]
    [SerializeField] private WinterCarnivalDailyView _dailyView;
    
    private bool isSending = false;

    private List<WinterCarnivalRewardItem> eventViews = new List<WinterCarnivalRewardItem>();

    private ActivityInfo _info;

    public override void Init(ActivityInfo info) {
        base.Init(info);
        this._info = info;
        _btn_RewardPreview.onClick.AddListener(OnBtnRewardPreviewClick);
        isSending = false;
        InitRewardItemView();
        InitDailyView();
        UpdateCurrency(_info.currencyAmount);
    }
    
    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        _info = info;
        RefreshRewardItems();
        _dailyView.RefreshListUI(_info.eventList);
        if (!string.IsNullOrEmpty(info.leftTime))
        {
            Txt_LeftTime.text = "距活动结束还有：" + info.leftTime;
        }
        InitProgress();
    }

    private void OnBtnRewardPreviewClick()
    {
        if (_info == null)
        {
            return;
        }
        var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel,ActivityId.WinterCarnival);
        panel.SetData(_info, _info.currencyAmount, i =>
        {
        });
    }

    #region 初始化下方RewardItem

    private void InitRewardItemView()
    {
        InitListUI(_info.eventList, _info.rewardList);
        InitProgress();
    }

    private void InitProgress()
    {
        float progress = 0;
        if (_info.currencyAmount != 0)
        {
            progress = ((_info.currencyAmount - 30) * 1.00f) / 120.0f;
        }
        progressLable.fillAmount = progress;
    }

    private void InitListUI(List<ActivityEventInfo> eventDatas, List<ActivityRewardInfo> rewardDatas)
    {
        if (eventDatas == null || eventDatas.Count == 0 || rewardDatas == null || rewardDatas.Count == 0)
        {
            return;
        }

        for (int i = 0; i < rewardDatas.Count; i++)
        {
            var obj = Instantiate(_rewardItem, rewardItemContent);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            WinterCarnivalRewardItem eventItem = obj.GetComponent<WinterCarnivalRewardItem>();
            var eventData = eventDatas.Find(x => x.eventId == rewardDatas[i].rewardId);
            eventItem.Init(eventData, rewardDatas[i], ClaimRewardItem, OnBtnRewardPreviewClick);
            eventViews.Add(eventItem);
        }
    }
    
    public void RefreshRewardItems()
    {
        eventViews.ForEach(x =>
        {
            var eventInfo = _info.eventList.Find(eventItem => eventItem.eventId == x.eventId);
            if(eventInfo != null)
                x.Refresh(eventInfo);
        });
    }
    
    private void ClaimRewardItem(ActivityEventInfo eventInfo) {
        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Claimed) {
            return;
        }

        if ((ClaimStatus)eventInfo.eventStatus == ClaimStatus.Lock) {
            if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource && !string.IsNullOrEmpty(eventInfo.pgcId))
            {
                string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";

                EventRewardPanelData data = new EventRewardPanelData()
                {
                    bgColor = "#A168FF",
                    rewardItemBgColor = "#7FAAFF",
                    atlasPath = atlasPath,
                    iconList = new List<string>()
                    {
                        "CoinAssist_1",
                        "CoinAssist_2",
                    },
                    rewardList = new List<string>() { eventInfo.pgcId }
                };
                UIManager.Inst.OpenPanel<EventCenterRewardPanel>(PanelId.EventCenterRewardPanel, data);
            }
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
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimRewardItemSuccess(activityEventClaimResponse);
            },
            (error) =>
            {
                isSending = false;
            });
    }
    
    
    private void OnClaimRewardItemSuccess(ActivityEventClaimResponse response) {
        if (this == null || gameObject == null) {
            return;
        }
        var eventInfo = _info.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        var rewardInfo = _info.rewardList.Find(x => x.rewardId == response.eventInfo.eventId);
        if (eventInfo==null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        RefreshRewardItems();

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            panel.ShowPgcRewards(new List<string>() {eventInfo.pgcId}, rewardInfo.rewardName);
        }
        else
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)eventInfo.rewardType);
            commonRewardItemData.RewardAmount = response.claimAmount;
            rewardList.Add(commonRewardItemData);

            panel.ShowRewards(rewardList);
        }

        UpdateRedDot();

        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }
    #endregion
    

    #region 每日任务相关
    //初始化每日任务
    private void InitDailyView()
    {
        _dailyView.InitListUI(_info.eventList, _info.activityId);
        _dailyView.ClaimSuccessAction = ClaimSuccessHandler;
    }
    
    private void ClaimSuccessHandler(ActivityEventClaimResponse res) {
        if (res == null) {
            return;
        }
        
        var d = _info.eventList.Find(x => x.eventId == res.eventInfo.eventId);
        if (d == null) {
            return;
        }

        d.eventStatus = (int)TaskClaimState.Finished;
        UpdateCurrency(res.currencyAmount);
        UpdateRedDot();
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }
    #endregion
    
    private void UpdateCurrency(int currencyAmount)
    {
        Txt_CurrencyAmount.text = currencyAmount.ToString();
    }
}

using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CoinAssistView : ActivityBaseView
{
    [SerializeField] private Transform content;
    [SerializeField] private CoinAssistItem _itemView;
    [SerializeField] private CButton rewardBtn;
    [SerializeField] private Text Txt_LeftTime;
    private bool isSending = false;

    private List<CoinAssistItem> eventViews = new List<CoinAssistItem>();

    private ActivityInfo _info;

    public override void Init(ActivityInfo info) {
        base.Init(info);
        isSending = false;
        var bgParent = GameObjectEx.FindChildByName(transform, "BG");
        InitBg(bgParent, "#A168FF",new List<string>()
        {
            "CoinAssist_1",
            "CoinAssist_2",
        });
        InitListUI(info.eventList, info.rewardList);
        rewardBtn?.onClick.RemoveAllListeners();
        rewardBtn?.onClick.AddListener(ShowExchangePage);
        if (!string.IsNullOrEmpty(info.leftTime))
        {
            Txt_LeftTime.text = "距活动结束还有：" + info.leftTime;
        }
    }


    private void InitListUI(List<ActivityEventInfo> datas, List<ActivityRewardInfo> rewardDatas)
    {
        if (datas == null || datas.Count == 0 || datas.Count != rewardDatas.Count)
        {
            return;
        }

        for (int i = 0; i < datas.Count; i++)
        {
            var obj = Instantiate(_itemView, content);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            CoinAssistItem eventItem = obj.GetComponent<CoinAssistItem>();
            eventItem.Init(datas[i], rewardDatas[i],ClaimReward);
            eventViews.Add(eventItem);
        }
    }

    public override void RefrashData(ActivityInfo info) {
        base.RefrashData(info);
        _info = info;
        RefreshItems();
    }

    public void RefreshItems()
    {
        if (eventViews.Count != _info.eventList.Count)
        {
            LoggerUtils.LogError("[SunnyDoll] Refresh Event ItemView fail");
            return;
        }

        for (int i = 0; i < _info.eventList.Count; i++)
        {
            eventViews[i].Refresh(_info.eventList[i]);
        }
    }

    private void ShowExchangePage()
    {
        UIManager.Inst.OpenPanel(PanelId.StoreMallPanel);
    }

    private void ClaimReward(ActivityEventInfo eventInfo) {
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
                OnClaimSuccess(activityEventClaimResponse);
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
        var eventInfo = _info.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        var rewardInfo = _info.rewardList.Find(x => x.rewardId == response.eventInfo.eventId);
        if (eventInfo==null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        RefreshItems();

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

    }

}

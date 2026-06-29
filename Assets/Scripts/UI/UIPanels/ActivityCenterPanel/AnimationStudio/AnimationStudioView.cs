using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;


public enum AnimationEventGroup {
    Daily = 0,
    AnimationPublish = 1,
    PostPublish = 2,
    AnimationUse = 3,
    AnimationMusicPublish = 4,
    AnimationBuy = 5,
}

public class AnimationStudioView : ActivityBaseView {
    [SerializeField] private Transform tabViewContent;
    [SerializeField] private AnimationStudioTabItemView tabItemView;

    [SerializeField] private EventCurrencyView currencyView;

    private Dictionary<string, AnimationStudioTabItemView> tabItemInfo =
        new Dictionary<string, AnimationStudioTabItemView>();

    private ActivityInfo activityInfo;
    private bool isSending;
    private readonly List<string> tabNames = new List<string>() { "每日任务", "累计任务" };
    [SerializeField] private AnimationStudioDailyView dailyView;
    [SerializeField] private AnimationStudioCumulativeView cumulativeView;
    [SerializeField] private CButton rewardExchangeBtn;
    [SerializeField] private Text Txt_LeftTime;

    private Dictionary<AnimationEventGroup, List<ActivityEventInfo>> eventGroupDic =
        new Dictionary<AnimationEventGroup, List<ActivityEventInfo>>();

    public override void Init(ActivityInfo info) {
        base.Init(info);
        activityInfo = info;

        InitData();
        InitUI();
        if (!string.IsNullOrEmpty(info.leftTime))
        {
            Txt_LeftTime.text = "距活动结束还有：" + info.leftTime;
        }
    }

    private void InitData() {
        eventGroupDic.Clear();
        foreach (var eventInfo in activityInfo.eventList) {
            var eventGroup = (AnimationEventGroup)eventInfo.groupId;
            if (!eventGroupDic.ContainsKey(eventGroup)) {
                eventGroupDic.Add(eventGroup, new List<ActivityEventInfo>());
            }

            eventGroupDic[eventGroup].Add(eventInfo);
        }
    }


    private void InitUI() {
        var bgParent = GameObjectEx.FindChildByName(transform, "BG");
        InitBg(bgParent, "#FFDBA5", new List<string>() {
            "AnimationStudio_1",
            "AnimationStudio_2",
            "AnimationStudio_3",
        });

        for (int i = 0; i < tabNames.Count; i++) {
            var key = tabNames[i];
            var itemView = Instantiate(tabItemView, tabViewContent);
            itemView.gameObject.SetActive(true);
            itemView.SetData(key, OnCategoryItemClick);
            tabItemInfo[key] = itemView;
        }

        if (tabNames.Count > 0) {
            OnCategoryItemClick(tabNames[0]);
        }

        dailyView.InitListUI(eventGroupDic[AnimationEventGroup.Daily], activityInfo.activityId);
        dailyView.ClaimSuccessAction = ClaimSuccessHandler;

        cumulativeView.InitListUI(eventGroupDic, activityInfo.activityId);
        cumulativeView.ClaimSuccessAction = ClaimSuccessHandler;

        rewardExchangeBtn?.onClick.AddListener(OnClickRewardExchange);
    }

    public override void RefrashData(ActivityInfo info) {
        activityInfo = info;
        InitData();
        currencyView.UpdateCurrency(info.currencyAmount);
        dailyView.RefreshListUI(eventGroupDic[AnimationEventGroup.Daily]);
        cumulativeView.RefreshListUI(eventGroupDic);

        RefreshRedDot();
    }

    private void OnCategoryItemClick(string key) {
        if (string.IsNullOrEmpty(key)) {
            return;
        }

        foreach (var element in tabItemInfo) {
            element.Value.UpdateSelected(element.Key == key);
        }

        var index = tabNames.FindIndex(x => x == key);
        dailyView.gameObject.SetActive(index == 0);
        cumulativeView.gameObject.SetActive(index == 1);
    }

    private void ClaimSuccessHandler(AnimationEventGroup type, ActivityEventClaimResponse res) {
        if (res == null) {
            return;
        }

        if (eventGroupDic.Keys.Contains(type)) {
            var list = eventGroupDic[type];
            var d = list.Find(x => x.eventId == res.eventInfo.eventId);
            if (d == null) {
                return;
            }

            d.eventStatus = (int)TaskClaimState.Finished;
            RefreshRedDot();
        }

        currencyView.UpdateCurrency(res.currencyAmount);

        UpdateRedDot();
    }


    private void RefreshRedDot() {
        bool showDailyReddot = false;
        bool showScheduleRedDot = false;

        foreach (var element in eventGroupDic) {
            if (AnimationEventGroup.Daily == element.Key) {
                var enableItem = element.Value.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
                showDailyReddot = enableItem != null;
            } else {
                if (showScheduleRedDot) {
                    continue;
                }

                var enableItem = element.Value.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
                showScheduleRedDot = enableItem != null;
            }
        }

        if (tabNames.Count > 1) {
            var key1 = tabNames[0];
            var key2 = tabNames[1];

            if (tabItemInfo.Keys.Contains(key1)) {
                tabItemInfo[key1].UpdateRedDot(showDailyReddot);
            }

            if (tabItemInfo.Keys.Contains(key2)) {
                tabItemInfo[key2].UpdateRedDot(showScheduleRedDot);
            }
        }
    }

    private void OnClickRewardExchange()
    {
        if (activityInfo == null)
        {
            return;
        }
        var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel,ActivityId.AnimationStudio);
        panel.SetData(activityInfo, currencyView.balance, i =>
        {
            currencyView.UpdateCurrency(i);
        });
    }
}

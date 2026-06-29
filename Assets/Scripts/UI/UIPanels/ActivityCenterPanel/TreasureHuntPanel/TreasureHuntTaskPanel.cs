using System;
using System.Collections.Generic;
using System.Linq;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

public class TreasureHuntTaskPanel : BasePanel<TreasureHuntTaskPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton DailyBtn;
    [SerializeField] private GameObject DailySelect;
    [SerializeField] private CButton GrandBtn;
    [SerializeField] private GameObject GrandSelect;
    [SerializeField] private Transform ContentTsf;
    [SerializeField] private TreasureHuntTaskItem TreasureHuntTaskItem;

    private static readonly List<int> DailyEventIds = new() { 1, 2, 3, 4, 5, 6 };
    private static readonly List<int> GrandEventIds = new() { 7, 8, 9, 10, 11, 12 };

    private readonly List<TreasureHuntTaskItem> _dailyItems = new();
    private readonly List<TreasureHuntTaskItem> _grandItems = new();
    private ActivityInfo _info;
    private bool _isSending;

    public override void OnCreate()
    {
        base.OnCreate();
        if (CloseBtn != null) CloseBtn.onClick.AddListener(CloseSelf);
        if (DailyBtn != null) DailyBtn.onClick.AddListener(OnDailyBtnClick);
        if (GrandBtn != null) GrandBtn.onClick.AddListener(OnGrandBtnClick);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (_dailyItems.Count > 0 || _grandItems.Count > 0)
            RequestTaskStatus();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length > 0 && args[0] is ActivityInfo info)
        {
            _info = info;
            InitItems();
        }
        ShowTab(0);
        RequestTaskStatus();
    }

    private void InitItems()
    {
        foreach (var item in _dailyItems) if (item != null) Destroy(item.gameObject);
        foreach (var item in _grandItems) if (item != null) Destroy(item.gameObject);
        _dailyItems.Clear();
        _grandItems.Clear();

        if (_info == null || _info.eventList == null) return;

        var sortedDaily = DailyEventIds
            .Select(id => _info.eventList.Find(x => x.eventId == id))
            .Where(e => e != null)
            .OrderBy(e => GetStatusPriority(e.eventStatus))
            .ToList();

        foreach (var eventInfo in sortedDaily)
        {
            var item = Instantiate(TreasureHuntTaskItem, ContentTsf);
            item.transform.localScale = Vector3.one;
            item.Init(eventInfo, OnClaimClick);
            _dailyItems.Add(item);
        }

        var sortedGrand = GrandEventIds
            .Select(id => _info.eventList.Find(x => x.eventId == id))
            .Where(e => e != null)
            .OrderBy(e => GetStatusPriority(e.eventStatus))
            .ToList();

        foreach (var eventInfo in sortedGrand)
        {
            var item = Instantiate(TreasureHuntTaskItem, ContentTsf);
            item.transform.localScale = Vector3.one;
            item.Init(eventInfo, OnClaimClick);
            _grandItems.Add(item);
        }

        TreasureHuntTaskItem.gameObject.SetActive(false);
    }

    private static int GetStatusPriority(int status)
    {
        if (status == (int)ClaimStatus.Unlocked) return 0;
        if (status == (int)ClaimStatus.Claimed) return 2;
        return 1;
    }

    private void RebuildItems()
    {
        bool isDaily = DailySelect.activeSelf;
        InitItems();
        ShowTab(isDaily ? 0 : 1);
    }

    private void ShowTab(int tabIndex)
    {
        bool isDaily = tabIndex == 0;
        DailySelect.SetActive(isDaily);
        GrandSelect.SetActive(!isDaily);
        _dailyItems.ForEach(x => x.gameObject.SetActive(isDaily));
        _grandItems.ForEach(x => x.gameObject.SetActive(!isDaily));
    }

    private void OnDailyBtnClick() => ShowTab(0);
    private void OnGrandBtnClick() => ShowTab(1);

    private bool _isRefreshing;

    private void RequestTaskStatus()
    {
        if (_isRefreshing) return;
        _isRefreshing = true;
        var req = new ActivityCenterInfoReq { idList = new List<string> { "TreasureHuntingDaily" } };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req),
            content =>
            {
                _isRefreshing = false;
                var response = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (response?.list == null || response.list.Count == 0) return;
                var eventList = response.list[0].eventList;
                if (eventList == null) return;
                var dailyEvents = eventList.GetRange(0, Math.Min(6, eventList.Count));
                var grandEvents = eventList.Count > 6
                    ? eventList.GetRange(6, eventList.Count - 6)
                    : new List<ActivityEventInfo>();

                for (int i = 0; i < dailyEvents.Count; i++)
                {
                    int eventId = DailyEventIds[i];
                    var localEvent = _info.eventList?.Find(x => x.eventId == eventId);
                    if (localEvent != null)
                    {
                        localEvent.eventStatus = dailyEvents[i].eventStatus;
                        localEvent.finishAmount = dailyEvents[i].finishAmount;
                    }
                    var dailyItem = FindItem(eventId);
                    if (dailyItem != null) dailyItem.RefreshData(localEvent ?? dailyEvents[i]);
                }

                for (int i = 0; i < grandEvents.Count; i++)
                {
                    int eventId = GrandEventIds[i];
                    var localEvent = _info.eventList?.Find(x => x.eventId == eventId);
                    if (localEvent != null)
                    {
                        localEvent.eventStatus = grandEvents[i].eventStatus;
                        localEvent.finishAmount = grandEvents[i].finishAmount;
                    }
                    var grandItem = FindItem(eventId);
                    if (grandItem != null) grandItem.RefreshData(localEvent ?? grandEvents[i]);
                }

                RebuildItems();

                var treasureHuntPanel = FindObjectOfType<TreasureHuntPanel>();
                if (treasureHuntPanel != null) treasureHuntPanel.RequestTaskStatus();
            },
            error => { _isRefreshing = false; });
    }

    private void OnClaimClick(ActivityEventInfo data)
    {
        if (_info == null || string.IsNullOrEmpty(_info.activityId) || _isSending) return;
        _isSending = true;

        var item = FindItem(data.eventId);
        if (item != null) item.ClaimStart();

        var jObject = new JObject
        {
            ["activityId"] = "TreasureHuntingDaily",
            ["eventId"] = data.eventId
        };

        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            content =>
            {
                _isSending = false;
                if (item != null) item.ClaimCallBack();
                var response = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                OnClaimSuccess(response, item);
            },
            error =>
            {
                _isSending = false;
                if (item != null) item.ClaimCallBack();
            });
    }

    private void OnClaimSuccess(ActivityEventClaimResponse response, TreasureHuntTaskItem clickedItem)
    {
        if (this == null || gameObject == null) return;

        var eventInfo = _info.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null) return;

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        AccountDataManager.Inst.BalanceInfo.Refresh();
        AccountDataManager.Inst.BalanceInfo.SyncBalance(CurrencyType.Shovel, response.currencyAmount);

        if (clickedItem != null)
        {
            clickedItem._info.eventStatus = response.eventInfo.eventStatus;
            clickedItem.RefreshData(clickedItem._info);
        }
        RequestTaskStatus();

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        if (response.rewardList != null && response.rewardList.Count > 0)
        {
            var serverReward = response.rewardList[0];
            var serverRewardType = (BUDRewardType)serverReward.rewardType;
            if (serverRewardType == BUDRewardType.RewardPgcResource)
            {
                panel.ShowPgcRewards(new List<string> { eventInfo.pgcId }, eventInfo.eventName);
            }
            else
            {
                var rewardList = new List<CommonRewardItemData>
                {
                    new()
                    {
                        IconSp = PgcUtils.LoadRewardIcon(serverRewardType, panel.gameObject),
                        rewardName = PgcUtils.GetRewardName(serverRewardType),
                        RewardAmount = serverReward.amount
                    }
                };
                panel.ShowRewards(rewardList);
            }
        }
    }

    public bool HasAnyClaimable()
    {
        return _dailyItems.Exists(x => x._info?.eventStatus == (int)ClaimStatus.Unlocked) ||
               _grandItems.Exists(x => x._info?.eventStatus == (int)ClaimStatus.Unlocked);
    }

    private TreasureHuntTaskItem FindItem(int eventId)
    {
        var item = _dailyItems.Find(x => x.EventId == eventId);
        if (item != null) return item;
        return _grandItems.Find(x => x.EventId == eventId);
    }
}

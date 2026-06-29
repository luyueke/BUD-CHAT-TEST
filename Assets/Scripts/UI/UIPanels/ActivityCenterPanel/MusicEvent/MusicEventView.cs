using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;


public enum NotesJumpGroupType
{
    Daily = 0,
    InstrumentCreation = 1, // 乐器创作
    MusicComposition = 2,  // 乐谱创作
    SoundCreation = 3, // 音色创作
    InstrumentPlay = 4, // 乐器演奏
}

public class MusicEventView : ActivityBaseView
{
    [SerializeField] private Transform bgParent;
    [SerializeField] private Transform tabViewContent;
    [SerializeField] private NotesJumpTabItemView tabItemView;
    [SerializeField] private MusicEventDailyView _dailyView;
    [SerializeField] private MusicEventCumulativeView _cumulativeView;
    [SerializeField] private MusicCurrencyView currencyView;
    [SerializeField] private CButton rewardExchangeBtn;

    private Dictionary<string, NotesJumpTabItemView> _tabItemInfo = new Dictionary<string, NotesJumpTabItemView>();
    private ActivityInfo _info;
    private bool isSending;
    private List<string> tabNames = new List<string>() {  "每日任务", "累计任务" };

    private Dictionary<NotesJumpGroupType, List<ActivityEventInfo>> groupDatas =
        new Dictionary<NotesJumpGroupType, List<ActivityEventInfo>>();

    public override void Init(ActivityInfo info)
    {
        _info = info;

        ConvertDatas(info);
        
        InitUI();

        AddListeners();
        
        _dailyView.InitListUI(groupDatas[NotesJumpGroupType.Daily], info.activityId);
        _dailyView.ClaimSuccessAction = ClaimSuccessHandler;
        
        _cumulativeView.InitListUI(groupDatas, info.activityId);
        _cumulativeView.ClaimSuccessAction = ClaimSuccessHandler;
    }
    
    private void InitUI()
    {
        if (bgParent == null)
        {
            bgParent = GameObjectEx.FindChildByName(transform, "BG");
        }

        InitBg(bgParent, "#FFB7EC", new List<string>()
        {
            "MusicalInstrument_1",
            "MusicalInstrument_2",
            "MusicalInstrument_3"
        });
        
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
    }

    private void AddListeners()
    {
        rewardExchangeBtn?.onClick.AddListener(OnClickRewardExchange);
    }

    private void OnClickRewardExchange()
    {
        if (_info == null)
        {
            return;
        }
       var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel, ActivityId.NotesJump);
       panel.SetData(_info, currencyView.balance, i =>
       {
           currencyView.UpdateCurrency(i);
       });
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        _info = info;
        ConvertDatas(info);
        currencyView.UpdateCurrency(info.currencyAmount);
        _dailyView.RefreshListUI(groupDatas[NotesJumpGroupType.Daily]);
        _cumulativeView.RefreshListUI(groupDatas);

        RefreshRedDot();
    }

    private void RefreshRedDot()
    {

        bool showDailyReddot = false;
        bool showScheduleRedDot = false;

        foreach (var element in groupDatas)
        {
            if (NotesJumpGroupType.Daily == element.Key)
            {
                var enableItem = element.Value.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
                showDailyReddot = enableItem != null;
            }
            else
            {
                if (showScheduleRedDot)
                {
                    continue;
                }
                var enableItem = element.Value.Find(x => x.eventStatus == (int)TaskClaimState.Enable);
                showScheduleRedDot = enableItem != null;  
            }
        }

        if (tabNames.Count > 1)
        {
            var key1 = tabNames[0];
            var key2 = tabNames[1];

            if (_tabItemInfo.Keys.Contains(key1))
            {
                _tabItemInfo[key1].UpdateRedDot(showDailyReddot);
            }
            
            if (_tabItemInfo.Keys.Contains(key2))
            {
                _tabItemInfo[key2].UpdateRedDot(showScheduleRedDot);
            }
        }
    }
    
    private void OnCategoryItemClick(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }
        
        foreach (var element in _tabItemInfo)
        {
            element.Value.UpdateSelected(element.Key == key);
        }

        var index = tabNames.FindIndex(x => x == key);
        _dailyView.gameObject.SetActive(index == 0);
        _cumulativeView.gameObject.SetActive(index == 1);
    }

    private void ConvertDatas(ActivityInfo info)
    {
        var lists = info.eventList;
        if (lists == null || lists.Count == 0)
        {
            return;
        }

        var fixedDict = new Dictionary<NotesJumpGroupType, List<ActivityEventInfo>>();
        
        foreach (NotesJumpGroupType groupType in System.Enum.GetValues(typeof(NotesJumpGroupType)))
        {
            fixedDict[groupType] = new List<ActivityEventInfo>();
        }

     
        foreach (var element in lists)
        {
            if (element.groupId == (int)NotesJumpGroupType.Daily)
            {
                fixedDict[NotesJumpGroupType.Daily].Add(element);
            }
            else if (element.groupId == (int)NotesJumpGroupType.InstrumentCreation)
            {
                fixedDict[NotesJumpGroupType.InstrumentCreation].Add(element);
            } 
            else if (element.groupId == (int)NotesJumpGroupType.MusicComposition)
            {
                fixedDict[NotesJumpGroupType.MusicComposition].Add(element);
            } 
            else if (element.groupId == (int)NotesJumpGroupType.SoundCreation)
            {
                fixedDict[NotesJumpGroupType.SoundCreation].Add(element);
            } 
            else if (element.groupId == (int)NotesJumpGroupType.InstrumentPlay)
            {
                fixedDict[NotesJumpGroupType.InstrumentPlay].Add(element);
            } 
        }

        groupDatas = fixedDict;
    }

    private void ClaimSuccessHandler(NotesJumpGroupType type, ActivityEventClaimResponse res)
    {
        if (res == null)
        {
            return;
        }

        if (groupDatas.Keys.Contains(type))
        {
            var list = groupDatas[type];
            var d = list.Find(x => x.eventId == res.eventInfo.eventId);
            if (d == null)
            {
                return;
            }

            d.eventStatus = (int)TaskClaimState.Finished;
            RefreshRedDot();
        }
        
        currencyView.UpdateCurrency(res.currencyAmount);
        
        UpdateRedDot();
    }
}
using System.Collections.Generic;
using System.Linq;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;


public enum PetStudioJumpType
{
    Daily = 0,
    EditAvatar = 1,
    MapPlay = 2,
    AvatarCreate = 3,
    AvatarPublish = 4,
}

public class PetStudio : ActivityBaseView
{
    [SerializeField] private Transform bgParent;
    [SerializeField] private Transform tabViewContent;
    [SerializeField] private PetStudioTabItemView tabItemView;
    [SerializeField] private PetStudioDailyView _dailyView;
    [SerializeField] private PetStudioCumulativeView _cumulativeView;
    [SerializeField] private MusicCurrencyView currencyView;
    [SerializeField] private CButton rewardExchangeBtn;

    private Dictionary<string, PetStudioTabItemView> _tabItemInfo = new Dictionary<string, PetStudioTabItemView>();
    private ActivityInfo _info;
    private bool isSending;
    private List<string> tabNames = new List<string>() {  "每日任务", "累计任务" };

    private Dictionary<PetStudioJumpType, List<ActivityEventInfo>> groupDatas =
        new Dictionary<PetStudioJumpType, List<ActivityEventInfo>>();

    public override void Init(ActivityInfo info)
    {
        _info = info;

        ConvertDatas(info);
        
        InitUI();

        AddListeners();
        
        _dailyView.InitListUI(groupDatas[PetStudioJumpType.Daily], info.activityId);
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

        InitBg(bgParent, "#DDE2E8", new List<string>()
        {
            "pet_studio_bg1",
            "pet_studio_bg2",
            "pet_studio_bg3",
            "pet_studio_bg4"
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
        
        currencyView.SetData(ActivityId.PetStudio);
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
       var panel = UIManager.Inst.OpenPanel<ActivityRewardPanel>(PanelId.ActivityRewardPanel,ActivityId.PetStudio);
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
        _dailyView.RefreshListUI(groupDatas[PetStudioJumpType.Daily]);
        _cumulativeView.RefreshListUI(groupDatas);

        RefreshRedDot();
    }

    private void RefreshRedDot()
    {

        bool showDailyReddot = false;
        bool showScheduleRedDot = false;

        foreach (var element in groupDatas)
        {
            if (PetStudioJumpType.Daily == element.Key)
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

        var fixedDict = new Dictionary<PetStudioJumpType, List<ActivityEventInfo>>();
        
        foreach (PetStudioJumpType groupType in System.Enum.GetValues(typeof(PetStudioJumpType)))
        {
            fixedDict[groupType] = new List<ActivityEventInfo>();
        }

     
        foreach (var element in lists)
        {
            if (element.groupId == (int)PetStudioJumpType.Daily)
            {
                fixedDict[PetStudioJumpType.Daily].Add(element);
            }
            else if (element.groupId == (int)PetStudioJumpType.EditAvatar)
            {
                fixedDict[PetStudioJumpType.EditAvatar].Add(element);
            } 
            else if (element.groupId == (int)PetStudioJumpType.MapPlay)
            {
                fixedDict[PetStudioJumpType.MapPlay].Add(element);
            } 
            else if (element.groupId == (int)PetStudioJumpType.AvatarCreate)
            {
                fixedDict[PetStudioJumpType.AvatarCreate].Add(element);
            } 
            else if (element.groupId == (int)PetStudioJumpType.AvatarPublish)
            {
                fixedDict[PetStudioJumpType.AvatarPublish].Add(element);
            } 
        }

        groupDatas = fixedDict;
    }

    private void ClaimSuccessHandler(PetStudioJumpType type, ActivityEventClaimResponse res)
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
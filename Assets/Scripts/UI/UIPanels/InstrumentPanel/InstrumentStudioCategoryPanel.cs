using System.Collections.Generic;
using EventTracking;
using Game.Base;
using Game.CommunityGame;
using Game.PropStore;
using GameData;
using Message;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using UnityEngine;

public class InstrumentStudioCategoryPanel : BasePanel<InstrumentStudioCategoryPanel>
{
    [SerializeField] private Transform _trans_Bg;
    [SerializeField] private CButton backBtn;
    [SerializeField] private CButton instrumentStudioBtn;
    [SerializeField] private CButton instrumentStoreBtn;
    [SerializeField] private CButton musicScoreStudioBtn;
    [SerializeField] private CButton musicScoreStoreBtn;
    [SerializeField] private CButton timbreStoreBtn;

    [SerializeField] private Transform ActiveContestView;
    [SerializeField] private SelectableContestGroupView contestView;
    
    public override void OnCreate()
    {
        InitBG();
        AddListeners();
        LoadEvent.ReportTask(157, 5);
        var showContestTypes = new List<BUDContestType>()
        {
            BUDContestType.Vehicle,
            BUDContestType.Instrument,
            BUDContestType.MusicScore
        };
        bool hasActiveContest = ContestDataManager.Inst.HasActiveContest(showContestTypes);
        ActiveContestView.gameObject.SetActive(hasActiveContest);
        
        if (hasActiveContest)
        {
            contestView.InitViewInfo(showContestTypes);
            contestView.OnSelect = OnSelectContest;
        }
    }
    
    private void InitBG()
    {
        if (_trans_Bg == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_trans_Bg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "music_icon_1", "music_icon_2", "music_icon_3"
        });
        item.gameObject.SetActive(true);
    }

    private void AddListeners()
    {
        backBtn?.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
            UIManager.Inst.ClosePanel(this);
        });
        
        instrumentStudioBtn?.onClick.AddListener(OnClickInstrumentStudio);
        instrumentStoreBtn?.onClick.AddListener(OnClickInstrumentStore);
        musicScoreStudioBtn?.onClick.AddListener(OnClickMusicScoreStudio);
        musicScoreStoreBtn?.onClick.AddListener(OnClickMusicScoreStore);
        timbreStoreBtn?.onClick.AddListener(OnClickTimbreStore);
    }
    private void OnClickInstrumentStudio()
    {
        UIManager.Inst.OpenPanel(PanelId.MusicalInstrumentStudioPanel);
    }
    private void OnClickInstrumentStore()
    {
        var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
        if (fittingRoom)
        {
            fittingRoom.JumpTo(MainTabs.Tab.Ugc, 50024);
        }
        
    }
    private void OnClickMusicScoreStudio()
    {
        UIManager.Inst.OpenPanel(PanelId.MusicScoreStudioPanel);
    }
    private void OnClickMusicScoreStore()
    {
        // var fittingRoom = UIManager.Inst.SwapPanel(PanelId.FittingRoomPanel) as FittingRoomPanel;
        // if (fittingRoom)
        // {
        //     fittingRoom.JumpTo(MainTabs.Tab.Ugc, 60001);
        // }
        UIManager.Inst.OpenPanel(PanelId.MusicStorePanel);
    }

    private void OnClickTimbreStore()
    {
        UIManager.Inst.OpenPanel(PanelId.TimbreStorePanel);
    }
    
    private void OnSelectContest(ContestInfo contestInfo)
    {
        if (contestInfo == null)
        {
            return;
        }
        
        ContestEventManager.Inst.OpenContestPage(contestInfo.contestId);
    }
    
}

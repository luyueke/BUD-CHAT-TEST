using System;
using System.Collections;
using System.Collections.Generic;
using Message;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class NewSeasonPassPanel : BasePanel<NewSeasonPassPanel>
{
    public CButton btn_Back;

    [Header("左侧Tab")] 
    public GameObject Go_TabView;
    public SeasonPassTabItem tabItemPrefab;
    public Transform tabContent;
    private List<SeasonPassTabItem> _tabItems = new List<SeasonPassTabItem>();
    
    [Header("右侧View")]
    public Transform viewContent;
    private SeasonPassBaseView _seasonPassView;
    public Action BackAction;

    public override void OnCreate()
    {
        base.OnCreate();
        InitLeftTab();
        InitUIComponent();
        SeasonPassDataManager.Inst.OnSeasonPassDataChangeHandler += OnSeasonPassDataChange;
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (_seasonPassView != null) return;
        var type = SeasonPassDataManager.Inst.CurrentSeasonPassType;
        var config = SeasonPassDataManager.Inst.GetSeasonPassConfig(type, this.gameObject);
        var viewObj = Loader.Load<GameObject>(config.ViewPrefabPath).Instantiate(viewContent);
        _seasonPassView = viewObj.GetComponent<SeasonPassBaseView>();
        _seasonPassView.InitData(config);
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        MessageHelper.Broadcast(MessageName.RefreshSeasonPassTask);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        SeasonPassDataManager.Inst.OnSeasonPassDataChangeHandler -= OnSeasonPassDataChange;
    }

    private void InitUIComponent()
    {
        btn_Back.onClick.AddListener(OnBackClick);
    }
    
    private void OnBackClick() {
        if (this == null) {
            return;
        }
        CloseSelf();
        BackAction?.Invoke();
    }

    #region 左侧Tab栏
    private void InitLeftTab()
    {
        var type = SeasonPassDataManager.Inst.CurrentSeasonPassType;
        var config = SeasonPassDataManager.Inst.GetSeasonPassConfig(type, this.gameObject);
        if (config != null)
        {
            var itemComp = Instantiate(tabItemPrefab, tabContent);
            itemComp.Init(type, config.TabName, OnTabClick);
            itemComp.gameObject.SetActive(true);
            _tabItems.Add(itemComp);
        }
        SeasonPassDataManager.Inst.GetSeasonPassList(type, (b, rsp) => { });
    }

    private void OnTabClick(SeasonPassType type)
    {
        _tabItems.ForEach(x => x.SetSelect(false));
        var curItem = _tabItems.Find(x => x._curSeasonPassType == type);
        curItem?.SetSelect(true);
        _seasonPassView?.gameObject.SetActive(true);
    }

    #endregion

    #region 外部调用方法

    public void SwitchView(SeasonPassType seasonPassType, string viewName)
    {
        _seasonPassView?.SwitchView(viewName);
    }

    public void RefreshData(SeasonPassType seasonPassType)
    {
        _seasonPassView?.Reload();
    }

    public void SetTabViewEnable(bool isEnable)
    {
        Go_TabView.SetActive(isEnable);
        btn_Back.gameObject.SetActive(isEnable);
    }

    public SeasonPassType GetCurSeasonPassType()
    {
        return SeasonPassDataManager.Inst.CurrentSeasonPassType;
    }
    #endregion

    private void OnSeasonPassDataChange(SeasonPassType type, SeasonPassListRsp rsp)
    {
        var isCanClaim = rsp.seasonLobbyStatus == SeasonLobbyStatus.Claimable;
        var curTabItem = _tabItems.Find(x => x._curSeasonPassType == type);
        curTabItem?.SetRedDot(isCanClaim);
    }
}

public class SeasonPassConfig
{
    public string TabName;
    public string Title;
    public int SeasonPassType;
    public List<string> TaskIdList;
    public string ViewPrefabPath;
    public string DailyTaskConfigPath;
    public string SeasonTaskConfigPath;
    public string WeeklyTaskConfigPath;
}

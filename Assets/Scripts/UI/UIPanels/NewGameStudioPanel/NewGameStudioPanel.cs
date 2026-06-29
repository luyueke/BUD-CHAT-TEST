using System;
using System.Collections.Generic;
using BUD.GameStudio;
using Game.Audio;
using Game.Base;
using GameData;
using GameData.Managers;
using GameData.MapData;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-06-20 15:24:43
/// </summary>
public class NewGameStudioPanel : BasePanel<NewGameStudioPanel>
{
    private NavigationBarTabs _navigationBarTabs;
    private GameStudioDraftAndPublishedPanel _draftPanel;
    private GameStudioDraftAndPublishedPanel _publishedPanel;
    
    #region UIConfig
    private List<TopBarConfig> _topBarConfig = new()
    {
        new(){title = "草稿箱", type = GameStudioTypeEnum.Draft},
        new(){title = "已发布", type = GameStudioTypeEnum.Published}
    };
    
    private enum GameStudioTypeEnum
    {
        Draft,
        Published,
    }
    
    private class TopBarConfig
    {
        public string title;
        public GameStudioTypeEnum type;
    }
    #endregion
    
    public override void OnCreate()
    {
        base.OnCreate();
        BindUI();
        InitSubPanel();
        InitTopBar();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow();
    }

    public override void OnHidden()
    {
        base.OnHidden();
    }

    protected override void OnDestroy()
    {
    }

    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
    }

    public override void OnWindowPop()
    {
    }

    private void BindUI()
    {
        _navigationBarTabs = GameObjectEx.FindChildByName(this.transform, "NavigationBarTabs").GetComponent<NavigationBarTabs>();
        _draftPanel = GameObjectEx.FindChildByName(this.transform, "DraftPanel").GetComponent<GameStudioDraftAndPublishedPanel>();
        _publishedPanel = GameObjectEx.FindChildByName(this.transform, "PublishedPanel").GetComponent<GameStudioDraftAndPublishedPanel>();
    }

    private void InitTopBar()
    {
        _navigationBarTabs.AddBackBtnClickListener(() =>
        {
            UIManager.Inst.BackToLastWindow();
        });
        foreach (var cfg in _topBarConfig)
        {
            _navigationBarTabs.CreateItem(cfg.type.ToString(), cfg.title).SetIsSelect(false);
        }
        _navigationBarTabs.SetSelect(0);
        _navigationBarTabs.AddItemSelectCallBack(OnTopBarItemClick);
    }

    private void OnTopBarItemClick(TabItem item, int index)
    {
        item.SetIsSelect(true);
        _draftPanel.gameObject.SetActive(false);
        _publishedPanel.gameObject.SetActive(false);

        switch ((GameStudioTypeEnum)index)
        {
            case GameStudioTypeEnum.Draft:
                _draftPanel.gameObject.SetActive(true);
                _draftPanel.RefreshView();
                break;
            case GameStudioTypeEnum.Published:
                _publishedPanel.gameObject.SetActive(true);
                _publishedPanel.RefreshView();
                break;
        }
    }

    private void InitSubPanel()
    {
        _draftPanel.Init();
        _publishedPanel.Init();
    }
}
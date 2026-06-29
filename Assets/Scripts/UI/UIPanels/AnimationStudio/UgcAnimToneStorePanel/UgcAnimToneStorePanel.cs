using System;
using System.Collections.Generic;
using Game.Store;
using GameData;
using UnityEngine;
using UI.Base;
using UI.BaseWidgets;

public class UgcAnimToneStorePanel : BasePanel<UgcAnimToneStorePanel>
{
    private Transform _transBG;
    private CButton _btnClose;
    private CButton _searchBtn;
    private UgcAnimToneStoreSectionView sectionView;
    private UgcAnimToneStoreListView listView;
    private UgcAnimToneStoreDetailView detailView;

    public Action<String> DidPurchasedAction;

    private UgcType _lastUgcType = (UgcType)(-1);

    public override void OnCreate()
    {
        base.OnCreate();
        BindUI();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var ugcType = args?.Length > 0 && args[0] is UgcType t ? t : UgcType.AnimMusic;
        if (ugcType != _lastUgcType)
        {
            _lastUgcType = ugcType;
            sectionView.Reload(ugcType);
        }
    }

    public override void OnHidden()
    {
        base.OnHidden();
        detailView.StopPlayIfNeed();
        
        var UgcAnimChooseTonePanel = UIManager.Inst.FindPanel<UgcAnimChooseTonePanel>(PanelId.UgcAnimChooseTonePanel);
        if (UgcAnimChooseTonePanel != null)
        {
            UgcAnimChooseTonePanel.RefreshOwnedList();
        }
    }

    private void BindUI()
    {
        _btnClose = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        _searchBtn = GameObjectEx.FindChildByName(this.transform, "SearchButton").GetComponent<CButton>();
        _transBG = GameObjectEx.FindChildByName(this.transform, "BG");
        sectionView = GameObjectEx.FindChildByName(this.transform, "SectionView").GetComponent<UgcAnimToneStoreSectionView>();

        listView = GameObjectEx.FindChildByName(this.transform, "ListView").GetComponent<UgcAnimToneStoreListView>();
        detailView = GameObjectEx.FindChildByName(this.transform, "DetailView").GetComponent<UgcAnimToneStoreDetailView>();

        InitUI();
        _btnClose.onClick.AddListener(() => { CloseSelf(); });
        _searchBtn.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            detailView.StopPlayIfNeed();
            UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel, SearchPanel.SearchType.UgcAnimMusic);
        });
        sectionView.SelectSectionAction = ShowCurRightPanel;

        detailView.DidPayAssetAction = data =>
        {
            if (this == null)
            {
                return;
            }

            var ugcId = data?.UgcInfo?.id;
            if (!string.IsNullOrEmpty(ugcId))
            {
                DidPurchasedAction?.Invoke(ugcId);
            }
            
            listView.UpdateSingleItem(data);
        };
    }

    private void InitUI()
    {
        if (_transBG == null)
        {
            return;
        }

        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(_transBG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#FFFFFF", atlasPath, new List<string>()
        {
            "animStudio_icon1", "animStudio_icon2", "animStudio_icon3"
        });
        item.gameObject.SetActive(true);
    }
    
    private void ShowCurRightPanel(string sectionId)
    {
        listView.SetActions(sectionId, OnSelectPropStoreItem, null);
    }

    private void OnSelectPropStoreItem(RecommendItemData data)
    {
        detailView.RefreshUIByData(data);
    }
}
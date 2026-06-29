using System;
using System.Collections.Generic;
using Game.Store;
using UnityEngine;
using UI.Base;
using UI.BaseWidgets;

public class TimbreStorePanel : BasePanel<TimbreStorePanel>
{
    private Transform _transBG;
    private CButton _btnClose;
    private CButton _searchBtn;
    private TimbreStoreSectionView sectionView;
    private TimbreStoreListView listView;
    private TimbreStoreDetailView detailView;

    public Action<String> DidPurchasedAction;
    
    public override void OnCreate()
    {
        base.OnCreate();
        BindUI();
        
        sectionView.Reload();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        detailView.StopPlayIfNeed();
    }

    private void BindUI()
    {
        _btnClose = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        _searchBtn = GameObjectEx.FindChildByName(this.transform, "SearchButton").GetComponent<CButton>();
        _transBG = GameObjectEx.FindChildByName(this.transform, "BG");
        sectionView = GameObjectEx.FindChildByName(this.transform, "SectionView").GetComponent<TimbreStoreSectionView>();

        listView = GameObjectEx.FindChildByName(this.transform, "ListView").GetComponent<TimbreStoreListView>();
        detailView = GameObjectEx.FindChildByName(this.transform, "DetailView").GetComponent<TimbreStoreDetailView>();

        InitUI();
        _btnClose.onClick.AddListener(() => { CloseSelf(); });
        _searchBtn.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            detailView.StopPlayIfNeed();
            UIManager.Inst.OpenPanel<SearchPanel>(PanelId.SearchPanel, SearchPanel.SearchType.MusicTone);
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
            "store_icon4", "store_icon5", "store_icon6"
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
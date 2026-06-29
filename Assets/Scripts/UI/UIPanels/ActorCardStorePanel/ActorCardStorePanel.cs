using System;
using System.Collections.Generic;
using Game.Store;
using UnityEngine;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;

// 演员卡商城面板：从 FittingRoomPanel 的「Ugc 标签 + 演员卡(ActorCard)分类」流程迁出，
// 结构参照 MusicStorePanel（乐谱商城），使用独立的 ActorCardStore* 视图，数据为演员卡(UgcType.ActorCard)。
// 详情区由 ActorCardStoreDetailView 自建预览角色展示演员服装（可切换），购买走 ActorShopCarPanel（与原商城一致）。
public class ActorCardStorePanel : BasePanel<ActorCardStorePanel>
{
    private Transform _transBG;
    private CButton _btnClose;
    private CButton _searchBtn;
    public CButton BtnBuyPinkCoin;
    private ActorCardStoreSectionView sectionView;
    private ActorCardStoreListView listView;
    private ActorCardStoreDetailView detailView;
    private string _curSectionId;

    public Action<String> DidPurchasedAction;

    public override void OnCreate()
    {
        base.OnCreate();
        BindUI();
        // 预拉搜索热词（searchDiscoverAndHistoryView 的发现页数据），与原商城 FittingRoomPanel.OnCreate 一致
        SearchLogicMgr.Inst.PreGetData();
        BtnBuyPinkCoin.onClick.AddListener(() =>
        {
            UIManager.Inst.OpenPanelTakeAni<GetMorePinkCoinPanel>(PanelId.GetMorePinkCoinPanel);
        });
        sectionView.Reload();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        // 隐藏 FittingRoomPanel 的角色模型，避免两个模型同时显示（参照 ActorShopCarPanel）。
        // 隐藏前先停掉试衣间的乐谱/乐器试听：PlayMusicScoreBev 的音节回调会在角色上
        // StartCoroutine，角色 inactive 后会持续报错
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null)
        {
            fittingRoom.CancelPreviewMusicScore();
            fittingRoom.CancelPreviewMusicalInstrument();
            fittingRoom.characterRoot.gameObject.SetActive(false);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // 恢复 FittingRoomPanel 的角色模型
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.characterRoot.gameObject.SetActive(true);
    }

    private void BindUI()
    {
        _btnClose = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        _searchBtn = GameObjectEx.FindChildByName(this.transform, "SearchButton").GetComponent<CButton>();
        _transBG = GameObjectEx.FindChildByName(this.transform, "BG");
        sectionView = GameObjectEx.FindChildByName(this.transform, "SectionView").GetComponent<ActorCardStoreSectionView>();

        listView = GameObjectEx.FindChildByName(this.transform, "ListView").GetComponent<ActorCardStoreListView>();
        detailView = GameObjectEx.FindChildByName(this.transform, "DetailView").GetComponent<ActorCardStoreDetailView>();

        InitUI();
        _btnClose.onClick.AddListener(() => { CloseSelf(); });
        // 顶部搜索按钮与 DetailView 上的搜索按钮等效：都打开 SearchView
        _searchBtn.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            detailView.OpenSearchView();
        });
        sectionView.SelectSectionAction = ShowCurRightPanel;

        // 搜索结果灌入列表（搜索翻页由 ListView 上拉触发 nextAction）
        detailView.SearchResultAction = (datas, nextAction) =>
        {
            if (this == null)
            {
                return;
            }
            listView.ShowSearchResults(datas, nextAction);
        };
        // 取消搜索：退出搜索模式并恢复当前栏目列表
        detailView.SearchCancelAction = () =>
        {
            if (this == null)
            {
                return;
            }
            listView.ExitSearchMode();
            if (!string.IsNullOrEmpty(_curSectionId))
            {
                ShowCurRightPanel(_curSectionId);
            }
        };

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
            "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
        });
        item.gameObject.SetActive(true);
    }

    private void ShowCurRightPanel(string sectionId)
    {
        _curSectionId = sectionId;
        listView.SetActions(sectionId, OnSelectPropStoreItem, null);
    }

    private void OnSelectPropStoreItem(RecommendItemData data)
    {
        detailView.RefreshUIByData(data);
    }
}

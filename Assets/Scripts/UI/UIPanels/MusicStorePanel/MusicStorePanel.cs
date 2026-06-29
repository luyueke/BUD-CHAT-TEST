using System;
using System.Collections.Generic;
using Game.Store;
using UnityEngine;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;

// 乐谱商城面板：从 FittingRoomPanel 的「Ugc 标签 + 乐谱(MusicScore)分类」流程迁出，
// 结构参照 TimbreStorePanel（音色商城），但全部使用独立的 MusicStore* 视图，数据为乐谱(UgcType.MusicScore)。
// 试听由 MusicStoreDetailView 在面板内自建预览角色完成（与 FittingRoom 一致）。
public class MusicStorePanel : BasePanel<MusicStorePanel>
{
    private Transform _transBG;
    private CButton _btnClose;
    private CButton _searchBtn;
    public CButton BtnBuyPinkCoin;
    private MusicStoreSectionView sectionView;
    private MusicStoreListView listView;
    private MusicStoreDetailView detailView;
    private string _curSectionId;

    public Action<String> DidPurchasedAction;

    private AmbientLightSetting _srcLightSetting;
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;

    public override void OnCreate()
    {
        base.OnCreate();
        BindUI();
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

        _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
        _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
        _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
        _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        // 恢复 FittingRoomPanel 的角色模型
        var fittingRoom = UIManager.Inst.FindPanel<FittingRoomPanel>(PanelId.FittingRoomPanel);
        if (fittingRoom != null) fittingRoom.characterRoot.gameObject.SetActive(true);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        detailView.StopPlayIfNeed();
        AmbientLightManager.Inst.CloseUILight(_srcLightSetting);
        AmbientLightManager.Inst.RevertHallLight(_srcHallLightVisible);
        AmbientLightManager.Inst.RevertGameSceneLight(_srcGameSceneLightVisible);
        AmbientLightManager.Inst.RevertPreviewLight(_srcPreviewSceneLightVisible);
    }

    private void BindUI()
    {
        _btnClose = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        _searchBtn = GameObjectEx.FindChildByName(this.transform, "SearchButton1").GetComponent<CButton>();
        _transBG = GameObjectEx.FindChildByName(this.transform, "BG");
        sectionView = GameObjectEx.FindChildByName(this.transform, "SectionView").GetComponent<MusicStoreSectionView>();

        listView = GameObjectEx.FindChildByName(this.transform, "ListView").GetComponent<MusicStoreListView>();
        detailView = GameObjectEx.FindChildByName(this.transform, "DetailView").GetComponent<MusicStoreDetailView>();

        InitUI();
        _btnClose.onClick.AddListener(() => { CloseSelf(); });
        _searchBtn.onClick.AddListener(() =>
        {
            if (this == null)
            {
                return;
            }
            detailView.OpenSearchView();
        });
        sectionView.SelectSectionAction = ShowCurRightPanel;

        detailView.SearchResultAction = (datas, nextAction) =>
        {
            if (this == null)
            {
                return;
            }
            listView.ShowSearchResults(datas, nextAction);
        };
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

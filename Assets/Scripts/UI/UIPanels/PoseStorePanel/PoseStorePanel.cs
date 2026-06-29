using System;
using System.Collections.Generic;
using Game.Store;
using UnityEngine;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.FittingRoom;
using Game.Avatar;
using Game.Config;

// 姿势商城面板：从 FittingRoomPanel 的「Ugc 标签 + 姿势(UgcPose)分类」流程迁出，
// 结构参照 ActorCardStorePanel，使用独立的 PoseStore* 视图，数据为姿势(UgcType.Pose)。
// 详情区由 PoseStoreDetailView 自建带 IK 的预览角色应用姿势，购买为直购（同 OperationView 的 BuyBtn）。
public class PoseStorePanel : BasePanel<PoseStorePanel>
{
    private Transform _transBG;
    private CButton _btnClose;
    private CButton _searchBtn;
    public CButton BtnBuyPinkCoin;
 
    private PoseStoreSectionView sectionView;
    private PoseStoreListView listView;
    private PoseStoreDetailView detailView;
    private string _curSectionId;

    public Action<String> DidPurchasedAction;
    // 面板关闭时回调（参照 FittingRoomPanel.OnCloseAction），供外部监听面板关闭后刷新
    public Action OnCloseAction;

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
        if (detailView.BtnChangeOc != null)
        {
            detailView.BtnChangeOc.onClick.AddListener(() =>
            {
                UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel,
                    OcChangeScene.DoubleEmote).OnCloseAction = (avatarData) =>
                {
                    if (avatarData != null)
                    {
                        try
                        {
                            var data = (CharacterData)avatarData;
                            PlayerPrefs.SetString(GameConsts.EmoteOtherPlayerOcKey + AccountDataManager.Inst.Uid,
                                CharacterData.SerializeObject(data));
                        }
                        catch { }
                    }
                    detailView.ResetAndRePreview();
                };
            });
        }
        sectionView.Reload();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        // 隐藏 FittingRoomPanel 的角色模型，避免两个模型同时显示（参照 ActorShopCarPanel）。
        // 隐藏前先停掉试衣间的乐谱/乐器试听，避免音节回调在已隐藏角色上 StartCoroutine 报错
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
        OnCloseAction?.Invoke();
        OnCloseAction = null;
    }

    private void BindUI()
    {
        _btnClose = GameObjectEx.FindChildByName(this.transform, "BackButton").GetComponent<CButton>();
        _searchBtn = GameObjectEx.FindChildByName(this.transform, "SearchButton").GetComponent<CButton>();
        _transBG = GameObjectEx.FindChildByName(this.transform, "BG");
        sectionView = GameObjectEx.FindChildByName(this.transform, "SectionView").GetComponent<PoseStoreSectionView>();

        listView = GameObjectEx.FindChildByName(this.transform, "ListView").GetComponent<PoseStoreListView>();
        detailView = GameObjectEx.FindChildByName(this.transform, "DetailView").GetComponent<PoseStoreDetailView>();

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

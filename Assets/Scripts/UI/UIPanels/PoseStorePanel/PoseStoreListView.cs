using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;
using UnityEngine;

// 姿势商城列表视图：结构与 ActorCardStoreListView 一致，列表首位固定「去创作」入口。
public class PoseStoreListView : MonoBehaviour
{
    public PoseStoreListAdapter adapter;
    private PoseStoreListDataLoader requester = new PoseStoreListDataLoader();
    // 搜索模式：非空时上拉翻页走搜索的下一页而非栏目分页
    private Action _searchNextAction;
    private bool _isSearchMode = false;

    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdatedAct.AddListener(refreshController.HideGizmo);
    }

    public void ResetAdpater()
    {
        if (adapter != null && adapter.IsInitialized)
        {
            adapter.ResetItems(0);
        }
    }

    public void SetActions(string sectionId, Action<RecommendItemData> onSelectItemAct, Action isEmptyAction)
    {
        _isSearchMode = false;
        _searchNextAction = null;
        requester.RefreshSectionId(sectionId, 0);
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.EmptyDataAct = isEmptyAction;
        GetFirstPageDatas();
    }

    // 搜索模式：展示搜索结果（SearchGoodsData 每次回调给的是累积全量列表，直接整表刷新）。
    // 搜索结果不插「去创作」占位，与原商城搜索页一致。
    public void ShowSearchResults(List<RecommendItemData> datas, Action nextAction)
    {
        bool firstShow = !_isSearchMode;
        _isSearchMode = true;
        _searchNextAction = nextAction;

        ResetAdpater();
        adapter.OnItemsUpdatedAct?.Invoke();
        var list = datas ?? new List<RecommendItemData>();
        adapter.Data.ResetItems(list);
        // 仅首次进入搜索结果时自动选中第一项，翻页刷新不打断当前选中
        if (firstShow && list.Count > 0)
        {
            adapter.SetDataSelected(list[0]);
        }
    }

    // 退出搜索模式（取消/清空），由面板随后恢复栏目数据
    public void ExitSearchMode()
    {
        _isSearchMode = false;
        _searchNextAction = null;
    }

    public void GetFirstPageDatas(Action<List<RecommendItemData>> complete = null)
    {
        requester.GetSectionInfoData(resultAction: datas =>
        {
            complete?.Invoke(datas);
            ResetAdpater();
            adapter.OnItemsUpdatedAct?.Invoke();
            var list = datas ?? new List<RecommendItemData>();
            // 列表首位固定为「去创作」入口占位项
            list.Insert(0, PoseStoreItemView.CreateDesignEntryData());
            adapter.Data.ResetItems(list);
            // 默认选中第一个真实商品（跳过「去创作」占位）
            if (list.Count > 1)
            {
                adapter.SetDataSelected(list[1]);
            }
        });
    }

    public void RemoveSingleItem(string mapId)
    {
        adapter.RemoveSingleItem(mapId);
    }

    public void UpdateSingleItem(RecommendItemData draftListItem)
    {
        if (draftListItem == null)
        {
            return;
        }
        adapter.UpdateSingleItem(draftListItem);
    }

    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            if (_isSearchMode)
            {
                // 搜索模式：拉搜索下一页（结果经 ShowSearchResults 整表刷新）
                _searchNextAction?.Invoke();
                return;
            }
            requester.GetSectionInfoData(OnReceivedNewModelsForInsert);
        }
    }

    void OnReceivedNewModelsForInsert(List<RecommendItemData> newModels)
    {
        if (newModels == null || newModels.Count == 0)
        {
            adapter.OnItemsUpdatedAct?.Invoke();
            return;
        }

        adapter.Data.InsertItems(adapter.GetItemsCount(), newModels);
        adapter.OnItemsUpdatedAct?.Invoke();
    }
}

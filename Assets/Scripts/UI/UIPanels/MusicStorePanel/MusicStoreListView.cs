using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;
using UnityEngine;

// 乐谱商城列表视图：从 FittingRoom 的 Ugc+乐谱 流程迁出，结构与 TimbreStoreListView 一致。
public class MusicStoreListView : MonoBehaviour
{
    public MusicStoreListAdapter adapter;
    private MusicStoreListDataLoader requester = new MusicStoreListDataLoader();
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

    public void ShowSearchResults(List<RecommendItemData> datas, Action nextAction)
    {
        bool firstShow = !_isSearchMode;
        _isSearchMode = true;
        _searchNextAction = nextAction;

        ResetAdpater();
        adapter.OnItemsUpdatedAct?.Invoke();
        var list = datas ?? new List<RecommendItemData>();
        adapter.Data.ResetItems(list);
        if (firstShow && list.Count > 0)
        {
            adapter.SetDataSelected(list[0]);
        }
    }

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
            if (datas != null && datas.Count > 0)
            {
                adapter.Data.ResetItems(datas);
                adapter.SetDataSelected(datas[0]);
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

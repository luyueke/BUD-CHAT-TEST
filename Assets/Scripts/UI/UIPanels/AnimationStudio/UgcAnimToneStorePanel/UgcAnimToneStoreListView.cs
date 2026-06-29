
using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.Store;
using UnityEngine;

public class UgcAnimToneStoreListView: MonoBehaviour 
{
    public  UgcAnimToneStoreListAdapter adapter;
    private UgcAnimToneStoreListDataLoader requester = new UgcAnimToneStoreListDataLoader();
    
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

    public void SetActions(string sectionId, Action<RecommendItemData> onSelectItemAct,  Action isEmptyAction)
    {
        requester.RefreshSectionId(sectionId, 0);
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.EmptyDataAct = isEmptyAction;
        GetFirstPageDatas();
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

using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using UnityEngine;

public class ContestListViewEntry : MonoBehaviour
{
    public ContestListViewAdapter adapter;
    private ContestRequester requester = new ContestRequester();

    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdatedAct.AddListener(refreshController.HideGizmo);
    }

    public bool isInSearch
    {
        get
        {
            return requester?.isInSearch ?? false;
        }
    }

    public void ResetAdpater()
    {
        if (adapter != null && adapter.IsInitialized)
        {
            adapter.ResetItems(0);
            adapter.ClearPool();
        }
    }

    public void SetActions(ContestPageType pageType, ContestInfo contestInfo, Action<ContestEntryInfo> onSelectItemAct,  Action isEmptyAction, bool hideRankForce = false)
    {
        requester.Init(pageType, contestInfo.contestId);
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.EmptyDataAct = isEmptyAction;
        adapter.hideRankForce = hideRankForce;
        adapter.bUDContestType = contestInfo.CurrentContestType;
    }

    public void EnterSearch(string key)
    {
        requester.EnterSearch(key);
    }
    
    public void LeaveSearch()
    {
        requester.LeaveSearch();
    }

    public void GetFirstPageDatas(Action<List<ContestEntryInfo>> complete)
    {
        requester.ResetCookie();
        requester.GetData(true, resultAction: datas =>
        {
            complete?.Invoke(datas);
            ResetAdpater();
            adapter.OnItemsUpdatedAct?.Invoke();
            if (datas != null && datas.Count > 0)
            {
                adapter.Data.ResetItems(datas);
            }
        });
    }
    
    public void RemoveSingleItem(string cId)
    {
        adapter.RemoveSingleItem(cId);
    }

    public void UpdateSingleItem(ContestEntryInfo data)
    {
        adapter.UpdateSingleItem(data);
    }

    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            requester.GetData(false, OnReceivedNewModelsForInsert);
        }
    }

    void OnReceivedNewModelsForInsert(List<ContestEntryInfo> newModels)
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

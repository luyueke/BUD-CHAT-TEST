
using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using UnityEngine;

public class ContestCreateOcEntry : MonoBehaviour
{
    public ContestCreateOcAdapter adapter;
    private ContestCreateOcRequester requester = new ContestCreateOcRequester();
    private bool IsPet = false;
    
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
            adapter.ClearPool();
        }
    }

    public void SetActions(bool isPet = false, Action<bool, AvatarOcFixData> onSelectItemAct = null,  Action isEmptyAction = null)
    {
        IsPet = isPet;
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.EmptyDataAct = isEmptyAction;
    }

    public void GetFirstPageDatas(Action<List<AvatarOcFixData>> complete)
    {
        requester.ResetCookie();
        requester.GetData(true, IsPet, resultAction: datas =>
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

    public void UpdateSelect(AvatarOcFixData ocData, bool isSelect)
    {
        if (ocData == null)
        {
            return;
        }
    }
    
    public void RemoveSingleItem(string cId)
    {
        adapter.RemoveSingleItem(cId);
    }

    public void UpdateSingleItem(AvatarOcFixData data)
    {
        adapter.UpdateSingleItem(data);
    }

    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            requester.GetData(false, IsPet, OnReceivedNewModelsForInsert);
        }
    }

    void OnReceivedNewModelsForInsert(List<AvatarOcFixData> newModels)
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

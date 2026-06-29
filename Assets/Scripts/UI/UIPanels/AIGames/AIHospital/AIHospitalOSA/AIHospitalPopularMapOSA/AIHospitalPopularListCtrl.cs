using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.CommunityGame;
using UI.TopList;

public class AIHospitalPopularListCtrl : MonoBehaviour
{
    public AIHospitalPopularAdaptar Adapter;
    public PullToRefreshBehaviour refreshController;
    public AIHospitalPopularDataLoader DataLoader;
    
    private Action _onGetData;
    private Action<bool> _onHasData;

    private bool _bInit = false;
    private void Start()
    {
        InitOSA();
    }

    private void InitOSA()
    {
        if (refreshController != null)
        {
            refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        }

        if (Adapter != null)
        {
            Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
            Adapter.Data = new SimpleDataHelper<RankItem>(Adapter);
            Adapter.Init();
        }
    }

    public void OnSelectSection( Action act = null, Action<bool> hasData = null)
    {
        if (_bInit) return;

        this._onGetData = act;
        this._onHasData = hasData;
        gameObject.SetActive(true);
        HideGizmo();
        DataLoader.GetSectionInfoData(OnGetFirstPageDatas);
    }
    
    public void ResetAdapter()
    {
        if(!Adapter.IsInitialized)
            return;
        
        Adapter.ResetItems(0);
        Adapter.ClearPool();
    }
    
    private void HideGizmo()
    {
        if (refreshController != null)
        {
            refreshController.HideGizmo();
        }
    }
    
    public virtual void OnGetFirstPageDatas(List<RankItem> rankItems)
    {
        ResetAdapter();
        this._onGetData?.Invoke();

        if (rankItems == null || rankItems.Count == 0)
        {
            this._onHasData?.Invoke(false);
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }
        _bInit = true;
        this._onHasData?.Invoke(true);
        Adapter.Data.ResetItems(rankItems);
        Adapter.OnItemsUpdated?.Invoke();
    }
    
    private void OnPullReleased()
    {
        DataLoader.GetSectionInfoData(OnReceivedNewModelsForInsert);
    }
    
    private void OnReceivedNewModelsForInsert(List<RankItem> rankItems)
    {
        if (rankItems == null || rankItems.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }

        Adapter.Data.List.AddRange(rankItems);
        Adapter.Refresh(false);
    }

    private void OnDestroy()
    {
        if (refreshController != null)
        {
            refreshController.OnRefreshWithSlideUp.RemoveListener(OnPullReleased);
        }

        if (Adapter != null)
        {
            Adapter.OnItemsUpdated.RemoveListener(refreshController.HideGizmo);
        }
    }

    #region Public Methods
    public void RefreshList()
    {
        //if (!string.IsNullOrEmpty(_curSectionId))
        //{
            DataLoader.GetSectionInfoData(OnGetFirstPageDatas);
        //}
    }

    public void ClearList()
    {
        ResetAdapter();
    }
    #endregion
}

using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.CommunityGame;

public class AIHospitalOfficialListCtrl : MonoBehaviour
{
    [SerializeField]
    public AIHospitalOfficialAdaptar Adapter;
    [SerializeField]
    public PullToRefreshBehaviour refreshController;
    [SerializeField]
    public S9ReccommendInfoDataLoader DataLoader;

    private string _curSectionId;
    private Action _onGetData;
    private Action<bool> _onHasData;

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
            Adapter.Data = new SimpleDataHelper<RecommendItemData>(Adapter);
            Adapter.Init();
        }
    }

    public void OnSelectSection(string sectionId, Action act = null, Action<bool> hasData = null)
    {
        this._curSectionId = sectionId;
        this._onGetData = act;
        this._onHasData = hasData;
        HideGizmo();
        DataLoader.RefreshSectionId(this._curSectionId);
        DataLoader.GetSectionInfoData(OnGetFirstPageDatas);
    }

    public void ResetAdapter()
    {
        if (!Adapter.IsInitialized)
            return;
        
        // 重置适配器，清空所有项
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

    public virtual void OnGetFirstPageDatas(List<RecommendItemData> recommendItemDatas)
    {
        ResetAdapter();
        this._onGetData?.Invoke();

        if (recommendItemDatas == null || recommendItemDatas.Count == 0)
        {
            this._onHasData?.Invoke(false);
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }

        this._onHasData?.Invoke(true);
        Adapter.Data.ResetItems(recommendItemDatas);
        Adapter.OnItemsUpdated?.Invoke();
    }

    private void OnPullReleased()
    {
        DataLoader.GetSectionInfoData(OnReceivedNewModelsForInsert);
    }

    private void OnReceivedNewModelsForInsert(List<RecommendItemData> recommendItemDatas)
    {
        if (recommendItemDatas == null || recommendItemDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }

        Adapter.Data.List.AddRange(recommendItemDatas);
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
        if (!string.IsNullOrEmpty(_curSectionId))
        {
            DataLoader.GetSectionInfoData(OnGetFirstPageDatas);
        }
    }

    public void ClearList()
    {
        ResetAdapter();
    }
    #endregion
}

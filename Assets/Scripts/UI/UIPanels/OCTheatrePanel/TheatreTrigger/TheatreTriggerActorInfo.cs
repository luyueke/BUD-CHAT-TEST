using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;
using UnityEngine;

public class TheatreTriggerActorInfo : MonoBehaviour
{
    [SerializeField] public TheatreTriggerActorAdapter Adapter;
    [SerializeField] public PullToRefreshBehaviour refreshController;
    [SerializeField] public TheatreTriggerActorDataLoader DataLoader;

    private void Start()
    {
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        Adapter.Data = new SimpleDataHelper<DraftListItem>(Adapter);
        Adapter.Init();
    }

    public void GetData()
    {
        refreshController.HideGizmo();
        DataLoader.Init();
        DataLoader.GetData(OnGetFirstPage);
    }

    public void ResetAdapter()
    {
        if (!Adapter.IsInitialized) return;
        Adapter.ResetItems(0);
        Adapter.ClearPool();
    }

    public void SetOnSelectAct(Action<DraftListItem> act)
    {
        Adapter.OnSelectItemAct = act;
    }

    public void SetSelectedId(string id)
    {
        Adapter.SelectedId = id;
        if (Adapter.IsInitialized)
            Adapter.Refresh(false);
    }

    private void OnPullReleased()
    {
        DataLoader.GetData(OnGetMorePages);
    }

    private void OnGetFirstPage(List<DraftListItem> list)
    {
        ResetAdapter();
        Adapter.Data.ResetItems(list ?? new List<DraftListItem>());
        Adapter.Refresh(false);
    }

    private void OnGetMorePages(List<DraftListItem> list)
    {
        if (list == null || list.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }
        Adapter.Data.List.AddRange(list);
        Adapter.Refresh(false);
    }
}

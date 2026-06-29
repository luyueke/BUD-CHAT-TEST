using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData.BaseInfo;
using UI.BaseWidgets;

public class TheatreEditorStudioInfoPanel : MonoBehaviour
{
    public TheatreEditorStudioAdapter Adapter;
    public PullToRefreshBehaviour refreshController;
    public TheatreEditorStudioDataLoader DataLoader;
    public TheatreStudioSubType studioType;

    private void Start()
    {
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        Adapter.Data = new SimpleDataHelper<DraftListItem>(Adapter);
        Adapter.Init();
    }

    public void GetData()
    {
        HideGizmo();
        DataLoader.InitData(studioType);
        DataLoader.GetTheatreStudioList(OnGetFirstPageDatas);
    }

    public void ResetAdpater()
    {
        if (!Adapter.IsInitialized)
            return;
        Adapter.ResetItems(0);
        Adapter.ClearPool();
    }

    private void HideGizmo()
    {
        refreshController.HideGizmo();
    }

    public virtual void OnGetFirstPageDatas(List<DraftListItem> ListDatas)
    {
        ResetAdpater();
        if (ListDatas == null || ListDatas.Count == 0)
        {
            ListDatas = new List<DraftListItem>();
        }

        if (studioType == TheatreStudioSubType.Drafts)
        {
            var firstItem = new DraftListItem();
            ListDatas.Insert(0, firstItem);
        }

        Adapter.Data.ResetItems(ListDatas);
        Adapter.OnItemsUpdated?.Invoke();
    }

    private void OnPullReleased()
    {
        DataLoader.GetTheatreStudioList(OnReceivedNewModelsForInsert);
    }

    private void OnReceivedNewModelsForInsert(List<DraftListItem> ListDatas)
    {
        if (ListDatas == null || ListDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }
        Adapter.Data.List.AddRange(ListDatas);
        Adapter.Refresh(false);
    }

    public void SetItemOnClickAct(Action<DraftListItem> act)
    {
        Adapter.OnSelectItemAct = act;
    }
}

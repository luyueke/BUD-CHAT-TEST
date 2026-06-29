using System;
using System.Collections.Generic;
using BUD.GameStudio;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset.Draft;
using UnityEngine;
using UnityEngine.Events;

public class AssetStudioEntry : MonoBehaviour
{
    public AssetStudioAdapter adapter;
    private GameStudioDataManager requester = new GameStudioDataManager();
    private StudioSubType _studioSubType;

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

    public void SetActions(StudioSubType studioSubType, MainViewType viewType, Action<DraftListItem> onSelectItemAct, Action<DraftListItem, AvatarStudioBaseItem> uploadAction, Action isEmptyAction, Action CreateAction)
    {
        requester.SetData(studioSubType, viewType);
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.UploadAct = uploadAction;
        adapter.EmptyDataAct = isEmptyAction;
        adapter.CreateDataAct = CreateAction;
        this._studioSubType = studioSubType;
        
    }

    public void GetFirstPageDatas(Action<List<DraftListItem>> complete)
    {
        requester.ResetCookie();
        requester.GetGameStudioDraftsData(true, resultAction: datas =>
        {
            //如果是草稿箱-需要增加创建按钮
            if(this._studioSubType == StudioSubType.Drafts)
                datas.Insert(0,null);
            
            complete?.Invoke(datas);
            ResetAdpater();
            adapter.OnItemsUpdatedAct?.Invoke();
            if (datas != null && datas.Count > 0)
            {
                adapter.Data.ResetItems(datas);
            }
        });
    }
    
    public void RemoveSingleItem(string mapId)
    {
        adapter.RemoveSingleItem(mapId);
    }

    public void UpdateSingleItem(DraftListItem draftListItem)
    {
        adapter.UpdateSingleItem(draftListItem);
    }

    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            requester.GetGameStudioDraftsData(false, OnReceivedNewModelsForInsert);
        }
    }

    void OnReceivedNewModelsForInsert(List<DraftListItem> newModels)
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

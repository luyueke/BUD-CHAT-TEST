using System;
using System.Collections.Generic;
using BUD.GameStudio;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset.Draft;
using UnityEngine;
using UnityEngine.Events;

public class GameStudioEntry : MonoBehaviour
{
    public GameStudioAdapter adapter;
    private StudioSubType _studioSubType;
    private bool _isEnd;
    private string _cookie;
    private int _gameType;

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
            adapter.StudioSubType = this._studioSubType;
        }
    }

    public void SetActions(Action<DraftListItem> onSelectItemAct, Action<DraftListItem, DraftsItem> uploadAction, Action isEmptyAction, StudioSubType studioSubType, GameType gameType = GameType.Normal)
    {
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.UploadAct = uploadAction;
        adapter.EmptyDataAct = isEmptyAction;
        this._studioSubType = studioSubType;
        this._gameType = (int)gameType;
    }

    public void GetFirstPageDatas(Action<List<DraftListItem>> complete)
    {
        ResetCookies();
        GetStudioDatas(datas =>
        {
            //如果是草稿箱-需要增加创建按钮
            if(this._studioSubType == StudioSubType.Drafts)
                datas.Insert(0,null);
            
            complete?.Invoke(datas);
            ResetAdpater();
            adapter.OnItemsUpdatedAct?.Invoke();
            adapter.Data.ResetItems(datas);
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
            GetStudioDatas(OnReceivedNewModelsForInsert);
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

    private void ResetCookies()
    {
        this._cookie = "";
        this._isEnd = false;
    }
    
    private void GetStudioDatas(UnityAction<List<DraftListItem>> resultAction = null)
    {
        if(_isEnd)
            return;
        
        if(!this)
            return;
        
        var req = new MapListReq
        {
            gameType = _gameType,
            cookie = this._cookie,
            uid = AccountDataManager.Inst.Uid,
        };

        var reqHead = _studioSubType == StudioSubType.Drafts ? HttpUrlDefine.createList : HttpUrlDefine.publishList;
        NetworkManager.Inst.SendHttpRequest(reqHead, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                MapListResponse mapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
                this._isEnd = mapListResponse.isEnd == 1;
                this._cookie = mapListResponse.cookie;

                if (mapListResponse.list == null)
                {
                    mapListResponse.list = new List<DraftListItem>();
                }

                resultAction?.Invoke(mapListResponse.list);
            },
            (error) =>
            {
                resultAction?.Invoke(new List<DraftListItem>());
            });
    }
    
}
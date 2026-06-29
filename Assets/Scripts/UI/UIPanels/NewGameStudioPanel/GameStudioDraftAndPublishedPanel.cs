using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UGCAsset;
using UGCAsset.Draft;
using UnityEngine;

public class GameStudioDraftAndPublishedPanel : MonoBehaviour
{
    public StudioSubType Cur_StudioSubType;
    public NewGameStudioDataLoader _dataLoader;
    public GameObject _emptyText;
    public NewGameStudioAdapter _adapter;
    private PullToRefreshBehaviour refreshController;

    private void Awake()
    {
        //需要动态拉取数据必须要做的初始化操作
        refreshController = _adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        _adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        _adapter.Init();
        ResetAdpater();
    }
    
    public void Init()
    {
        RefreshView();
    }
    
    public void ResetAdpater()
    {
        if (refreshController != null)
            refreshController.isDrag = false;
        // Resetting to 0 count clears everything, including visible items, so nothing will be recycled
        _adapter.ResetItems(0);
        _adapter.ClearPool();
    }
    
    public void RefreshView()
    {
        ResetAdpater();
        _dataLoader.InitData(Cur_StudioSubType, UgcType.Map);
        _dataLoader.SetCallBack(OnGetDatas);
        _dataLoader.GetDatas();
        _adapter.SetOnClickAct(OnGameItemClick);
    }
    
    public void OnGetDatas(List<DraftListItem> datas)
    {
        if (datas == null || datas.Count == 0)
        {
            _adapter.OnItemsUpdated?.Invoke();
            return;
        }
        
        _adapter.Data.ResetItems(datas);
    }
    
    public void OnPullReleased()
    {
        if (_dataLoader != null)
        {
            _dataLoader.SetCallBack(OnReceivedNewModelsForInsert);
            _dataLoader.GetDatas();
        }
    }

    private void OnReceivedNewModelsForInsert(List<DraftListItem> newModels)
    {
        if (newModels == null || newModels.Count == 0)
        {
            _adapter.OnItemsUpdated?.Invoke();
            return;
        }

        _adapter.Data.List.AddRange(newModels);
        _adapter.Refresh();
    }

    private void OnGameItemClick(DraftListItem data)
    {
        //TODO
        
    }

}

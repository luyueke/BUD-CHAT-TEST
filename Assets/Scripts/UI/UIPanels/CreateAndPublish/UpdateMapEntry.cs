using System;
using System.Collections.Generic;
using BUD.GameStudio;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

public class UpdateMapEntry : MonoBehaviour
{
    public UpdateMapAdapter adapter;
    private UpdateMapDataManager dataLoader;
    private List<DraftListItem> allModels = new List<DraftListItem>();


    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        
        adapter.Data = new LazyDataHelper<DraftListItem>(adapter, CreateNewModel);
        adapter.Init();
    }


    public void SetLoader(UpdateMapDataManager loader)
    {
        dataLoader = loader;
    }

    public void SetActions(Action<DraftListItem> dataAction, Action isEmptyAction)
    {
        adapter.dataAction = dataAction;
        // adapter.isEmptyAction = isEmptyAction;
    }

    public void GetFirstPageDatas(Action<List<DraftListItem>> complete)
    {
        dataLoader.ResetCookie();
        dataLoader.GetGameStudioDraftsData(true,datas =>
        {
            complete?.Invoke(datas);
            allModels.AddRange(datas);
            adapter.Data.ResetItems(datas.Count, false);
            adapter.OnItemsUpdated?.Invoke();
        });
    }
    
    /// <summary>
    /// 可以调整单元格数据内容,实现单元格大小分类致等特殊需求
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    private DraftListItem CreateNewModel(int index)
    {
        if (index >= 0 && index < allModels.Count)
        {
            return allModels[index];
        }

        return new DraftListItem();
    }

    

    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            dataLoader.GetGameStudioDraftsData(false,OnReceivedNewModelsForInsert);
        }
    }


    void OnReceivedNewModelsForInsert(List<DraftListItem> newModels)
    {
        if (newModels == null || newModels.Count == 0)
        {
            adapter.OnItemsUpdated?.Invoke();
            return;
        }

        adapter.Data.List.AddRange(newModels);
        // adapter.OnItemsUpdated?.Invoke();
        adapter.Refresh(false);
    }
}
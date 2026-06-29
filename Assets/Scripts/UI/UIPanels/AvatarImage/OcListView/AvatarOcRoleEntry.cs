using System;
using System.Collections.Generic;
using BUD.AvatarRole;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

public class AvatarOcRoleEntry : MonoBehaviour
{
    public AvatarOcRoleAdapter adapter;
    private bool isInitFlag = false;
    /// <summary>
    /// 下拉加载更多响应
    /// </summary>
    public Action pullAction;

    public Action<AvatarOcData> clickAction;

    public Action<AvatarOcData> longClickAction;

    public bool HasData
    {
        get
        {
            if (adapter.Data != null && adapter.Data.List != null && adapter.Data.List.Count > 0)
            {
                return true;
            }

            return false;
        }
    }

    public void ResetAdpater()
    {
        // Resetting to 0 count clears everything, including visible items, so nothing will be recycled
        if (adapter != null && adapter.IsInitialized)
        {
            adapter.ResetItems(0);
            adapter.ClearPool();
        }
    }

    public void ReloadData(List<AvatarOcData> datas, bool RefreshForce = false)
    {
        if (datas == null || datas.Count == 0)
        {
            adapter.OnItemsUpdated?.Invoke();
            return;
        }

        if (adapter.Data == null) return;

        if (isInitFlag)
        {
            if (RefreshForce)
            {
                adapter.Data.ResetItems(datas);
            }
            else
            {
                adapter.Data.List.AddRange(datas); 
            }

            adapter.Refresh(false);
        }
        else
        {
            isInitFlag = true;
            adapter.Data.ResetItems(datas);
            // adapter.ResetItems(datas.Count, false);
        }
    }
    

    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);

        adapter.Data = new SimpleDataHelper<AvatarOcData>(adapter);
        // adapter.Data = new LazyDataHelper<AvatarOcData>(adapter, CreateNewModel);
        adapter.RegisterAction(clickAction, longClickAction);
        adapter.Init();
    }
    
    public void OnPullReleased()
    {
        pullAction?.Invoke();
    }

}
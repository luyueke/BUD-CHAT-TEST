using BUD.AvatarRole;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AvatarCollectListEntry : MonoBehaviour
{
    public AvatarCollectListAdapter adapter;
    private bool isInitFlag = false;
    /// <summary>
    /// 下拉加载更多响应
    /// </summary>
    public Action pullAction;
    public Action<RoleActionType, AvatarRoleItemProtocol> clickAction;

    public bool HasData
    {
        get
        {
            if (adapter.Data != null && adapter.Data.List != null)
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

    public void ReloadData(List<AvatarRoleItemProtocol> datas, bool RefreshForce = false)
    {
        if (datas == null || datas.Count == 0)
        {
            adapter.OnItemsUpdated?.Invoke();
            return;
        }

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
        }
    }

    public void RemoveData(AvatarRoleItemProtocol itemData)
    {
        if (adapter.Data == null) return;

        for (int i = 0; i < adapter.Data.Count; i++)
        {
            if (adapter.Data[i].itemId == itemData.itemId && adapter.Data[i].templateId == itemData.templateId)
            {
                adapter.Data.RemoveOne(i);

                break;
            }
        }
    }

    public void AddData(AvatarRoleItemProtocol itemData)
    {
        adapter.Data.InsertOne(0, itemData);
    }

    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);

        adapter.Data = new SimpleDataHelper<AvatarRoleItemProtocol>(adapter);
        adapter.RegisterAction(clickAction);
        adapter.Init();
    }

    public void OnPullReleased()
    {
        pullAction?.Invoke();
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using UnityEngine;

public class CreatorWayEntry : MonoBehaviour
{
    public CreatorWayAdapter adapter;
    public Action GetDataAction;
    protected void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
    }
    public void SetActions(Action<CreatorCenterTaskLocalInfo> onSelectItemAct)
    {
        adapter.OnSelectItemAct = onSelectItemAct;
       
    }
    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            // GetDataAction?.Invoke();
        }
    }
    public void MoveTo(int index)
    {
       
        adapter.ScrollTo(index);
        
    }
    public void OnReceivedNewModelsForInsert(List<CreatorCenterTaskLocalInfo> newModels)
    {
        if (newModels == null || newModels.Count == 0)
        {
            return;
        }
        adapter.SetData( newModels);
        
    }
 
}

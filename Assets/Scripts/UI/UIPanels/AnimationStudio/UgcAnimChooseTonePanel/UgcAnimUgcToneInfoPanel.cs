using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using UI.BaseWidgets;
using UnityEngine;

public class UgcAnimUgcToneInfoPanel : MonoBehaviour
{
    public CButton Btn_Refresh;
    public UgcAnimUgcToneInfoPanel_Adapter Adapter;
    public PullToRefreshBehaviour refreshController;
    public UgcAnimUgcToneInfoPanel_DataLoader DataLoader;

    public void SetOnToneItemSelectAct(Action<AnimMusicInfo> act)
    {
        Adapter.SetOnToneItemSelectAct(act, OnBtnRefreshClick);
    }

    private void Awake()
    {
        if (Btn_Refresh != null)
        {
            Btn_Refresh.onClick.AddListener(OnBtnRefreshClick);
        }
    }

    private void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        Adapter.Data = new SimpleDataHelper<AnimMusicListRspData>(Adapter);
        Adapter.Init();
    }

    public void GetPublishedData()
    {
        HideGizmo();
        DataLoader.ResetCookie();
        DataLoader.GetTonePublishedData(OnGetFirstPageDatas);
    }

    public void ResetAdpater()
    {
        if (!Adapter.IsInitialized)
            return;
        // Resetting to 0 count clears everything, including visible items, so nothing will be recycled
        Adapter.ResetItems(0);
    }

    private void HideGizmo()
    {
        //需要动态拉取数据必须要做的初始化操作
        refreshController.HideGizmo();
    }

    public virtual void OnGetFirstPageDatas(List<AnimMusicListRspData> tonePublishedDatas)
    {
        ResetAdpater();
        if (tonePublishedDatas == null || tonePublishedDatas.Count == 0)
        {
            tonePublishedDatas = new List<AnimMusicListRspData>();
        }

        var firstItem = new AnimMusicListRspData();
        tonePublishedDatas.Insert(0, firstItem);
        Adapter.Data.ResetItems(tonePublishedDatas);
        Adapter.OnItemsUpdated?.Invoke();
    }

    private void OnPullReleased()
    {
        DataLoader.GetTonePublishedData(OnReceivedNewModelsForInsert);
    }

    private void OnReceivedNewModelsForInsert(List<AnimMusicListRspData> tonePublishedDatas)
    {
        if (tonePublishedDatas == null || tonePublishedDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }

        Adapter.Data.List.AddRange(tonePublishedDatas);
        Adapter.Refresh(false);
    }

    private void OnBtnRefreshClick()
    {
        GetPublishedData();
    }
}

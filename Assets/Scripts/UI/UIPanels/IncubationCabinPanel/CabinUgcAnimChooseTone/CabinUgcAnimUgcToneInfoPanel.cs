using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.MusicalInstrument;
using GameData.BaseInfo;
using Message;
using UI.BaseWidgets;
using UnityEngine;

public class CabinUgcAnimUgcToneInfoPanel : MonoBehaviour
{
    public CButton Btn_Refresh;
    public CabinUgcAnimUgcToneInfoPanel_Adapter Adapter;
    public PullToRefreshBehaviour refreshController;
    public CabinUgcAnimUgcToneInfoPanel_DataLoader DataLoader;
    public CabinCharacterUgcInfo CharacterUgcInfo;
    public void SetOnToneItemSelectAct(Action<CabinToneInfo> act)
    {
        Adapter.SetOnToneItemSelectAct(act);
    }

    public void SetOnToneItemSelectAct(CabinCharacterUgcInfo cabinCharacter)
    {
        this.CharacterUgcInfo = cabinCharacter;
        Adapter.SetOnToneItemSelectAct(CharacterUgcInfo);
    }

    private void Awake()
    {
        if (Btn_Refresh != null)
        {
            Btn_Refresh.onClick.AddListener(OnBtnRefreshClick);
        }
        MessageHelper.AddListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        MessageHelper.AddListener<string>(MessageName.OnCreatToneInfo, OnBuyUgcItemSuccess);
    }

    private void Start()
    {
        //需要动态拉取数据必须要做的初始化操作
        refreshController.OnRefreshWithSlideUp.AddListener(OnPullReleased);
        Adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        Adapter.Data = new SimpleDataHelper<CabinToneInfo>(Adapter);
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

    public virtual void OnGetFirstPageDatas(List<CabinToneInfo> tonePublishedDatas)
    {
        ResetAdpater();
        if (tonePublishedDatas == null || tonePublishedDatas.Count == 0)
        {
            tonePublishedDatas = new List<CabinToneInfo>();
        }

        var firstItem = new CabinToneInfo();
        tonePublishedDatas.Insert(0, firstItem);
        Adapter.Data.ResetItems(tonePublishedDatas);
        Adapter.OnItemsUpdated?.Invoke();
        Adapter.SelectFirstRealItem(tonePublishedDatas);
    }

    private void OnPullReleased()
    {
        DataLoader.GetTonePublishedData(OnReceivedNewModelsForInsert);
    }

    private void OnReceivedNewModelsForInsert(List<CabinToneInfo> tonePublishedDatas)
    {
        if (tonePublishedDatas == null || tonePublishedDatas.Count == 0)
        {
            Adapter.OnItemsUpdated?.Invoke();
            return;
        }

        // 收集当前列表已存在的 id，下拉加载更多时跳过重复，防止服务端分页游标返回重叠数据导致列表出现重复
        var existingIds = new HashSet<string>();
        foreach (var item in Adapter.Data.List)
        {
            if (item != null && !string.IsNullOrEmpty(item.id))
            {
                existingIds.Add(item.id);
            }
        }

        foreach (var item in tonePublishedDatas)
        {
            if (item == null)
                continue;

            // id 非空且已存在则视为重复，跳过；id 为空的条目原样追加
            if (!string.IsNullOrEmpty(item.id) && existingIds.Contains(item.id))
            {
                LoggerUtils.Log($"[CabinUgcAnimUgcToneInfoPanel] 下拉加载更多时发现重复音色 id={item.id}，已跳过");
                continue;
            }

            Adapter.Data.List.Add(item);

            if (!string.IsNullOrEmpty(item.id))
            {
                existingIds.Add(item.id);
            }
        }

        Adapter.Refresh(false);
    }

    private void OnBtnRefreshClick()
    {
        GetPublishedData();
    }

    private void OnBuyUgcItemSuccess(string ugcId)
    {
        if (!gameObject.activeSelf)
        {
            return;
        }
        GetPublishedData();
    }
    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string>(MessageName.OnBuyUgcItemSuccess, OnBuyUgcItemSuccess);
        MessageHelper.RemoveListener<string>(MessageName.OnCreatToneInfo, OnBuyUgcItemSuccess);

    }
}

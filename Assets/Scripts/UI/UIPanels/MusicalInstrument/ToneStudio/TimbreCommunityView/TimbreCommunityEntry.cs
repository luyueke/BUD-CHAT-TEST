using System;
using System.Collections.Generic;
using System.Linq;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using Game.MusicalInstrument;
using GameData;
using GameData.BaseInfo;
using UnityEngine;

public enum TimbreStoreViewType
{
    Publish = 0,
    Purchased = 1,
}

public class TimbreCommunityEntry : MonoBehaviour
{
    public TimbreCommunityAdapter adapter;
    private TimbreCommunityPublishDataLoader publishLoader = new TimbreCommunityPublishDataLoader();
    private TimbreCommunityDataLoader purchasedLoader = new TimbreCommunityDataLoader();

    private TimbreStoreViewType pageType = TimbreStoreViewType.Publish;
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
        }
    }

    public void SetActions(TimbreStoreViewType pageType, Action<ToneFixedInfo> onSelectItemAct, Action isEmptyAction)
    {
        this.pageType = pageType;
        adapter.OnSelectItemAct = onSelectItemAct;
        adapter.EmptyDataAct = isEmptyAction;
    }

    public void GetFirstPageDatas()
    {
        if (pageType == TimbreStoreViewType.Publish)
        {
            publishLoader.ResetCookie();
            publishLoader.GetTonePublishedData(data =>
            {
                if (this == null)
                {
                    return;
                }
                var datas = ContertFromPublish(data);
                var storeItem = ToneFixedInfo.Init(new ToneInfo(), ToneFixedInfo.ToneFixedStyle.Store);
                var publishItem = ToneFixedInfo.Init(new ToneInfo(), ToneFixedInfo.ToneFixedStyle.Publish);
                datas.Insert(0, publishItem);
                datas.Insert(0, storeItem);
                
                //如果是草稿箱-需要增加创建按钮
                ResetAdpater();
                adapter.OnItemsUpdatedAct?.Invoke();
                if (datas != null && datas.Count > 0)
                {
                    adapter.Data.ResetItems(datas);
                }
            });
        }
        else if (pageType == TimbreStoreViewType.Purchased)
        {
            purchasedLoader.ResetCookie();
            purchasedLoader.GetToneData(data =>
            {
                if (this == null)
                {
                    return;
                }  
                
                var datas = ContertFromPurchased(data);
                var storeItem = ToneFixedInfo.Init(new ToneInfo(), ToneFixedInfo.ToneFixedStyle.Store);
                var publishItem = ToneFixedInfo.Init(new ToneInfo(), ToneFixedInfo.ToneFixedStyle.Publish);
                datas.Insert(0, publishItem);
                datas.Insert(0, storeItem);

                ResetAdpater();
                adapter.OnItemsUpdatedAct?.Invoke();
                if (datas != null && datas.Count > 0)
                {
                    adapter.Data.ResetItems(datas);
                }
            });
        }
    }

    private List<ToneFixedInfo> ContertFromPublish(List<ToneItemData> items)
    {
        if (items == null)
        {
            return new List<ToneFixedInfo>();
        }

        var results = new List<ToneFixedInfo>();
        foreach (var toneItemData in items)
        {
            if (toneItemData.musicToneInfo != null)
            {
                results.Add(ToneFixedInfo.Init(toneItemData.musicToneInfo)); 
            }
        }
        return results;
    }
    
    private List<ToneFixedInfo> ContertFromPurchased(List<TimbreCommunityDataLoader.TimbrePurchasedInfo> items)
    {
        if (items == null)
        {
            return new List<ToneFixedInfo>();
        }

        var results = new List<ToneFixedInfo>();
        foreach (var toneItemData in items)
        {
            if (toneItemData.ugcInfo != null)
            {
                results.Add(ToneFixedInfo.Init(toneItemData.ugcInfo)); 
            }
        }
        return results;
        // var fixedItems = items.Select(element => ToneFixedInfo.Init(element.ugcInfo)).ToList();
    }
    
    public void RemoveSingleItem(string cId)
    {
        adapter.RemoveSingleItem(cId);
    }

    public void UpdateSingleItem(ToneFixedInfo data)
    {
        adapter.UpdateSingleItem(data);
    }

    public void OnPullReleased(float sign)
    {
        if (sign < 0)
        {
            if (pageType == TimbreStoreViewType.Publish)
            {
                publishLoader.GetTonePublishedData(data =>
                {
                    if (this == null)
                    {
                        return;
                    }
                    var datas = ContertFromPublish(data);
                    OnReceivedNewModelsForInsert(datas);
                });
            }
            else if (pageType == TimbreStoreViewType.Purchased)
            {
                purchasedLoader.GetToneData(data =>
                {
                    if (this == null)
                    {
                        return;
                    }  
                
                    var datas = ContertFromPurchased(data);
                    OnReceivedNewModelsForInsert(datas);
                });
            }

        }
    }

    void OnReceivedNewModelsForInsert(List<ToneFixedInfo> newModels)
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

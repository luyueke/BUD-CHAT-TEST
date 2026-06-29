using System;
using System.Collections.Generic;
using Basic.Utils;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using UI.UIPanels.GashaponPanel;
using UnityEngine;

public class GashaponPreviewListView : MonoBehaviour
{
    [SerializeField] private Transform cacheNode;
    [SerializeField] private Transform scrollContent;
    [SerializeField] private GashaponPriceItem itemPrefab;

    private List<GashaponPriceItem> items = new List<GashaponPriceItem>();
    private LinkedList<GashaponPriceItem> cacheItems = new LinkedList<GashaponPriceItem>();
    private Action<GashaponRewardData> _itemSelectListener;

    [HideInInspector] public GashaponPriceItem CurSelectItem = null;
    public int targetIdx = -1;
    bool afterInit = false;
    void Start()
    {
        InitUI();
        Invoke("AfterInit", 0.2f);
    }

    private void InitUI()
    {
    }

    void AfterInit(){
        afterInit = true;
        if(targetIdx == -1){
            DefClickFirst();
        }else{
            items[targetIdx].OnItemClick();
        }
    }

    public void UpdateListview(List<GashaponRewardData> rewardList, GashaponInfoRsp gashaponInfoRsp = null)
    {
        ClearItems();
        if(rewardList == null || rewardList.Count <= 0) return;
        for (int i = 0; i < rewardList.Count; i++)
        {
            GashaponRewardData priceData =rewardList[i];
            GashaponPriceItem itemScript = GetItem();
            itemScript.Init(priceData, OnItemClick);
            if (gashaponInfoRsp != null && gashaponInfoRsp.rewardPool != null) {
                var drawnInfo = gashaponInfoRsp.rewardPool.Find(tmp => tmp.rewardId == priceData.RewardId);
                if (drawnInfo != null) {
                    itemScript.SetOwnedStatus(drawnInfo.everDrawn == 1);
                }
            }
            items.Add(itemScript);
        }
    }

    public void DefClickFirst()
    {
        if (items.Count > 0)
        {
            items[0].OnItemClick();
        }
    }

    public void Turn2Preview(string bundleId)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if(items[i].GetBindData().BundleId == bundleId){
                if(!afterInit){
                    targetIdx = i;
                }else{
                    items[i].OnItemClick();
                }
                break;
            }
        }
        
    }

    public void HideItemsLoading()
    {
        foreach (var item in items)
        {
            item.SetLoadingVisible(false);
        }
    }

    public void AddItemClickListener(Action<GashaponRewardData> callback)
    {
        _itemSelectListener += callback;
    }

    public void ClearItemClickListener()
    {
        _itemSelectListener = null;
    }

    #region Item相关

    private GashaponPriceItem GetItem()
    {
        if (cacheItems != null && cacheItems.Count != 0)
        {
            GashaponPriceItem cache = cacheItems.Last.Value;
            cacheItems.RemoveLast();
            cache.gameObject.SetActive(true);
            cache.transform.SetParent(scrollContent);
            return cache;
        }
        GashaponPriceItem newIns = Instantiate(itemPrefab, scrollContent);
        return newIns;
    }
    private void RecycleItem(GashaponPriceItem item)
    {
        if (cacheItems == null)
        {
            cacheItems = new LinkedList<GashaponPriceItem>();
        }

        cacheItems.AddLast(item);
        item.gameObject.SetActive(false);
        item.SetSelectStatus(false);
        item.transform.SetParent(cacheNode);
    }
    private void ClearItems()
    {
        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                RecycleItem(items[i]);
            }

            items.Clear();
        }
    }

    private void OnItemClick(GashaponPriceItem item,GashaponRewardData info)
    {
        HideItemsLoading();

        if (GashaponUtils.HasPGCData(info))
        {
            var asset = info.PgcDatas[0];
            item.SetLoadingVisible(asset.ResourceType == ResourceType.Avatar || asset.ResourceType == ResourceType.PGCPetAvatar);
        }

        bool isSelect = false;

        for (int i = 0; i < items.Count; i++)
        {
            items[i].SetSelectStatus(false);
        }
        CurSelectItem = item;
        _itemSelectListener?.Invoke(info);
    }
    #endregion

}

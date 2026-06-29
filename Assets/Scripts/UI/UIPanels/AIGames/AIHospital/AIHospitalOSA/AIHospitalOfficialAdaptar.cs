using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using Game.CommunityGame;
using System.Collections.Generic;

public class AIHospitalOfficialAdaptar : GridAdapter<GridParams, AIHospitalRecommendedMapViewsHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    public SimpleDataHelper<RecommendItemData> Data { get; set; }
    public IPool texturePool;

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<RecommendItemData>(this);
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearPool();
    }

    public void ClearPool()
    {
        texturePool?.Clear();
    }

    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
    {
        if (Data == null)
            return;

        _CellsCount = Data.Count;
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void OnCellViewsHolderCreated(AIHospitalRecommendedMapViewsHolder cellVH, CellGroupViewsHolder<AIHospitalRecommendedMapViewsHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
    }

    protected override void UpdateCellViewsHolder(AIHospitalRecommendedMapViewsHolder newOrRecycled)
    {
        var itemData = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(itemData);
    }

    #region 数据操作方法
    public void SetItems(IList<RecommendItemData> items)
    {
        Data.ResetItems(items);
        Refresh();
    }

    public void AddItems(IList<RecommendItemData> items)
    {
        Data.InsertItemsAtEnd(items);
        Refresh();
    }

    //public void InsertItems(int index, IList<RecommendItemData> items)
    //{
    //    Data.InsertItemsAt(index, items);
    //    Refresh();
    //}

    //public void RemoveItems(int index, int count)
    //{
    //    Data.RemoveItemsAt(index, count);
    //    Refresh();
    //}

    public void ClearItems()
    {
        Data.ResetItems(new RecommendItemData[0]);
        Refresh();
    }
    #endregion
}

using UnityEngine;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using Game.CommunityGame;
using System.Collections.Generic;
using UI.TopList;

public class AIHospitalPopularAdaptar : GridAdapter<GridParams, AIHospitalPopularMapViewsHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    public SimpleDataHelper<RankItem> Data { get; set; }
    public IPool texturePool;

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<RankItem>(this);
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

    protected override void UpdateCellViewsHolder(AIHospitalPopularMapViewsHolder newOrRecycled)
    {
        var itemData = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(itemData);
    }

    #region 数据操作方法
    public void SetItems(IList<RankItem> items)
    {
        Data.ResetItems(items);
        Refresh();
    }

    public void AddItems(IList<RankItem> items)
    {
        Data.InsertItemsAtEnd(items);
        Refresh();
    }

    public void ClearItems()
    {
        Data.ResetItems(new RankItem[0]);
        Refresh();
    }
    #endregion
}
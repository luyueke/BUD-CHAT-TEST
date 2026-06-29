using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.MapData;

public class RecommendAdapter : GridAdapter<GridParams, RecommendItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    
    public LazyDataHelper<RecommendData> Data { get; set; }
    private IPool texturePool;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        texturePool = new FIFOCachingPool(36, TextureDestoryer);
    }
    
    /// <summary>
    /// 销毁必须清空池对象
    /// </summary>
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
    
    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        _CellsCount = Data.Count;
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }
    
    
    protected override void OnCellViewsHolderCreated(RecommendItemHolder cellVH, CellGroupViewsHolder<RecommendItemHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.iconRemoteImageBehaviour.InitializeWithPool(texturePool);
    }

    // This is called anytime a previously invisible item become visible, or after it's created, 
    // or when anything that requires a refresh happens
    // Here you bind the data from the model to the item's views
    // *For the method's full description check the base implementation
    protected override void UpdateCellViewsHolder(RecommendItemHolder newOrRecycled)
    {
        var model = Data.GetOrCreate(newOrRecycled.ItemIndex);
        newOrRecycled.UpdateViews(model);
        // newOrRecycled.iconRemoteImageBehaviour.Load(model.mapCover, true, null);
    }
}


public class RecommendItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour iconRemoteImageBehaviour;
    public RecommendItem recommendItem;

    public override void CollectViews()
    {
        base.CollectViews();
        root.GetComponentAtPath("Views/MapCoverMask/MapCover", out iconRemoteImageBehaviour);
        recommendItem = root.GetComponentInParent<RecommendItem>();
    }


    public void UpdateViews(RecommendData data)
    {
        recommendItem.Init(data);
    }
}
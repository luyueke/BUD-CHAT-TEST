using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.Events;

public class TheatreTriggerTheatreAdapter : GridAdapter<GridParams, TheatreTriggerTheatreVH>
{
    public Action<DraftListItem> OnSelectItemAct;
    public UnityEvent OnItemsUpdated;
    public SimpleDataHelper<DraftListItem> Data { get; set; }
    public string SelectedId { get; set; }
    public IPool texturePool;

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        texturePool?.Clear();
    }

    private void TextureDestoryer(object urlKey, object texture)
    {
        var tex = texture as UnityEngine.Object;
        if (tex != null)
            Destroy(tex);
    }

    public void ClearPool()
    {
        texturePool?.Clear();
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
    {
        _CellsCount = Data.Count;
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void OnCellViewsHolderCreated(TheatreTriggerTheatreVH cellVH, CellGroupViewsHolder<TheatreTriggerTheatreVH> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.Item.iconImage.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(TheatreTriggerTheatreVH newOrRecycled)
    {
        var data = Data[newOrRecycled.ItemIndex];
        bool isSelected = !string.IsNullOrEmpty(SelectedId) && data?.theatreInfo?.id == SelectedId;
        newOrRecycled.Item.InitTheatre(data, OnSelectItemAct, isSelected);
        if (!string.IsNullOrEmpty(data?.theatreInfo?.cover))
            newOrRecycled.Item.iconImage.Load(data.theatreInfo.cover);
    }
}

public class TheatreTriggerTheatreVH : CellViewsHolder
{
    public TheatreTriggerItem Item;

    public override void CollectViews()
    {
        base.CollectViews();
        root.TryGetComponent(out Item);
    }
}

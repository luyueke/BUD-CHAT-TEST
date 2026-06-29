using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.Events;

public class TheatreTriggerClothsAdapter : GridAdapter<GridParams, TheatreTriggerClothsVH>
{
    public Action<OTCAvatarClothes> OnSelectItemAct;
    public UnityEvent OnItemsUpdated;
    public SimpleDataHelper<OTCAvatarClothes> Data { get; set; }
    public int SelectedIndex { get; set; } = -1;
    public IPool texturePool;

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestroyer);
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        texturePool?.Clear();
    }

    private void TextureDestroyer(object urlKey, object texture)
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

    protected override void OnCellViewsHolderCreated(TheatreTriggerClothsVH cellVH, CellGroupViewsHolder<TheatreTriggerClothsVH> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.Item.iconImage.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(TheatreTriggerClothsVH newOrRecycled)
    {
        var data = Data[newOrRecycled.ItemIndex];
        bool isSelected = data != null && data.clothesIndex == SelectedIndex;
        newOrRecycled.Item.InitClothes(data, OnSelectItemAct, isSelected);
        if (!string.IsNullOrEmpty(data?.clothesURL))
            newOrRecycled.Item.iconImage.Load(data.clothesURL);
    }
}

public class TheatreTriggerClothsVH : CellViewsHolder
{
    public TheatreTriggerItem Item;

    public override void CollectViews()
    {
        base.CollectViews();
        root.TryGetComponent(out Item);
    }
}

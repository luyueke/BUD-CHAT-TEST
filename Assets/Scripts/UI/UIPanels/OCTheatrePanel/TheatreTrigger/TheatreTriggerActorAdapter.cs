using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData.BaseInfo;
using UnityEngine;
using UnityEngine.Events;

public class TheatreTriggerActorAdapter : GridAdapter<GridParams, TheatreTriggerActorVH>
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

    protected override void OnCellViewsHolderCreated(TheatreTriggerActorVH cellVH, CellGroupViewsHolder<TheatreTriggerActorVH> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.Item.iconImage.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(TheatreTriggerActorVH newOrRecycled)
    {
        var data = Data[newOrRecycled.ItemIndex];
        bool isSelected = !string.IsNullOrEmpty(SelectedId) && data?.actorInfo?.id == SelectedId;
        newOrRecycled.Item.InitActor(data, OnSelectItemAct, isSelected);
        if (!string.IsNullOrEmpty(data?.actorInfo?.cover))
            newOrRecycled.Item.iconImage.Load(data.actorInfo.cover);
    }
}

public class TheatreTriggerActorVH : CellViewsHolder
{
    public TheatreTriggerItem Item;

    public override void CollectViews()
    {
        base.CollectViews();
        root.TryGetComponent(out Item);
    }
}

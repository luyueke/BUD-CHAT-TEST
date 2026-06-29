using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using Game.MusicalInstrument;
using GameData;
using UnityEngine;

public class VehicleStudioAdapter : GridAdapter<GridParams, VehicleStudioListViewsHolder>
{
    public StudioSubType curStudioType;
    public Action<DraftListItem> OnSelectItemAct;
    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    public SimpleDataHelper<DraftListItem> Data { get; set; }
    public IPool texturePool;


    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        base.Start();
    }

    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
    {
        base.Refresh(contentPanelEndEdgeStationary, keepVelocity);
        _CellsCount = Data.Count;
        OnItemsUpdated?.Invoke();
    }

    protected override void OnCellViewsHolderCreated(VehicleStudioListViewsHolder cellVH, CellGroupViewsHolder<VehicleStudioListViewsHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(VehicleStudioListViewsHolder viewsHolder)
    {
        var model = Data[viewsHolder.ItemIndex];
        viewsHolder.UpdateViews(model, OnSelectItemAct, curStudioType);
        string url = model?.vehicleInfo?.cover;
        //不保存使用默认封面时，url为null
        if (!string.IsNullOrEmpty(url))
        {
            viewsHolder.IconRemoteImageBehaviour.Load(model?.vehicleInfo?.cover);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearPool();
    }

    public void ClearPool()
    {
        if (texturePool != null)
            texturePool.Clear();
    }
}
public class VehicleStudioListViewsHolder : CellViewsHolder
{
    public RemoteImageBehaviour IconRemoteImageBehaviour;
    public VehicleStudioItem StudioItem;

    public override void CollectViews()
    {
        base.CollectViews();
        IconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "InstrumentIcon").GetComponent<RemoteImageBehaviour>();
        root.TryGetComponent(out StudioItem);
    }

    public virtual void UpdateViews(DraftListItem data, Action<DraftListItem> onSelect, StudioSubType studioType)
    {
        if (StudioItem == null)
            return;

        if (studioType == StudioSubType.Drafts)
        {
            //说明是CreateBtn
            bool isCreate = (data == null) || GameStudioUtils.GetBaseInfo(data) == null;
            if (isCreate)
            {
                StudioItem.InitCreateMode(EnterGameModel.UgcVehicleEmpty);
            }
            else
            {
                StudioItem.InitSelectMode(data, onSelect, studioType);
            }
        }
        else
        {
            StudioItem.InitSelectMode(data, onSelect, studioType);
        }
    }
}
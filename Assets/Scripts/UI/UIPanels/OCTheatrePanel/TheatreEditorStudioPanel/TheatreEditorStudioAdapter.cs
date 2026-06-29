using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData;
using GameData.BaseInfo;
using UnityEngine;

public class TheatreEditorStudioAdapter : GridAdapter<GridParams, TheatreEditorStudioListViewsHolder>
{
    public TheatreStudioSubType curStudioType;
    public Action<DraftListItem> OnSelectItemAct;
    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    public SimpleDataHelper<DraftListItem> Data { get; set; }
    public IPool texturePool;

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        base.Start();
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

    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
    {
        _CellsCount = Data.Count;
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void OnCellViewsHolderCreated(TheatreEditorStudioListViewsHolder cellVH, CellGroupViewsHolder<TheatreEditorStudioListViewsHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.IconRemoteImageBehaviour.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(TheatreEditorStudioListViewsHolder newOrRecycled)
    {
        var model = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(model, OnSelectItemAct, curStudioType);
        string url = model?.theatreInfo?.cover;
        if (!string.IsNullOrEmpty(url))
        {
            newOrRecycled.IconRemoteImageBehaviour.Load(url);
        }
    }
}

public class TheatreEditorStudioListViewsHolder : CellViewsHolder
{
    public RemoteImageBehaviour IconRemoteImageBehaviour;
    public TheatreEditorStudioItem StudioItem;

    public override void CollectViews()
    {
        base.CollectViews();
        IconRemoteImageBehaviour = GameObjectEx.FindChildByName(views, "TheatreIcon").GetComponent<RemoteImageBehaviour>();
        root.TryGetComponent(out StudioItem);
    }

    public virtual void UpdateViews(DraftListItem data, Action<DraftListItem> onSelect, TheatreStudioSubType studioType)
    {
        if (StudioItem == null)
            return;

        if (studioType == TheatreStudioSubType.Drafts)
        {
            bool isCreate = (data == null) || GameStudioUtils.GetBaseInfo(data) == null;
            if (isCreate)
            {
                StudioItem.InitCreateMode();
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

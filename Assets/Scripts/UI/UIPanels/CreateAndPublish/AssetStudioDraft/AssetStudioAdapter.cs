using System;
using Basic.Utils;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using GameData.Base;
using UGCAsset;
using UGCAsset.Draft;

public class AssetStudioAdapter : GridAdapter<GridParams, AssetStudioItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
    public Action<DraftListItem> OnSelectItemAct;
    public Action EmptyDataAct;
    public Action CreateDataAct;
    public Action<DraftListItem, AvatarStudioBaseItem> UploadAct;

    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<DraftListItem> Data { get; private set; }
    private IPool texturePool;

    #region OSA implementation

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<DraftListItem>(this);
        base.Start();
    }

    /// <summary>
    /// 销毁必须清空池对象
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        texturePool?.Clear();
    }

    public void ClearPool()
    {
        texturePool?.Clear();
    }


    /// <summary>
    /// 更新单个上传状态
    /// </summary>
    public void UpdateSingleUploadStatus(string draftId, UploadStatus uploadStatus)
    {
        Refresh();
    }

    public void RemoveSingleItem(string infoId)
    {
        if (Data.Count == 0)
        {
            return;
        }

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;

            UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(Data[i]);
            if (ugcBaseInfo != null && ugcBaseInfo.id == infoId)
            {
                index = i;
                break;
            }
        }

        Data.RemoveOne(index);
        Refresh();
        if (Data.Count == 0)
        {
            EmptyDataAct.Invoke();
        }
    }

    public void UpdateSingleItem(DraftListItem draftsListItem)
    {
        if (Data.Count == 0)
            return;

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;

            UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(Data[i]);
            UgcBaseInfo draftBaseInfo = GameStudioUtils.GetBaseInfo(draftsListItem);
            if (ugcBaseInfo != null && ugcBaseInfo.id == draftBaseInfo.id)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    private void OnSelectItem(DraftListItem info)
    {
        OnSelectItemAct?.Invoke(info);
    }

    private void onCreateDraft()
    {
        CreateDataAct?.Invoke();
    }

    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdatedAct?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void OnCellViewsHolderCreated(AssetStudioItemHolder cellVH,
        CellGroupViewsHolder<AssetStudioItemHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.Rm_Cover.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(AssetStudioItemHolder newOrRecycled)
    {
        DraftListItem data = Data[newOrRecycled.ItemIndex];

        newOrRecycled.UpdateViews(OnSelectItem, UploadAct, data, onCreateDraft);
        UgcBaseInfo ugcBaseInfo = GameStudioUtils.GetBaseInfo(data);
        var path = ugcBaseInfo?.cover;
        if (data != null && !string.IsNullOrEmpty(path))
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
            newOrRecycled.Rm_Cover.Load(path, true,
                (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });
        }
    }

    #endregion
}


public class AssetStudioItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public AvatarStudioBaseItem draftsItem;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views, "Rm_Cover").GetComponent<RemoteImageBehaviour>();
        draftsItem = root.GetComponentInParent<AvatarStudioBaseItem>();
    }

    public void UpdateViews(Action<DraftListItem> onSelect, Action<DraftListItem, AvatarStudioBaseItem> uploadAction,
        DraftListItem model, Action onCreate)
    {
        if (draftsItem != null)
        {
            draftsItem.Init(onSelect, onCreate, model);
            // draftsItem.Init(onSelect, uploadAction, model, onCreate);
        }
    }
}
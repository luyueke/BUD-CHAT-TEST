
using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData;

public class ContestCreateOcAdapter : GridAdapter<GridParams, ContestCreateOcItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
    public Action<bool, AvatarOcFixData> OnSelectItemAct;
    public Action EmptyDataAct;

    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<AvatarOcFixData> Data { get; private set; }
    private IPool texturePool;

    #region OSA implementation

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<AvatarOcFixData>(this);
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
            var creationId = Data[i].ocInfo.ocId;
            if (!string.IsNullOrEmpty(creationId) && creationId == infoId)
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

    public void UpdateSingleItem(AvatarOcFixData draftsListItem)
    {
        if (Data.Count == 0)
            return;

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;
            var leftId = Data[i].ocInfo.ocId;
            var rightId = draftsListItem.ocInfo.ocId;
            if (!string.IsNullOrEmpty(rightId) && leftId == rightId)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    private void OnSelectItem(bool isAdd, AvatarOcFixData info)
    {
        OnSelectItemAct?.Invoke(isAdd, info);
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

    protected override void OnCellViewsHolderCreated(ContestCreateOcItemHolder cellVH,
        CellGroupViewsHolder<ContestCreateOcItemHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.Rm_Cover.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(ContestCreateOcItemHolder newOrRecycled)
    {
        AvatarOcFixData data = Data[newOrRecycled.ItemIndex];

        newOrRecycled.UpdateViews(data, OnSelectItem);
        var path = data?.ocInfo?.ocCover;
        if (data != null && !string.IsNullOrEmpty(path))
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
            newOrRecycled.Rm_Cover.Load(path, true,
                (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });
        }
    }

    #endregion
}

public class ContestCreateOcItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public ContestCreateOcItemView itemView;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views, "Rm_Cover").GetComponent<RemoteImageBehaviour>();
        itemView = root.GetComponentInParent<ContestCreateOcItemView>();
    }

    public void UpdateViews(AvatarOcFixData model, Action<bool, AvatarOcFixData> onSelect)
    {
        if (itemView != null)
        {
            itemView.SetData(model, onSelect);
        }
    }
}
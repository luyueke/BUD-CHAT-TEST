
using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData;

public class ContestRankAdapter : GridAdapter<GridParams, ContestRankItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
    public Action<ContestEntryInfo> OnSelectItemAct;
    public Action EmptyDataAct;

    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<ContestEntryInfo> Data { get; private set; }
    private IPool texturePool;

    #region OSA implementation

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<ContestEntryInfo>(this);
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
            var creationId = Data[i].creationInfo.creationId;
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

    public void UpdateSingleItem(ContestEntryInfo draftsListItem)
    {
        if (Data.Count == 0)
            return;

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;
            var leftId = Data[i].creationInfo.creationId;
            var rightId = draftsListItem.creationInfo.creationId;
            if (!string.IsNullOrEmpty(rightId) && leftId == rightId)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    private void OnSelectItem(ContestEntryInfo info)
    {
        OnSelectItemAct?.Invoke(info);
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

    protected override void OnCellViewsHolderCreated(ContestRankItemHolder cellVH,
        CellGroupViewsHolder<ContestRankItemHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.Rm_Cover.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(ContestRankItemHolder newOrRecycled)
    {
        ContestEntryInfo data = Data[newOrRecycled.ItemIndex];

        newOrRecycled.UpdateViews(data, OnSelectItem);
        var path = data?.creator?.portraitUrl;
        if (data != null && !string.IsNullOrEmpty(path))
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
            newOrRecycled.Rm_Cover.Load(path, true,
                (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });
        }
    }

    #endregion
}


public class ContestRankItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public ContestRankViewItem draftsItem;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views, "Rm_Cover").GetComponent<RemoteImageBehaviour>();
        draftsItem = root.GetComponentInParent<ContestRankViewItem>();
    }

    public void UpdateViews(ContestEntryInfo model, Action<ContestEntryInfo> onSelect)
    {
        if (draftsItem != null)
        {
            draftsItem.SetData(model);
        }
    }
}

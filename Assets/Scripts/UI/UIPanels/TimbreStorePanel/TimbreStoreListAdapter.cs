using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.Util.IO;
using Game.Store;
using Com.TheFallenGames.OSA.DataHelpers;

public class TimbreStoreListAdapter : GridAdapter<GridParams, TimbreStoreListItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
    public Action<RecommendItemData> OnSelectItemAct;
    public Action EmptyDataAct;

    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<RecommendItemData> Data { get; private set; }

    #region OSA implementation

    protected override void Start()
    {
        Data = new SimpleDataHelper<RecommendItemData>(this);
        base.Start();
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
            var creationId = Data[i].ugcId;
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

    public void UpdateSingleItem(RecommendItemData draftsListItem)
    {
        if (Data.Count == 0)
            return;

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;
            var leftId = Data[i].ugcId;
            var rightId = draftsListItem.ugcId;
            if (!string.IsNullOrEmpty(rightId) && leftId == rightId)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    private string curSelectId = "";
    public void SetDataSelected(RecommendItemData data)
    {
        if (data == null)
        {
            return;
        }
        curSelectId = data.UgcInfo.id;
        OnSelectItemAct?.Invoke(data);
        Refresh();
    }

    private void OnSelectItem(RecommendItemData info)
    {
        var id = info?.UgcInfo?.id;
        if (!string.IsNullOrEmpty(id) && id != curSelectId)
        {
            curSelectId = id; 
            Refresh();
        }
        
        OnSelectItemAct?.Invoke(info);
    }
    
    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdatedAct?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void UpdateCellViewsHolder(TimbreStoreListItemHolder newOrRecycled)
    {
        RecommendItemData data = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(data, curSelectId, OnSelectItem);
        
        var path = data?.UgcInfo?.cover;
        if (data != null && !string.IsNullOrEmpty(path))
        {
            newOrRecycled.Rm_Cover.gameObject.SetActive(false);
            newOrRecycled.Rm_Cover.Load(path, true,
                (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });
        }
    }

    #endregion
}

public class TimbreStoreListItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public TimbreStoreItemView draftsItem;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views, "RemoteIcon").GetComponent<RemoteImageBehaviour>();
        draftsItem = root.GetComponentInParent<TimbreStoreItemView>();
    }

    public void UpdateViews(RecommendItemData model, string curSelectId, Action<RecommendItemData> onSelect)
    {
        if (draftsItem != null)
        {
            draftsItem.SetData(model, onSelect);
            draftsItem.SetSelectState(curSelectId == model.UgcInfo.id);
        }
    }
    
}

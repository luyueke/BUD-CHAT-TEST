using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Store;
using UnityEngine.Events;

public class AnimStoreListAdapter : GridAdapter<GridParams, AnimStoreItemHolder>
{
    public UnityEvent OnItemsUpdatedAct;
    public Action<RecommendItemData> OnSelectItemAct;
    public Action EmptyDataAct;

    public SimpleDataHelper<RecommendItemData> Data { get; private set; }

    private string curSelectId = "";

    protected override void Start()
    {
        Data = new SimpleDataHelper<RecommendItemData>(this);
        base.Start();
    }

    public void SetDataSelected(RecommendItemData data)
    {
        if (data == null) return;
        curSelectId = data.UgcInfo?.id ?? "";
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

    public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
    {
        OnItemsUpdatedAct?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void UpdateCellViewsHolder(AnimStoreItemHolder newOrRecycled)
    {
        var data = Data[newOrRecycled.ItemIndex];
        newOrRecycled.UpdateViews(data, curSelectId, OnSelectItem);
    }
}

public class AnimStoreItemHolder : CellViewsHolder
{
    public AnimStoreItemView animStoreItem;

    public override void CollectViews()
    {
        base.CollectViews();
        animStoreItem = root.GetComponentInParent<AnimStoreItemView>();
    }

    public void UpdateViews(RecommendItemData model, string curSelectId, Action<RecommendItemData> onSelect)
    {
        if (animStoreItem == null || model == null) return;
        animStoreItem.SetData(model, onSelect);
        animStoreItem.SetSelectState(curSelectId == model.UgcInfo?.id);
    }
}

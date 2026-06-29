using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Game.Avatar;
using UnityEngine;

public class WardrobeClothsAdapter : GridAdapter<GridParams, WardrobeClothsItemHolder>
{
    public LazyDataHelper<CharacterPartData> Data { get; set; }

    public Action<CharacterPartData> OnItemSelected;
    public Func<string, bool> IsSelected;
    public Action<string> OnDirectBuy;

    private bool _forceShowAll;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Data = new LazyDataHelper<CharacterPartData>(this, idx => null);
    }

    protected override void UpdateCellViewsHolder(WardrobeClothsItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        if (model == null) return;
        viewsHolder.UpdateViews(model, OnItemSelected, IsSelected?.Invoke(model.Id) ?? false);
        if (viewsHolder.item != null)
        {
            viewsHolder.item.OnDirectBuy = OnDirectBuy;
            if (_forceShowAll) viewsHolder.item.SetSelected(true);
        }
    }

    public void SetAllSelectedUI(bool show)
    {
        _forceShowAll = show;
        for (int i = 0; i < VisibleItemsCount; i++)
        {
            var group = _VisibleItems[i];
            for (int j = 0; j < group.NumActiveCells; j++)
            {
                var cellVH = group.ContainingCellViewsHolders[j];
                if (cellVH?.item == null) continue;
                cellVH.item.SetSelected(show);
            }
        }
    }

    public void RefreshVisibleSelectionState()
    {
        for (int i = 0; i < VisibleItemsCount; i++)
        {
            var group = _VisibleItems[i];
            for (int j = 0; j < group.NumActiveCells; j++)
            {
                var cellVH = group.ContainingCellViewsHolders[j];
                if (cellVH == null || cellVH.item == null) continue;
                var model = Data.GetOrCreate(cellVH.ItemIndex);
                if (model == null) continue;
                cellVH.item.SetSelected(IsSelected?.Invoke(model.Id) ?? false);
            }
        }
    }

    public void RefreshVisibleOwnedState()
    {
        for (int i = 0; i < VisibleItemsCount; i++)
        {
            var group = _VisibleItems[i];
            for (int j = 0; j < group.NumActiveCells; j++)
            {
                var cellVH = group.ContainingCellViewsHolders[j];
                if (cellVH?.item == null) continue;
                var model = Data.GetOrCreate(cellVH.ItemIndex);
                if (model == null) continue;
                cellVH.item.RefreshOwnedState();
                cellVH.item.SetSelected(IsSelected?.Invoke(model.Id) ?? false);
            }
        }
    }
}

public class WardrobeClothsItemHolder : CellViewsHolder
{
    public WardrobeClothsItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponentInChildren<WardrobeClothsItem>(true);
    }

    public void UpdateViews(CharacterPartData data, Action<CharacterPartData> action, bool isSelected)
    {
        if (data == null || item == null) return;
        item.SetData(data, action, isSelected);
    }
}

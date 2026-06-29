using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;

public class TheatreActorProfileAdpter : GridAdapter<GridParams, TheatreActorProfileItemHolder>
{
    public LazyDataHelper<DraftListItem> Data { get; set; }
    public Action<DraftListItem> OnItemClick;

    protected override void UpdateCellViewsHolder(TheatreActorProfileItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        if (model == null) return;
        viewsHolder.UpdateViews(model, OnItemClick);
    }
}

public class TheatreActorProfileItemHolder : CellViewsHolder
{
    public TheatreActorProfileItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<TheatreActorProfileItem>();
    }

    public void UpdateViews(DraftListItem data, Action<DraftListItem> onSelect)
    {
        if (data == null) return;
        item.SetData(data, onSelect);
    }
}

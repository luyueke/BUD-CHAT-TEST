using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;

public class TheatreProfileAdpter : GridAdapter<GridParams, TheatreProfileItemHolder>
{
    public LazyDataHelper<DraftListItem> Data { get; set; }

    protected override void UpdateCellViewsHolder(TheatreProfileItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        if (model == null) return;
        viewsHolder.UpdateViews(model);
    }
}

public class TheatreProfileItemHolder : CellViewsHolder
{
    public TheatreProfileItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<TheatreProfileItem>();
    }

    public void UpdateViews(DraftListItem data)
    {
        if (data == null) return;
        item.SetData(data);
    }
}

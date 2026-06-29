using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using System;
using Game.Event;
using UnityEngine;

public class CameraExpandAdpter : GridAdapter<GridParams, CameraExpandItemHolder>
{
    public LazyDataHelper<TaskItemData> Data { get; set; }

    public Action<TaskItemData> OnItemSelected;

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void UpdateCellViewsHolder(CameraExpandItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        if (model == null)
        {
            return;
        }
        viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
    }
}

public class CameraExpandItemHolder : CellViewsHolder
{
    public CameraExpandItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<CameraExpandItem>();
    }

    public void UpdateViews(TaskItemData data, Action<TaskItemData> action, int idx)
    {
        if (data == null)
        {
            return;
        }

        item.SetData(data, action, idx);
    }
}
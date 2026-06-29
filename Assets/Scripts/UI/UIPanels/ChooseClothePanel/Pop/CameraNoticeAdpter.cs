using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using GameData.Base;
using GameUI;
using System;
using System.Collections;
using UnityEngine;

public class CameraNoticeAdpter : GridAdapter<GridParams, CameraNoticeItemHolder>
{
    public LazyDataHelper<CameraNoticeCellData> Data { get; set; }

    public Action<object> OnItemSelected;

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void UpdateCellViewsHolder(CameraNoticeItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        if (model == null)
        {
            return;
        }
        viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
    }
}

public class CameraNoticeItemHolder : CellViewsHolder
{
    public CameraNoticeItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<CameraNoticeItem>();
    }

    public void UpdateViews(object data, Action<object> action, int idx)
    {
        if (data == null)
        {
            return;
        }

        item.SetData(data, action, idx);
    }
}
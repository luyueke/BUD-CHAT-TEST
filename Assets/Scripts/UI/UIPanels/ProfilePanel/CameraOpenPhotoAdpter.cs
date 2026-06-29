using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;

public class CameraOpenPhotoAdpter : GridAdapter<GridParams, CameraOpenPhotoItemHolder>
{
    public LazyDataHelper<AlbumPhotoInfo> Data { get; set; }

    public Action<AlbumPhotoInfo> OnItemSelected;

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void UpdateCellViewsHolder(CameraOpenPhotoItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        if (model == null)
        {
            return;
        }
        viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex);
    }
}

public class CameraOpenPhotoItemHolder : CellViewsHolder
{
    public CameraOpenPhotoItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<CameraOpenPhotoItem>();
    }

    public void UpdateViews(AlbumPhotoInfo data, Action<AlbumPhotoInfo> action, int idx)
    {
        if (data == null)
        {
            return;
        }

        item.SetData(data, action, idx);
    }
}
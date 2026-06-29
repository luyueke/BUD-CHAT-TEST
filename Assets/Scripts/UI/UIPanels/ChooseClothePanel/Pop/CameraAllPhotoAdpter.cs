using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using GameData.Base;
using GameUI;
using System;
using System.Collections;
using UnityEngine;

public class CameraAllPhotoAdpter : GridAdapter<GridParams, CameraAllPhotoItemHolder>
{
    public LazyDataHelper<CameraImagePack> Data { get; set; }

    public Action<CameraImagePack> OnItemSelected;

    /// <summary>用于滑动复用后恢复「选中」勾选状态（与 curSelectList 一致）</summary>
    public System.Func<CameraImagePack, bool> IsPackSelected;

    protected override void OnInitialized()
    {
        base.OnInitialized();
    }

    protected override void UpdateCellViewsHolder(CameraAllPhotoItemHolder viewsHolder)
    {
        var model = Data.GetOrCreate(viewsHolder.ItemIndex);
        if (model == null)
        {
            return;
        }
        bool selected = IsPackSelected != null && IsPackSelected(model);
        viewsHolder.UpdateViews(model, OnItemSelected, viewsHolder.ItemIndex, selected);
    }
}

public class CameraAllPhotoItemHolder : CellViewsHolder
{
    public CameraAllPhotoItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<CameraAllPhotoItem>();
    }

    public void UpdateViews(CameraImagePack data, Action<CameraImagePack> action, int idx, bool selected)
    {
        if (data == null)
        {
            return;
        }

        item.SetData(data, action, idx, selected);
    }
}
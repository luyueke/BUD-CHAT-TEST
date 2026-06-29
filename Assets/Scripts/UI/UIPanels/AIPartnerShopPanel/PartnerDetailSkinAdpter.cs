using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using UnityEngine;

public class PartnerDetailSkinAdpter : GridAdapter<GridParams, PartnerDetailSkinItemHolder>
{
    public Action<SkinPackInfo> OnItemSelected;

    private List<SkinPackInfo> _items = new List<SkinPackInfo>();
    private string _selectedId;

    public void SetItems(List<SkinPackInfo> items)
    {
        _items = items ?? new List<SkinPackInfo>();
        _selectedId = null;
        ResetItems(_items.Count);
    }

    public void SelectById(string id)
    {
        if (_selectedId == id) return;
        _selectedId = id;
        ResetItems(_items.Count);
    }

    protected override void UpdateCellViewsHolder(PartnerDetailSkinItemHolder viewsHolder)
    {
        if (viewsHolder.ItemIndex >= _items.Count) return;
        var data = _items[viewsHolder.ItemIndex];
        viewsHolder.UpdateViews(data, OnItemSelected, viewsHolder.ItemIndex, data?.packId == _selectedId);
    }
}

public class PartnerDetailSkinItemHolder : CellViewsHolder
{
    public PartnerDetailSkinItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<PartnerDetailSkinItem>();
    }

    public void UpdateViews(SkinPackInfo data, Action<SkinPackInfo> action, int idx, bool isSelected = false)
    {
        if (data == null) return;
        item.SetData(data, action, idx, isSelected);
    }
}

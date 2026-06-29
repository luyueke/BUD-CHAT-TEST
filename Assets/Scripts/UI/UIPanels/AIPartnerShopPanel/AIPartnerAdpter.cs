using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using UI.UIPanels.IncubationCabin;
using UnityEngine;

public class AIPartnerAdpter : GridAdapter<GridParams, AIPartnerItemHolder>
{
    public Action<CabinCharacterUgcInfo> OnItemSelected;

    private List<CabinCharacterUgcInfo> _items = new List<CabinCharacterUgcInfo>();
    private string _selectedId;

    public void SetItems(List<CabinCharacterUgcInfo> items, string preSelectedId = null)
    {
        _items = items ?? new List<CabinCharacterUgcInfo>();
        _selectedId = preSelectedId;
        ResetItems(_items.Count);
    }

    public void SelectById(string id)
    {
        if (_selectedId == id) return;
        _selectedId = id;
        ResetItems(_items.Count);
    }

    protected override void UpdateCellViewsHolder(AIPartnerItemHolder viewsHolder)
    {
        if (viewsHolder.ItemIndex >= _items.Count) return;
        var data = _items[viewsHolder.ItemIndex];
        viewsHolder.UpdateViews(data, OnItemSelected, data?.id == _selectedId);
    }
}

public class AIPartnerItemHolder : CellViewsHolder
{
    public CabinCharacterCardItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<CabinCharacterCardItem>();
    }

    public void UpdateViews(CabinCharacterUgcInfo data, Action<CabinCharacterUgcInfo> action, bool isSelected = false)
    {
        if (data == null) return;
        item.SetData(data, baseInfo => action?.Invoke((CabinCharacterUgcInfo)baseInfo));
        item.SetBadgesVisible(false);
        item.SetSelected(isSelected);
    }
}

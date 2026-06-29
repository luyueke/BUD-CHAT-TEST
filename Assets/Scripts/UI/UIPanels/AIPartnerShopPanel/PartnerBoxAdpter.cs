using System;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;

public class PartnerBoxAdpter : GridAdapter<GridParams, PartnerBoxItemHolder>
{
    public Action<CharacterBoxInfo> OnItemSelected;

    private List<CharacterBoxInfo> _items = new List<CharacterBoxInfo>();
    private string _selectedId;

    public void SetItems(List<CharacterBoxInfo> items, string preSelectedId = null)
    {
        _items = items ?? new List<CharacterBoxInfo>();
        _selectedId = preSelectedId;
        ResetItems(_items.Count);
    }

    public void SelectById(string id)
    {
        if (_selectedId == id) return;
        _selectedId = id;
        ResetItems(_items.Count);
    }

    protected override void UpdateCellViewsHolder(PartnerBoxItemHolder viewsHolder)
    {
        if (viewsHolder.ItemIndex >= _items.Count) return;
        var data = _items[viewsHolder.ItemIndex];
        viewsHolder.UpdateViews(data, OnItemSelected, data?.id == _selectedId);
    }
}

public class PartnerBoxItemHolder : CellViewsHolder
{
    public PartnerBoxItem item;

    public override void CollectViews()
    {
        base.CollectViews();
        item = root.GetComponent<PartnerBoxItem>();
    }

    public void UpdateViews(CharacterBoxInfo data, Action<CharacterBoxInfo> action, bool isSelected = false)
    {
        if (data == null) return;
        item.SetData(data, action, isSelected);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using UnityEngine;
using UnityEngine.EventSystems;

public class BadgeListAdapter : OSA<BaseParamsWithPrefab, BadgeListItemHolder>, IPointerClickHandler
{
    public UnityEngine.Events.UnityEvent OnItemsUpdated;

    public SimpleDataHelper<CreatorBadgeInfoData> Data { get; private set; }

    private Action<CreatorBadgeInfoData> _externalOnItemClick;
    private int _selectedId = -1;
    private bool _isHistoryMode;

    #region OSA implementation

    protected override void Start()
    {
        Data = new SimpleDataHelper<CreatorBadgeInfoData>(this);
        base.Start();
    }

    protected override BadgeListItemHolder CreateViewsHolder(int itemIndex)
    {
        var instance = new BadgeListItemHolder();
        instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
        return instance;
    }

    protected override void UpdateViewsHolder(BadgeListItemHolder newOrRecycled)
    {
        var model = Data[newOrRecycled.ItemIndex];
        bool isSelected = model != null && model.id == _selectedId;
        newOrRecycled.UpdateViews(model, OnItemClickInternal, isSelected, _isHistoryMode);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    #endregion

    public void SetOnItemClick(Action<CreatorBadgeInfoData> onClick)
    {
        _externalOnItemClick = onClick;
    }

    public void SetHistoryMode(bool isHistory, bool refresh = true)
    {
        if (_isHistoryMode == isHistory) return;
        _isHistoryMode = isHistory;
        if (refresh)
        {
            Refresh(keepVelocity: true);
        }
        else
        {
            // 尝试只更新可见项（避免整表刷新）
            int count = Data != null ? Data.Count : 0;
            for (int i = 0; i < count; i++)
            {
                var vh = GetItemViewsHolderIfVisible(i);
                if (vh != null && vh.BadgeListItem != null)
                {
                    var m = Data[i];
                    bool selected = m != null && m.id == _selectedId;
                    vh.BadgeListItem.SetData(m, OnItemClickInternal, selected, _isHistoryMode);
                }
            }
        }
    }

    public void SetSelected(CreatorBadgeInfoData model, bool refresh = true)
    {
        SetSelectedId(model != null ? model.id : -1, refresh);
    }

    public void SetSelectedId(int id, bool refresh = true)
    {
        var prevId = _selectedId;
        if (prevId == id) return;

        _selectedId = id;
        UpdateSelectionVisualOnly(prevId, _selectedId);
        if (refresh)
        {
            // 仅在需要时刷新：一般选中状态已由 UpdateSelectionVisualOnly 处理
        }
    }

    private void OnItemClickInternal(CreatorBadgeInfoData model)
    {
        if (model != null)
        {
            var prevId = _selectedId;
            _selectedId = model.id;
            UpdateSelectionVisualOnly(prevId, _selectedId);
        }
        _externalOnItemClick?.Invoke(model);
    }

    private void UpdateSelectionVisualOnly(int prevId, int curId)
    {
        var prevIndex = IndexOfId(prevId);
        if (prevIndex >= 0)
        {
            var vh = GetItemViewsHolderIfVisible(prevIndex);
            if (vh != null && vh.BadgeListItem != null)
            {
                vh.BadgeListItem.SetData(Data[prevIndex], OnItemClickInternal, false, _isHistoryMode);
            }
        }

        var curIndex = IndexOfId(curId);
        if (curIndex >= 0)
        {
            var vh = GetItemViewsHolderIfVisible(curIndex);
            if (vh != null && vh.BadgeListItem != null)
            {
                vh.BadgeListItem.SetData(Data[curIndex], OnItemClickInternal, true, _isHistoryMode);
            }
        }
    }

    private int IndexOfId(int id)
    {
        if (id < 0 || Data == null) return -1;
        int count = Data.Count;
        for (int i = 0; i < count; i++)
        {
            var m = Data[i];
            if (m != null && m.id == id) return i;
        }
        return -1;
    }

    private Action<PointerEventData> _clickedHandler;
    public void AddClickListener(Action<PointerEventData> clickAction)
    {
        _clickedHandler = clickAction;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsDragging)
        {
            _clickedHandler?.Invoke(eventData);
        }
    }
}

public class BadgeListItemHolder : BaseItemViewsHolder
{
    public BadgeListItem BadgeListItem;
    public override void CollectViews()
    {
        base.CollectViews();
        BadgeListItem = root.GetComponent<BadgeListItem>();
        BadgeListItem.Init();
    }

    public void UpdateViews(CreatorBadgeInfoData model, Action<CreatorBadgeInfoData> onClick, bool isSelected, bool isHistory)
    {
        BadgeListItem.SetData(model, onClick, isSelected, isHistory);
    }
}
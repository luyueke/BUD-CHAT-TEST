using System;
using System.Collections.Generic;
using Pb.Theatre;
using UnityEngine;
using UnityEngine.UI;

public class TheatreEditorMainList : TheatreEditorUIBase<TheatreEditorDataCenter>
{
    [SerializeField] private TheatreEditorSectionItem sectionItemPrefab;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ScrollRect scrollRect;

    private readonly List<TheatreEditorSectionItem> sectionItems = new();
    public Action<int> OnScrollFirstIndexChanged;

    private Action<POCTheatreSection> OnSectionSelected;
    private Action<int> onJumpWindowRequested;
    private Action<POCTheatreSection> onPlayClicked;

    public override void OnInit(TheatreEditorDataCenter param)
    {
        base.OnInit(param);

        if (contentRoot == null)
            contentRoot = GameObjectEx.FindChildByName(transform, "Content");
        // 事件订阅统一放到 OnShow，避免 DataRoot 过期时双重订阅
    }

    public override void OnShow(TheatreEditorDataCenter param)
    {
        // 先从旧 DataRoot 取消订阅
        if (DataRoot != null)
        {
            DataRoot.OnSectionCreated -= OnSectionCreated;
            DataRoot.OnSectionRemoved -= OnSectionRemoved;
            DataRoot.OnSectionSelected -= OnSectionSelectedByData;
            DataRoot.OnDataChanged -= OnDataChanged;
            DataRoot.OnSectionListReordered -= OnSectionListReordered;
        }

        base.OnShow(param);
        DataRoot = param;

        // 订阅新 DataRoot
        DataRoot.OnSectionCreated += OnSectionCreated;
        DataRoot.OnSectionRemoved += OnSectionRemoved;
        DataRoot.OnSectionSelected += OnSectionSelectedByData;
        DataRoot.OnDataChanged += OnDataChanged;
        DataRoot.OnSectionListReordered += OnSectionListReordered;

        scrollRect?.onValueChanged.RemoveListener(OnScrollChanged);
        scrollRect?.onValueChanged.AddListener(OnScrollChanged);

        RefreshAllSections();

        // 应用初始选中高亮（不触发 SectionEdit，仅视觉）
        if (DataRoot.currentSelected != null)
            OnSectionSelectedByData(DataRoot.currentSelected);
    }

    public void SetOnSectionSelected(Action<POCTheatreSection> action)
    {
        OnSectionSelected = action;
    }

    public void SetOnJumpWindowRequested(Action<int> action)
    {
        onJumpWindowRequested = action;
        foreach (var item in sectionItems)
            item.onJumpWindowRequested = action;
    }

    public void SetOnPlayClicked(Action<POCTheatreSection> action)
    {
        onPlayClicked = action;
        foreach (var item in sectionItems)
            item.onPlayClicked = action;
    }

    /// <summary>
    /// 滚动到指定 Section 所在位置，供 MainEdit 跳转使用。
    /// </summary>
    public void RefreshSectionItem(POCTheatreSection section)
    {
        var item = sectionItems.Find(x => x.SectionData == section);
        item?.RefreshDisplay();
    }

    public void ScrollToSection(POCTheatreSection section)
    {
        if (scrollRect == null || section == null) return;
        var item = sectionItems.Find(x => x.SectionData == section);
        if (item == null) return;

        Canvas.ForceUpdateCanvases();

        var itemRT = item.GetComponent<RectTransform>();
        var contentRT = scrollRect.content;
        if (itemRT == null || contentRT == null) return;

        float contentHeight = contentRT.rect.height;
        float viewportHeight = scrollRect.viewport != null ? scrollRect.viewport.rect.height : scrollRect.GetComponent<RectTransform>().rect.height;
        if (contentHeight <= viewportHeight) return;

        // content 顶部对齐，item.anchoredPosition.y 为负值（向下）
        float itemTop = Mathf.Abs(itemRT.anchoredPosition.y);
        float scrollPos = Mathf.Clamp01((itemTop - viewportHeight * 0.5f) / (contentHeight - viewportHeight));
        scrollRect.verticalNormalizedPosition = 1f - scrollPos;
    }

    private void RefreshAllSections()
    {
        ClearAllItems();
        if (DataRoot == null) return;
        foreach (var section in DataRoot.SectionList)
            CreateSectionItemUI(section);
    }

    private void OnSectionCreated(POCTheatreSection section)
    {
        // Find predecessor to insert at the correct position
        int insertAfterListIndex = -1;
        for (int i = 0; i < sectionItems.Count; i++)
        {
            if (sectionItems[i].SectionData.SectionIndex < section.SectionIndex)
                insertAfterListIndex = i;
        }

        CreateSectionItemUI(section, insertAfterListIndex);

        // Refresh all items — indices of existing sections may have shifted
        foreach (var si in sectionItems)
            si.RefreshDisplay();

        ScrollToSection(section);
    }

    private void OnSectionRemoved(POCTheatreSection section)
    {
        var item = sectionItems.Find(x => x.SectionData == section);
        if (item != null)
        {
            sectionItems.Remove(item);
            Destroy(item.gameObject);
        }
        // 刷新所有剩余 item 的 index 显示
        foreach (var si in sectionItems)
            si.RefreshDisplay();
    }

    private void OnSectionSelectedByData(POCTheatreSection section)
    {
        foreach (var item in sectionItems)
            item.SetSelected(item.SectionData == section);
    }

    private void OnDataChanged()
    {
        if (DataRoot?.currentSelected == null) return;
        var item = sectionItems.Find(x => x.SectionData == DataRoot.currentSelected);
        item?.RefreshDisplay();
    }

    private void OnSectionListReordered()
    {
        RefreshAllSections();
        if (DataRoot?.currentSelected != null)
            OnSectionSelectedByData(DataRoot.currentSelected);
    }

    private void CreateSectionItemUI(POCTheatreSection section, int insertAfterListIndex = -1)
    {
        if (sectionItemPrefab == null || contentRoot == null) return;
        var itemObj = Instantiate(sectionItemPrefab.gameObject, contentRoot);
        var item = itemObj.GetComponent<TheatreEditorSectionItem>();
        item.Init(section, DataRoot, OnItemClicked);
        item.onJumpWindowRequested = onJumpWindowRequested;
        item.onPlayClicked = onPlayClicked;

        if (insertAfterListIndex >= 0 && insertAfterListIndex < sectionItems.Count)
        {
            int siblingIndex = sectionItems[insertAfterListIndex].transform.GetSiblingIndex() + 1;
            itemObj.transform.SetSiblingIndex(siblingIndex);
            sectionItems.Insert(insertAfterListIndex + 1, item);
        }
        else
        {
            sectionItems.Add(item);
        }

        itemObj.SetActive(true);
    }

    private void OnItemClicked(TheatreEditorSectionItem item)
    {
        OnSectionSelected?.Invoke(item.SectionData);
    }

    private void ClearAllItems()
    {
        foreach (var item in sectionItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        sectionItems.Clear();
    }

    private void OnScrollChanged(Vector2 _)
    {
        OnScrollFirstIndexChanged?.Invoke(GetFirstVisibleIndex());
    }

    private int GetFirstVisibleIndex()
    {
        if (scrollRect == null || sectionItems.Count == 0) return 0;
        var contentRT = scrollRect.content;
        var viewportRT = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
        float contentH = contentRT.rect.height;
        float viewportH = viewportRT.rect.height;
        if (contentH <= viewportH) return 0;
        float scrolledDown = (1f - scrollRect.verticalNormalizedPosition) * (contentH - viewportH);
        for (int i = 0; i < sectionItems.Count; i++)
        {
            var rt = sectionItems[i].transform as RectTransform;
            if (rt != null && Mathf.Abs(rt.anchoredPosition.y) >= scrolledDown)
                return i;
        }
        return sectionItems.Count - 1;
    }

    public override void Destroy()
    {
        base.Destroy();
        scrollRect?.onValueChanged.RemoveListener(OnScrollChanged);
        if (DataRoot != null)
        {
            DataRoot.OnSectionCreated -= OnSectionCreated;
            DataRoot.OnSectionRemoved -= OnSectionRemoved;
            DataRoot.OnSectionSelected -= OnSectionSelectedByData;
            DataRoot.OnDataChanged -= OnDataChanged;
            DataRoot.OnSectionListReordered -= OnSectionListReordered;
        }
    }
}

using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData.BaseInfo;
using UnityEngine;

/// <summary>
/// 演员选择列表 Adapter（GridView）。
/// 数据：SimpleDataHelper&lt;OCTheatreAvatarInfo&gt;。
/// 选中状态通过 IsSelected 委托查询 DataCenter，切换通过 OnToggleItem 回调通知 AvatarEdit。
/// </summary>
public class TheatreEditorAvatarAdapter : GridAdapter<GridParams, TheatreEditorAvatarViewsHolder>
{
    /// <summary>切换演员选中时回调 (avatarInfo, isSelected)。</summary>
    public Action<OCTheatreAvatarInfo, bool> OnToggleItem;

    /// <summary>查询演员是否已选中，由 AvatarEdit 注入。</summary>
    public Func<string, bool> IsSelected;

    public UnityEngine.Events.UnityEvent OnItemsUpdated;

    public SimpleDataHelper<OCTheatreAvatarInfo> Data { get; set; }

    private IPool texturePool;

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestroyer);
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ClearPool();
        OnToggleItem = null;
        IsSelected = null;
    }

    public void ClearPool()
    {
        texturePool?.Clear();
    }

    private void TextureDestroyer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false, bool keepVelocity = false)
    {
        _CellsCount = Data.Count;
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void OnCellViewsHolderCreated(
        TheatreEditorAvatarViewsHolder cellVH,
        CellGroupViewsHolder<TheatreEditorAvatarViewsHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        if (cellVH.AvatarRemoteImage != null)
            cellVH.AvatarRemoteImage.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(TheatreEditorAvatarViewsHolder newOrRecycled)
    {
        var model = Data[newOrRecycled.ItemIndex];
        bool selected = model != null && (IsSelected?.Invoke(model.id) ?? false);
        newOrRecycled.UpdateViews(model, selected, (info, isOn) => OnToggleItem?.Invoke(info, isOn));

        string url = TheatreEditorAvatarItem.ResolveDisplayUrl(model);
        if (!string.IsNullOrEmpty(url) && newOrRecycled.AvatarRemoteImage != null)
            newOrRecycled.AvatarRemoteImage.Load(url);
    }
}

/// <summary>每个演员格子的 ViewsHolder。</summary>
public class TheatreEditorAvatarViewsHolder : CellViewsHolder
{
    public RemoteImageBehaviour AvatarRemoteImage;
    public TheatreEditorAvatarItem AvatarItem;

    // protected override UnityEngine.RectTransform GetViews()
    // {
    //     // Cell prefab 结构：Root（LayoutElement）→ 第一个子节点持有实际内容
    //     return root.GetChild(0) as UnityEngine.RectTransform;
    // }

    public override void CollectViews()
    {
        base.CollectViews();
        root.TryGetComponent(out AvatarItem);
        if (AvatarItem != null)
            AvatarRemoteImage = AvatarItem.GetComponentInChildren<RemoteImageBehaviour>();
    }

    public void UpdateViews(
        OCTheatreAvatarInfo info,
        bool selected,
        Action<OCTheatreAvatarInfo, bool> onToggle)
    {
        if (AvatarItem == null || info == null) return;
        AvatarItem.Init(info.id, info, selected, isOn => onToggle?.Invoke(info, isOn));
    }
}

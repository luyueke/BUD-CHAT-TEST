using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using frame8.Logic.Misc.Other.Extensions;
using UnityEngine.EventSystems;

public class PhotoListAdapter : OSA<BaseParamsWithPrefab,PhotoListItemHolder>,IPointerClickHandler
{
    public UnityEngine.Events.UnityEvent OnItemsUpdated;
    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<CameraImagePack> Data { get; private set; }
    private IPool texturePool;
    private Action<CameraImagePack> _externalOnItemClick;
    private string _selectedName;

    #region OSA implementation
    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<CameraImagePack>(this);
        base.Start();
    }
    
    /// <summary>
    /// 销毁必须清空池对象
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (texturePool!=null)
        {
            texturePool.Clear();
        }
    }


    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }
    
    
    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdated?.Invoke();
        base.Refresh(false, keepVelocity);
    }
    
    
    /// <inheritdoc/>
    protected override PhotoListItemHolder CreateViewsHolder(int itemIndex)
    {
        var instance = new PhotoListItemHolder();
        instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
        return instance;
    }
    
    
    protected override void UpdateViewsHolder(PhotoListItemHolder newOrRecycled)
    {
        CameraImagePack model = Data[newOrRecycled.ItemIndex];
        bool isSelected = !string.IsNullOrEmpty(_selectedName) && model != null && model.name == _selectedName;
        newOrRecycled.UpdateViews(model, OnItemClickInternal, isSelected);
    }
    
    #endregion

    
    private Action<PointerEventData> mClickedHandler = null;
    //竖直方向移动到列表尾
    private Action<PointerEventData> mMoveEndHandler = null;
    /// <summary>
    /// 用来区分拖动的点击和滑动的点击
    /// 滑动时不会响应点击事件
    /// </summary>
    private bool m_canClick = true; 

    public void AddClickListener(Action<PointerEventData> clickAction)
    {
        mClickedHandler = clickAction;
    }

    public void SetOnItemClick(Action<CameraImagePack> onClick)
    {
        _externalOnItemClick = onClick;
    }

    public void SetSelected(CameraImagePack model, bool refresh = true)
    {
        SetSelectedName(model?.name, refresh);
    }

    public void SetSelectedName(string name, bool refresh = true)
    {
        var prevName = _selectedName;
        if (prevName == name) return;

        _selectedName = name;
        UpdateSelectionVisualOnly(prevName, _selectedName);
    }

    private void OnItemClickInternal(CameraImagePack model)
    {
        if (model != null)
        {
            var prevName = _selectedName;
            _selectedName = model.name;
            LoggerUtils.Log($"OnItemClickInternal: {_selectedName}");
            LoggerUtils.Log($"prevName: {prevName}");
            UpdateSelectionVisualOnly(prevName, _selectedName);
        }
        _externalOnItemClick?.Invoke(model);
    }

    private void UpdateSelectionVisualOnly(string prevName, string curName)
    {
        var prevIndex = IndexOfName(prevName);
        if (prevIndex >= 0)
        {
            var vh = GetItemViewsHolderIfVisible(prevIndex);
            if (vh != null && vh.PhotoListItem != null)
            {
                vh.PhotoListItem.SetSelected(false);
            }
        }

        var curIndex = IndexOfName(curName);
        if (curIndex >= 0)
        {
            var vh = GetItemViewsHolderIfVisible(curIndex);
            if (vh != null && vh.PhotoListItem != null)
            {
                vh.PhotoListItem.SetSelected(true);
            }
        }
    }

    private int IndexOfName(string name)
    {
        if (string.IsNullOrEmpty(name) || Data == null) return -1;
        int count = Data.Count;
        for (int i = 0; i < count; i++)
        {
            var m = Data[i];
            if (m != null && m.name == name) return i;
        }
        return -1;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsDragging)
        {
            mClickedHandler?.Invoke(eventData);
        }
    }
}

public class PhotoListItemHolder : BaseItemViewsHolder
{
    public PhotoListItem PhotoListItem;
    public override void CollectViews()
    {
        base.CollectViews();
        PhotoListItem = root.GetComponent<PhotoListItem>();
        PhotoListItem.Init();
    }

    public void UpdateViews(CameraImagePack model, Action<CameraImagePack> onClick, bool isSelected)
    {
        PhotoListItem.SetData(model, onClick, isSelected);
    }
}
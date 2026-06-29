using System;
using Com.TheFallenGames.OSA.Core;
using Com.TheFallenGames.OSA.CustomParams;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using UnityEngine.EventSystems;

public class PartnerSkinAdpter : OSA<BaseParamsWithPrefab, PartnerSkinItemHolder>, IPointerClickHandler
{
    public SimpleDataHelper<SkinPackInfo> Data { get; private set; }
    private IPool _texturePool;
    private Action<SkinPackInfo> _onItemClick;
    private string _selectedPackId;

    protected override void Start()
    {
        _texturePool = new FIFOCachingPool(12, (_, tex) => { if (tex is UnityEngine.Object o) Destroy(o); });
        Data = new SimpleDataHelper<SkinPackInfo>(this);
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _texturePool?.Clear();
    }

    protected override PartnerSkinItemHolder CreateViewsHolder(int itemIndex)
    {
        var instance = new PartnerSkinItemHolder();
        instance.Init(_Params.ItemPrefab, _Params.Content, itemIndex);
        instance.PartnerSkinItem.InitPool(_texturePool);
        return instance;
    }

    protected override void UpdateViewsHolder(PartnerSkinItemHolder newOrRecycled)
    {
        var model = Data[newOrRecycled.ItemIndex];
        bool isSelected = model != null && model.packId == _selectedPackId;
        newOrRecycled.UpdateViews(model, OnItemClickInternal, isSelected);
    }

    public void SetOnItemClick(Action<SkinPackInfo> onClick) => _onItemClick = onClick;

    public void SetSelected(SkinPackInfo model)
    {
        var newId = model?.packId;
        if (_selectedPackId == newId) return;
        var prevId = _selectedPackId;
        _selectedPackId = newId;
        UpdateSelectionVisualOnly(prevId, newId);
    }

    private void OnItemClickInternal(SkinPackInfo model)
    {
        if (model == null) return;
        var prevId = _selectedPackId;
        _selectedPackId = model.packId;
        UpdateSelectionVisualOnly(prevId, _selectedPackId);
        _onItemClick?.Invoke(model);
    }

    private void UpdateSelectionVisualOnly(string prevId, string curId)
    {
        SetVisibleItemSelected(prevId, false);
        SetVisibleItemSelected(curId, true);
    }

    private void SetVisibleItemSelected(string packId, bool isSelected)
    {
        if (string.IsNullOrEmpty(packId) || Data == null) return;
        for (int i = 0; i < Data.Count; i++)
        {
            if (Data[i]?.packId != packId) continue;
            var vh = GetItemViewsHolderIfVisible(i);
            if (vh != null && vh.PartnerSkinItem != null)
                vh.PartnerSkinItem.SetSelected(isSelected);
            break;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // IsDragging 时不触发，防止滑动误触
    }
}

public class PartnerSkinItemHolder : BaseItemViewsHolder
{
    public PartnerSkinItem PartnerSkinItem;

    public override void CollectViews()
    {
        base.CollectViews();
        PartnerSkinItem = root.GetComponent<PartnerSkinItem>();
        PartnerSkinItem.Init();
    }

    public void UpdateViews(SkinPackInfo model, Action<SkinPackInfo> onClick, bool isSelected)
    {
        PartnerSkinItem.SetData(model, onClick, isSelected);
    }
}

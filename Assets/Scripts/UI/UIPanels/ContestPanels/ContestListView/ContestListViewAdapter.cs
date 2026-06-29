
using System;
using Com.TheFallenGames.OSA.CustomAdapters.GridView;
using Com.TheFallenGames.OSA.DataHelpers;
using Com.TheFallenGames.OSA.Util.IO;
using Com.TheFallenGames.OSA.Util.IO.Pools;
using GameData;
using UnityEngine;
using UnityEngine.UI;

public class ContestListViewAdapter : GridAdapter<GridParams, ContestAvatarItemHolder>
{
    public UnityEngine.Events.UnityEvent OnItemsUpdatedAct;
    public Action<ContestEntryInfo> OnSelectItemAct;
    public Action EmptyDataAct;

    // Helper that stores data and notifies the adapter when items count changes
    // Can be iterated and can also have its elements accessed by the [] operator
    public SimpleDataHelper<ContestEntryInfo> Data { get; private set; }
    private IPool texturePool;

    public bool hideRankForce = false;

    public BUDContestType bUDContestType;

    private Vector2 _photoImageOriginalSize;
    private bool _photoImageOriginalSizeCached;

    #region OSA implementation

    protected override void Start()
    {
        texturePool = new FIFOCachingPool(12, TextureDestoryer);
        Data = new SimpleDataHelper<ContestEntryInfo>(this);
        base.Start();
    }

    /// <summary>
    /// 销毁必须清空池对象
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        texturePool?.Clear();
    }

    public void ClearPool()
    {
        texturePool?.Clear();
    }

    public void RemoveSingleItem(string infoId)
    {
        if (Data.Count == 0)
        {
            return;
        }

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;
            var creationId = Data[i].creationInfo.creationId;
            if (!string.IsNullOrEmpty(creationId) && creationId == infoId)
            {
                index = i;
                break;
            }
        }

        Data.RemoveOne(index);
        Refresh();
        if (Data.Count == 0)
        {
            EmptyDataAct.Invoke();
        }
    }

    public void UpdateSingleItem(ContestEntryInfo draftsListItem)
    {
        if (Data.Count == 0)
            return;

        int index = 0;
        for (var i = 0; i < Data.Count; i++)
        {
            if (Data[i] == null)
                continue;
            var leftId = Data[i].creationInfo.creationId;
            var rightId = draftsListItem.creationInfo.creationId;
            if (!string.IsNullOrEmpty(rightId) && leftId == rightId)
            {
                index = i;
                break;
            }
        }

        Data.UpdateItem(index, draftsListItem);
        Refresh();
    }

    private void OnSelectItem(ContestEntryInfo info)
    {
        OnSelectItemAct?.Invoke(info);
    }
    
    private void TextureDestoryer(object urlKey, object texture)
    {
        var asUnityObject = texture as UnityEngine.Object;
        if (asUnityObject != null)
            Destroy(asUnityObject);
    }

    public override void Refresh(bool contentPanelEndEdgeStationary = false /*ignored*/, bool keepVelocity = false)
    {
        OnItemsUpdatedAct?.Invoke();
        base.Refresh(false, keepVelocity);
    }

    protected override void OnCellViewsHolderCreated(ContestAvatarItemHolder cellVH,
        CellGroupViewsHolder<ContestAvatarItemHolder> cellGroup)
    {
        base.OnCellViewsHolderCreated(cellVH, cellGroup);
        cellVH.Rm_Cover.InitializeWithPool(texturePool);
    }

    protected override void UpdateCellViewsHolder(ContestAvatarItemHolder newOrRecycled)
    {
        ContestEntryInfo data = Data[newOrRecycled.ItemIndex];

        newOrRecycled.UpdateViews(data, OnSelectItem);

        if (hideRankForce)
        {
            newOrRecycled.HideRankForce();
        }

        var path = data?.creationInfo?.cover;

        if(bUDContestType == BUDContestType.Camera)
        {
            newOrRecycled.HidePrice();
            if (data != null && !string.IsNullOrEmpty(path))
            {
                newOrRecycled.Rm_Cover.gameObject.SetActive(false);
                // newOrRecycled.Rm_Cover.Load(path, true,
                //     (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });

                newOrRecycled.Rm_Cover.Load(path, true, (fromCache, success) =>
                {
                    if (!success && newOrRecycled.Rm_Cover != null)
                    {
                        newOrRecycled.Rm_Cover.gameObject.SetActive(false);
                        return;
                    }
                    newOrRecycled.Rm_Cover.gameObject.SetActive(true);
                    var raw = newOrRecycled.Rm_Cover != null ? newOrRecycled.Rm_Cover.RawImage : null;
                    if (raw != null && raw.texture != null)
                    {
                        if (_photoImageOriginalSizeCached)
                        {
                            // 已缓存，直接同步应用
                            ApplyCoverSizeByOriginal(raw, raw.texture, ref _photoImageOriginalSize, ref _photoImageOriginalSizeCached);
                        }
                        else
                        {
                            // 首次未缓存，延到帧末 layout 完成后再读尺寸
                            StartCoroutine(ApplyCoverNextFrame(raw, raw.texture));
                        }
                    }
                });
            }
        }
        else
        {
            if (data != null && !string.IsNullOrEmpty(path))
            {
                newOrRecycled.Rm_Cover.gameObject.SetActive(false);
                newOrRecycled.Rm_Cover.Load(path, true,
                    (from, success) => { newOrRecycled.Rm_Cover.gameObject.SetActive(true); });
            }
        }
    }

    private void ApplyCoverSizeByOriginal(RawImage raw, Texture tex, ref Vector2 cachedBoxSize, ref bool cached)
    {
        if (raw == null || tex == null) return;
        if (tex.width <= 0 || tex.height <= 0) return;

        var rt = raw.rectTransform;
        if (rt == null) return;
        if (!cached)
        {
            CacheOriginalSize(raw, ref cachedBoxSize, ref cached);
        }

        var box = cached && cachedBoxSize.x > 0f && cachedBoxSize.y > 0f
            ? cachedBoxSize
            : (rt.sizeDelta.x > 0f && rt.sizeDelta.y > 0f ? rt.sizeDelta : rt.rect.size);
        if (box.x <= 0f || box.y <= 0f) return;
        // 保持旧行为：使用现有 box 计算，不强制居中（不改 anchoredPosition）
        CameraImgDataUtils.TryApplyRawImageCover(raw, tex.width, tex.height, box, center: false);
    }

    private System.Collections.IEnumerator ApplyCoverNextFrame(RawImage raw, Texture tex)
    {
        yield return new WaitForEndOfFrame();
        if (raw == null || tex == null) yield break;
        CacheOriginalSize(raw, ref _photoImageOriginalSize, ref _photoImageOriginalSizeCached);
        ApplyCoverSizeByOriginal(raw, tex, ref _photoImageOriginalSize, ref _photoImageOriginalSizeCached);
    }

    private void CacheOriginalSize(RawImage raw, ref Vector2 size, ref bool cached)
    {
        if (raw == null)
        {
            cached = false;
            size = default;
            return;
        }

        var rt = raw.rectTransform;
        if (rt == null)
        {
            cached = false;
            size = default;
            return;
        }

        // 优先使用设计时 sizeDelta（更稳定）
        var s = rt.sizeDelta;
        if (s.x <= 0f || s.y <= 0f)
        {
            s = rt.rect.size;
        }

        size = s;
        cached = size.x > 0f && size.y > 0f;
    }

    #endregion
}

public class ContestAvatarItemHolder : CellViewsHolder
{
    public RemoteImageBehaviour Rm_Cover;
    public ContestBaseItemView draftsItem;

    public override void CollectViews()
    {
        base.CollectViews();
        Rm_Cover = GameObjectEx.FindChildByName(views, "Rm_Cover").GetComponent<RemoteImageBehaviour>();
        draftsItem = root.GetComponentInParent<ContestBaseItemView>();
    }

    public void UpdateViews(ContestEntryInfo model, Action<ContestEntryInfo> onSelect)
    {
        if (draftsItem != null)
        {
            draftsItem.Init(model, onSelect);
        }
    }

    public void HideRankForce()
    {
        if (draftsItem != null)
        {
            draftsItem.UpdateRankState(true);
        }
    }

    public void HidePrice()
    {
        if (draftsItem != null)
        {
            draftsItem.HidePrice(true);
        }
    }
}
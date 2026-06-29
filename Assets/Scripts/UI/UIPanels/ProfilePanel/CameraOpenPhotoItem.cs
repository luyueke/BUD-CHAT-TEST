using System;
using Com.TheFallenGames.OSA.Util.IO;
using UI.BaseWidgets;
using UnityEngine;

public class CameraOpenPhotoItem : MonoBehaviour
{
    [SerializeField] private RemoteImageBehaviour cover;
    [SerializeField] private CButton clickBtn;

    private AlbumPhotoInfo _data;
    private Action<AlbumPhotoInfo> _onClick;

    // Cover 显示：按“盒子”尺寸等比铺满（可裁剪不留白）
    private Vector2 _coverBoxSize;
    private bool _coverBoxSizeCached;

    private void Awake()
    {
        if (cover == null)
        {
            cover = GetComponentInChildren<RemoteImageBehaviour>(true);
        }
        if (clickBtn == null)
        {
            clickBtn = GetComponentInChildren<CButton>(true);
        }
        CacheCoverBoxSize();
    }

    public void SetData(AlbumPhotoInfo data, Action<AlbumPhotoInfo> action, int idx)
    {
        _data = data;
        _onClick = action;

        var coverUrl = data?.albumItem?.cover;
        if (string.IsNullOrEmpty(coverUrl))
        {
            coverUrl = data?.albumItem?.coverFull;
        }

        if (cover != null)
        {
            if (string.IsNullOrEmpty(coverUrl))
            {
                cover.gameObject.SetActive(false);
            }
            else
            {
                cover.gameObject.SetActive(true);
                cover.Load(coverUrl, true, (fromCache, success) =>
                {
                    if (!success && cover != null)
                    {
                        cover.gameObject.SetActive(false);
                        return;
                    }

                    TryApplyCoverFill();
                });
            }
        }

        if (clickBtn != null)
        {
            clickBtn.onClick.RemoveAllListeners();
            clickBtn.onClick.AddListener(() => { _onClick?.Invoke(_data); });
        }
    }

    private void CacheCoverBoxSize()
    {
        if (_coverBoxSizeCached) return;
        if (cover == null || cover.RawImage == null) return;
        if (CameraImgDataUtils.TryGetRawImageCoverBoxSize(cover.RawImage, out var boxSize))
        {
            _coverBoxSize = boxSize;
            _coverBoxSizeCached = true;
        }
    }

    private void TryApplyCoverFill()
    {
        if (cover == null || cover.RawImage == null) return;
        var raw = cover.RawImage;
        if (raw.texture == null) return;

        CacheCoverBoxSize();
        if (!_coverBoxSizeCached) return;

        CameraImgDataUtils.TryApplyRawImageCover(raw, raw.texture.width, raw.texture.height, _coverBoxSize, center: true);
    }
}

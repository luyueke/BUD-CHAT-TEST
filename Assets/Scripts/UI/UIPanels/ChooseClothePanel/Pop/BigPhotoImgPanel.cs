using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class BigPhotoImgPanel : BasePanel<BigPhotoImgPanel>
{
    [SerializeField] private RemoteImageBehaviour PhotoImage;
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private RawImage LocalRawImage;
    private CameraImagePack _pack;

    // 以 RawImage 原本尺寸作为“基准框”，在框内等比缩放（不铺满屏幕）
    private Vector2 _photoImageOriginalSize;
    private bool _photoImageOriginalSizeCached;
    private Vector2 _localRawOriginalSize;
    private bool _localRawOriginalSizeCached;

    private new void Awake()
    {
        if (PhotoImage == null) PhotoImage = GetComponentInChildren<RemoteImageBehaviour>(true);
        if (CloseBtn == null) CloseBtn = GetComponentInChildren<CButton>(true);
        if (LocalRawImage == null) LocalRawImage = GetComponentInChildren<RawImage>(true);

        CacheOriginalSize(LocalRawImage, ref _localRawOriginalSize, ref _localRawOriginalSizeCached);
        CacheOriginalSize(PhotoImage != null ? PhotoImage.RawImage : null, ref _photoImageOriginalSize, ref _photoImageOriginalSizeCached);
    }
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _pack = args[0] as CameraImagePack;
        LoadPhoto();
    }

    private void LoadPhoto()
    {
        // 本地图片优先：只要本地能解密加载到 texture，就不再走网络图
        if (_pack != null && _pack.mediaType != 1)
        {
            var localTex = CameraImgDataUtils.Inst.LoadTextureLocalOriginal(_pack.name, _pack.uploader);
            if (localTex != null && LocalRawImage != null)
            {
                LocalRawImage.texture = localTex;
                ApplyContainSizeByOriginal(LocalRawImage, localTex, ref _localRawOriginalSize, ref _localRawOriginalSizeCached);
                return;
            }
        }
        LocalRawImage.texture = null;
        var coverUrl = _pack?.url;
        if (string.IsNullOrEmpty(coverUrl))
        {
            coverUrl = _pack?.previewUrl;
        }

        if (string.IsNullOrEmpty(coverUrl))
        {
            if (LocalRawImage == null)
            {
                TipPanel.ShowToast("图片数据异常，无法显示");
                if (PhotoImage != null) PhotoImage.gameObject.SetActive(false);
                return;
            }

            var tex = _pack != null ? CameraImgDataUtils.Inst.LoadTextureLocalOriginal(_pack.name, _pack.uploader) : null;
            if (tex == null)
            {
                TipPanel.ShowToast("本地图片加载失败");
                return;
            }

            LocalRawImage.texture = tex;
            ApplyContainSizeByOriginal(LocalRawImage, tex, ref _localRawOriginalSize, ref _localRawOriginalSizeCached);
            return;
        }

        PhotoImage.Load(coverUrl, true, (fromCache, success) =>
        {
            if (!success)
            {
                if (PhotoImage != null) PhotoImage.gameObject.SetActive(false);
                return;
            }

            var raw = PhotoImage != null ? PhotoImage.RawImage : null;
            if (raw != null && raw.texture != null)
            {
                ApplyContainSizeByOriginal(raw, raw.texture, ref _photoImageOriginalSize, ref _photoImageOriginalSizeCached);
            }
        });
    }

    /// <summary>
    /// 以 RawImage 原本尺寸为基准框，在框内等比缩放（Contain：不裁切、不拉伸，可能留白）。
    /// </summary>
    private static void ApplyContainSizeByOriginal(RawImage raw, Texture tex, ref Vector2 cachedBoxSize, ref bool cached)
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

        float texAspect = (float)tex.width / tex.height;
        float boxAspect = box.x / box.y;

        float targetW;
        float targetH;
        if (texAspect > boxAspect)
        {
            targetW = box.x;
            targetH = targetW / texAspect;
        }
        else
        {
            targetH = box.y;
            targetW = targetH * texAspect;
        }

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetW);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetH);
    }

    private static void CacheOriginalSize(RawImage raw, ref Vector2 size, ref bool cached)
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
}

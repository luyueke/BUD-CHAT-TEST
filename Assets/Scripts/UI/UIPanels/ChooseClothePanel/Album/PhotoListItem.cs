using System;
using System.Collections;
using System.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PhotoListItem : MonoBehaviour
{
    /// <summary>列表格缩略图最长边；与 Duplicate 成本近似随像素数线性，略大于常见格子即可。</summary>
    private const int ListThumbnailMaxEdge = 512;

    [SerializeField] private Image PhotoImg;
    [SerializeField] private CButton SelectBtn;
    [SerializeField] private GameObject SelectImg;
    [SerializeField] private GameObject LoadingMask;

    private CameraImagePack _model;
    private Action<CameraImagePack> _onClick;
    private Sprite _runtimeSprite;
    private Texture2D _runtimeTextureOwned;
    private bool _isSelected;

    /// <summary>每次 SetData 递增，丢弃过期网络协程结果，避免泄漏与错绑（StopCoroutine 在已 GetTexture 后会泄漏）</summary>
    private int _loadGen;

    // 缩略图显示：按原始设计尺寸做 Cover（不留白允许裁切）
    private Vector2 _photoImgOriginalSize;
    private bool _photoImgOriginalSizeCached;

    private void Awake()
    {
        if (SelectBtn != null)
        {
            SelectBtn.onClick.AddListener(OnSelectBtnClick);
        }
    }

    private void OnDestroy()
    {
        _loadGen++;
        ClearPhotoImageDisplay();
    }

    public void Init()
    {

    }

    private void ClearPhotoImageDisplay()
    {
        if (PhotoImg != null)
        {
            PhotoImg.sprite = null;
        }

        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }

        if (_runtimeTextureOwned != null)
        {
            Destroy(_runtimeTextureOwned);
            _runtimeTextureOwned = null;
        }
    }

    private void SetPhotoLoadingMask(bool show)
    {
        if (LoadingMask != null)
        {
            LoadingMask.SetActive(show);
        }
    }

    public void SetData(CameraImagePack model, Action<CameraImagePack> onClick = null, bool isSelected = false)
    {
        // 同一引用复用且缩略图仍有效时，避免重复 Blit/读像素（滑动卡顿主因）
        if (model != null && _model == model && _runtimeSprite != null && PhotoImg != null && PhotoImg.sprite == _runtimeSprite)
        {
            _onClick = onClick;
            SetSelected(isSelected);
            return;
        }

        _loadGen++;
        var token = _loadGen;

        SetPhotoLoadingMask(false);

        _model = model;
        _onClick = onClick;
        SetSelected(isSelected);
        CacheOriginalSize();
        if (PhotoImg != null) PhotoImg.preserveAspect = true;

        ClearPhotoImageDisplay();

        var runtimeTexture = TryLoadTextureWithFallback(model, out _);
        if (runtimeTexture == null)
        {
            var loadUrl = GetListThumbnailUrl(model);

            if (!string.IsNullOrEmpty(loadUrl))
            {
                SetPhotoLoadingMask(true);
                StartCoroutine(CoLoadNetworkTexture(model, loadUrl, token));
                return;
            }

            SetPhotoLoadingMask(false);
            return;
        }

        if (token != _loadGen)
        {
            return;
        }

        _runtimeTextureOwned = CameraImgDataUtils.DuplicateTextureForListThumbnail(runtimeTexture, ListThumbnailMaxEdge);
        var texForSprite = _runtimeTextureOwned != null ? _runtimeTextureOwned : runtimeTexture;
        _runtimeSprite = Sprite.Create(texForSprite, new Rect(0, 0, texForSprite.width, texForSprite.height), new Vector2(0.5f, 0.5f));
        PhotoImg.sprite = _runtimeSprite;
        ApplyCoverSizeByOriginal(texForSprite.width, texForSprite.height);
        SetPhotoLoadingMask(false);
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        SelectImg.SetActive(selected);
    }

    private Texture2D TryLoadTextureWithFallback(CameraImagePack pack, out string usedUploaderKey)
    {
        usedUploaderKey = pack.uploader;
        var tex = CameraImgDataUtils.Inst.LoadTextureLocal(pack.name, pack.uploader);
        if (tex != null)
        {
            return tex;
        }

        var uploaderFromName = ExtractUploaderFromName(pack.name);
        if (!string.IsNullOrEmpty(uploaderFromName) && uploaderFromName != pack.uploader)
        {
            tex = CameraImgDataUtils.Inst.LoadTextureLocal(pack.name, uploaderFromName);
            if (tex != null)
            {
                usedUploaderKey = uploaderFromName;
                Debug.LogWarning($"[CameraImgDataUtilsTester] 使用 name 解析的 uploader 回退成功: {uploaderFromName}");
                return tex;
            }
        }

        if (pack.uploader != "shotTemplate")
        {
            tex = CameraImgDataUtils.Inst.LoadTextureLocal(pack.name, "shotTemplate");
            if (tex != null)
            {
                usedUploaderKey = "shotTemplate";
                Debug.LogWarning("[CameraImgDataUtilsTester] 使用 shotTemplate uploader 回退成功。");
                return tex;
            }
        }

        var loadUrl = GetListThumbnailUrl(pack);
        if (!string.IsNullOrEmpty(loadUrl))
        {
            tex = CameraImgDataUtils.Inst.LoadTextureFromCloudCache(loadUrl);
            if (tex != null)
            {
                // 磁盘缓存每次 new 的纹理，不在 LRU；仍复制可避免与 LRU 缓存路径混用时的生命周期问题
                return tex;
            }
        }

        return null;
    }

    /// <summary>
    /// 列表优先使用缩略图地址（previewUrl），原图地址（url）兜底。
    /// </summary>
    private static string GetListThumbnailUrl(CameraImagePack pack)
    {
        if (pack == null)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(pack.previewUrl))
        {
            return pack.previewUrl;
        }

        return pack.url;
    }

    private static string ExtractUploaderFromName(string imageName)
    {
        if (string.IsNullOrEmpty(imageName))
        {
            return null;
        }

        var idx = imageName.IndexOf('_');
        if (idx <= 0)
        {
            return null;
        }

        return imageName.Substring(0, idx);
    }

    private IEnumerator CoLoadNetworkTexture(CameraImagePack pack, string url, int token)
    {
        while (!AlbumThumbnailNetworkThrottle.TryBeginDownload())
        {
            yield return null;
        }

        try
        {
            var expectedName = pack != null ? pack.name : null;

            var requestUrl = NormalizeTextureUrl(url);
            // nonReadable：解码与显存路径更轻，列表仅用于显示；后续 Duplicate 仍可用 Blit 源。
            using var request = UnityWebRequestTexture.GetTexture(requestUrl, true);
            yield return request.SendWebRequest();

            if (token != _loadGen)
            {
                yield break;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning(
                    $"[CameraImgDataUtilsTester] 本地与网络都加载失败: name={pack.name}, hasLocal={pack.hasLocal}, " +
                    $"url={requestUrl}, err={request.error}"
                );
                SetPhotoLoadingMask(false);
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(request);
            if (tex == null)
            {
                Debug.LogWarning($"[CameraImgDataUtilsTester] 网络图片解析失败: name={pack.name}, url={url}");
                SetPhotoLoadingMask(false);
                yield break;
            }

            if (token != _loadGen)
            {
                Destroy(tex);
                yield break;
            }

            if (_model == null || _model.name != expectedName)
            {
                Destroy(tex);
                yield break;
            }

            if (_runtimeSprite != null)
            {
                Destroy(_runtimeSprite);
                _runtimeSprite = null;
            }

            if (_runtimeTextureOwned != null)
            {
                Destroy(_runtimeTextureOwned);
                _runtimeTextureOwned = null;
            }

            // 独立缩略图 URL：只把缩小后的图写入磁盘缓存，避免主线程写整图 PNG 导致卡顿。
            // 与 url 相同或仅有 url 时：仍缓存原图，供右侧大图等共用同一地址的场景。
            var isDistinctPreviewThumb = pack != null &&
                                         !string.IsNullOrEmpty(pack.previewUrl) &&
                                         !string.IsNullOrEmpty(pack.url) &&
                                         !string.Equals(pack.previewUrl, pack.url, StringComparison.OrdinalIgnoreCase) &&
                                         string.Equals(url, pack.previewUrl, StringComparison.OrdinalIgnoreCase);

            Texture2D displayTex;
            if (isDistinctPreviewThumb)
            {
                displayTex = CameraImgDataUtils.DuplicateTextureForListThumbnail(tex, ListThumbnailMaxEdge);
                Destroy(tex);
                if (displayTex != null)
                {
                    CameraImgDataUtils.Inst.SaveToCloudCache(pack.previewUrl, displayTex);
                }
            }
            else
            {
                CameraImgDataUtils.Inst.SaveToCloudCache(url, tex);
                displayTex = CameraImgDataUtils.DuplicateTextureForListThumbnail(tex, ListThumbnailMaxEdge);
                Destroy(tex);
            }

            _runtimeTextureOwned = displayTex;

            if (_runtimeTextureOwned == null)
            {
                SetPhotoLoadingMask(false);
                yield break;
            }

            _runtimeSprite = Sprite.Create(
                _runtimeTextureOwned,
                new Rect(0, 0, _runtimeTextureOwned.width, _runtimeTextureOwned.height),
                new Vector2(0.5f, 0.5f));
            PhotoImg.sprite = _runtimeSprite;
            ApplyCoverSizeByOriginal(_runtimeTextureOwned.width, _runtimeTextureOwned.height);
            SetPhotoLoadingMask(false);
        }
        finally
        {
            AlbumThumbnailNetworkThrottle.EndDownload();
        }
    }

    private void CacheOriginalSize()
    {
        if (_photoImgOriginalSizeCached) return;
        if (PhotoImg == null) return;

        var rt = PhotoImg.rectTransform;
        if (rt == null) return;

        var size = rt.sizeDelta;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = rt.rect.size;
        }

        if (size.x <= 0f || size.y <= 0f) return;
        _photoImgOriginalSize = size;
        _photoImgOriginalSizeCached = true;
    }

    private void ApplyCoverSizeByOriginal(int texWidth, int texHeight)
    {
        if (PhotoImg == null) return;
        if (texWidth <= 0 || texHeight <= 0) return;
        CacheOriginalSize();
        if (!_photoImgOriginalSizeCached) return;

        CameraImgDataUtils.TryApplyImageCover(PhotoImg, texWidth, texHeight, _photoImgOriginalSize, center: false);
    }

    private static string NormalizeTextureUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("file://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("content://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        try
        {
            var fullPath = Path.GetFullPath(url);
            if (File.Exists(fullPath))
            {
                return new Uri(fullPath).AbsoluteUri;
            }
        }
        catch
        {
            // ignore
        }

        return url;
    }

    private void OnSelectBtnClick()
    {
        _onClick?.Invoke(_model);
    }
}

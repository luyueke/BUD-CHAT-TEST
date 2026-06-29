using Com.TheFallenGames.OSA.Util.IO;
using System;
using System.Collections;
using System.IO;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CameraAllPhotoItem : MonoBehaviour
{
    private const int ListThumbnailMaxEdge = 512;

    public GameObject State;

    public GameObject On;

    public Image PhotoImg;

    public CButton Btn;

    Action<CameraImagePack> Action;

    private CameraImagePack _data;

    private Sprite _runtimeSprite;
    /// <summary>本地 Duplicate 或网络下载得到的、需与 Sprite 一并销毁的纹理</summary>
    private Texture2D _runtimeTextureOwned;

    /// <summary>每次 SetData 递增，用于丢弃过期的网络协程结果，避免泄漏与错绑</summary>
    private int _loadGen;

    // 与 PhotoListItem 一致：以预制体设计尺寸为框做 Cover（原比例铺满、可裁切、不留白）
    private Vector2 _photoImgOriginalSize;
    private bool _photoImgOriginalSizeCached;

    private void Awake()
    {
        Btn.onClick.AddListener(OnBtn);
    }

    private void OnDestroy()
    {
        _loadGen++;
        ClearRuntimeImage();
    }

    void OnBtn()
    {
        bool next = !On.gameObject.activeSelf;
        ApplySelectVisual(next);
        Action?.Invoke(_data);
    }

    public void SetData(CameraImagePack data, Action<CameraImagePack> action, int idx, bool selected)
    {
        // 复用同一 CameraImagePack 引用且缩略图仍有效时，避免重复 Blit/ReadPixels（滑动卡顿主因之一）
        if (data != null && _data == data && _runtimeSprite != null && PhotoImg != null && PhotoImg.sprite == _runtimeSprite)
        {
            Action = action;
            State.gameObject.SetActive(false);
            ApplySelectVisual(selected);
            return;
        }

        _loadGen++;
        var token = _loadGen;

        _data = data;
        Action = action;
        State.gameObject.SetActive(false);
        ApplySelectVisual(selected);

        CacheOriginalSize();
        if (PhotoImg != null)
        {
            PhotoImg.preserveAspect = true;
        }

        ClearRuntimeImage();

        var runtimeTexture = TryLoadTextureWithFallback(data, out _);
        if (runtimeTexture == null)
        {
            var loadUrl = GetListThumbnailUrl(data);
            if (!string.IsNullOrEmpty(loadUrl))
            {
                StartCoroutine(CoLoadNetworkTexture(data, loadUrl, selected, token));
                return;
            }
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
        ApplySelectVisual(selected);
    }

    private void ClearRuntimeImage()
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

    private void ApplySelectVisual(bool selected)
    {
        if (On != null)
        {
            On.SetActive(selected);
        }
    }

    private void CacheOriginalSize()
    {
        if (_photoImgOriginalSizeCached)
        {
            return;
        }

        if (PhotoImg == null)
        {
            return;
        }

        var rt = PhotoImg.rectTransform;
        if (rt == null)
        {
            return;
        }

        var size = rt.sizeDelta;
        if (size.x <= 0f || size.y <= 0f)
        {
            size = rt.rect.size;
        }

        if (size.x <= 0f || size.y <= 0f)
        {
            return;
        }

        _photoImgOriginalSize = size;
        _photoImgOriginalSizeCached = true;
    }

    private void ApplyCoverSizeByOriginal(int texWidth, int texHeight)
    {
        if (PhotoImg == null)
        {
            return;
        }

        if (texWidth <= 0 || texHeight <= 0)
        {
            return;
        }

        CacheOriginalSize();
        if (!_photoImgOriginalSizeCached)
        {
            return;
        }

        CameraImgDataUtils.TryApplyImageCover(PhotoImg, texWidth, texHeight, _photoImgOriginalSize, center: false);
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
                return tex;
            }
        }

        return null;
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

    private IEnumerator CoLoadNetworkTexture(CameraImagePack pack, string url, bool selected, int token)
    {
        while (!AlbumThumbnailNetworkThrottle.TryBeginDownload())
        {
            yield return null;
        }

        try
        {
            var expectedName = pack != null ? pack.name : null;

            var requestUrl = NormalizeTextureUrl(url);
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
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(request);
            if (tex == null)
            {
                Debug.LogWarning($"[CameraImgDataUtilsTester] 网络图片解析失败: name={pack.name}, url={url}");
                yield break;
            }

            if (token != _loadGen)
            {
                Destroy(tex);
                yield break;
            }

            if (_data == null || _data.name != expectedName)
            {
                Destroy(tex);
                yield break;
            }

            ClearRuntimeImage();

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
                yield break;
            }

            _runtimeSprite = Sprite.Create(
                _runtimeTextureOwned,
                new Rect(0, 0, _runtimeTextureOwned.width, _runtimeTextureOwned.height),
                new Vector2(0.5f, 0.5f));
            PhotoImg.sprite = _runtimeSprite;
            ApplyCoverSizeByOriginal(_runtimeTextureOwned.width, _runtimeTextureOwned.height);
            ApplySelectVisual(selected);
        }
        finally
        {
            AlbumThumbnailNetworkThrottle.EndDownload();
        }
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
}

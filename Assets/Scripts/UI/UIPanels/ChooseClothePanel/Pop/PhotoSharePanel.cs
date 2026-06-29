using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.IO;
using Game;
using GameData;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class PhotoSharePanel : BasePanel<PhotoSharePanel>
{
    private const string ShareDialogTitle = "碧优蒂的世界";

    [SerializeField] private CButton closeBtn;
    [SerializeField] private CButton SaveBtn;
    [SerializeField] private CButton RelayBtn;
    [SerializeField] private Image PhotoImage;
    [SerializeField] private GameObject LandMarkRoot;
    private CameraImagePack _pack;

    private Sprite _runtimeSprite;

    // PhotoImage 按“Cover”显示：以可视框（PhotoImage 的父节点 Rect）为基准，不留白允许裁切
    private Vector2 _photoBoxSize;
    private bool _photoBoxSizeCached;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(CloseSelf);
        SaveBtn.onClick.AddListener(SaveClick);
        RelayBtn.onClick.AddListener(RelayClick);
    }

    private void SaveClick()
    {
        SaveCurrentPhotoToAlbum();
    }

    private void RelayClick()
    {
        ShareToPlatform("");
    }

    private void SaveCurrentPhotoToAlbum()
    {

        var baseTex = TryCreateReadableExportBaseTexture();
        if (baseTex == null)
        {
            TipPanel.ShowToast("图片加载中或加载失败，请稍后再试");
            return;
        }

        Texture2D outTex = null;
        try
        {

            if (outTex == null)
            {
                outTex = baseTex;
            }

            byte[] pngBytes = null;
            try
            {
                pngBytes = outTex.EncodeToPNG();
            }
            catch
            {
                // 理论上 outTex 已经是 readable，这里兜底一下
                var readableCopy = CreateReadableCopy(outTex);
                if (readableCopy != null)
                {
                    pngBytes = readableCopy.EncodeToPNG();
                    if (readableCopy != outTex) Destroy(readableCopy);
                }
            }

            if (pngBytes == null || pngBytes.Length == 0)
            {
                TipPanel.ShowToast("保存失败，请重试");
                return;
            }

            var uid = AccountDataManager.Inst.Uid;
            uid = string.IsNullOrEmpty(uid) ? "shotTemplate" : uid;
            string filePath = LocalDataUtils.Inst.SaveTempImgRes(uid, pngBytes);
            LoggerUtils.Log($"[PhotoSharePanel] 保存照片路径: {filePath}");
            var data = new SaveMediaParams
            {
                mediaType = 1,
                mediaUrl = filePath
            };
            MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
            TipPanel.ShowToast("照片已保存");
        }
        finally
        {
            if (outTex != null && outTex != baseTex) Destroy(outTex);
            if (baseTex != null) Destroy(baseTex);
        }
    }

    /// <summary>
    /// 使用多平台分享组件（SunShineNativeShare）调起系统分享，用户可选择 QQ、微信等。
    /// </summary>
    private void ShareToPlatform(string shareMessage)
    {
        if (_pack == null)
        {
            TipPanel.ShowToast("图片数据异常，无法分享");
            return;
        }

#if !UNITY_EDITOR
        if (SunShineNativeShare.instance == null)
        {
            TipPanel.ShowToast("分享功能暂不可用");
            return;
        }
#endif

        var path = GetShareImagePath();
        if (string.IsNullOrEmpty(path))
        {
            TipPanel.ShowToast("图片加载中或加载失败，请稍后再试");
            return;
        }
        EventTracking.LoadEvent.ReportTask(178, 0);
#if UNITY_EDITOR
        TipPanel.ShowToast($"分享（真机可用）: {shareMessage}");
        return;
#else
        SunShineNativeShare.instance.ShareSingleFile(path, SunShineNativeShare.TYPE_IMAGE, shareMessage, ShareDialogTitle);
#endif
    }

    /// <summary>
    /// 获取当前照片的临时文件路径，用于分享：
    /// - VIP：不叠加水印（与 <see cref="IsWatermarkExemptVipUser"/> 一致，含 subscribeData 兜底）
    /// - 非 VIP：叠加图片/昵称/时间/头像等水印（与「含水印保存」同一套合成管线）
    /// </summary>
    private string GetShareImagePath()
    {
        var baseTex = TryCreateReadableExportBaseTexture();
        if (baseTex == null) return null;

        Texture2D outTex = null;
        try
        {

            if (outTex == null)
            {
                outTex = baseTex;
            }

            byte[] pngBytes = null;
            try
            {
                pngBytes = outTex.EncodeToPNG();
            }
            catch
            {
                var readableCopy = CreateReadableCopy(outTex);
                if (readableCopy != null)
                {
                    pngBytes = readableCopy.EncodeToPNG();
                    if (readableCopy != outTex) Destroy(readableCopy);
                }
            }

            if (pngBytes == null || pngBytes.Length == 0) return null;

            string fileName = string.IsNullOrEmpty(_pack?.name) ? "photo_share.png" : _pack.name + "_share.png";
            if (!fileName.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) fileName += ".png";
            string path = System.IO.Path.Combine(Application.persistentDataPath, fileName);
            System.IO.File.WriteAllBytes(path, pngBytes);
            LoggerUtils.Log($"[PhotoSharePanel] 分享图片路径: {path}");
            return path;
        }
        catch
        {
            return null;
        }
        finally
        {
            if (outTex != null && outTex != baseTex) Destroy(outTex);
            if (baseTex != null) Destroy(baseTex);
        }
    }

    /// <summary>
    /// 导出/分享用底图：优先从相册数据包加载原始像素，与界面预览解耦，避免输出尺寸与 VIP 无水印保存不一致；
    /// 水印合成在「原图分辨率」上进行，避免仅按预览 Sprite 尺寸导出导致「加水印后分辨率变了」的观感。
    /// </summary>
    private Texture2D TryCreateReadableExportBaseTexture()
    {
        if (_pack != null)
        {
            var tex = TryLoadTextureWithFallback(_pack, out _);
            if (tex != null)
            {
                if (tex.isReadable)
                {
                    var dup = DuplicateTexturePixels(tex);
                    if (dup != null) return dup;
                }

                var copy = CreateReadableCopy(tex);
                if (copy != null) return copy;
            }
        }

        var photoSprite = PhotoImage != null ? PhotoImage.sprite : null;
        if (photoSprite == null || photoSprite.texture == null) return null;
        return ExtractReadableSpriteTexture(photoSprite);
    }

    private static Texture2D DuplicateTexturePixels(Texture2D tex)
    {
        if (tex == null) return null;
        try
        {
            var pixels = tex.GetPixels32();
            var dup = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
            dup.SetPixels32(pixels);
            dup.Apply(false, false);
            return dup;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 将纹理复制为可读的 Texture2D（通过 RenderTexture 读取像素），避免源纹理 isReadable=false 时 EncodeToPNG 报错。
    /// </summary>
    private static Texture2D CreateReadableCopy(Texture2D source)
    {
        if (source == null) return null;
        int w = source.width;
        int h = source.height;
        if (w <= 0 || h <= 0) return null;

        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        try
        {
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);
            Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readable.Apply();
            RenderTexture.active = prev;
            return readable;
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    /// <summary>
    /// 从 Sprite 提取一个“可读”的 Texture2D（只包含 sprite 的 textureRect 区域，避免图集整张大图参与合成）。
    /// </summary>
    private static Texture2D ExtractReadableSpriteTexture(Sprite sp)
    {
        if (sp == null || sp.texture == null) return null;
        var tex = sp.texture as Texture2D;
        if (tex == null) return null;

        var readableAtlas = CreateReadableCopy(tex);
        if (readableAtlas == null) return null;

        try
        {
            var r = sp.textureRect;
            int x = Mathf.Clamp(Mathf.RoundToInt(r.x), 0, readableAtlas.width - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(r.y), 0, readableAtlas.height - 1);
            int w = Mathf.Clamp(Mathf.RoundToInt(r.width), 1, readableAtlas.width - x);
            int h = Mathf.Clamp(Mathf.RoundToInt(r.height), 1, readableAtlas.height - y);

            var pixels = readableAtlas.GetPixels(x, y, w, h);
            var cropped = new Texture2D(w, h, TextureFormat.RGBA32, false);
            cropped.SetPixels(pixels);
            cropped.Apply(false, false);
            return cropped;
        }
        finally
        {
            Destroy(readableAtlas);
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        _pack = args[0] as CameraImagePack;
        LoadPhoto();
        LandMarkRoot.SetActive(_pack.isLandMark);
    }

    private void LoadPhoto()
    {
        var _runtimeTexture = TryLoadTextureWithFallback(_pack, out var usedUploaderKey);
        if(_runtimeTexture == null)
        {
            if (!string.IsNullOrEmpty(_pack.url))
            {
                StartCoroutine(CoLoadNetworkTexture(_pack, usedUploaderKey));
                return;
            }
            return;
        }

        if(_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }
        _runtimeSprite = Sprite.Create(_runtimeTexture, new Rect(0, 0, _runtimeTexture.width, _runtimeTexture.height), new Vector2(0.5f, 0.5f));
        PhotoImage.sprite = _runtimeSprite;
        ApplyPhotoImageCover(_runtimeTexture.width, _runtimeTexture.height);
    }

    private Texture2D TryLoadTextureWithFallback(CameraImagePack pack, out string usedUploaderKey)
    {
        usedUploaderKey = pack.uploader;
        var tex = CameraImgDataUtils.Inst.LoadTextureLocalOriginal(pack.name, pack.uploader);
        if (tex != null )
        {
            return tex;
        }

        var uploaderFromName = ExtractUploaderFromName(pack.name);
        if (!string.IsNullOrEmpty(uploaderFromName) && uploaderFromName != pack.uploader)
        {
            tex = CameraImgDataUtils.Inst.LoadTextureLocalOriginal(pack.name, uploaderFromName);
            if (tex != null)
            {
                usedUploaderKey = uploaderFromName;
                Debug.LogWarning($"[CameraImgDataUtilsTester] 使用 name 解析的 uploader 回退成功: {uploaderFromName}");
                return tex;
            }
        }

        if (pack.uploader != "shotTemplate")
        {
            tex = CameraImgDataUtils.Inst.LoadTextureLocalOriginal(pack.name, "shotTemplate");
            if (tex != null)
            {
                usedUploaderKey = "shotTemplate";
                Debug.LogWarning("[CameraImgDataUtilsTester] 使用 shotTemplate uploader 回退成功。");
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

    private System.Collections.IEnumerator CoLoadNetworkTexture(CameraImagePack pack, string usedUploaderKey)
    {
        using var request = UnityWebRequestTexture.GetTexture(pack.url);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[CameraImgDataUtilsTester] 本地与网络都加载失败: name={pack.name}, hasLocal={pack.hasLocal}, " +
                $"url={pack.url}, err={request.error}"
            );
            yield break;
        }

        var tex = DownloadHandlerTexture.GetContent(request);
        if (tex == null)
        {
            Debug.LogWarning($"[CameraImgDataUtilsTester] 网络图片解析失败: name={pack.name}, url={pack.url}");
            yield break;
        }

        if(_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }
        _runtimeSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        PhotoImage.sprite = _runtimeSprite;
        ApplyPhotoImageCover(tex.width, tex.height);

    }

    private RectTransform GetPhotoBoxRect()
    {
        if (PhotoImage == null) return null;
        var rt = PhotoImage.rectTransform;
        if (rt == null) return null;
        return rt.parent as RectTransform ?? rt;
    }

    private void CachePhotoBoxSize()
    {
        if (_photoBoxSizeCached) return;
        var boxRt = GetPhotoBoxRect();
        if (boxRt == null) return;

        var s = boxRt.sizeDelta;
        if (s.x <= 0f || s.y <= 0f)
        {
            s = boxRt.rect.size;
        }
        if (s.x <= 0f || s.y <= 0f) return;

        _photoBoxSize = s;
        _photoBoxSizeCached = true;
    }

    private void ApplyPhotoImageCover(int texWidth, int texHeight)
    {
        if (PhotoImage == null) return;
        if (texWidth <= 0 || texHeight <= 0) return;

        CachePhotoBoxSize();
        if (!_photoBoxSizeCached) return;

        PhotoImage.preserveAspect = true;
        // 保持旧行为：使用缓存 boxSize，且不强制居中（不改 anchoredPosition）
        CameraImgDataUtils.TryApplyImageCover(PhotoImage, texWidth, texHeight, _photoBoxSize, center: false);
    }
}

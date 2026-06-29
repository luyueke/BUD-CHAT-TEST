using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// 开发期测试脚本：
/// - 按 H 键循环读取 CameraImgDataUtils 中的图片
/// - 把当前图片展示到 Image
/// - 把图片信息转成 JSON 打印到日志
/// </summary>
public class CameraImgDataUtilsTester : MonoBehaviour
{
    #if UNITY_EDITOR
    [Header("UI")]
    [SerializeField] private Image previewImage;

    [Header("Input")]
    [SerializeField] private KeyCode nextKey = KeyCode.H;

    [Header("Debug")]
    [SerializeField] private bool printJsonInfo = true;
    [SerializeField] private bool enableUploaderFallback = true;

    private readonly List<CameraImagePack> _cachedPacks = new List<CameraImagePack>();
    private int _currentIndex = -1;
    private Texture2D _runtimeTexture;
    private Texture2D _downloadedTexture;
    private Sprite _runtimeSprite;

    private void Update()
    {
        if (Input.GetKeyDown(nextKey))
        {
            ShowNextImage();
        }
    }

    [ContextMenu("Show Next Image")]
    public void ShowNextImage()
    {
        ReloadList();
        if (_cachedPacks.Count == 0)
        {
            Debug.LogWarning("[CameraImgDataUtilsTester] 当前没有可用图片。");
            return;
        }

        _currentIndex = (_currentIndex + 1) % _cachedPacks.Count;
        var pack = _cachedPacks[_currentIndex];

        // GetAll 返回的是元数据，显示时需要按 name 额外加载纹理。
        _runtimeTexture = TryLoadTextureWithFallback(pack, out var usedUploaderKey);
        if (_runtimeTexture == null)
        {
            if (!string.IsNullOrEmpty(pack.url))
            {
                StartCoroutine(CoLoadNetworkTexture(pack, usedUploaderKey));
                return;
            }

            var expectedPath = GetExpectedLocalPath(pack.name, pack.uploader);
            Debug.LogWarning(
                $"[CameraImgDataUtilsTester] 纹理加载失败: name={pack.name}, hasLocal={pack.hasLocal}, " +
                $"uploader(meta)={pack.uploader}, uploader(fromName)={ExtractUploaderFromName(pack.name)}, " +
                $"expectedPath={expectedPath}, fileExists={File.Exists(expectedPath)}"
            );
            return;
        }

        if (previewImage != null)
        {
            if (_runtimeSprite != null)
            {
                Destroy(_runtimeSprite);
                _runtimeSprite = null;
            }

            _runtimeSprite = Sprite.Create(
                _runtimeTexture,
                new Rect(0, 0, _runtimeTexture.width, _runtimeTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            previewImage.sprite = _runtimeSprite;
        }
        else
        {
            Debug.LogWarning("[CameraImgDataUtilsTester] 未绑定 Image，已跳过显示。");
        }

        if (printJsonInfo)
        {
            var info = new CameraImageDebugInfo
            {
                name = pack.name,
                size = pack.size,
                width = pack.width,
                height = pack.height,
                isCloud = pack.isCloud,
                url = pack.url,
                uploader = pack.uploader,
                saveTime = pack.saveTime,
                uploadTime = pack.uploadTime,
                hasLocal = pack.hasLocal,
                usedUploader = usedUploaderKey,
                textureWidth = _runtimeTexture.width,
                textureHeight = _runtimeTexture.height
            };

            Debug.Log($"[CameraImgDataUtilsTester] 当前图片信息:\n{JsonConvert.SerializeObject(pack, Formatting.Indented)}");
        }
    }

    [ContextMenu("Reload List")]
    public void ReloadList()
    {
        _cachedPacks.Clear();
        var list = CameraImgDataUtils.Inst.GetAll();
        if (list != null && list.Count > 0)
        {
            // 只保留“可显示”的记录：有本地文件或有可用网络地址。
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (item == null) continue;

                var hasRemote = !string.IsNullOrEmpty(item.url);
                if (item.hasLocal || hasRemote)
                {
                    _cachedPacks.Add(item);
                }
            }
        }

        Debug.Log($"[CameraImgDataUtilsTester] ReloadList 完成: all={list?.Count ?? 0}, displayable={_cachedPacks.Count}");
    }

    private void OnDestroy()
    {
        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }

        if (_downloadedTexture != null)
        {
            Destroy(_downloadedTexture);
            _downloadedTexture = null;
        }
    }

    [System.Serializable]
    private class CameraImageDebugInfo
    {
        public string name;
        public float size;
        public float width;
        public float height;
        public bool isCloud;
        public string url;
        public string uploader;
        public long saveTime;
        public long uploadTime;
        public bool hasLocal;
        public string usedUploader;
        public int textureWidth;
        public int textureHeight;
    }

    private Texture2D TryLoadTextureWithFallback(CameraImagePack pack, out string usedUploaderKey)
    {
        usedUploaderKey = pack.uploader;
        var tex = CameraImgDataUtils.Inst.LoadTextureLocal(pack.name, pack.uploader);
        if (tex != null || !enableUploaderFallback)
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

    private static string GetExpectedLocalPath(string imageName, string uploader)
    {
        uploader = string.IsNullOrEmpty(uploader) ? "shotTemplate" : uploader;
        return Path.Combine(Application.persistentDataPath, "U3D", "CameraAlbum", uploader, imageName + ".budimg");
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

        if (_downloadedTexture != null)
        {
            Destroy(_downloadedTexture);
        }
        _downloadedTexture = tex;
        _runtimeTexture = tex;

        if (previewImage != null)
        {
            if (_runtimeSprite != null)
            {
                Destroy(_runtimeSprite);
                _runtimeSprite = null;
            }

            _runtimeSprite = Sprite.Create(
                _runtimeTexture,
                new Rect(0, 0, _runtimeTexture.width, _runtimeTexture.height),
                new Vector2(0.5f, 0.5f)
            );
            previewImage.sprite = _runtimeSprite;
        }

        if (printJsonInfo)
        {
            var info = new CameraImageDebugInfo
            {
                name = pack.name,
                size = pack.size,
                width = pack.width,
                height = pack.height,
                isCloud = pack.isCloud,
                url = pack.url,
                uploader = pack.uploader,
                saveTime = pack.saveTime,
                uploadTime = pack.uploadTime,
                hasLocal = pack.hasLocal,
                usedUploader = usedUploaderKey,
                textureWidth = _runtimeTexture.width,
                textureHeight = _runtimeTexture.height
            };
            Debug.Log($"[CameraImgDataUtilsTester] 网络回退加载成功:\n{JsonConvert.SerializeObject(info, Formatting.Indented)}");
        }
    }
    #endif
}

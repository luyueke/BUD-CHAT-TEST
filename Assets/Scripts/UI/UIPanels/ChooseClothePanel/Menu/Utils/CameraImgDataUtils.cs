using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Basic.Utils;
using Game.Config;
using Game.COSXML;
using GameData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 相机图片数据中间件：
/// - 本地保存：对图片字节加密后再写入沙盒（通过 LocalDataUtils 保存）
/// - 本地读取：仅提供解密后的原始字节（用于上传等），不提供可直接“观看”的 Texture/图片文件逻辑
/// - 云端状态：维护 url / uploadTime / hasLocal 等元数据
/// 
/// 存储结构：
/// - 加密文件：{persistent}/U3D/CameraAlbum/{name}.cimg
/// - 索引文件：{persistent}/U3D/CameraAlbum/index.json
/// </summary>
public sealed class CameraImgDataUtils : GlobalInstance<CameraImgDataUtils>
{
    private const string AlbumDirName = "CameraAlbum";
    private const string VideoDirName = "Video";
    private const string CloudCacheDirName = "CloudCache";
    private const string IndexFileName = "index.json";
    private const string EncryptedExt = ".budimg";
    private const string ThumbnailEncryptedExt = ".budthumb";
    private const int LocalThumbMaxLongSide = 512;
    private const int LocalThumbQuality = 65;
    /// <summary>相册同步到 COS 的 cover 图：长边上限（与本地缩略图策略一致，偏小以省流量）。</summary>
    private const int AlbumCosCoverMaxLongSide = 512;
    /// <summary>相册 cover JPEG 质量 1-100。</summary>
    private const int AlbumCosCoverQuality = 70;
    /// <summary>相册 coverFull（大图）JPEG 长边上限，约 1920。</summary>
    private const int AlbumCosCoverFullMaxLongSide = 1920;
    /// <summary>相册 coverFull（大图）JPEG 质量。</summary>
    private const int AlbumCosCoverFullQuality = 85;

    // 与 LocalDataUtils 一致的根目录
    private static readonly string BaseDir = Application.persistentDataPath + "/U3D/";
    private static readonly string AlbumRootDir = Path.Combine(BaseDir, AlbumDirName);
    private string _loadedUid = "";

    private readonly object _lock = new object();
    private CameraAlbumIndex _index;
    // O(1) 按 name 查找的辅助索引，与 _index.items 严格同步，消除高频路径上的 O(n) 线性扫描
    private readonly Dictionary<string, CameraImageMeta> _metaByName = new Dictionary<string, CameraImageMeta>();

    // 运行时 Texture 缓存（用于相册/预览等）。
    // 注意：Texture2D 属于 Unity 对象，默认假设这些 API 在主线程调用。
    private const int TextureCacheCapacity = 32;
    private readonly Dictionary<string, Texture2D> _textureCache = new Dictionary<string, Texture2D>();
    private readonly LinkedList<string> _textureCacheLru = new LinkedList<string>();

    // SaveIndex 防抖：同帧内多次修改只触发一次写盘；Application.quitting 兜底刷新
    private bool _indexDirty;
    private int _lastSaveFrame = int.MinValue;

    // 本地视频包扫描结果缓存，仅在保存/删除视频或切换 uid 时失效
    private List<CameraImagePack> _videoPacksCache;
    private string _videoPacksCachedUid;

    public override void Initialize()
    {
        base.Initialize();
        EnsureIndexForUid(GetCurrentUid());
        CleanupIndex();
        Application.quitting += FlushDirtyIndexOnQuit;
    }

    #region Public API

    /// <summary>
    /// 保存拍照图片（写入加密原图 + 加密缩略图 + 索引元数据）。
    /// 注意：此处不生成任何可直接“观看”的明文图片文件。
    /// </summary>
    public CameraImagePack SaveCaptured(byte[] imgBytes, float width, float height, string uploaderUid, string locationName = null, List<atListItem> atList = null, string mapId = null, bool isLandMark = false)
    {
        if (imgBytes == null || imgBytes.Length == 0) throw new ArgumentException("imgBytes empty");
        uploaderUid = string.IsNullOrEmpty(uploaderUid) ? "shotTemplate" : uploaderUid;
        EnsureIndexForUid(uploaderUid);

        var now = GameUtils.GetTimeStamp();
        var name = $"{uploaderUid}_{now}_{Guid.NewGuid():N}";

        var meta = new CameraImageMeta
        {
            name = name,
            albumId = null,
            size = imgBytes.Length,
            width = width,
            height = height,
            isCloud = false,
            url = null,
            uploader = uploaderUid,
            saveTime = now,
            uploadTime = 0,
            hasLocal = false,
            isPublic = false,
            locationName = locationName,
            mapId = mapId,
            isLandMark = isLandMark,
            atList = CloneAtList(atList)
        };

        var encrypted = Encrypt(imgBytes, uploaderUid);
        var encryptedFileName = $"{AlbumDirName}/{uploaderUid}/{name}{EncryptedExt}";
        EnsureDirs(uploaderUid);
        // 通过 LocalDataUtils 写入（满足“加密后再给 LocalDataUtils 储存”）
        var encryptedFullPath = LocalDataUtils.SaveDataToSandbox(encrypted, encryptedFileName);
        meta.hasLocal = !string.IsNullOrEmpty(encryptedFullPath) && File.Exists(encryptedFullPath);

        // 额外保存一份本地缩略图（加密后存储），用于列表快速展示与降低内存占用。
        // 缩略图生成失败不影响原图保存，读取时会自动回退到原图。
        try
        {
            var thumbBytes = CompressImage(imgBytes, LocalThumbMaxLongSide, LocalThumbQuality);
            if (thumbBytes != null && thumbBytes.Length > 0)
            {
                var encryptedThumb = Encrypt(thumbBytes, uploaderUid);
                var thumbFileName = $"{AlbumDirName}/{uploaderUid}/{name}{ThumbnailEncryptedExt}";
                LocalDataUtils.SaveDataToSandbox(encryptedThumb, thumbFileName);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CameraImgDataUtils] Save thumbnail failed: {e.Message}");
        }

        lock (_lock)
        {
            UpsertMeta(meta);
            SaveIndex_NoLock();
        }

        // 按需：保存后如果你希望立刻能显示，可在相册页再调用 LoadTextureLocal(name)
        return BuildPack(meta);
    }

    /// <summary>
    /// 获取所有图片（按 saveTime 倒序）。
    /// 注意：不提供 texture 解密填充（避免直接“观看”）。
    /// </summary>
    public List<CameraImagePack> GetAll()
    {
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            var list = _index.items
                .OrderByDescending(i => i.saveTime)
                .Select(i =>
                {
                    return BuildPack(i);
                })
                .ToList();
            return list;
        }
    }

    /// <summary>
    /// 回调方式获取“本地 + 远端”合并后的相册列表：
    /// 1) 先收集本地照片和本地视频，并立即回调一次（首屏快速展示）
    /// 2) 再请求远端相册数据，合并后再次回调（刷新为完整列表）
    ///
    /// type: -1=全部, 0=照片, 1=视频
    /// 约定：
    /// - 视频项 url 为视频地址（本地路径或远端视频地址）
    /// - 视频缩略图放在 previewUrl
    /// </summary>
    public void GetAllMerged(string uid, int type, Action<List<CameraImagePack>> onComplete, Action<string> onError = null)
    {
        EnsureIndexForUid(uid);
        var localList = GetLocalMediaPacks(type, uid);
        var merged = new List<CameraImagePack>(localList.Count);
        var keyToPack = new Dictionary<string, CameraImagePack>();
        var localAlbumIds = new HashSet<string>();

        foreach (var item in localList)
        {
            if (item == null) continue;
            if (!string.IsNullOrEmpty(item.albumId))
            {
                localAlbumIds.Add(item.albumId);
            }
            var key = BuildDedupKey(item);
            if (keyToPack.TryGetValue(key, out var existed))
            {
                MergePack(existed, item);
                continue;
            }

            keyToPack[key] = item;
            merged.Add(item);
        }

        // 优化：先立即用本地列表回调一次，相册可马上展示，减少等待感
        var localOnlySorted = merged.OrderByDescending(i => i.saveTime).ToList();
        onComplete?.Invoke(localOnlySorted);

        GetRemoteMediaPacks(uid, type, remoteList =>
        {
            if (remoteList != null && remoteList.Count > 0)
            {
                for (int i = 0; i < remoteList.Count; i++)
                {
                    var remote = remoteList[i];
                    if (remote == null) continue;
                    // 兜底：服务端可能忽略/不严格按 type 过滤，这里客户端再做一次过滤，避免“切到视频仍混入照片”等问题。
                    if (type != -1 && remote.mediaType != type) continue;
                    if (!string.IsNullOrEmpty(remote.albumId) && localAlbumIds.Contains(remote.albumId))
                    {
                        continue;
                    }

                    var key = BuildDedupKey(remote);
                    if (keyToPack.ContainsKey(key)) continue;
                    keyToPack[key] = remote;
                    merged.Add(remote);

                    if (!string.IsNullOrEmpty(remote.albumId))
                    {
                        localAlbumIds.Add(remote.albumId);
                    }
                }
            }
            

            onComplete?.Invoke(merged
                .OrderByDescending(i => i.saveTime)
                .ToList());
        }, error =>
        {
            // 远端失败时降级：仍返回本地结果，避免主流程阻塞。
            Debug.LogWarning($"[CameraImgDataUtils] 获取远端相册失败，已降级返回本地数据: {error}");
            onError?.Invoke(error);
            onComplete?.Invoke(merged
                .OrderByDescending(i => i.saveTime)
                .ToList());
        });
    }

    /// <summary>
    /// 获取单张图片（会尝试解密并填充 texture，供游戏内直接显示）。
    /// </summary>
    public CameraImagePack Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;

        // 先在锁内取出元数据快照，避免在锁内做 IO/解密/创建纹理
        CameraImageMeta meta;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out meta)) return null;
        }

        var pack = BuildPack(meta);
        if (pack.hasLocal)
        {
            pack.texture = LoadTextureLocal(name, meta.uploader);
        }
        return pack;
    }

    /// <summary>
    /// 更新云端信息（上传成功后调用）。
    /// </summary>
    public void MarkUploaded(string name, string url, long uploadTime, string uploaderUid)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta)) return;

            meta.isCloud = true;
            meta.url = url;
            meta.uploadTime = uploadTime <= 0 ? GameUtils.GetTimeStamp() : uploadTime;
            if (!string.IsNullOrEmpty(uploaderUid)) meta.uploader = uploaderUid;
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 标记为“仅云端，无本地”（例如本地被清理/用户主动删除本地）。
    /// </summary>
    public void MarkCloudOnly(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta)) return;
            meta.hasLocal = false;
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 更新本地索引中的服务端相册 id 并保存到文件。
    /// </summary>
    public void SetAlbumId(string name, string albumId)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta)) return;
            meta.albumId = albumId;
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 更新本地索引中的公开状态并保存到文件。
    /// </summary>
    public void SetPublicState(string name, bool isPublic)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta)) return;
            meta.isPublic = isPublic;
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 更新本地索引中的打卡点名称并保存到文件。
    /// </summary>
    public void SetLocationName(string name, string locationName)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta)) return;
            meta.locationName = locationName;
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 更新本地索引中的地图 id 并保存到文件。
    /// </summary>
    public void SetMapId(string name, string mapId)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta)) return;
            meta.mapId = mapId;
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 更新本地索引中的 @列表 并保存到文件。
    /// </summary>
    public void SetAtList(string name, List<atListItem> atList)
    {
        if (string.IsNullOrEmpty(name)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta)) return;
            meta.atList = CloneAtList(atList);
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 将 CameraImagePack 转成服务端更新接口需要的 CameraAlbumInfo。
    /// - albumId 优先取 pack.albumId，其次在云端数据场景下回退 pack.name
    /// - 本地未上传数据（无有效 id）返回 null
    /// </summary>
    public CameraAlbumInfo BuildAlbumInfoForUpdate(CameraImagePack pack)
    {
        if (pack == null) return null;

        var id = !string.IsNullOrEmpty(pack.albumId)
            ? pack.albumId
            : (pack.isCloud ? pack.name : null);
        
        var type = pack.mediaType == 1 ? 1 : 0;
        var cover = !string.IsNullOrEmpty(pack.previewUrl) ? pack.previewUrl : pack.url;
        var coverFull = type == 1
            ? (!string.IsNullOrEmpty(pack.mediaUrl) ? pack.mediaUrl : pack.url)
            : (!string.IsNullOrEmpty(pack.url) ? pack.url : pack.previewUrl);
        var cameraAlbumInfo = new CameraAlbumInfo
        {
            id = id,
            cover = cover,
            coverFull = coverFull,
            creator = pack.uploader,
            isDelete = 0,
            type = type,
            isBan = 0,
            isPublic = pack.isPublic ? 1 : 0,
            locationInfo = new locationInfo
            {
                mapId = !string.IsNullOrEmpty(pack.mapId)
                    ? pack.mapId
                    : (pack.locationName ?? string.Empty),
                locationName = pack.locationName,
                isLandMark = false
            },
            atList = CloneAtList(pack.atList),
            createTime = (int)Math.Max(0, pack.saveTime)
        };
        return cameraAlbumInfo;
    }

    /// <summary>
    /// 将服务器相册结构（AlbumPhotoInfo/CameraAlbumInfo）转换成通用的 CameraImagePack，供大图预览/分享/列表复用。
    /// 约定：
    /// - 照片：url 优先 coverFull，其次 cover；previewUrl 同 url（用于缩略图/预览一致）
    /// - 视频：previewUrl 使用 cover；url/mediaUrl 使用 coverFull（若为空则回退 cover）
    /// </summary>
    public static CameraImagePack BuildCameraImagePackFromAlbumPhotoInfo(AlbumPhotoInfo info)
    {
        var albumItem = info?.albumItem;
        if (albumItem == null) return null;

        var mediaType = albumItem.type == 1 ? 1 : 0;

        var preview = mediaType == 1
            ? albumItem.cover
            : (!string.IsNullOrEmpty(albumItem.coverFull) ? albumItem.coverFull : albumItem.cover);

        var mediaUrl = mediaType == 1
            ? (!string.IsNullOrEmpty(albumItem.coverFull) ? albumItem.coverFull : albumItem.cover)
            : null;

        var url = mediaType == 1
            ? mediaUrl
            : (!string.IsNullOrEmpty(albumItem.coverFull) ? albumItem.coverFull : albumItem.cover);

        return new CameraImagePack
        {
            name = !string.IsNullOrEmpty(albumItem.id) ? albumItem.id : $"remote_{albumItem.creator}_{albumItem.createTime}_{mediaType}",
            albumId = albumItem.id,
            size = 0,
            width = 0,
            height = 0,
            isCloud = true,
            url = url,
            texture = null,
            uploader = albumItem.creator,
            saveTime = albumItem.createTime,
            uploadTime = albumItem.createTime,
            hasLocal = false,
            isPublic = albumItem.isPublic == 1,
            mediaType = mediaType,
            mediaUrl = mediaUrl,
            previewUrl = preview,
            locationName = albumItem.locationInfo?.locationName,
            mapId = albumItem.locationInfo?.mapId,
            atList = albumItem.atList
        };
    }


    public static bool TryGetImageCoverBoxSize(Image img, out Vector2 boxSize)
    {
        boxSize = default;
        if (img == null) return false;

        var rt = img.rectTransform;
        if (rt == null) return false;

        var boxRt = rt.parent as RectTransform;
        var s = boxRt != null ? boxRt.rect.size : rt.rect.size;
        if (s.x <= 0f || s.y <= 0f)
        {
            s = boxRt != null ? boxRt.sizeDelta : rt.sizeDelta;
        }

        if (s.x <= 0f || s.y <= 0f) return false;
        boxSize = s;
        return true;
    }

    public static bool TryGetRawImageCoverBoxSize(RawImage raw, out Vector2 boxSize)
    {
        boxSize = default;
        if (raw == null) return false;

        var rt = raw.rectTransform;
        if (rt == null) return false;

        var boxRt = rt.parent as RectTransform;
        var s = boxRt != null ? boxRt.rect.size : rt.rect.size;
        if (s.x <= 0f || s.y <= 0f)
        {
            s = boxRt != null ? boxRt.sizeDelta : rt.sizeDelta;
        }

        if (s.x <= 0f || s.y <= 0f) return false;
        boxSize = s;
        return true;
    }

    /// <summary>
    /// 将 Image 按纹理宽高比 Cover 到指定 boxSize（不留白，允许裁剪）。
    /// </summary>
    public static bool TryApplyImageCover(Image img, int texWidth, int texHeight, Vector2? boxSizeOverride = null, bool center = false)
    {
        if (img == null) return false;
        if (texWidth <= 0 || texHeight <= 0) return false;

        var rt = img.rectTransform;
        if (rt == null) return false;

        Vector2 box;
        if (boxSizeOverride.HasValue)
        {
            box = boxSizeOverride.Value;
        }
        else
        {
            if (!TryGetImageCoverBoxSize(img, out box)) return false;
        }

        if (box.x <= 0.0001f || box.y <= 0.0001f) return false;

        float scale = Mathf.Max(box.x / texWidth, box.y / texHeight);
        float targetW = texWidth * scale;
        float targetH = texHeight * scale;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetW);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetH);

        if (center)
        {
            rt.anchoredPosition = Vector2.zero;
        }

        return true;
    }

    /// <summary>
    /// 将 RawImage 按纹理宽高比 Cover 到指定 boxSize（不留白，允许裁剪）。
    /// </summary>
    public static bool TryApplyRawImageCover(RawImage raw, int texWidth, int texHeight, Vector2? boxSizeOverride = null, bool center = true)
    {
        if (raw == null) return false;
        if (texWidth <= 0 || texHeight <= 0) return false;

        var rt = raw.rectTransform;
        if (rt == null) return false;

        Vector2 box;
        if (boxSizeOverride.HasValue)
        {
            box = boxSizeOverride.Value;
        }
        else
        {
            if (!TryGetRawImageCoverBoxSize(raw, out box)) return false;
        }

        if (box.x <= 0.0001f || box.y <= 0.0001f) return false;

        float scale = Mathf.Max(box.x / texWidth, box.y / texHeight);
        float targetW = texWidth * scale;
        float targetH = texHeight * scale;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetW);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetH);

        if (center)
        {
            rt.anchoredPosition = Vector2.zero;
        }

        return true;
    }

    /// <summary>
    /// 仿照 UGC 封面图上传流程：将 pack.texture（或本地解密得到的 texture）上传到 COS，
    /// cover 使用压缩 JPEG（省流量）；照片 type=0 时 coverFull 单独上传原图 PNG，视频 coverFull 仍沿用外部逻辑。
    /// </summary>
    public void BuildAlbumInfoForUpdateWithCoverUpload(CameraImagePack pack, Action<CameraAlbumInfo> onComplete, Action<string> onError = null)
    {
        if (pack == null)
        {
            onError?.Invoke("pack is null");
            return;
        }

        // 先按旧逻辑构建基础字段（id/type/location/atList 等）
        var info = BuildAlbumInfoForUpdate(pack);
        if (info == null)
        {
            onError?.Invoke("BuildAlbumInfoForUpdate returned null");
            return;
        }

        // 如果已经是 http(s) 的 cover，直接返回（不重复上传）
        bool hasHttpCover = !string.IsNullOrEmpty(info.cover) &&
                            (info.cover.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                             info.cover.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        bool hasHttpCoverFull = !string.IsNullOrEmpty(info.coverFull) &&
                                (info.coverFull.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                                 info.coverFull.StartsWith("http://", StringComparison.OrdinalIgnoreCase));
        if (hasHttpCover && hasHttpCoverFull)
        {
            onComplete?.Invoke(info);
            return;
        }

        var needCoverUpload = !hasHttpCover;
        var needFullUpload = info.type == 0 && !hasHttpCoverFull;

        if (!needCoverUpload && !needFullUpload)
        {
            onComplete?.Invoke(info);
            return;
        }

        // 获取 texture（优先 pack.texture，否则从本地解密加载原图）
        var tex = pack.texture;
        if (tex == null && !string.IsNullOrEmpty(pack.name))
        {
            tex = LoadTextureLocalOriginal(pack.name, pack.uploader);
        }

        if (tex == null)
        {
            onError?.Invoke("cover texture is null");
            return;
        }

        var uid = AccountDataManager.Inst.Uid;
        var safeName = !string.IsNullOrEmpty(pack.name) ? pack.name : $"album_{GameUtils.GetTimeStamp()}";

        void invokeComplete()
        {
            onComplete?.Invoke(info);
        }

        void uploadFullJpgThen(Action then)
        {
            var fullLocalPath = BuildLocalAlbumCoverPath(pack, "_albumfull", ".jpg");
            try
            {
                EnsureDir(Path.GetDirectoryName(fullLocalPath));
                var jpgBytes = CompressTexture(tex, AlbumCosCoverFullMaxLongSide, AlbumCosCoverFullQuality);
                if (jpgBytes == null || jpgBytes.Length == 0)
                {
                    onError?.Invoke("CompressTexture failed for coverFull");
                    return;
                }

                File.WriteAllBytes(fullLocalPath, jpgBytes);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Write coverFull jpg failed: {e.Message}");
                return;
            }

            var remoteFull = $"{GlobalConfig.UPLOAD_PATH_ALBUM}{uid}/{safeName}_albumfull.jpg";
            CosXmlUploadManager.UploadFile(remoteFull, fullLocalPath, (fullUrl, fullErr) =>
            {
                if (!string.IsNullOrEmpty(fullErr))
                {
                    onError?.Invoke(fullErr);
                    return;
                }

                info.coverFull = NormalizeAlbumUploadCdnUrl(fullUrl);
                then?.Invoke();
            });
        }

        if (needCoverUpload)
        {
            var coverLocalPath = BuildLocalAlbumCoverPath(pack, "_albumcover", ".jpg");
            try
            {
                EnsureDir(Path.GetDirectoryName(coverLocalPath));
                var jpgBytes = CompressTexture(tex, AlbumCosCoverMaxLongSide, AlbumCosCoverQuality);
                if (jpgBytes == null || jpgBytes.Length == 0)
                {
                    onError?.Invoke("CompressTexture failed for cover");
                    return;
                }

                File.WriteAllBytes(coverLocalPath, jpgBytes);
            }
            catch (Exception e)
            {
                onError?.Invoke($"Write cover jpg failed: {e.Message}");
                return;
            }

            var remoteCover = $"{GlobalConfig.UPLOAD_PATH_ALBUM}{uid}/{safeName}_albumcover.jpg";
            CosXmlUploadManager.UploadFile(remoteCover, coverLocalPath, (url, err) =>
            {
                if (!string.IsNullOrEmpty(err))
                {
                    onError?.Invoke(err);
                    return;
                }

                info.cover = NormalizeAlbumUploadCdnUrl(url);

                if (info.type == 0)
                {
                    if (needFullUpload)
                    {
                        uploadFullJpgThen(invokeComplete);
                    }
                    else
                    {
                        invokeComplete();
                    }
                }
                else
                {
                    // 视频：coverFull 仍沿用外部逻辑（通常是视频地址），如果没有则也回填为压缩封面图
                    if (string.IsNullOrEmpty(info.coverFull) || !hasHttpCoverFull)
                    {
                        info.coverFull = info.cover;
                    }

                    invokeComplete();
                }
            });
        }
        else
        {
            uploadFullJpgThen(invokeComplete);
        }
    }

    public void BuildAlbumInfoForUpdateWithCoverUpload(List<CameraImagePack> packs, Action<List<CameraAlbumInfo>> onComplete, Action<string> onError = null)
    {
        if (packs == null)
        {
            onError?.Invoke("packs is null");
            return;
        }

        if (packs.Count == 0)
        {
            onComplete?.Invoke(new List<CameraAlbumInfo>());
            return;
        }

        var results = new CameraAlbumInfo[packs.Count];
        var remaining = packs.Count;
        var hasFailed = false;

        for (int i = 0; i < packs.Count; i++)
        {
            var idx = i;
            var pack = packs[idx];
            if (pack == null)
            {
                if (!hasFailed)
                {
                    hasFailed = true;
                    onError?.Invoke($"packs[{idx}] is null");
                }
                // 用 continue 而非 return：return 会使 remaining 永不归零，onComplete 永远无法被调用
                remaining--;
                continue;
            }

            BuildAlbumInfoForUpdateWithCoverUpload(pack,
                onComplete: info =>
                {
                    if (hasFailed) return;
                    results[idx] = info;
                    remaining--;
                    if (remaining <= 0)
                    {
                        onComplete?.Invoke(new List<CameraAlbumInfo>(results));
                    }
                },
                onError: err =>
                {
                    if (hasFailed) return;
                    hasFailed = true;
                    onError?.Invoke(err);
                });
        }
    }

    /// <summary>
    /// 将视频保存到应用私有沙盒目录（iOS 相册/Photos 无法访问的位置）。
    /// 返回保存后的完整路径；失败返回 null。
    /// </summary>
    /// <param name="sourceVideoPath">原始视频文件路径</param>
    /// <param name="uid">用户 uid</param>
    /// <param name="thumbnailBytes">可选的缩略图 PNG 字节，为 null 则不保存缩略图</param>
    public string SaveVideoToPrivate(string sourceVideoPath, string uid, byte[] thumbnailBytes = null)
    {
        if (string.IsNullOrEmpty(sourceVideoPath) || !File.Exists(sourceVideoPath))
        {
            Debug.LogWarning("[CameraImgDataUtils] SaveVideoToPrivate: source video not found");
            return null;
        }

        uid = NormalizeUid(uid);
        EnsureDirs(uid);
        var videoDir = GetUserVideoDir(uid);

        var fileName = Path.GetFileName(sourceVideoPath);
        if (string.IsNullOrEmpty(fileName))
            fileName = $"BUD_{GameUtils.GetTimeStamp()}.mp4";

        var destPath = Path.Combine(videoDir, fileName);

        if (File.Exists(destPath) && !string.Equals(Path.GetFullPath(sourceVideoPath), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
        {
            var nameNoExt = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            destPath = Path.Combine(videoDir, $"{nameNoExt}_{GameUtils.GetTimeStamp()}{ext}");
        }

        try
        {
            if (!string.Equals(Path.GetFullPath(sourceVideoPath), Path.GetFullPath(destPath), StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(sourceVideoPath, destPath, true);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[CameraImgDataUtils] SaveVideoToPrivate failed: {e.Message}");
            return null;
        }

        if (thumbnailBytes != null && thumbnailBytes.Length > 0)
        {
            try
            {
                var thumbName = Path.GetFileNameWithoutExtension(destPath) + "_thumb.png";
                var thumbPath = Path.Combine(videoDir, thumbName);
                File.WriteAllBytes(thumbPath, thumbnailBytes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CameraImgDataUtils] Save video thumbnail failed: {e.Message}");
            }
        }

        _videoPacksCache = null;
        _videoPacksCachedUid = null;
        Debug.Log($"[CameraImgDataUtils] Video saved to private: {destPath}");
        return destPath;
    }

    /// <summary>
    /// 将私有沙盒中的视频导出到用户系统相册（iOS Photos / Android Gallery）。
    /// 仅处理视频文件，通过原生接口保存到系统相册可见位置。
    /// iOS 会通过 PHPhotoLibrary 保存到 Camera Roll。
    /// </summary>
    public bool ExportPrivateVideoToAlbum(string privateVideoPath, out string error)
    {
        error = null;
        if (string.IsNullOrEmpty(privateVideoPath))
        {
            error = "video path is null or empty";
            return false;
        }

        if (!File.Exists(privateVideoPath))
        {
            error = $"video file not found: {privateVideoPath}";
            return false;
        }

#if UNITY_IOS && !UNITY_EDITOR
        // iOS: 通过 PHPhotoLibrary 保存到 Camera Roll。
        // 为避免原生端在保存过程中"移动/清理"源文件，传一份副本。
        var exportPath = privateVideoPath;
        try
        {
            var dir = Path.GetDirectoryName(privateVideoPath);
            var nameNoExt = Path.GetFileNameWithoutExtension(privateVideoPath);
            var ext = Path.GetExtension(privateVideoPath);
            var copyPath = Path.Combine(dir ?? string.Empty, $"{nameNoExt}_export{ext}");
            File.Copy(privateVideoPath, copyPath, true);
            exportPath = copyPath;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CameraImgDataUtils] Copy for export failed, using original: {e.Message}");
        }

        try
        {
            var data = new SaveMediaParams
            {
                mediaType = 2,
                mediaUrl = exportPath
            };
            MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
            Debug.Log($"[CameraImgDataUtils] Video exported to system album (iOS): {exportPath}");
            return true;
        }
        catch (Exception e)
        {
            error = $"native save failed: {e.Message}";
            return false;
        }
#elif UNITY_ANDROID && !UNITY_EDITOR
        // Android: 系统相册（Gallery/MediaStore）无法访问应用私有沙盒目录，
        // 必须先将视频复制到公共目录（DCIM/BUD），再通知 MediaScanner 扫描。
        if (EnsurePlayerAlbumPermission(out error))
        {
            var baseName = SanitizeFileName(Path.GetFileNameWithoutExtension(privateVideoPath));
            var videoExt = Path.GetExtension(privateVideoPath);
            if (string.IsNullOrEmpty(videoExt)) videoExt = ".mp4";

            if (TryWriteOrCopyToPlayerAlbum(privateVideoPath, null, baseName, videoExt, out var publicPath, out error))
            {
                TryNotifyNativeSaveMedia(publicPath, 2);
                Debug.Log($"[CameraImgDataUtils] Video exported to system album (Android): {publicPath}");
                return true;
            }
        }

        // 降级：权限或复制失败时，尝试通过原生桥接直接保存
        try
        {
            var data = new SaveMediaParams
            {
                mediaType = 2,
                mediaUrl = privateVideoPath
            };
            MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
            Debug.Log($"[CameraImgDataUtils] Video exported via native fallback (Android): {privateVideoPath}");
            error = null;
            return true;
        }
        catch (Exception e)
        {
            error = $"native save failed: {e.Message}";
            return false;
        }
#else
        var fileNameNoExt = Path.GetFileNameWithoutExtension(privateVideoPath);
        var fileExt = Path.GetExtension(privateVideoPath);
        if (string.IsNullOrEmpty(fileExt)) fileExt = ".mp4";
        return TryWriteOrCopyToPlayerAlbum(privateVideoPath, null, fileNameNoExt, fileExt, out _, out error);
#endif
    }

    /// <summary>
    /// 将 CameraImagePack 对应的私有视频导出到用户系统相册。
    /// </summary>
    public bool ExportPrivateVideoToAlbum(CameraImagePack pack, out string error)
    {
        error = null;
        if (pack == null)
        {
            error = "pack is null";
            return false;
        }

        if (pack.mediaType != 1)
        {
            error = "pack is not a video";
            return false;
        }

        var localPath = ResolveLocalMediaPath(!string.IsNullOrEmpty(pack.mediaUrl) ? pack.mediaUrl : pack.url);
        if (string.IsNullOrEmpty(localPath) || !File.Exists(localPath))
        {
            error = "local video file not found";
            return false;
        }

        return ExportPrivateVideoToAlbum(localPath, out error);
    }

    /// <summary>
    /// 将原始图片字节直接导出到玩家系统相册，跳过读盘/解密步骤。
    /// 适用于拍照后 bytes 仍在内存中的场景，避免 SaveCaptured 加密写盘后再解密读盘的冗余 IO。
    /// </summary>
    public bool ExportRawImageBytesToPlayerAlbum(byte[] imgBytes, string name, out string error)
    {
        error = null;
        if (imgBytes == null || imgBytes.Length == 0)
        {
            error = "imgBytes empty";
            return false;
        }

        if (!EnsurePlayerAlbumPermission(out error))
        {
            return false;
        }

        var fileNameNoExt = !string.IsNullOrEmpty(name)
            ? SanitizeFileName(name)
            : $"BUD_{GameUtils.GetTimeStamp()}";

#if UNITY_IOS && !UNITY_EDITOR
        var tempImgPath = Path.Combine(Application.temporaryCachePath, fileNameNoExt + ".jpg");
        try
        {
            File.WriteAllBytes(tempImgPath, imgBytes);
        }
        catch (Exception e)
        {
            error = $"Write temp image failed: {e.Message}";
            return false;
        }
        try
        {
            var data = new SaveMediaParams { mediaType = 1, mediaUrl = tempImgPath };
            MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
            return true;
        }
        catch (Exception e)
        {
            error = $"native save failed: {e.Message}";
            return false;
        }
#else
        if (!TryWriteOrCopyToPlayerAlbum(null, imgBytes, fileNameNoExt, ".jpg", out var savedPath, out error))
        {
            return false;
        }
        TryNotifyNativeSaveMedia(savedPath, 1);
        return true;
#endif
    }

    /// <summary>
    /// 将图片/视频导出到玩家系统相册常用目录（BUD）。
    /// - Android: /storage/emulated/0/DCIM/BUD（回退 /sdcard/DCIM/BUD）
    /// - iOS: /var/mobile/Media/DCIM/BUD（回退 /private/var/mobile/Media/DCIM/BUD）
    /// </summary>
    public bool ExportMediaToPlayerAlbum(CameraImagePack pack, out string savedPath, out string error)
    {
        savedPath = null;
        error = null;
        if (pack == null)
        {
            error = "pack is null";
            return false;
        }

        if (!EnsurePlayerAlbumPermission(out error))
        {
            return false;
        }

        var fileNameNoExt = !string.IsNullOrEmpty(pack.name)
            ? pack.name
            : $"BUD_{GameUtils.GetTimeStamp()}";
        fileNameNoExt = SanitizeFileName(fileNameNoExt);

        if (pack.mediaType == 1)
        {
            var localVideo = ResolveLocalMediaPath(pack.mediaUrl);
            if (string.IsNullOrEmpty(localVideo))
            {
                localVideo = ResolveLocalMediaPath(pack.url);
            }

            if (string.IsNullOrEmpty(localVideo) || !File.Exists(localVideo))
            {
                error = "local video path not found";
                return false;
            }

            var ext = Path.GetExtension(localVideo);
            if (string.IsNullOrEmpty(ext)) ext = ".mp4";
            if (!TryWriteOrCopyToPlayerAlbum(localVideo, null, fileNameNoExt, ext, out savedPath, out error))
            {
                return false;
            }

            TryNotifyNativeSaveMedia(savedPath, 2);
            return true;
        }

        var plainImageBytes = LoadBytesLocal(pack.name, pack.uploader);
        if (plainImageBytes != null && plainImageBytes.Length > 0)
        {
#if UNITY_IOS && !UNITY_EDITOR
            // iOS 沙盒外路径（/var/mobile/Media/...）应用没有写权限，必须先写临时目录再通过原生桥接保存到系统相册。
            var tempImgPath = Path.Combine(Application.temporaryCachePath, fileNameNoExt + ".png");
            try
            {
                File.WriteAllBytes(tempImgPath, plainImageBytes);
            }
            catch (Exception e)
            {
                error = $"Write temp image failed: {e.Message}";
                return false;
            }
            try
            {
                var data = new SaveMediaParams { mediaType = 1, mediaUrl = tempImgPath };
                MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
                savedPath = tempImgPath;
                return true;
            }
            catch (Exception e)
            {
                error = $"native save failed: {e.Message}";
                return false;
            }
#else
            if (!TryWriteOrCopyToPlayerAlbum(null, plainImageBytes, fileNameNoExt, ".png", out savedPath, out error))
            {
                return false;
            }

            TryNotifyNativeSaveMedia(savedPath, 1);
            return true;
#endif
        }

        var localImage = ResolveLocalMediaPath(pack.url);
        if (string.IsNullOrEmpty(localImage))
        {
            localImage = ResolveLocalMediaPath(pack.previewUrl);
        }

        if (string.IsNullOrEmpty(localImage) || !File.Exists(localImage))
        {
            error = "local image path not found";
            return false;
        }

        var imageExt = Path.GetExtension(localImage);
        if (string.IsNullOrEmpty(imageExt)) imageExt = ".png";
#if UNITY_IOS && !UNITY_EDITOR
        // 同上：iOS 用原生桥接保存，不直接写系统相册目录
        try
        {
            var data = new SaveMediaParams { mediaType = 1, mediaUrl = localImage };
            MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
            savedPath = localImage;
            return true;
        }
        catch (Exception e)
        {
            error = $"native save failed: {e.Message}";
            return false;
        }
#else
        if (!TryWriteOrCopyToPlayerAlbum(localImage, null, fileNameNoExt, imageExt, out savedPath, out error))
        {
            return false;
        }

        TryNotifyNativeSaveMedia(savedPath, 1);
        return true;
#endif
    }

    private static bool EnsurePlayerAlbumPermission(out string error)
    {
        error = null;
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            bool hasWrite = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
            bool hasRead = UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead);
            if (hasWrite && hasRead)
            {
                return true;
            }

            // 弹出权限管理（系统授权弹窗）
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += permissionName =>
            {
                Debug.Log($"[CameraImgDataUtils] Permission granted: {permissionName}");
            };
            callbacks.PermissionDenied += permissionName =>
            {
                Debug.LogWarning($"[CameraImgDataUtils] Permission denied: {permissionName}");
            };
            callbacks.PermissionDeniedAndDontAskAgain += permissionName =>
            {
                Debug.LogWarning($"[CameraImgDataUtils] Permission denied and don't ask again: {permissionName}");
            };

            UnityEngine.Android.Permission.RequestUserPermissions(
                new[] { UnityEngine.Android.Permission.ExternalStorageWrite, UnityEngine.Android.Permission.ExternalStorageRead },
                callbacks
            );

            error = "storage permission required";
            return false;
        }
        catch (Exception e)
        {
            error = $"permission request failed: {e.Message}";
            return false;
        }
#else
        return true;
#endif
    }

    private static void EnsureDir(string dir)
    {
        if (string.IsNullOrEmpty(dir)) return;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }

    private static string BuildLocalAlbumCoverPath(CameraImagePack pack, string nameSuffix = "", string ext = ".png")
    {
        var uid = AccountDataManager.Inst.Uid;
        var safeName = !string.IsNullOrEmpty(pack?.name) ? pack.name : $"album_{GameUtils.GetTimeStamp()}";
        return Path.Combine(Application.persistentDataPath, "U3D", "CameraAlbum", "UploadCache", uid, safeName + nameSuffix + ext);
    }

    private static string NormalizeAlbumUploadCdnUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return url;
        }

        if (url.Contains(GameConsts.BusinessBaseUrl))
        {
            return url.Replace(GameConsts.BusinessBaseUrl, GameConsts.BusinessCdnUrl);
        }

        if (url.Contains(GameConsts.AccBusinessBaseUrl))
        {
            return url.Replace(GameConsts.AccBusinessBaseUrl, GameConsts.BusinessCdnUrl);
        }

        return url;
    }

    private static byte[] EncodeToPngSafe(Texture2D tex)
    {
        if (tex == null) return null;
        try
        {
            return tex.EncodeToPNG();
        }
        catch
        {
            // 非 readable 的 texture，走一次 RT 拷贝再编码
            RenderTexture rt = null;
            try
            {
                rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(tex, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var readable = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                readable.Apply(false, false);
                RenderTexture.active = prev;
                var bytes = readable.EncodeToPNG();
                UnityEngine.Object.Destroy(readable);
                return bytes;
            }
            finally
            {
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
            }
        }
    }

    /// <summary>
    /// 删除本地加密文件（元数据保留；若有云端则变为“仅云端”）。
    /// </summary>
    public bool DeleteLocal(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return DeleteLocalPhotoByName(name, uploaderUidHint: null);
    }

    /// <summary>
    /// 按 pack 删除本地资源：
    /// - 图片：删除加密图并清理纹理缓存
    /// - 视频：删除本地视频文件及同名缩略图（_thumb.png）
    /// </summary>
    public bool DeleteLocal(CameraImagePack pack)
    {
        if (pack == null) return false;
        if (pack.mediaType == 1)
        {
            return DeleteLocalVideo(pack);
        }

        return DeleteLocalPhotoByName(pack.name, pack.uploader);
    }

    /// <summary>
    /// 通过服务端相册 id 删除本地索引项（并尝试删除对应本地加密文件）。
    /// </summary>
    public void DeleteByAlbumId(string albumId)
    {
        if (string.IsNullOrEmpty(albumId)) return;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            for (int i = _index.items.Count - 1; i >= 0; i--)
            {
                var meta = _index.items[i];
                if (meta == null) continue;
                if (meta.albumId != albumId && meta.name != albumId) continue;

                var path = GetEncryptedPath(meta.name, meta.uploader);
                var thumbPath = GetEncryptedThumbnailPath(meta.name, meta.uploader);
                try
                {
                    if (File.Exists(path)) File.Delete(path);
                    if (File.Exists(thumbPath)) File.Delete(thumbPath);
                }
                catch
                {
                    // ignore
                }

                RemoveTextureCache_NoLock(meta.name);
                _metaByName.Remove(meta.name);
                _index.items.RemoveAt(i);
            }
            SaveIndex_NoLock();
        }
    }

    /// <summary>
    /// 读取本地原始字节（解密后）— 供上传使用。
    /// </summary>
    public byte[] LoadBytesLocal(string name, string uploaderUid = null)
    {
        if (string.IsNullOrEmpty(name)) return null;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta) || !meta.hasLocal) return null;
            byte[] blob;
            try
            {
                blob = File.ReadAllBytes(GetEncryptedPath(name, meta.uploader));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CameraImgDataUtils] LoadBytesLocal read failed: {e.Message}");
                return null;
            }
            return Decrypt(blob, uploaderUid ?? meta.uploader);
        }
    }

    /// <summary>
    /// 读取本地图片并返回“解密后的 Texture2D”（用于 UI 显示）。
    /// - 会走缓存（同 name 多次获取不会重复解密）
    /// - 若本地不存在/解密失败/图片数据损坏，返回 null
    /// - 默认优先读取缩略图（若缩略图不存在会自动回退原图）
    /// </summary>
    public Texture2D LoadTextureLocal(string name, string uploaderUid = null, bool markNonReadable = true, bool preferThumbnail = true)
    {
        if (string.IsNullOrEmpty(name)) return null;
        var cacheKey = BuildTextureCacheKey(name, preferThumbnail);

        // 先命中缓存
        lock (_lock)
        {
            if (_textureCache.TryGetValue(cacheKey, out var cached) && cached != null)
            {
                TouchTextureCacheKey_NoLock(cacheKey);
                return cached;
            }
        }

        // 锁外做 IO/解密/解析图片（避免阻塞其它索引操作）
        string path;
        string uid;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta) || !meta.hasLocal) return null;
            uid = uploaderUid ?? meta.uploader;
            path = preferThumbnail ? GetEncryptedThumbnailPath(name, meta.uploader) : GetEncryptedPath(name, meta.uploader);
            if (preferThumbnail && !File.Exists(path))
            {
                path = GetEncryptedPath(name, meta.uploader);
            }
        }

        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;

        byte[] blob;
        try
        {
            blob = File.ReadAllBytes(path);
        }
        catch
        {
            return null;
        }

        byte[] plain;
        try
        {
            plain = Decrypt(blob, uid);
        }
        catch
        {
            plain = null;
        }

        if (plain == null || plain.Length == 0) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        bool ok;
        try
        {
            ok = ImageConversion.LoadImage(tex, plain, markNonReadable);
        }
        catch
        {
            ok = false;
        }

        if (!ok)
        {
            DestroySafe(tex);
            return null;
        }

        lock (_lock)
        {
            // 可能在加载过程中已经被其它线程/调用填充缓存：以已有为准，避免重复对象
            if (_textureCache.TryGetValue(cacheKey, out var existed) && existed != null)
            {
                DestroySafe(tex);
                TouchTextureCacheKey_NoLock(cacheKey);
                return existed;
            }

            _textureCache[cacheKey] = tex;
            TouchTextureCacheKey_NoLock(cacheKey);
            TrimTextureCache_NoLock();
        }

        return tex;
    }

    /// <summary>
    /// 读取本地原图（不走缩略图回退链路），用于大图查看/分享等场景。
    /// </summary>
    public Texture2D LoadTextureLocalOriginal(string name, string uploaderUid = null, bool markNonReadable = true)
    {
        return LoadTextureLocal(name, uploaderUid, markNonReadable, preferThumbnail: false);
    }

    /// <summary>
    /// 清理全部 Texture 缓存（例如关闭相册页时调用）。
    /// </summary>
    public void ClearTextureCache()
    {
        lock (_lock)
        {
            foreach (var kv in _textureCache)
            {
                DestroySafe(kv.Value);
            }
            _textureCache.Clear();
            _textureCacheLru.Clear();
        }
    }

    /// <summary>
    /// 从云端图片磁盘缓存加载纹理（URL 下载过的图会落盘，下次优先读缓存）。
    /// url 为空或缓存不存在时返回 null。
    /// </summary>
    public Texture2D LoadTextureFromCloudCache(string url, bool markNonReadable = true)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var uid = GetCurrentUid();
        var path = GetCloudCachePath(url, uid);
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            // 兼容历史版本：旧逻辑为全局 CloudCache（未按 uid 分层）。
            path = GetCloudCachePath(url, null, useLegacyGlobalPath: true);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
        }
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch
        {
            return null;
        }
        if (bytes == null || bytes.Length == 0) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(tex, bytes, markNonReadable))
            {
                DestroySafe(tex);
                return null;
            }
        }
        catch
        {
            DestroySafe(tex);
            return null;
        }
        return tex;
    }

    /// <summary>
    /// 将纹理写入云端图片磁盘缓存，供下次 LoadTextureFromCloudCache 使用。
    /// </summary>
    public void SaveToCloudCache(string url, Texture2D tex)
    {
        if (string.IsNullOrEmpty(url) || tex == null) return;
        var uid = GetCurrentUid();
        var path = GetCloudCachePath(url, uid);
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var pngBytes = EncodeToPngSafe(tex);
            if (pngBytes != null && pngBytes.Length > 0)
                File.WriteAllBytes(path, pngBytes);
        }
        catch
        {
            // ignore
        }
    }

    private static string GetCloudCachePath(string url, string uid, bool useLegacyGlobalPath = false)
    {
        if (string.IsNullOrEmpty(url)) return null;
        var key = HashUrlForCache(url);
        if (string.IsNullOrEmpty(key)) return null;
        if (useLegacyGlobalPath)
        {
            return Path.Combine(AlbumRootDir, CloudCacheDirName, key + ".png");
        }

        return Path.Combine(GetUserAlbumDir(uid), CloudCacheDirName, key + ".png");
    }

    private static string HashUrlForCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(url);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var sb = new StringBuilder(32);
                for (int i = 0; i < 16 && i < hash.Length; i++)
                    sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 压缩图片：将 Texture2D 按指定最大边长缩放，再以 JPEG 质量编码输出字节数组。
    /// 同时满足两种场景：
    ///   1. 缩略图生成 —— 指定较小的 maxLongSide（如 256/512）；
    ///   2. 上传前压缩 —— 保留较大的 maxLongSide（如 1920），降低 quality 控制体积。
    /// 
    /// <param name="source">原始纹理（支持 non-readable，内部会自动走 RT 拷贝）</param>
    /// <param name="maxLongSide">输出图片长边最大像素值。0 或负数表示不缩放，保持原尺寸</param>
    /// <param name="quality">JPEG 压缩质量 1-100，越低体积越小（推荐缩略图 60、上传 75-85）</param>
    /// <returns>JPEG 编码后的字节数组；失败返回 null</returns>
    /// </summary>
    public static byte[] CompressTexture(Texture2D source, int maxLongSide = 0, int quality = 75)
    {
        if (source == null) return null;
        quality = Mathf.Clamp(quality, 1, 100);

        int srcW = source.width;
        int srcH = source.height;
        if (srcW <= 0 || srcH <= 0) return null;

        int dstW = srcW;
        int dstH = srcH;
        if (maxLongSide > 0)
        {
            int longSide = Mathf.Max(srcW, srcH);
            if (longSide > maxLongSide)
            {
                float scale = (float)maxLongSide / longSide;
                dstW = Mathf.Max(1, Mathf.RoundToInt(srcW * scale));
                dstH = Mathf.Max(1, Mathf.RoundToInt(srcH * scale));
            }
        }

        RenderTexture rt = null;
        Texture2D readable = null;
        try
        {
            rt = RenderTexture.GetTemporary(dstW, dstH, 0, RenderTextureFormat.ARGB32);
            Graphics.Blit(source, rt);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            readable = new Texture2D(dstW, dstH, TextureFormat.RGB24, false);
            readable.ReadPixels(new Rect(0, 0, dstW, dstH), 0, 0);
            readable.Apply(false, false);
            RenderTexture.active = prev;

            return readable.EncodeToJPG(quality);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CameraImgDataUtils] CompressTexture failed: {e.Message}");
            return null;
        }
        finally
        {
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (readable != null) UnityEngine.Object.Destroy(readable);
        }
    }

    /// <summary>
    /// 从原始图片字节（PNG/JPG 等 Unity 支持的格式）直接压缩，无需外部先构造 Texture2D。
    /// 同时满足两种场景：
    ///   1. 缩略图生成 —— 指定较小的 maxLongSide（如 256/512）；
    ///   2. 上传前压缩 —— 保留较大的 maxLongSide（如 1920），降低 quality 控制体积。
    /// 
    /// <param name="imgBytes">原始图片数据（PNG / JPEG / 其它 Unity ImageConversion 支持的格式）</param>
    /// <param name="maxLongSide">输出图片长边最大像素值。0 或负数表示不缩放</param>
    /// <param name="quality">JPEG 压缩质量 1-100</param>
    /// <returns>JPEG 编码后的字节数组；失败返回 null</returns>
    /// </summary>
    public static byte[] CompressImage(byte[] imgBytes, int maxLongSide = 0, int quality = 75)
    {
        if (imgBytes == null || imgBytes.Length == 0) return null;

        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!ImageConversion.LoadImage(tex, imgBytes, false))
            {
                DestroySafe(tex);
                return null;
            }

            return CompressTexture(tex, maxLongSide, quality);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CameraImgDataUtils] CompressImage failed: {e.Message}");
            return null;
        }
        finally
        {
            DestroySafe(tex);
        }
    }

    #endregion

    #region Index / Meta

    [Serializable]
    private sealed class CameraAlbumIndex
    {
        public List<CameraImageMeta> items = new List<CameraImageMeta>();
        public int version = 1;
    }

    [Serializable]
    private sealed class CameraImageMeta
    {
        public string name;
        public string albumId;
        public float size;
        public float height;
        public float width;
        public bool isCloud;
        public string url;
        public string uploader;
        public long saveTime;
        public long uploadTime;
        public bool hasLocal;
        public bool isPublic;
        public string locationName;
        public string mapId;
        public bool isLandMark;
        public List<atListItem> atList;
    }

    private void UpsertMeta(CameraImageMeta meta)
    {
        if (_metaByName.ContainsKey(meta.name))
        {
            // 已存在：更新 list 中对应引用（dict + list 保持严格同步）
            var idx = _index.items.FindIndex(i => i.name == meta.name);
            if (idx >= 0) _index.items[idx] = meta;
        }
        else
        {
            _index.items.Add(meta);
        }
        _metaByName[meta.name] = meta;
    }

    private static CameraImagePack BuildPack(CameraImageMeta meta)
    {
        return new CameraImagePack
        {
            name = meta.name,
            albumId = meta.albumId,
            size = meta.size,
            width = meta.width,
            height = meta.height,
            isCloud = meta.isCloud,
            url = meta.url,
            texture = null, // Get()/LoadTextureLocal 会按需填充
            uploader = meta.uploader,
            saveTime = meta.saveTime,
            uploadTime = meta.uploadTime,
            hasLocal = meta.hasLocal,
            isPublic = meta.isPublic,
            mediaType = 0,
            mediaUrl = null,
            previewUrl = meta.url,
            locationName = meta.locationName,
            mapId = meta.mapId,
            atList = CloneAtList(meta.atList)
        };
    }

    private void LoadIndex(string uid)
    {
        lock (_lock)
        {
            EnsureDirs(uid);
            try
            {
                var indexPath = GetIndexPath(uid);
                if (File.Exists(indexPath))
                {
                    var json = File.ReadAllText(indexPath, Encoding.UTF8);
                    _index = JsonConvert.DeserializeObject<CameraAlbumIndex>(json);
                }
            }
            catch
            {
                _index = null;
            }
            _index ??= new CameraAlbumIndex();
            _index.items ??= new List<CameraImageMeta>();
            // 重建 name→meta 辅助索引（以 list 为权威数据源）
            _metaByName.Clear();
            foreach (var item in _index.items)
            {
                if (item != null && !string.IsNullOrEmpty(item.name))
                    _metaByName[item.name] = item;
            }
            _loadedUid = NormalizeUid(uid);
        }
    }

    private void SaveIndex_NoLock()
    {
        _indexDirty = true;
        // 同帧内多次调用只写一次盘（如批量更新多个字段时），减少序列化 + IO 次数
        if (UnityEngine.Time.frameCount == _lastSaveFrame) return;
        WriteIndex_NoLock();
    }

    private void WriteIndex_NoLock()
    {
        if (!_indexDirty) return;
        _indexDirty = false;
        _lastSaveFrame = UnityEngine.Time.frameCount;
        try
        {
            EnsureDirs(_loadedUid);
            var json = JsonConvert.SerializeObject(_index);
            File.WriteAllText(GetIndexPath(_loadedUid), json, Encoding.UTF8);
        }
        catch
        {
            // ignore
        }
    }

    // Application.quitting 回调：确保 dirty 状态在正常退出时落盘
    private void FlushDirtyIndexOnQuit()
    {
        lock (_lock) { WriteIndex_NoLock(); }
    }

    private void CleanupIndex()
    {
        lock (_lock)
        {
            for (int i = _index.items.Count - 1; i >= 0; i--)
            {
                var meta = _index.items[i];
                if (meta == null || string.IsNullOrEmpty(meta.name))
                {
                    if (meta?.name != null) _metaByName.Remove(meta.name);
                    _index.items.RemoveAt(i);
                    continue;
                }

                meta.hasLocal = File.Exists(GetEncryptedPath(meta.name, meta.uploader));
            }
            SaveIndex_NoLock();
        }
    }

    #endregion

    private static string GetEncryptedPath(string name, string uid)
    {
        return Path.Combine(GetUserAlbumDir(uid), name + EncryptedExt);
    }

    private static string GetEncryptedThumbnailPath(string name, string uid)
    {
        return Path.Combine(GetUserAlbumDir(uid), name + ThumbnailEncryptedExt);
    }

    private bool DeleteLocalPhotoByName(string name, string uploaderUidHint)
    {
        if (string.IsNullOrEmpty(name)) return false;
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(GetCurrentUid());
            if (!_metaByName.TryGetValue(name, out var meta) && !string.IsNullOrEmpty(uploaderUidHint))
            {
                // 兼容历史数据中 uploader 与当前 uid 不一致的情况
                EnsureIndexForUid_NoLock(uploaderUidHint);
                _metaByName.TryGetValue(name, out meta);
            }
            if (meta == null) return false;

            var path = GetEncryptedPath(meta.name, meta.uploader);
            var thumbPath = GetEncryptedThumbnailPath(meta.name, meta.uploader);
            DeleteFileQuietly(path);
            DeleteFileQuietly(thumbPath);

            // 删除时同时从索引移除，避免 index.json 残留无效条目。
            _metaByName.Remove(meta.name);
            _index.items.Remove(meta);
            SaveIndex_NoLock();

            // 同步清理缓存纹理（避免 UI 仍引用已删除的本地资源）
            RemoveTextureCache_NoLock(name);
            return true;
        }
    }

    private bool DeleteLocalVideo(CameraImagePack pack)
    {
        if (pack == null) return false;
        var deletedAny = false;
        var uid = GetCurrentUid();
        var videoPath = ResolveLocalMediaPath(!string.IsNullOrEmpty(pack.mediaUrl) ? pack.mediaUrl : pack.url);

        if (!string.IsNullOrEmpty(videoPath))
        {
            deletedAny |= DeleteFileQuietly(videoPath);
            deletedAny |= DeleteVideoThumbByVideoPath(videoPath, uid);
        }

        // 兜底：当 pack 不带有效路径时，按 name 在当前 uid 的私有视频目录里尝试删除同名视频
        if (!deletedAny && !string.IsNullOrEmpty(pack.name))
        {
            var roots = GetVideoSearchRoots(uid);
            var exts = new[] { ".mp4", ".mov", ".m4v", ".avi" };
            for (int r = 0; r < roots.Count; r++)
            {
                var root = roots[r];
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;

                for (int i = 0; i < exts.Length; i++)
                {
                    var candidate = Path.Combine(root, pack.name + exts[i]);
                    if (!File.Exists(candidate)) continue;
                    deletedAny |= DeleteFileQuietly(candidate);
                    deletedAny |= DeleteVideoThumbByVideoPath(candidate, uid);
                }
            }
        }

        if (deletedAny)
        {
            _videoPacksCache = null;
            _videoPacksCachedUid = null;
        }
        return deletedAny;
    }

    private static bool DeleteVideoThumbByVideoPath(string videoPath, string uid)
    {
        if (string.IsNullOrEmpty(videoPath)) return false;
        var deleted = false;
        var fileNoExt = Path.GetFileNameWithoutExtension(videoPath);
        if (string.IsNullOrEmpty(fileNoExt)) return false;

        var dir = Path.GetDirectoryName(videoPath);
        if (!string.IsNullOrEmpty(dir))
        {
            deleted |= DeleteFileQuietly(Path.Combine(dir, fileNoExt + "_thumb.png"));
            deleted |= DeleteFileQuietly(Path.Combine(dir, fileNoExt + ".png"));
            deleted |= DeleteFileQuietly(Path.Combine(dir, fileNoExt + ".jpg"));
            deleted |= DeleteFileQuietly(Path.Combine(dir, fileNoExt + ".jpeg"));
        }

        var localVideoDir = GetUserVideoDir(uid);
        if (!string.IsNullOrEmpty(localVideoDir))
        {
            deleted |= DeleteFileQuietly(Path.Combine(localVideoDir, fileNoExt + "_thumb.png"));
            deleted |= DeleteFileQuietly(Path.Combine(localVideoDir, fileNoExt + ".png"));
            deleted |= DeleteFileQuietly(Path.Combine(localVideoDir, fileNoExt + ".jpg"));
            deleted |= DeleteFileQuietly(Path.Combine(localVideoDir, fileNoExt + ".jpeg"));
        }

        return deleted;
    }

    private static bool DeleteFileQuietly(string filePath)
    {
        if (string.IsNullOrEmpty(filePath)) return false;
        try
        {
            if (!File.Exists(filePath)) return false;
            File.Delete(filePath);
            return !File.Exists(filePath);
        }
        catch
        {
            return false;
        }
    }

    private static string BuildDedupKey(CameraImagePack pack)
    {
        if (pack == null) return "null";
        if (!string.IsNullOrEmpty(pack.name)) return $"name:{pack.name}";
        if (pack.mediaType == 1 && !string.IsNullOrEmpty(pack.mediaUrl)) return $"video:{pack.mediaUrl}";
        if (!string.IsNullOrEmpty(pack.url)) return $"url:{pack.url}";
        if (!string.IsNullOrEmpty(pack.previewUrl)) return $"preview:{pack.previewUrl}";
        return $"fallback:{pack.uploader}:{pack.saveTime}:{pack.mediaType}";
    }

    private static void MergePack(CameraImagePack target, CameraImagePack incoming)
    {
        if (target == null || incoming == null) return;

        target.isCloud = target.isCloud || incoming.isCloud;
        target.hasLocal = target.hasLocal || incoming.hasLocal;
        target.uploadTime = Math.Max(target.uploadTime, incoming.uploadTime);
        target.saveTime = Math.Max(target.saveTime, incoming.saveTime);

        if (target.mediaType != 1 && incoming.mediaType == 1) target.mediaType = 1;
        if (string.IsNullOrEmpty(target.url)) target.url = incoming.url;
        if (string.IsNullOrEmpty(target.mediaUrl)) target.mediaUrl = incoming.mediaUrl;
        if (string.IsNullOrEmpty(target.previewUrl)) target.previewUrl = incoming.previewUrl;
        if (string.IsNullOrEmpty(target.uploader)) target.uploader = incoming.uploader;
        if (string.IsNullOrEmpty(target.name)) target.name = incoming.name;
        if (string.IsNullOrEmpty(target.albumId)) target.albumId = incoming.albumId;
        target.isPublic = target.isPublic || incoming.isPublic;
        if (string.IsNullOrEmpty(target.locationName)) target.locationName = incoming.locationName;
        if (string.IsNullOrEmpty(target.mapId)) target.mapId = incoming.mapId;
        if ((target.atList == null || target.atList.Count == 0) && incoming.atList != null && incoming.atList.Count > 0)
        {
            target.atList = CloneAtList(incoming.atList);
        }
    }

    private List<CameraImagePack> GetLocalMediaPacks(int type, string uid)
    {
        var result = new List<CameraImagePack>();

        if (type == -1 || type == 0)
        {
            var localPhotos = GetAll();
            for (int i = 0; i < localPhotos.Count; i++)
            {
                var p = localPhotos[i];
                if (p == null) continue;
                p.mediaType = 0;
                p.mediaUrl = null;
                p.previewUrl = p.url;
                result.Add(p);
            }
        }

        if (type == -1 || type == 1)
        {
            result.AddRange(GetLocalVideoPacks(uid));
        }

        return result;
    }

    private List<CameraImagePack> GetLocalVideoPacks(string uid)
    {
        var normalizedUid = NormalizeUid(uid);
        if (_videoPacksCache != null && _videoPacksCachedUid == normalizedUid)
            return new List<CameraImagePack>(_videoPacksCache); // 返回浅拷贝防止调用方修改 list 影响缓存

        var result = new List<CameraImagePack>();
        // 仅扫描当前 uid 的私有沙盒 Video 目录。
        // 业务约定：在”保存到系统相册”之前，视频和图片一致，均按 uid 隔离在私有目录中管理。
        var processedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keyToIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var roots = GetVideoSearchRoots(uid);
        var userVideoDir = GetUserVideoDir(uid);
        var videoExts = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".mp4", ".mov", ".m4v", ".avi" };

        for (int r = 0; r < roots.Count; r++)
        {
            var root = roots[r];
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;

            string[] files;
            try
            {
                files = Directory.GetFiles(root, "*.*", SearchOption.AllDirectories);
            }
            catch
            {
                continue;
            }

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var ext = Path.GetExtension(file);
                if (!videoExts.Contains(ext)) continue;
                if (!processedPaths.Add(file)) continue;

                FileInfo fi;
                try
                {
                    fi = new FileInfo(file);
                }
                catch
                {
                    continue;
                }

                var name = Path.GetFileNameWithoutExtension(file);
                var saveTime = fi.Exists
                    ? new DateTimeOffset(fi.LastWriteTimeUtc).ToUnixTimeSeconds()
                    : 0;
                var uploader = ExtractUploaderFromName(name, uid);
                var preview = TryFindVideoPreviewPath(file, uid);

                var pack = new CameraImagePack
                {
                    name = name,
                    albumId = null,
                    size = fi.Exists ? fi.Length : 0,
                    width = 0,
                    height = 0,
                    isCloud = false,
                    url = file, // 视频项 url 存本地视频路径
                    texture = null,
                    uploader = uploader,
                    saveTime = saveTime,
                    uploadTime = 0,
                    hasLocal = fi.Exists,
                    isPublic = false,
                    mediaType = 1,
                    mediaUrl = file,
                    previewUrl = preview,
                    locationName = null,
                    mapId = null,
                    atList = null
                };

                var key = $"{name}|{pack.size}";
                if (keyToIndex.TryGetValue(key, out var existedIndex))
                {
                    var existed = existedIndex >= 0 && existedIndex < result.Count ? result[existedIndex] : null;
                    bool existedIsUser = IsPathUnderDir(existed?.mediaUrl, userVideoDir);
                    bool curIsUser = IsPathUnderDir(pack.mediaUrl, userVideoDir);
                    // 优先保留沙盒 Video 目录那份
                    if (!existedIsUser && curIsUser)
                    {
                        result[existedIndex] = pack;
                    }
                    continue;
                }

                keyToIndex[key] = result.Count;
                result.Add(pack);
            }
        }

        _videoPacksCache = result;
        _videoPacksCachedUid = normalizedUid;
        return new List<CameraImagePack>(result);
    }

    private static bool IsPathUnderDir(string filePath, string dir)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(dir)) return false;
        try
        {
            var fullFile = Path.GetFullPath(filePath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var fullDir = Path.GetFullPath(dir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;
            return fullFile.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static List<string> GetVideoSearchRoots(string uid)
    {
        // 仅返回当前 uid 的私有沙盒视频目录，避免不同账号在同设备互相看到视频。
        var roots = new List<string> { GetUserVideoDir(uid) };
        return roots
            .Where(r => !string.IsNullOrEmpty(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string TryFindVideoPreviewPath(string videoPath, string uid)
    {
        if (string.IsNullOrEmpty(videoPath)) return null;
        var dir = Path.GetDirectoryName(videoPath);
        var fileNoExt = Path.GetFileNameWithoutExtension(videoPath);
        if (string.IsNullOrEmpty(fileNoExt)) return null;

        if (!string.IsNullOrEmpty(dir))
        {
            var candidates = new[]
            {
                Path.Combine(dir, fileNoExt + "_thumb.png"),
                Path.Combine(dir, fileNoExt + ".png"),
                Path.Combine(dir, fileNoExt + ".jpg"),
                Path.Combine(dir, fileNoExt + ".jpeg")
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i])) return candidates[i];
            }
        }

        // 视频在系统相册目录时，缩略图通常仍保留在应用沙盒的 Video 目录。
        var localVideoDir = GetUserVideoDir(uid);
        if (!string.IsNullOrEmpty(localVideoDir))
        {
            var localCandidates = new[]
            {
                Path.Combine(localVideoDir, fileNoExt + "_thumb.png"),
                Path.Combine(localVideoDir, fileNoExt + ".png"),
                Path.Combine(localVideoDir, fileNoExt + ".jpg"),
                Path.Combine(localVideoDir, fileNoExt + ".jpeg")
            };
            for (int i = 0; i < localCandidates.Length; i++)
            {
                if (File.Exists(localCandidates[i])) return localCandidates[i];
            }
        }

        try
        {
            if (!string.IsNullOrEmpty(localVideoDir) && Directory.Exists(localVideoDir))
            {
                var thumbs = Directory.GetFiles(localVideoDir, "*video_thumb*.png", SearchOption.TopDirectoryOnly);
                if (thumbs.Length > 0)
                {
                    return thumbs
                        .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                        .FirstOrDefault();
                }
            }

            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var thumbs = Directory.GetFiles(dir, "*video_thumb*.png", SearchOption.TopDirectoryOnly);
                if (thumbs.Length > 0)
                {
                    return thumbs
                        .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                        .FirstOrDefault();
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static string ExtractUploaderFromName(string name, string fallbackUid)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var idx = name.IndexOf('_');
            if (idx > 0) return name.Substring(0, idx);
        }

        return string.IsNullOrEmpty(fallbackUid) ? "shotTemplate" : fallbackUid;
    }

    private void EnsureIndexForUid(string uid)
    {
        lock (_lock)
        {
            EnsureIndexForUid_NoLock(uid);
        }
    }

    private void EnsureIndexForUid_NoLock(string uid)
    {
        var normalizedUid = NormalizeUid(uid);
        if (_index == null || _loadedUid != normalizedUid)
        {
            LoadIndex(normalizedUid);
            CleanupIndex();
            _textureCache.Clear();
            _textureCacheLru.Clear();
            _videoPacksCache = null;
            _videoPacksCachedUid = null;
        }
    }

    private static string GetCurrentUid()
    {
        if (AccountDataManager.Inst != null && !string.IsNullOrEmpty(AccountDataManager.Inst.Uid))
        {
            return AccountDataManager.Inst.Uid;
        }
        return "shotTemplate";
    }

    private static string NormalizeUid(string uid)
    {
        return string.IsNullOrEmpty(uid) ? "shotTemplate" : uid;
    }

    private static string ResolveLocalMediaPath(string pathOrUrl)
    {
        if (string.IsNullOrEmpty(pathOrUrl)) return null;
        var value = pathOrUrl.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (value.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                return new Uri(value).LocalPath;
            }
            catch
            {
                return value.Replace("file://", "");
            }
        }

        return value;
    }

    private static bool TryWriteOrCopyToPlayerAlbum(string sourcePath, byte[] data, string baseName, string ext, out string savedPath, out string error)
    {
        savedPath = null;
        error = null;
        var candidates = GetPlayerAlbumDirCandidates();

        for (int i = 0; i < candidates.Count; i++)
        {
            var dir = candidates[i];
            if (string.IsNullOrEmpty(dir)) continue;

            try
            {
                EnsureDir(dir);
                var fileName = $"{baseName}_{GameUtils.GetTimeStamp()}{ext}";
                var dst = Path.Combine(dir, fileName);

                if (data != null && data.Length > 0)
                {
                    File.WriteAllBytes(dst, data);
                }
                else if (!string.IsNullOrEmpty(sourcePath) && File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, dst, true);
                }
                else
                {
                    error = "source not found";
                    return false;
                }

                savedPath = dst;
                return true;
            }
            catch (Exception e)
            {
                error = e.Message;
            }
        }

        if (string.IsNullOrEmpty(error))
        {
            error = "no writable album directory";
        }
        return false;
    }

    private static List<string> GetPlayerAlbumDirCandidates()
    {
        var dirs = new List<string>();
#if UNITY_ANDROID
        dirs.Add("/storage/emulated/0/DCIM/BUD");
        dirs.Add("/sdcard/DCIM/BUD");
#elif UNITY_IOS
        dirs.Add("/var/mobile/Media/DCIM/BUD");
        dirs.Add("/private/var/mobile/Media/DCIM/BUD");
#else
        try
        {
            var pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (!string.IsNullOrEmpty(pictures))
            {
                dirs.Add(Path.Combine(pictures, "BUD"));
            }
        }
        catch
        {
            // ignore
        }
        dirs.Add(Path.Combine(Application.persistentDataPath, "ExportedAlbum", "BUD"));
#endif
        return dirs
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string SanitizeFileName(string fileName)
    {
        if (string.IsNullOrEmpty(fileName)) return "BUD";
        var invalids = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(fileName.Length);
        for (int i = 0; i < fileName.Length; i++)
        {
            var ch = fileName[i];
            sb.Append(invalids.Contains(ch) ? '_' : ch);
        }
        return sb.ToString();
    }

    private static void TryNotifyNativeSaveMedia(string mediaPath, int mediaType)
    {
        if (string.IsNullOrEmpty(mediaPath)) return;
#if UNITY_ANDROID || UNITY_IOS
        try
        {
            var data = new SaveMediaParams
            {
                mediaType = mediaType,
                mediaUrl = mediaPath
            };
            MobileInterface.Instance.SaveMediaToLocal(JsonConvert.SerializeObject(data));
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CameraImgDataUtils] Native save media call failed: {e.Message}");
        }
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
        TryAndroidMediaScan(mediaPath, mediaType);
#endif
    }

    /// <summary>
    /// 直接通过 Unity JNI 调用 Android MediaScannerConnection.scanFile，
    /// 解决部分设备原生桥接无法触发相册刷新的问题。
    /// </summary>
    private static void TryAndroidMediaScan(string filePath, int mediaType)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (string.IsNullOrEmpty(filePath)) return;
        try
        {
            string mimeType;
            var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
            switch (ext)
            {
                case ".mp4":  mimeType = "video/mp4";  break;
                case ".mov":  mimeType = "video/quicktime"; break;
                case ".avi":  mimeType = "video/x-msvideo"; break;
                case ".jpg":
                case ".jpeg": mimeType = "image/jpeg"; break;
                case ".png":  mimeType = "image/png";  break;
                case ".webp": mimeType = "image/webp"; break;
                default:      mimeType = mediaType == 2 ? "video/*" : "image/*"; break;
            }

            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var scannerClass = new AndroidJavaClass("android.media.MediaScannerConnection"))
            {
                scannerClass.CallStatic("scanFile",
                    activity,
                    new string[] { filePath },
                    new string[] { mimeType },
                    null);
            }
            Debug.Log($"[CameraImgDataUtils] MediaScannerConnection.scanFile triggered: {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[CameraImgDataUtils] Android MediaScan failed: {e.Message}");
        }
#endif
    }

    private static string GetUserAlbumDir(string uid)
    {
        return Path.Combine(AlbumRootDir, NormalizeUid(uid));
    }

    private static string GetUserVideoDir(string uid)
    {
        return Path.Combine(GetUserAlbumDir(uid), VideoDirName);
    }

    private static string GetIndexPath(string uid)
    {
        return Path.Combine(GetUserAlbumDir(uid), IndexFileName);
    }

    private void GetRemoteMediaPacks(string uid, int type, Action<List<CameraImagePack>> onComplete, Action<string> onError = null)
    {
        var result = new List<CameraImagePack>();
        if (string.IsNullOrEmpty(uid))
        {
            onComplete?.Invoke(result);
            return;
        }

        string cookie = "";
        int page = 0;
        const int maxPageCount = 20;

        Action nextPage = null;
        nextPage = () =>
        {
            if (page >= maxPageCount)
            {
                onComplete?.Invoke(result);
                return;
            }

            AlbumRequestCtrl.Inst.RequestRemoteAlbumPage(uid, type, cookie, 0, pageRes =>
            {
                if (pageRes?.list != null)
                {
                    for (int i = 0; i < pageRes.list.Count; i++)
                    {
                        if(pageRes.list[i].interactInfo == null)
                        {
                            pageRes.list[i].interactInfo = new InteractInfo();
                            pageRes.list[i].interactInfo.liked = 0;
                            pageRes.list[i].interactInfo.likeAmount = 0;
                        }
                        var pack = ConvertRemoteToPack(pageRes.list[i]);
                        if (pack != null) result.Add(pack);
                    }
                }

                if (pageRes == null || pageRes.isEnd == 1)
                {
                    onComplete?.Invoke(result);
                    return;
                }

                if (string.IsNullOrEmpty(pageRes.cookie) || pageRes.cookie == cookie)
                {
                    onComplete?.Invoke(result);
                    return;
                }

                cookie = pageRes.cookie;
                page++;
                nextPage?.Invoke();
            }, error =>
            {
                onError?.Invoke(error);
            });
        };

        nextPage.Invoke();
    }

    private static CameraImagePack ConvertRemoteToPack(AlbumPhotoInfo item)
    {
        var info = item?.albumItem;
        if (info == null || info.isDelete == 1) return null;

        var mediaType = info.type == 1 ? 1 : 0;
        var preview = mediaType == 1
            ? info.cover
            : (!string.IsNullOrEmpty(info.coverFull) ? info.coverFull : info.cover);
        var mediaUrl = mediaType == 1 ? info.coverFull : null;
        var url = mediaType == 1
            ? mediaUrl // 视频项 url 语义为媒体地址
            : (!string.IsNullOrEmpty(info.coverFull) ? info.coverFull : info.cover);
        var saveTime = info.createTime;

        return new CameraImagePack
        {
            name = "",
            albumId = info.id,
            size = 0,
            width = 0,
            height = 0,
            isCloud = true,
            url = url,
            texture = null,
            uploader = info.creator,
            saveTime = saveTime,
            uploadTime = saveTime,
            hasLocal = false,
            isPublic = info.isPublic == 1,
            mediaType = mediaType,
            mediaUrl = mediaUrl,
            previewUrl = preview,
            locationName = info.locationInfo?.locationName,
            mapId = info.locationInfo?.mapId,
            atList = CloneAtList(info.atList)
        };
    }

    private static List<atListItem> CloneAtList(List<atListItem> atList)
    {
        if (atList == null) return null;
        var copy = new List<atListItem>(atList.Count);
        for (int i = 0; i < atList.Count; i++)
        {
            var item = atList[i];
            if (item == null)
            {
                copy.Add(null);
                continue;
            }

            copy.Add(new atListItem
            {
                uid = item.uid,
                username = item.username
            });
        }
        return copy;
    }

    #region Texture Cache (LRU)

    private void TouchTextureCacheKey_NoLock(string name)
    {
        // 移到 LRU 头部
        var node = _textureCacheLru.Find(name);
        if (node != null) _textureCacheLru.Remove(node);
        _textureCacheLru.AddFirst(name);
    }

    private void TrimTextureCache_NoLock()
    {
        while (_textureCacheLru.Count > TextureCacheCapacity)
        {
            var last = _textureCacheLru.Last;
            if (last == null) break;
            var key = last.Value;
            _textureCacheLru.RemoveLast();

            if (_textureCache.TryGetValue(key, out var tex))
            {
                _textureCache.Remove(key);
                DestroySafe(tex);
            }
        }
    }

    private void RemoveTextureCache_NoLock(string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        RemoveTextureCacheKey_NoLock(BuildTextureCacheKey(name, useThumbnail: true));
        RemoveTextureCacheKey_NoLock(BuildTextureCacheKey(name, useThumbnail: false));
    }

    private static string BuildTextureCacheKey(string name, bool useThumbnail)
    {
        return useThumbnail ? $"thumb:{name}" : $"full:{name}";
    }

    private void RemoveTextureCacheKey_NoLock(string cacheKey)
    {
        if (string.IsNullOrEmpty(cacheKey)) return;

        if (_textureCache.TryGetValue(cacheKey, out var tex))
        {
            _textureCache.Remove(cacheKey);
            DestroySafe(tex);
        }

        var node = _textureCacheLru.Find(cacheKey);
        if (node != null) _textureCacheLru.Remove(node);
    }

    private static void DestroySafe(Texture2D tex)
    {
        if (tex == null) return;
        try
        {
            UnityEngine.Object.Destroy(tex);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// 从 <see cref="LoadTextureLocal"/> 等 LRU 缓存中的纹理复制一份独立 Texture2D，供 UI Image/Sprite 使用。
    /// 多列表同时滑动时缓存会淘汰并 Destroy 原纹理，直接引用缓存纹理会导致白图。
    /// </summary>
    public static Texture2D DuplicateTextureForUiDisplay(Texture2D source)
    {
        if (source == null) return null;
        var w = source.width;
        var h = source.height;
        if (w <= 0 || h <= 0) return null;

        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(source, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(w, h, TextureFormat.RGBA32, false);
        copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        copy.Apply(false, false);
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return copy;
    }

    /// <summary>
    /// 列表缩略图：复制为独立纹理并限制最长边，避免对大图做全分辨率 Blit/ReadPixels（滑动卡顿主因），
    /// 仍不直接引用 LRU 缓存，避免淘汰后出现白图。宽高比不变，Cover 布局与全图一致。
    /// </summary>
    /// <param name="maxEdge">最长边像素上限，与格子显示尺寸同量级即可（如 512）</param>
    public static Texture2D DuplicateTextureForListThumbnail(Texture2D source, int maxEdge = 512)
    {
        if (source == null)
        {
            return null;
        }

        int w = source.width;
        int h = source.height;
        if (w <= 0 || h <= 0)
        {
            return null;
        }

        maxEdge = Mathf.Max(64, maxEdge);
        int maxDim = Mathf.Max(w, h);
        if (maxDim <= maxEdge)
        {
            return DuplicateTextureForUiDisplay(source);
        }

        float scale = maxEdge / (float)maxDim;
        int tw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
        int th = Mathf.Max(1, Mathf.RoundToInt(h * scale));

        var rt = RenderTexture.GetTemporary(tw, th, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        try
        {
            Graphics.Blit(source, rt);
            RenderTexture.active = rt;
            var copy = new Texture2D(tw, th, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, tw, th), 0, 0);
            copy.Apply(false, false);
            return copy;
        }
        finally
        {
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    #endregion

    #region Encryption

    // 文件格式：MAGIC(4) + VER(1) + IV(16) + CIPHERTEXT + HMAC(32)
    private static readonly byte[] Magic = Encoding.ASCII.GetBytes("CIMG");
    private const byte Version = 1;

    private static byte[] Encrypt(byte[] plain, string uploaderUid)
    {
        var key = DeriveKey(uploaderUid);
        // Unity/.NET 运行时兼容：不依赖 RandomNumberGenerator.GetBytes(int)（部分版本不存在）
        var iv = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        byte[] cipher;
        using (var aes = Aes.Create())
        {
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = iv;
            using var enc = aes.CreateEncryptor();
            cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
        }

        var headerLen = Magic.Length + 1 + iv.Length;
        var withoutHmac = new byte[headerLen + cipher.Length];
        Buffer.BlockCopy(Magic, 0, withoutHmac, 0, Magic.Length);
        withoutHmac[Magic.Length] = Version;
        Buffer.BlockCopy(iv, 0, withoutHmac, Magic.Length + 1, iv.Length);
        Buffer.BlockCopy(cipher, 0, withoutHmac, headerLen, cipher.Length);

        var hmac = ComputeHmac(key, withoutHmac);

        var all = new byte[withoutHmac.Length + hmac.Length];
        Buffer.BlockCopy(withoutHmac, 0, all, 0, withoutHmac.Length);
        Buffer.BlockCopy(hmac, 0, all, withoutHmac.Length, hmac.Length);
        return all;
    }

    private static byte[] Decrypt(byte[] blob, string uploaderUid)
    {
        if (blob == null || blob.Length < 4 + 1 + 16 + 32) return null;
        var key = DeriveKey(uploaderUid);

        // 校验 magic
        for (int i = 0; i < Magic.Length; i++)
        {
            if (blob[i] != Magic[i]) return null;
        }
        var ver = blob[Magic.Length];
        if (ver != Version) return null;

        var ivOffset = Magic.Length + 1;
        var iv = new byte[16];
        Buffer.BlockCopy(blob, ivOffset, iv, 0, 16);

        var hmacLen = 32;
        var withoutHmacLen = blob.Length - hmacLen;
        if (withoutHmacLen <= ivOffset + 16) return null;

        var withoutHmac = new byte[withoutHmacLen];
        Buffer.BlockCopy(blob, 0, withoutHmac, 0, withoutHmacLen);

        var expected = ComputeHmac(key, withoutHmac);
        // 常量时间比较
        int diff = 0;
        for (int i = 0; i < hmacLen; i++)
        {
            diff |= expected[i] ^ blob[withoutHmacLen + i];
        }
        if (diff != 0) return null;

        var cipherOffset = ivOffset + 16;
        var cipherLen = withoutHmacLen - cipherOffset;
        if (cipherLen <= 0) return null;
        var cipher = new byte[cipherLen];
        Buffer.BlockCopy(blob, cipherOffset, cipher, 0, cipherLen);

        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = iv;
        using var dec = aes.CreateDecryptor();
        return dec.TransformFinalBlock(cipher, 0, cipher.Length);
    }

    // PBKDF2 推导结果运行期缓存：(appId|deviceId|uid) 三元组在同一会话内不变，首次推导后直接复用。
    private static readonly Dictionary<string, byte[]> _derivedKeyCache = new Dictionary<string, byte[]>();
    private static readonly object _deriveKeyLock = new object();

    private static byte[] DeriveKey(string uploaderUid)
    {
        // 设备维度 + 上传者 uid 混合，保证同设备同用户稳定
        // 注意：deviceUniqueIdentifier 在部分平台可能为空，这里做兜底
        var device = SystemInfo.deviceUniqueIdentifier ?? "unknown_device";
        // cacheKey 与 seed 的可变部分完全一致，确保缓存命中与推导结果严格对应
        var cacheKey = $"{Application.identifier}|{device}|{uploaderUid}";

        lock (_deriveKeyLock)
        {
            if (_derivedKeyCache.TryGetValue(cacheKey, out var cached))
                return cached;

            // PBKDF2：仅在首次（同会话内每个 uid 最多一次）执行 10000 次迭代
            var seed = $"BUD_CAMERA_IMG|{cacheKey}";
            var salt = Encoding.UTF8.GetBytes("BUD_CAMERA_IMG_SALT_V1");
            byte[] derived;
            using (var kdf = new Rfc2898DeriveBytes(seed, salt, 10000, HashAlgorithmName.SHA256))
            {
                derived = kdf.GetBytes(32);
            }
            _derivedKeyCache[cacheKey] = derived;
            return derived;
        }
    }

    private static byte[] ComputeHmac(byte[] key, byte[] data)
    {
        using var h = new HMACSHA256(key);
        return h.ComputeHash(data);
    }

    #endregion

    #region Helpers

    private static void EnsureDirs(string uid)
    {
        try
        {
            if (!Directory.Exists(BaseDir)) Directory.CreateDirectory(BaseDir);
            if (!Directory.Exists(AlbumRootDir)) Directory.CreateDirectory(AlbumRootDir);
            var userDir = GetUserAlbumDir(uid);
            if (!Directory.Exists(userDir)) Directory.CreateDirectory(userDir);
            var videoDir = GetUserVideoDir(uid);
            if (!Directory.Exists(videoDir)) Directory.CreateDirectory(videoDir);
        }
        catch
        {
            // ignore
        }
    }

    #endregion

    /// <summary>
    /// 更新相册照片状态（把服务器返回的数据并入本地索引与内存列表）
    /// 注意：只更新已有条目，不新增。
    /// </summary>
    public void PersistPublicStateToLocal(List<CameraAlbumInfo> resultList, int opt)
    {
        if (AlbumRequestCtrl.Inst.curSelectPackList == null || AlbumRequestCtrl.Inst.curSelectPackList.Count == 0) return;
        resultList ??= new List<CameraAlbumInfo>();

        int n = Math.Min(AlbumRequestCtrl.Inst.curSelectPackList.Count, resultList.Count == 0 ? AlbumRequestCtrl.Inst.curSelectPackList.Count : resultList.Count);
        for (int i = 0; i < n; i++)
        {
            var pack = AlbumRequestCtrl.Inst.curSelectPackList[i];
            if (pack == null) continue;

            var info = (i >= 0 && i < resultList.Count) ? resultList[i] : null;
            var isPublicBool = info != null ? (info.isPublic == 1) : pack.isPublic;

            SetPublicState(pack.name, isPublicBool);
            pack.isPublic = isPublicBool;

            int idx = AlbumRequestCtrl.Inst.albumPhotoInfoList.FindIndex(x => x != null && x.name == pack.name);
            if (idx >= 0)
            {
                AlbumRequestCtrl.Inst.albumPhotoInfoList[idx].isPublic = isPublicBool;
            }
        }
        MessageHelper.Broadcast(MessageName.OnAlbumPhotoDataChanged, opt);
        AlbumRequestCtrl.Inst.curSelectPackList.Clear();
    }

    /// <summary>
    /// 上传/重新上传成功后：把服务器返回的数据并入本地索引与内存列表。
    /// 注意：只更新已有条目，不新增（避免额外增加照片数）。
    /// </summary>
    public void PersistUploadOrReuploadToLocal(List<CameraAlbumInfo> resultList, int opt)
    {
        if (AlbumRequestCtrl.Inst.curSelectPackList == null || AlbumRequestCtrl.Inst.curSelectPackList.Count == 0) return;
        resultList ??= new List<CameraAlbumInfo>();

        int n = Math.Min(AlbumRequestCtrl.Inst.curSelectPackList.Count, resultList.Count);
        for (int i = 0; i < n; i++)
        {
            var pack = AlbumRequestCtrl.Inst.curSelectPackList[i];
            var info = resultList[i];
            if (pack == null || info == null) continue;

            var isPublicBool = info.isPublic == 1;
            var urlToStore = !string.IsNullOrEmpty(info.coverFull) ? info.coverFull : info.cover;
            // 1) 落地到本地索引：下次进入相册拉本地数据可直接拿到修改结果
            if (!string.IsNullOrEmpty(info.id))
            {
                SetAlbumId(pack.name, info.id);
                pack.albumId = info.id;
            }
            if (!string.IsNullOrEmpty(urlToStore))
            {
                MarkUploaded(pack.name, urlToStore, info.createTime, pack.uploader);
                pack.isCloud = true;
                pack.url = urlToStore;
            }
            if (!string.IsNullOrEmpty(info.cover))
            {
                pack.previewUrl = info.cover;
            }
            SetPublicState(pack.name, isPublicBool);
            pack.isPublic = isPublicBool;

            if (!string.IsNullOrEmpty(info.locationInfo?.mapId))
            {
                SetLocationName(pack.locationName, info.locationInfo.mapId);
                SetMapId(pack.locationName, info.locationInfo.mapId);
                pack.locationName = info.locationInfo.locationName;
                pack.mapId = info.locationInfo.mapId;
            }
            if (info.atList != null)
            {
                SetAtList(pack.name, info.atList);
                pack.atList = info.atList;
            }

            // 2) 更新内存列表：用于本次 UI 即时刷新（不新增条目）
            int idx = AlbumRequestCtrl.Inst.albumPhotoInfoList.FindIndex(x => x != null && x.name == pack.name);
            if (idx < 0) continue;

            var target = AlbumRequestCtrl.Inst.albumPhotoInfoList[idx];
            target.isCloud = true;
            target.albumId = !string.IsNullOrEmpty(info.id) ? info.id : target.albumId;
            if (!string.IsNullOrEmpty(info.cover)) target.previewUrl = info.cover;
            if (!string.IsNullOrEmpty(urlToStore)) target.url = urlToStore;
            target.isPublic = isPublicBool;
            if (!string.IsNullOrEmpty(info.locationInfo?.mapId))
            {
                target.locationName = info.locationInfo.locationName;
                target.mapId = info.locationInfo.mapId;
            }
            if (info.atList != null) target.atList = info.atList;
        }
        MessageHelper.Broadcast(MessageName.OnAlbumPhotoDataChanged, opt);
        AlbumRequestCtrl.Inst.curSelectPackList.Clear();
    }

    public void DelectPhotoImg(List<CameraAlbumInfo> resultList, int opt)
    {
        var deletedIds = resultList == null
            ? new HashSet<string>()
            : new HashSet<string>(resultList.Where(x => x != null && !string.IsNullOrEmpty(x.id)).Select(x => x.id));
        

        foreach (var id in deletedIds)
        {
            DeleteByAlbumId(id);
        }

        for (int i = AlbumRequestCtrl.Inst.albumPhotoInfoList.Count - 1; i >= 0; i--)
        {
            var pack = AlbumRequestCtrl.Inst.albumPhotoInfoList[i];
            if (pack == null) continue;
            if (deletedIds.Contains(pack.albumId) || deletedIds.Contains(pack.name))
            {
                AlbumRequestCtrl.Inst.albumPhotoInfoList.RemoveAt(i);
            }
        }
        MessageHelper.Broadcast(MessageName.OnAlbumPhotoDataChanged, opt);
        AlbumRequestCtrl.Inst.curSelectPackList.Clear();
    }
}

/// <summary>
/// 相册列表格网络缩略图并发上限（PhotoList / CameraAllPhoto 等共用），减轻同时解码多张大图造成的卡顿。
/// </summary>
public static class AlbumThumbnailNetworkThrottle
{
    /// <summary>略提高并发：列表格较小、缩略图为主，过低的并发会导致大量格子排队等待。</summary>
    public const int MaxConcurrentDownloads = 8;
    private static int _active;
    private static readonly object _throttleLock = new object();

    public static bool TryBeginDownload()
    {
        lock (_throttleLock)
        {
            if (_active >= MaxConcurrentDownloads)
                return false;
            _active++;
            return true;
        }
    }

    public static void EndDownload()
    {
        lock (_throttleLock)
        {
            if (_active > 0)
                _active--;
        }
    }
}


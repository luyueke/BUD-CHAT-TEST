using System;
using System.Collections;
using System.Collections.Generic;
using Com.TheFallenGames.OSA.Util.PullToRefresh;
using GameData;
using GameData.Base;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;

public class AlbumPanel : BasePanel<AlbumPanel>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private CButton ManageBtn;
    [SerializeField] private CButton SelectTypeBtn;
    [SerializeField] private Text SelectTypeText;
    [SerializeField] private Transform SelectBtnTsf;
    [SerializeField] private List<Toggle> ToggleList;
    [SerializeField] private Text PhotoNum;
    [SerializeField] private CButton DilatationBtn;
    [SerializeField] private Image PhotoImg;
    [SerializeField] private CButton BigGlassBtn;
    [SerializeField] private CButton OpenBtn;
    [SerializeField] private Transform IsShowOpen;
    [SerializeField] private Transform OpenBtnImage;
    [SerializeField] private Transform OpenBtnAni;
    [SerializeField] private SuperTextMesh PosName;
    [SerializeField] private CButton HintUserBtn;
    [SerializeField] private CButton RelayBtn;
    [SerializeField] private CButton UploadBtn;
    [SerializeField] private CButton DeleteBtn;
    [SerializeField] private CButton SaveBtn;
    [SerializeField] private PhotoListAdapter adapter;
    [SerializeField] private GameObject NoneTipsObj;
    [SerializeField] private GameObject RightRoot;
    [SerializeField] private GameObject HintUserRoot;
    [SerializeField] private GameObject AtListRoot;
    [SerializeField] private Text AtListText;
    [SerializeField] private GameObject UplodInfo;
    // 右侧视频播放（mediaType==1）
    [SerializeField] private RawImage _videoRawImage;
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private GameObject LoadingMask;
    private RenderTexture _videoRenderTexture;

    private AlbumType _curAlbumType = AlbumType.All;
    private const string CurAlbumTypeKey = "AlbumPanel_CurAlbumType";
    private Coroutine _bindAdapterDataCo;
    private CameraImagePack _curSelectPack;
    // 防止重复点击上传：成功/失败回调前锁住 UploadBtn
    private bool _isUploadingAlbum = false;
    /// <summary>右侧大图网络加载代数，避免 StopCoroutine 在已下载纹理后泄漏</summary>
    private int _previewLoadGen;


    // PhotoImg 最小显示尺寸（避免出现留白：图片尺寸不小于该尺寸）
    private Vector2 _photoImgMinSize;
    private bool _photoImgMinSizeCached;

    private string defaultColor = "#D9D9D9";
    private string publicColor = "#FFD700";

    private bool IsUploaded(CameraImagePack pack)
    {
        if (pack == null) return false;
        if (pack.mediaType != 0) return false; // 只对照片做判断
        return pack.isCloud || !string.IsNullOrEmpty(pack.albumId) || !string.IsNullOrEmpty(pack.url);
    }

    private void RefreshUploadBtnColor(CameraImagePack pack)
    {
        if (UploadBtn == null) return;

        // 已上传云端的照片不允许再次点击上传（与 IsUploaded 一致）
        var uploaded = pack != null && IsUploaded(pack);
        UploadBtn.interactable = pack != null && !uploaded;

        var img = UploadBtn.image;
        if (img == null) return;

        var hex = uploaded ? publicColor : defaultColor;
        if (ColorUtility.TryParseHtmlString(hex, out var c))
        {
            img.color = c;
        }
    }

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(CloseSelf);
        ManageBtn.onClick.AddListener(OnManageBtnClick);
        SelectTypeBtn.onClick.AddListener(OnSelectTypeBtnClick);
        DilatationBtn.onClick.AddListener(OnDilatationBtnClick);
        OpenBtn.onClick.AddListener(OnOpenBtnClick);
        HintUserBtn.onClick.AddListener(OnHintUserBtnClick);
        RelayBtn.onClick.AddListener(OnRelayBtnClick);
        UploadBtn.onClick.AddListener(OnUploadBtnClick);
        DeleteBtn.onClick.AddListener(OnDeleteBtnClick);
        BigGlassBtn.onClick.AddListener(OnBigGlassBtnClick);
        SaveBtn.onClick.AddListener(OnSaveBtnClick);
        for(int i = 0; i < ToggleList.Count; i++)
        {
            int index = i - 1;
            ToggleList[i].onValueChanged.AddListener((isOn) =>
            {
                OnToggleValueChanged(isOn, index);
            });
        }
        MessageHelper.AddListener<int>(MessageName.OnAlbumPhotoDataChanged, OnAlbumPhotoDataChanged);
        //GetAlbumList(AccountDataManager.Inst.UserInfo.uid,0);//按理这里不需要进行请求，数据处理的时候会获取服务器相关数据，先临时放这里做展示等接口写好删除
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        LoadCurAlbumTypeFromLocal();
        InitAdapter();
        BindAdapterDataFromLocalAlbum();
    }

    protected override void OnDestroy()
    {
        _previewLoadGen++;
        _isUploadingAlbum = false;
        ClearPhotoImageDisplay();
        base.OnDestroy();
        CameraImgDataUtils.Inst.ClearTextureCache();
        MessageHelper.RemoveListener<int>(MessageName.OnAlbumPhotoDataChanged, OnAlbumPhotoDataChanged);
        StopVideoPlayback();
    }

    private void OnAlbumPhotoDataChanged(int opt)
    {
        if (!this || !gameObject.activeInHierarchy) return;
        // 上传/重新上传成功后：服务器消息回调回来，恢复按钮点击
        if (opt == 0 || opt == 2)
        {
            _isUploadingAlbum = false;
            RefreshUploadBtnColor(_curSelectPack);
        }
        BindAdapterDataFromLocalAlbum();
    }

    private void InitAdapter()
    {
        PullToRefreshBehaviour refreshController = adapter.GetComponent<PullToRefreshBehaviour>();
        //refreshController.OnRefreshWithSign.AddListener(OnPullReleased);
        adapter.OnItemsUpdated.RemoveListener(refreshController.HideGizmo);
        adapter.OnItemsUpdated.AddListener(refreshController.HideGizmo);
        adapter.SetOnItemClick(OnPhotoItemClick);
    }

    /// <summary>
    /// 打开相册界面时：从 CameraImgDataUtils.GetAll() 获取数据，赋值给 adapter 列表。
    /// </summary>
    private void BindAdapterDataFromLocalAlbum()
    {
        if (_bindAdapterDataCo != null)
        {
            StopCoroutine(_bindAdapterDataCo);
            _bindAdapterDataCo = null;
        }
        _bindAdapterDataCo = StartCoroutine(CoBindAdapterDataFromLocalAlbum());
    }

    private IEnumerator CoBindAdapterDataFromLocalAlbum()
    {
        int waitFrames = 10;
        while (waitFrames-- > 0 && (adapter == null || adapter.Data == null))
        {
            yield return null;
        }

        if (adapter == null || adapter.Data == null)
        {
            yield break;
        }
        if((int)_curAlbumType < -1 || (int)_curAlbumType > 1){
            _curAlbumType = AlbumType.All;
        }
        CameraImgDataUtils.Inst.GetAllMerged(AccountDataManager.Inst.UserInfo.uid, (int)_curAlbumType, OnGetAllMergedComplete);
    }
    private void OnGetAllMergedComplete(List<CameraImagePack> list)
    {
        if(AlbumRequestCtrl.Inst.albumPhotoInfoList != null && AlbumRequestCtrl.Inst.albumPhotoInfoList.Count > 0)
        {
            AlbumRequestCtrl.Inst.albumPhotoInfoList.Clear();
        }
        for(int i = 0; i < list.Count; i++)
        {
            AlbumRequestCtrl.Inst.albumPhotoInfoList.Add(list[i]);
        }
        NoneTipsObj.SetActive(list == null || list.Count <= 0);
        RightRoot.SetActive(list != null && list.Count > 0);

        // 打开界面默认选中第一张（若有）
        if (list != null && list.Count > 0)
        {
            if(_curSelectPack == null)
            {
                _curSelectPack = list[0];
                adapter.SetSelected(list[0], refresh: false);
                // 同步刷新右侧信息
                OnPhotoItemClick(list[0]);
            }
            else
            {
                adapter.SetSelected(_curSelectPack, refresh: false);
                OnPhotoItemClick(_curSelectPack);
            }
        }
        adapter.Data.ResetItems(list);
        adapter.Refresh();

        PhotoNum.text = string.Format("{0}/{1}", AlbumRequestCtrl.Inst.albumTotal, AlbumRequestCtrl.Inst.albumTotalSlot);//云端照片数/照片容量
    }

    private Sprite _runtimeSprite;
    /// <summary>
    /// 大图预览专用拷贝。列表滑动会触发 CameraImgDataUtils 纹理 LRU 淘汰并 Destroy 缓存纹理，
    /// 若大图 Sprite 仍引用该纹理会显示白图，故与列表缩略图解耦。
    /// </summary>
    private Texture2D _runtimeTextureOwned;

    private void ClearPhotoImageDisplay()
    {
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

        if (PhotoImg != null)
        {
            PhotoImg.sprite = null;
        }
    }

    private void SetPhotoLoadingMask(bool show)
    {
        if (LoadingMask != null)
        {
            LoadingMask.SetActive(show);
        }
    }

    private void ShowSelectPhotoInfo(CameraImagePack pack)
    {
        _curSelectPack = pack;
        OpenBtnAni.gameObject.SetActive(false);
        StopVideoPlayback();
        _previewLoadGen++;
        var previewToken = _previewLoadGen;

        // 先清图再关遮罩，避免切换选中项时短暂仍显示上一张照片
        ClearPhotoImageDisplay();
        SetPhotoLoadingMask(false);

        PhotoImg.gameObject.SetActive(true);
        _videoRawImage.gameObject.SetActive(false);
        RefreshUploadBtnColor(pack);
        PosName.SetText(pack.locationName);
        if(pack.isCloud == false || pack.atList == null || pack.atList.Count <= 0)
        {
            HintUserRoot.SetActive(true);
            AtListRoot.SetActive(false);
        }
        else
        {
            HintUserRoot.SetActive(false);
            AtListRoot.SetActive(true);
            AtListText.text = string.Empty;
            for(int i = 0; i < pack.atList.Count; i++)
            {
                AtListText.text += "@" + pack.atList[i].username + " ";
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(AtListText.rectTransform);
        }
        if(pack.isCloud == true)
        {
            IsShowOpen.gameObject.SetActive(pack.isPublic);
        }
        else
        {
            IsShowOpen.gameObject.SetActive(false);
        }
        var _runtimeTexture = TryLoadTextureWithFallback(pack, out var usedUploaderKey);
        if(_runtimeTexture == null)
        {
            var bigUrl = GetBigPhotoUrl(pack);
            if (!string.IsNullOrEmpty(bigUrl))
            {
                SetPhotoLoadingMask(true);
                StartCoroutine(CoLoadNetworkTexture(pack, bigUrl, previewToken));
                return;
            }

            SetPhotoLoadingMask(false);
            return;
        }

        _runtimeTextureOwned = CameraImgDataUtils.DuplicateTextureForUiDisplay(_runtimeTexture);
        var texForSprite = _runtimeTextureOwned != null ? _runtimeTextureOwned : _runtimeTexture;
        _runtimeSprite = Sprite.Create(texForSprite, new Rect(0, 0, texForSprite.width, texForSprite.height), new Vector2(0.5f, 0.5f));
        PhotoImg.sprite = _runtimeSprite;
        ApplyPhotoImgCover(texForSprite.width, texForSprite.height);
        SetPhotoLoadingMask(false);
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

        // 云端图：优先从磁盘缓存加载，避免重复网络请求
        var bigUrl = GetBigPhotoUrl(pack);
        if (!string.IsNullOrEmpty(bigUrl))
        {
            tex = CameraImgDataUtils.Inst.LoadTextureFromCloudCache(bigUrl);
            if (tex != null) return tex;
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

    private static string GetBigPhotoUrl(CameraImagePack pack)
    {
        if (pack == null)
        {
            return null;
        }

        // 大图预览优先使用大图地址（url），仅在缺失时回退缩略图地址（previewUrl）。
        return !string.IsNullOrEmpty(pack.url) ? pack.url : pack.previewUrl;
    }

    private System.Collections.IEnumerator CoLoadNetworkTexture(CameraImagePack pack, string url, int token)
    {
        var expectedName = pack != null ? pack.name : null;

        using var request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (token != _previewLoadGen)
        {
            yield break;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning(
                $"[CameraImgDataUtilsTester] 本地与网络都加载失败: name={pack.name}, hasLocal={pack.hasLocal}, " +
                    $"url={url}, err={request.error}"
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

        if (token != _previewLoadGen)
        {
            Destroy(tex);
            yield break;
        }

        if (_curSelectPack == null || _curSelectPack.name != expectedName)
        {
            Destroy(tex);
            yield break;
        }

        CameraImgDataUtils.Inst.SaveToCloudCache(url, tex);

        if(_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }

        if (_runtimeTextureOwned != null)
        {
            Destroy(_runtimeTextureOwned);
            _runtimeTextureOwned = null;
        }

        _runtimeTextureOwned = tex;
        _runtimeSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        PhotoImg.sprite = _runtimeSprite;
        ApplyPhotoImgCover(tex.width, tex.height);
        SetPhotoLoadingMask(false);
    }

    /// <summary>
    /// 按图片本身宽高比进行“铺满显示”（Cover）：不出现留白，允许裁切。
    /// 要求：最终 PhotoImg 的宽高都不小于其父容器宽高（同时也不小于 PhotoImg 初始尺寸）。
    /// </summary>
    private void ApplyPhotoImgCover(int texWidth, int texHeight)
    {
        if (PhotoImg == null) return;
        if (texWidth <= 0 || texHeight <= 0) return;

        PhotoImg.preserveAspect = true;

        var rt = PhotoImg.rectTransform;
        var parentRt = rt != null ? rt.parent as RectTransform : null;
        if (rt == null || parentRt == null) return;

        // 缓存 PhotoImg 初始尺寸，确保后续不缩小到比它还小（避免出现留白）
        if (!_photoImgMinSizeCached)
        {
            var rectSize = rt.rect.size;
            if (rectSize.x <= 0f || rectSize.y <= 0f) rectSize = rt.sizeDelta;
            _photoImgMinSize = rectSize;
            _photoImgMinSizeCached = _photoImgMinSize.x > 0f && _photoImgMinSize.y > 0f;
        }

        Canvas.ForceUpdateCanvases();

        var parentSize = parentRt.rect.size;
        if (parentSize.x <= 0f || parentSize.y <= 0f) return;

        var requireW = Mathf.Max(parentSize.x, _photoImgMinSizeCached ? _photoImgMinSize.x : 0f);
        var requireH = Mathf.Max(parentSize.y, _photoImgMinSizeCached ? _photoImgMinSize.y : 0f);

        // 保持旧行为：Cover 覆盖 requiredSize（父容器 size 与最小尺寸的 max），且不强制居中
        CameraImgDataUtils.TryApplyImageCover(PhotoImg, texWidth, texHeight, new Vector2(requireW, requireH), center: false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRt);
    }

    private void OnPhotoItemClick(CameraImagePack pack)
    {
        UplodInfo.SetActive(pack.mediaType == 0);
        HintUserBtn.gameObject.SetActive(pack.mediaType == 0);
        RelayBtn.gameObject.SetActive(pack.mediaType == 0);
        UploadBtn.gameObject.SetActive(pack.mediaType == 0);
        BigGlassBtn.gameObject.SetActive(pack.mediaType == 0);
        SaveBtn.gameObject.SetActive(pack.mediaType != 0);
        AlbumRequestCtrl.Inst.curSelectPackList.Clear();
        AlbumRequestCtrl.Inst.curSelectPackList.Add(pack);
        if(pack.mediaType == 0)
        {
            ShowSelectPhotoInfo(pack);
            return;
        }
        ShowSelectVideoInfo(pack);
    }

    private void ShowSelectVideoInfo(CameraImagePack pack)
    {
        _curSelectPack = pack;
        OpenBtnAni.gameObject.SetActive(false);

        // 使进行中的大图网络协程失效（避免 StopCoroutine 在已下载纹理后泄漏）
        _previewLoadGen++;

        SetPhotoLoadingMask(false);

        // 隐藏照片 Image，显示视频 RawImage
        PhotoImg.gameObject.SetActive(false);
        EnsureVideoComponents();
        _videoRawImage.gameObject.SetActive(true);

        var videoUrl = pack != null ? pack.url : null;
        if (string.IsNullOrEmpty(videoUrl))
        {
            // 兜底：如果外部把视频地址放在 mediaUrl
            videoUrl = pack != null ? pack.mediaUrl : null;
        }

        if (string.IsNullOrEmpty(videoUrl))
        {
            TipPanel.ShowToast("视频地址为空，无法播放");
            StopVideoPlayback();
            _videoRawImage.gameObject.SetActive(false);
            return;
        }

        StartVideoPlayback(videoUrl);
    }

    private void EnsureVideoComponents()
    {
        if (_videoPlayer == null)
        {
            _videoPlayer = gameObject.GetComponent<VideoPlayer>();
            if (_videoPlayer == null) _videoPlayer = gameObject.AddComponent<VideoPlayer>();

            _videoPlayer.playOnAwake = false;
            _videoPlayer.waitForFirstFrame = true;
            _videoPlayer.isLooping = true;
            _videoPlayer.source = VideoSource.Url;
            _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            _videoPlayer.audioOutputMode = VideoAudioOutputMode.None;

            _videoPlayer.prepareCompleted += OnVideoPrepared;
            _videoPlayer.errorReceived += OnVideoError;
        }

        if (_videoRawImage == null)
        {
            // 尽量复用 PhotoImg 的布局区域：创建一个覆盖在其上的 RawImage
            var host = PhotoImg != null ? PhotoImg.transform.parent : null;
            if (host == null) host = transform;

            var go = new GameObject("AlbumVideoRawImage", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(host, false);

            var rt = go.GetComponent<RectTransform>();
            var photoRt = PhotoImg != null ? PhotoImg.rectTransform : null;
            if (photoRt != null)
            {
                rt.anchorMin = photoRt.anchorMin;
                rt.anchorMax = photoRt.anchorMax;
                rt.pivot = photoRt.pivot;
                rt.anchoredPosition = photoRt.anchoredPosition;
                rt.sizeDelta = photoRt.sizeDelta;
                rt.localRotation = photoRt.localRotation;
                rt.localScale = photoRt.localScale;
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            _videoRawImage = go.GetComponent<RawImage>();
            _videoRawImage.raycastTarget = false;
            _videoRawImage.gameObject.SetActive(false);

            // 确保在图片之上显示
            go.transform.SetAsLastSibling();
        }
    }

    private void StartVideoPlayback(string videoUrl)
    {
        EnsureVideoComponents();
        StopVideoPlayback();

        if (_videoPlayer == null || _videoRawImage == null) return;

        _videoPlayer.url = videoUrl;
        _videoPlayer.Prepare();
    }

    private void StopVideoPlayback()
    {
        if (_videoPlayer != null)
        {
            if (_videoPlayer.isPlaying) _videoPlayer.Stop();
        }
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (vp == null || _videoRawImage == null) return;

        if (_videoRenderTexture != null)
        {
            _videoRenderTexture.Release();
            Destroy(_videoRenderTexture);
            _videoRenderTexture = null;
        }

        if (vp.width <= 0 || vp.height <= 0)
        {
            TipPanel.ShowToast("视频加载失败");
            return;
        }

        _videoRenderTexture = new RenderTexture((int)vp.width, (int)vp.height, 0);
        vp.targetTexture = _videoRenderTexture;
        _videoRawImage.texture = _videoRenderTexture;

        // 按视频宽高比，用 RawImage 原本大小做等比缩放（不铺满屏幕，不拉伸）
        var rt = _videoRawImage.rectTransform;
        var box = rt.sizeDelta.x > 0f && rt.sizeDelta.y > 0f ? rt.sizeDelta : rt.rect.size;
        if (box.x > 0f && box.y > 0f)
        {
            float texAspect = (float)vp.width / vp.height;
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

        _videoRawImage.gameObject.SetActive(true);
        vp.Play();
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        LoggerUtils.LogError($"AlbumPanel video error: {message}");
        TipPanel.ShowToast("视频播放失败");
        StopVideoPlayback();
        _videoRawImage.gameObject.SetActive(false);
    }

    private void OnManageBtnClick()
    {
        if(_curAlbumType == AlbumType.Video)
        {
            TipPanel.ShowToast("无法批量管理视频文件");
            return;
        }
        UIManager.Inst.OpenPanel(PanelId.CameraAllPhotoPanel, true, (int)_curAlbumType);
    }

    private void OnSelectTypeBtnClick()
    {
        SelectBtnTsf.gameObject.SetActive(!SelectBtnTsf.gameObject.activeSelf);
    }
    
    private void OnDilatationBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CameraExpandPanel);
    }

    private bool isRefreshImgStatus = false;

    private void OnOpenBtnClick()
    {
        OpenBtnImage.gameObject.SetActive(false);
        OpenBtnAni.gameObject.SetActive(true);
        if(_curSelectPack == null || _curSelectPack.isCloud == false)
        {
            IsShowOpen.gameObject.SetActive(false);
            UploadImg(true);
            return;
        }
        if(isRefreshImgStatus)
        {
            return;
        }

        isRefreshImgStatus = true;
        _curSelectPack.isPublic = !IsShowOpen.gameObject.activeSelf;
        IsShowOpen.gameObject.SetActive(false);
        AlbumRequestCtrl.Inst.curSelectPackList.Clear();
        AlbumRequestCtrl.Inst.curSelectPackList.Add(_curSelectPack);
        CameraImgDataUtils.Inst.BuildAlbumInfoForUpdateWithCoverUpload(_curSelectPack, data =>
        {
            AlbumRequestCtrl.Inst.PublicPhotoInfo(new List<CameraAlbumInfo>() { data }, 3, (list, opt) =>
            {
                isRefreshImgStatus = false;
                OpenBtnImage.gameObject.SetActive(true);
                OpenBtnAni.gameObject.SetActive(false);
                CameraImgDataUtils.Inst.PersistPublicStateToLocal(list,opt);
            }, (str) =>
            {
                
            });
        }, err =>
        {
            TipPanel.ShowToast("上传失败，请重试");
            OpenBtnImage.gameObject.SetActive(true);
            OpenBtnAni.gameObject.SetActive(false);
            LoggerUtils.LogError("BuildAlbumInfoForUpdateWithCoverUpload failed: " + err);
        });
    }

    private void OnHintUserBtnClick()
    {
        if(_curSelectPack == null || _curSelectPack.isCloud == false)
        {
            TipPanel.ShowToast("该照片为本地照片，无法@好友");
            return;
        }
        UIManager.Inst.OpenPanel(PanelId.CameraNoticePanel,_curSelectPack);
    }

    private void OnRelayBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.PhotoSharePanel,_curSelectPack);
    }

    private void OnUploadBtnClick()
    {
        if (IsUploaded(_curSelectPack))
        {
            TipPanel.ShowToast("该照片已上传");
            return;
        }
        UploadImg(false);
        
    }
    private void UploadImg(bool isPublic)
    {
        if (_isUploadingAlbum)
        {
            return;
        }
        if(AlbumRequestCtrl.Inst.albumTotal >= AlbumRequestCtrl.Inst.albumTotalSlot)
        {
            TipPanel.ShowToast("照片容量已满，无法上传");
            return;
        }
        _isUploadingAlbum = true;
        var req = new Dictionary<string, string>()
        {
            {"url", _curSelectPack.url}
        };

        NetworkManager.Inst.SendHttpRequest<AuditImageData>(HttpUrlDefine.AuditImage,
            HttpMethod.POST, req, rsp =>
            {
                if (rsp != null && rsp.auditResult == (int)AuditResult.Passed)
                {
                    if(_curSelectPack == null)
                    {
                        _isUploadingAlbum = false;
                        return;
                    }

                    var uploadType = _curSelectPack.isCloud == true ? 2 : 0;
                    _curSelectPack.isPublic = true;//这里点击上传恒定展示在相册中
                    AlbumRequestCtrl.Inst.curSelectPackList.Clear();
                    AlbumRequestCtrl.Inst.curSelectPackList.Add(_curSelectPack);
                    CameraImgDataUtils.Inst.BuildAlbumInfoForUpdateWithCoverUpload(_curSelectPack, data =>
                    {
                        data.isPublic = isPublic ? 1 : 0;
                        AlbumRequestCtrl.Inst.PublicPhotoInfo(new List<CameraAlbumInfo>() { data }, uploadType, (list, opt) =>
                        {
                            CameraImgDataUtils.Inst.PersistUploadOrReuploadToLocal(list, opt);
                            if(isPublic)
                            {
                                OpenBtnImage.gameObject.SetActive(true);
                                OpenBtnAni.gameObject.SetActive(false);
                            }
                        });
                    }, err =>
                    {
                        _isUploadingAlbum = false;
                        RefreshUploadBtnColor(_curSelectPack);
                        TipPanel.ShowToast("上传失败，请重试");
                        LoggerUtils.LogError("BuildAlbumInfoForUpdateWithCoverUpload failed: " + err);
                    });
                }
                else
                {
                    _isUploadingAlbum = false;
                    RefreshUploadBtnColor(_curSelectPack);
                    TipPanel.ShowToast("图片审核未通过，请重新上传!");
                }
            }, failRsp =>
            {
                _isUploadingAlbum = false;
                RefreshUploadBtnColor(_curSelectPack);
                TipPanel.ShowToast("图片审核未通过，请重新上传!");
            });
    }

    private void OnDeleteBtnClick()
    {
        var commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        commonConfirmPanel.SetLocalText("提示", "上传的照片会同步删除，是否继续删除？", "确定", "取消");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            if(_curSelectPack.isCloud == true)
            {
                AlbumRequestCtrl.Inst.curSelectPackList.Clear();
                AlbumRequestCtrl.Inst.curSelectPackList.Add(_curSelectPack);
                CameraAlbumInfo albumInfo = CameraImgDataUtils.Inst.BuildAlbumInfoForUpdate(_curSelectPack);
                AlbumRequestCtrl.Inst.PublicPhotoInfo(new List<CameraAlbumInfo>(){albumInfo}, 1, (list, opt) =>
                {
                    CameraImgDataUtils.Inst.DelectPhotoImg(list,opt);
                    TipPanel.ShowToast("照片删除成功");
                });
            }
            var localDeleted = CameraImgDataUtils.Inst.DeleteLocal(_curSelectPack);
            if (!localDeleted && _curSelectPack.isCloud == false)
            {
                TipPanel.ShowToast("本地删除失败，请重试");
            }
            _curSelectPack = null;
            BindAdapterDataFromLocalAlbum();
        }, null);
    }

    private void OnToggleValueChanged(bool isOn, int index)
    {
        if(isOn)
        {
            _curAlbumType = (AlbumType)index;
            switch(_curAlbumType)
            {
                case AlbumType.All:
                    SelectTypeText.text = "全部";
                    break;
                case AlbumType.Photo:
                    SelectTypeText.text = "照片";
                    break;
                case AlbumType.Video:
                    SelectTypeText.text = "视频";
                    break;
            }
            SelectBtnTsf.gameObject.SetActive(false);
            SaveCurAlbumTypeToLocal();

            // 切换类型时同步刷新列表数据
            BindAdapterDataFromLocalAlbum();
        }
    }

    private void SaveCurAlbumTypeToLocal()
    {
        PlayerPrefs.SetInt(CurAlbumTypeKey, (int)_curAlbumType);
        PlayerPrefs.Save();
    }

    private void LoadCurAlbumTypeFromLocal()
    {
        if (ToggleList == null || ToggleList.Count <= 0)
        {
            return;
        }

        if (PlayerPrefs.HasKey(CurAlbumTypeKey))
        {
            var saved = PlayerPrefs.GetInt(CurAlbumTypeKey, (int)AlbumType.All);
            if (saved >= 0 && saved < ToggleList.Count)
            {
                _curAlbumType = (AlbumType)saved;
            }
        }

        ToggleList[(int)_curAlbumType + 1].SetIsOnWithoutNotify(true);
        OnToggleValueChanged(true, (int)_curAlbumType);
    }

    private void OnBigGlassBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.BigPhotoImgPanel, _curSelectPack);
    }

    private void OnSaveBtnClick()
    {
        if (_curSelectPack == null)
        {
            TipPanel.ShowToast("请选择要保存的视频");
            return;
        }

        if (_curSelectPack.mediaType != 1)
        {
            TipPanel.ShowToast("当前仅支持保存视频");
            return;
        }

        bool success = CameraImgDataUtils.Inst.ExportPrivateVideoToAlbum(_curSelectPack.url, out string error);
        if (!success)
        {
            TipPanel.ShowToast("保存失败，请重试");
            return;
        }
        TipPanel.ShowToast("视频已保存到相册");
    }
}

public enum AlbumType
{
    All = -1,
    Photo = 0,
    Video = 1,
}

using System;
using System.Collections;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ProfileThemePreviewPanel : BasePanel<ProfileThemePreviewPanel>
{
    [SerializeField] private Button Btn_Close;
    [SerializeField] private Button Btn_Bg;
    [Header("视频播放组件")]
    [SerializeField] private RawImage videoRawImage; // 用于显示视频的RawImage
    [SerializeField] private VideoPlayer videoPlayer; // 视频播放器组件
    [SerializeField] private GameObject loadingIndicator; // 加载指示器
    [SerializeField] private Text title; // 主页皮肤标题
    [SerializeField] private Text previewTitle; // 主页皮肤标题
    private string currentVideoUrl; // 当前视频URL
    private bool isInitialized = false; // 是否已初始化
    private RenderTexture videoRenderTexture; // 存储创建的RenderTexture引用

    public override void OnCreate()
    {
        base.OnCreate();
        Btn_Close.onClick.AddListener(CloseSelf);
        Btn_Bg.onClick.AddListener(CloseSelf);
        InitializeVideoPlayer();
    }
    
    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        var themeId = (ProfileTheme)args[0];
        var config = ProfileThemeManager.Inst.GetThemeInfo((int)themeId);
        if (config != null)
        {
            if (config.title != null)
            {
                previewTitle.text = config.previewTitle;
            }
            else
            {
                previewTitle.gameObject.SetActive(false);
            }
            if (config.title != null)
            {
                title.text = config.title;
            }
            else
            {
                title.text = "个人主页皮肤";
            }

            if (!string.IsNullOrEmpty(config.localPath))
            {
                loadingIndicator.gameObject.SetActive(false);
                videoRawImage.gameObject.SetActive(false);

                var asset = Loader.Load<GameObject>(config.localPath);
                var obj =  asset.Instantiate(transform);
                if (obj != null) {
                    obj.transform.localScale = Vector3.one * 0.43f;
                    (obj.transform as RectTransform).anchoredPosition = new Vector2(0,-60);
                }
            }
            else
            {
                LoadVideo(config.previewUrl);
            }
        }
    }

    private void InitializeVideoPlayer()
    {
        if (isInitialized) return;

        // 确保组件存在
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        // 配置VideoPlayer为循环自动播放
        videoPlayer.playOnAwake = true;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.isLooping = true; // 循环播放
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        
        // 如果RawImage不存在，尝试查找
        if (videoRawImage == null)
        {
            videoRawImage = GetComponentInChildren<RawImage>();
        }

        // 设置视频事件回调
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;

        isInitialized = true;
    }

    // 加载并准备视频
    private void LoadVideo(string videoUrl)
    {
        if (string.IsNullOrEmpty(videoUrl))
        {
            LoggerUtils.LogError("视频URL为空");
            return;
        }

        // 记录URL
        currentVideoUrl = videoUrl;
        
        // 显示加载指示器
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(true);
        }

        // 释放之前的RenderTexture
        ReleaseRenderTexture();

        // 停止当前视频播放
        if (videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        // 设置视频源
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoUrl;

        // 准备视频
        videoPlayer.Prepare();
    }

    // 视频准备完成回调
    private void OnVideoPrepared(VideoPlayer vp)
    {
        // 创建RenderTexture并分配给VideoPlayer
        videoRenderTexture = new RenderTexture((int)vp.width, (int)vp.height, 24);
        videoRenderTexture.name = "VideoRenderTexture_" + GetInstanceID(); // 便于调试时识别
        vp.targetTexture = videoRenderTexture;
        
        // 设置RawImage的纹理
        if (videoRawImage != null)
        {
            videoRawImage.texture = videoRenderTexture;
            
            // 调整RawImage的宽高比与视频一致
            if (vp.width > 0 && vp.height > 0)
            {
                float videoAspect = (float)vp.width / (float)vp.height;
                var rectTransform = videoRawImage.GetComponent<RectTransform>();
                
                if (rectTransform != null)
                {
                    var sizeDelta = rectTransform.sizeDelta;
                    sizeDelta.x = sizeDelta.y * videoAspect;
                    rectTransform.sizeDelta = sizeDelta;
                }
            }
        }
        
        // 隐藏加载指示器
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
        
        // 自动开始播放
        videoPlayer.Play();
    }

    // 视频错误回调
    private void OnVideoError(VideoPlayer vp, string errorMsg)
    {
        LoggerUtils.LogError($"视频播放错误: {errorMsg}");
        
        // 隐藏加载指示器
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(false);
        }
    }

    // 设置音量
    public void SetVolume(float volume)
    {
        if (videoPlayer != null)
        {
            videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(volume));
        }
    }

    // 释放RenderTexture资源
    private void ReleaseRenderTexture()
    {
        if (videoRenderTexture != null)
        {
            if (videoRawImage != null)
            {
                videoRawImage.texture = null;
            }
            
            videoRenderTexture.Release();
            Destroy(videoRenderTexture);
            videoRenderTexture = null;
        }
    }

    // 清理所有视频相关资源
    private void CleanupVideoResources()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.targetTexture = null;
            videoPlayer.url = string.Empty;
        }

        ReleaseRenderTexture();
    }

    public override void OnHidden()
    {
        base.OnHidden();
        // 面板隐藏时停止视频播放并释放资源
        CleanupVideoResources();
        
        // 清理事件监听
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
        }
        
        // 确保释放所有资源
        CleanupVideoResources();

        // 安全起见，直接销毁VideoPlayer组件
        if (videoPlayer != null)
        {
            Destroy(videoPlayer);
            videoPlayer = null;
        }
    }
}

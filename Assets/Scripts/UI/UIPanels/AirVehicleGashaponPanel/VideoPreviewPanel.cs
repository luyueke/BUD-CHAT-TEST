using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 独立视频预览面板 — 接收 videoPath (string) 参数，加载并循环播放 VideoClip。
/// 使用方：UIManager.Inst.OpenPanel&lt;VideoPreviewPanel&gt;(PanelId.VideoPreviewPanel, videoPath);
/// </summary>
public class VideoPreviewPanel : BasePanel<VideoPreviewPanel>
{
    [SerializeField] private RectTransform videoContainer;   // 视频容器，随视频分辨率动态调整尺寸
    [SerializeField] private RawImage videoRawImage;         // 显示视频帧
    [SerializeField] private GameObject loadingObj;          // 加载中转圈动画
    [SerializeField] private CButton closeBtn;               // 关闭按钮

    private VideoPlayer m_VideoPlayer;
    private RenderTexture m_VideoRenderTexture;
    private int m_VideoLoadGen;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn?.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        var videoPath = args != null && args.Length > 0 ? args[0] as string : null;
        if (string.IsNullOrEmpty(videoPath))
        {
            Debug.LogError("[VideoPreviewPanel] videoPath 为空，无法播放视频");
            CloseSelf();
            return;
        }

        StartVideoLoad(videoPath);
    }

    public override void OnHidden()
    {
        base.OnHidden();
        StopAndCleanup();
    }

    // ──────────────────────────────────────────────
    // 私有逻辑
    // ──────────────────────────────────────────────

    private void StartVideoLoad(string videoPath)
    {
        // 生成计数器，使旧异步回调在快速重复打开时自动失效
        m_VideoLoadGen++;
        var currentGen = m_VideoLoadGen;

        // 先展示 loading，隐藏 RawImage
        loadingObj?.SetActive(true);
        videoContainer.gameObject.SetActive(false);
        if (videoRawImage != null) videoRawImage.gameObject.SetActive(false);

        // 停止并重置旧 VideoPlayer
        StopAndCleanup();

        // 配置 VideoPlayer
        m_VideoPlayer ??= gameObject.AddComponent<VideoPlayer>();
        m_VideoPlayer.playOnAwake = false;
        m_VideoPlayer.waitForFirstFrame = true;
        m_VideoPlayer.isLooping = true;
        m_VideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        m_VideoPlayer.source = VideoSource.VideoClip;
        m_VideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        m_VideoPlayer.prepareCompleted += OnVideoPrepared;
        m_VideoPlayer.errorReceived += OnVideoError;

        // 异步加载 VideoClip
        Loader.LoadAsync<VideoClip>(videoPath, (success, wrapper) =>
        {
            // 过期回调 — 丢弃
            if (currentGen != m_VideoLoadGen) return;

            if (!success || wrapper == null || wrapper.request?.asset == null)
            {
                loadingObj?.SetActive(false);
                TipPanel.ShowToast("视频加载失败");
                CloseSelf();
                return;
            }

            // RetainAsset(gameObject) 将资源生命周期绑定到面板，随面板销毁自动释放
            m_VideoPlayer.clip = wrapper.RetainAsset(gameObject);
            m_VideoPlayer.Prepare();
        });
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        videoContainer.gameObject.SetActive(true);
        // 释放旧 RenderTexture
        if (m_VideoRenderTexture != null)
        {
            m_VideoRenderTexture.Release();
            Object.Destroy(m_VideoRenderTexture);
        }

        // 按视频实际分辨率创建 RenderTexture
        m_VideoRenderTexture = new RenderTexture((int)vp.width, (int)vp.height, 24);
        vp.targetTexture = m_VideoRenderTexture;

        if (videoRawImage != null)
        {
            videoRawImage.texture = m_VideoRenderTexture;
            videoRawImage.gameObject.SetActive(true);

            //// 将 RawImage 与容器设置为视频实际分辨率（满足"窗口尺寸 = 视频尺寸"需求）
            //videoRawImage.rectTransform.sizeDelta = new Vector2(vp.width - 30, vp.height - 30);
            Debug.LogError($"vp.width ={vp.width},vp.height={vp.height}");
            //if (videoContainer != null)
            //    videoContainer.sizeDelta = new Vector2(vp.width, vp.height);
        }

        loadingObj?.SetActive(false);
        vp.Play();
    }

    private void OnVideoError(VideoPlayer vp, string errorMsg)
    {
        Debug.LogError($"[VideoPreviewPanel] 视频播放错误: {errorMsg}");
        loadingObj?.SetActive(false);    
        TipPanel.ShowToast("视频加载失败");
        CloseSelf();
    }

    private void StopAndCleanup()
    {
        if (m_VideoPlayer != null)
        {
            m_VideoPlayer.prepareCompleted -= OnVideoPrepared;
            m_VideoPlayer.errorReceived -= OnVideoError;
            if (m_VideoPlayer.isPlaying) m_VideoPlayer.Stop();
            m_VideoPlayer.targetTexture = null;
            m_VideoPlayer.clip = null;
        }

        if (m_VideoRenderTexture != null)
        {
            if (videoRawImage != null) videoRawImage.texture = null;
            m_VideoRenderTexture.Release();
            Object.Destroy(m_VideoRenderTexture);
            m_VideoRenderTexture = null;
        }
    }
}

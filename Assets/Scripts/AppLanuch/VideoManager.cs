using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using System;
using UnityEngine.UI;
using xasset;
using System.IO;
using UnityEngine.Networking;

public class VideoManager : MonoBehaviour { 

    public VideoPlayer _vp;

    public string _path;
    public bool _isLooping = true;
    private bool _isInit = false;

    private RenderTexture _videoTexture = null;
    public VideoPlayer vp => _vp;

    private static VideoManager inst;
  //  private const string cgVideo = "Assets/AppLanuch/CGVedio.prefab";
    public static VideoManager Inst
    {
        get
        {
            if (inst == null)
            {
                // 1. 加载预制体
                GameObject cgVideoPrefab = Resources.Load<GameObject>("CGVedio");
                if (cgVideoPrefab == null)
                {
                    Debug.LogError("Failed to load CGVedio prefab from Resources!");
                    return null;
                }

                // 2. 实例化并设置父对象
                Transform canvasTransform = GameObject.Find("UIRoot/Canvas").transform;
                GameObject cgVideoInstance = Instantiate(cgVideoPrefab, canvasTransform);

                // 3. 设置 RectTransform 全屏适配
                RectTransform rectTransform = cgVideoInstance.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = Vector2.zero;  // 左下角锚点 (0,0)
                    rectTransform.anchorMax = Vector2.one;    // 右上角锚点 (1,1)
                    rectTransform.offsetMin = Vector2.zero;   // 左、下边距 = 0
                    rectTransform.offsetMax = Vector2.zero;   // 右、上边距 = 0
                    rectTransform.localScale = Vector3.one;   // 确保缩放正常
                }

                // 4. 确保它在 Canvas 的最上层（设置 SiblingIndex 为最大）
                cgVideoInstance.transform.SetAsFirstSibling();

                inst = cgVideoInstance.GetComponent<VideoManager>();

                if(inst == null)
                {
                    inst = cgVideoInstance.AddComponent<VideoManager>();
                    inst._vp = cgVideoInstance.transform.Find("VideoPlayer").GetComponent<VideoPlayer>();
                }

                Debug.LogError($"Video Inst ={inst == null}");

            }
            return inst;
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        if (_vp != null && !_isInit)
        {
            InitRawImage();
        }
        gameObject.name = "CGVedio";

        InitMuteStatu();
    }

    private void InitMuteStatu()
    {
        var isCGMute = PlayerPrefs.GetInt("Login_CG_SoundPlay", 0);
        _vp.SetDirectAudioMute(0, isCGMute == 1);
        _vp.SetDirectAudioVolume(0, 0.5f);
    }

    private void OnVideoManagerEvent(bool isMute)
    {
        if (_vp != null)
        {
            Debug.Log("OnVideoManagerEvent isMute = " + isMute);
            SetCGSoundMute(isMute);
        }
    }


    void Release()
    {
        try
        {
            RawImage _rawImage = GetComponent<RawImage>();
            Debug.Log("Video Release ");
            if (_rawImage != null)
            {
                _rawImage.texture = null;
            }

            if (_vp != null)
            {
                _vp.targetTexture = null;
           
            }
            RenderTexture.ReleaseTemporary(_videoTexture);
            _videoTexture = null;
            inst = null;
        }catch(Exception e)
        {
            Debug.LogError("Video Exception e.message=" + e.Message);
        }
    }
    void OnDestroy()
    {
        Release();
    }

    public void InitRawImage()
    {
        RawImage _rawImage = GetComponent<RawImage>();
        // _rawImage.texture = new RenderTexture(1920, 1080, 24);
        if (_videoTexture == null)
        {
            _videoTexture = RenderTexture.GetTemporary(1280, 720, 24);
        }

        _rawImage.texture = _videoTexture;
        _vp.targetTexture = _rawImage.texture as RenderTexture;

        _isInit = true;
    }

    public void Play(Action<string> _callbackReached = null)
    {

        if (_vp != null)
        {
            _vp.source = VideoSource.Url;
            _vp.url = _path;
            _vp.isLooping = _isLooping;
            _vp.waitForFirstFrame = true;
            _vp.Play();
            _vp.loopPointReached += OnLoopPointReached =>
            {
                if (_callbackReached != null)
                    _callbackReached(_vp.url);
            };
        }
    }

    public void PrePareAndPlay(Action<object> _callbackPrePare, bool isPlay = true)
    {
        if (_vp != null)
        {
            _vp.source = VideoSource.Url;
            _vp.url = _path;
            _vp.isLooping = _isLooping;
            _vp.waitForFirstFrame = true;
            _vp.Prepare();

            _vp.prepareCompleted += OnPrepareCompleted =>
            {
                _vp.Pause();
                _vp.frame = 0;
                if (_callbackPrePare != null)
                    _callbackPrePare(_vp.frameCount);

                if (isPlay)
                    _vp.Play();
            };
        }
    }

    public void Play()
    {
        if (_vp != null)
        {
            _vp.Pause();
            _vp.frame = 0;
            _vp.Play();
        }
    }


    public void Pause()
    {
        if (_vp != null)
        {
            _vp.Pause();
        }
    }

    public void Stop()
    {
        if (_vp != null)
        {
            _vp.Stop();
        }
    }

    public void SetCGSoundMute(bool isOn)
    {
        _vp.SetDirectAudioMute(0, isOn);
    }


}

//public class VideoTools
//{
//    private const string cgMp4File = "Assets/AppLanuch/Video/CgRes.mp4";
//    private static string imgPath = Application.persistentDataPath + "/Bundles/Video/temp.png";
//    public static IEnumerator DoShowVideo()
//    {
//        Debug.Log("ShowCGVideo");
//        VideoManager videoManager = VideoManager.Inst;
//        // 在Android上，路径需要以"jar:file://"开头
//        string videoPath = Path.Combine(Application.streamingAssetsPath, "Bundles/Video/CgRes.mp4");
//        string persistentPath = Path.Combine(Application.persistentDataPath, "Bundles/Video/CgRes.mp4");

//        void SetVideoUrl()
//        {
//            videoManager._path = persistentPath;
//            videoManager._isLooping = true;
//             Debug.Log("ShowCGVideo 444");
//            videoManager.PrePareAndPlay((obj) =>
//            {
//               Debug.Log("[Boot] ShowCGVideo PrePare");
//            videoManager.StartCoroutine(SaveCGFrame(videoManager.GetComponent<RawImage>().texture as RenderTexture));
//            }, true);
//        }

//        if (videoManager != null)
//        {
//            //if (videoManager.gameObject.activeSelf)
//            //{
//            //    yield break;
//            //}
//            videoManager.gameObject.SetActive(true);
//            videoManager.InitRawImage();
//            var t = videoManager.GetComponent<RawImage>().texture;
//            //将图片转换为RT，在播放前先显示图片，避免黑屏
//            if (t != null)
//            {
//                TextTuretoRt(t as RenderTexture, imgPath, 1280, 720);
//            }
//            string directoryPath = Path.GetDirectoryName(persistentPath);
//            if (!Directory.Exists(directoryPath))
//            {
//                Directory.CreateDirectory(directoryPath);
//            }
//#if UNITY_ANDROID && !UNITY_EDITOR
//        videoPath = "jar:file://" + Application.dataPath + "!/assets/Bundles/Video/CgRes.mp4";
//#elif UNITY_EDITOR
//            videoPath = Path.Combine(Application.dataPath, cgMp4File);
//#endif
//              Debug.Log("ShowCGVideo 111 videoPath=" + videoPath+",File.exist="+File.Exists(persistentPath));
//            if (File.Exists(persistentPath))
//            {
//                SetVideoUrl();
//                yield break;
//            }

//#if UNITY_IOS && !UNITY_EDITOR
    
//        byte[] fileData = File.ReadAllBytes(videoPath);

//        // 保存到可读写目录
//        File.WriteAllBytes(persistentPath, fileData);

//        // 播放
//        SetVideoUrl();
//        yield break;
//#endif

//              Debug.Log("ShowCGVideo 222 videoPath=" + videoPath);
//            UnityWebRequest request = UnityWebRequest.Get(videoPath);
//            yield return request.SendWebRequest();

//            if (request.isNetworkError || request.isHttpError)
//            {
//                Debug.LogError(request.error);
//            }
//            else
//            {
//                   Debug.Log("ShowCGVideo 333 persistentPath=" + persistentPath);

//                // 保存到持久化路径      
//                File.WriteAllBytes(persistentPath, request.downloadHandler.data);
//                SetVideoUrl();
//            }
//        }

    
//    }

//    private static IEnumerator SaveCGFrame(RenderTexture rt)
//    {
//        //string path = Path.Combine(AssetsUtil.abRootPath, "rt_Login_bg.png");
//        if (File.Exists(imgPath))
//        {
//            Debug.Log("[Boot] SaveCGFrame File.Exists");
//            yield break;
//        }

//        //这里要延迟，不然会截取到上一个视频内容
//        yield return new WaitForSeconds(0.5f);

//        string directoryPath = Path.GetDirectoryName(imgPath);
//        if (!Directory.Exists(directoryPath))
//        {
//            Directory.CreateDirectory(directoryPath);
//        }


//        RenderTexture active = RenderTexture.active;
//        RenderTexture.active = rt;
//        Texture2D png = new Texture2D(rt.width, rt.height, TextureFormat.ARGB4444, false);
//        png.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
//        png.Apply();
//        RenderTexture.active = active;
//        byte[] bytes = png.EncodeToPNG();
//        File.WriteAllBytes(imgPath, bytes);
//        GameObject.Destroy(png);
//        png = null;
//        Debug.Log("保存CGFrame成功！" + imgPath);
//    }
//    public static void TextTuretoRt(RenderTexture targetRenderTexture, string path, int width, int height)
//    {
//        if (!File.Exists(path))
//            return;
//        // 读取 PNG 图片到 Texture2D
//        Texture2D sourceTexture = LoadTextureFromPNG(path, width, height);

//        // 将 Texture2D 内容复制到 RenderTexture
//        Graphics.Blit(sourceTexture, targetRenderTexture);

//        GameObject.Destroy(sourceTexture);

//        Debug.Log("[Loginsystem] TextTuretoRt");
//    }

//    public static Texture2D LoadTextureFromPNG(string filePath, int width, int height)
//    {
//        Texture2D texture = new Texture2D(width, height); // 创建一个临时纹理

//        byte[] fileData = File.ReadAllBytes(filePath); // 从文件中读取字节数据
//        texture.LoadImage(fileData); // 将字节数据加载到纹理中

//        return texture;
//    }


//    public static void GetCGFilePath(string filePath, Action<string> cb)
//    {

        
//        Debug.Log("GetCGFilePath filePath:" + filePath);
       
//        string buildInPath = Path.Combine(Assets.DownloadDataPath, "Video/CgRes.mp4");

//        Debug.Log($"母包路径: {buildInPath},isExist={File.Exists(buildInPath)}");
//        if (File.Exists(buildInPath))
//        {
//            cb?.Invoke(buildInPath);
//        }

//        var handle = RawAsset.LoadAsync(filePath);
//        handle.completed += (op) =>
//        {
//            if (op.status ==xasset.Request.Status.Complete)
//            {
//                Debug.Log("GetCGFilePath handle.path="+handle.path);
//              //  File.WriteAllBytes(buildInPath, request.downloadHandler.data);
//                cb?.Invoke(handle.path);
//            }
//            else
//            {
//                Debug.LogError($"视频加载错误：{filePath}");
//                cb?.Invoke(string.Empty);
//            }
//          //  handle.Release();
//        };
//    }
//}

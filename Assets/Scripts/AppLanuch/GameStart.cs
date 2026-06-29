using System;
using HybridCLR;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Google.Protobuf.Collections;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using xasset;
using xasset.example;
using Debug = UnityEngine.Debug;
using GravityEngine;

public class GameStart : MonoBehaviour
{
    /// <summary>
    /// 启动页UI. AppLanuchPanel
    /// </summary>
    public GameObject ApplanchResouce;

    /// <summary>
    /// 强制更新
    /// </summary>
    public GameObject ForceUpdatePanelResource;
    
    private AppLanuchPanel launchUI;

    public GameObject videoUI;

    /// <summary>
    /// 需要清除Bundle热更资源才修改
    /// </summary>
    private string CacheVersion = "1.0.2";
    /// <summary>
    /// 热更页面UI. AppHotUpdatePanel
    /// </summary>
    public GameObject LoadingResouce;

    public Font MediumFont;


    /// <summary>
    /// 重试页面
    /// </summary>
    public MessageBox tempMessageBox;

    private AppHotUpdatePanel updateUI;
    private UnityEvent<int, int> test;
    private RepeatedField<uint> _repeatedFieldInt;
    /// <summary>
    /// 热更新是否完成
    /// </summary>
    private bool updateIsComplete = false;

    public static string PanelTag = "hotUpdatePanelName";
    public static string PanelLocalPath  => $"{Application.persistentDataPath}/U3D/UpdatePanel/";
    
    public static readonly string[] AOTDllNames =
    {
        "mscorlib.dll",
        "System.dll",
        "System.Core.dll", // 如果使用了Linq，需要这个
    };

    private const int FailMax = 3;
    private int FailCount = 0;
    private ABUpdateInfo info;
    public static bool isBoxWhiteUser;
    private Transform uiCanvas;
    private Coroutine netCheckCor;


    private bool CanBeNext()
    {
        return launchUI.isLanuchFinsh && updateIsComplete;
    }

    private void Start()
    {
        var time1 = GetSystemTime();
        DontDestroyOnLoad(this.gameObject);
        Assets.OfflineMode = false;
        //var cameraNode = GameObject.Find("UIRoot");
        //uiCanvas = cameraNode.transform.Find("Canvas");
        //DontDestroyOnLoad(cameraNode);
        var cameraNode = GameObject.Find("Camera");
        uiCanvas = cameraNode.transform.Find("Canvas");
        DontDestroyOnLoad(cameraNode);
        ClearBundleCache();
        tempMessageBox.onExitGame = ExitApp;
        InitUpdateUI();
      
        updateUI = Instantiate(LoadingResouce, uiCanvas).AddComponent<AppHotUpdatePanel>();
        updateUI.gameObject.SetActive(false);
        updateUI.Init();
        updateUI.SetFont(MediumFont);

        launchUI = GameObject.Instantiate(ApplanchResouce, uiCanvas).GetComponent<AppLanuchPanel>();
        var time2 = GetSystemTime();
        Logger.isOn = true;
        Debug.LogError("##GameStar 初始化时间："+(time2 - time1));
        launchUI.gameObject.SetActive(true);
        launchUI.transform.SetAsLastSibling();
        StartCoroutine(DelayPlayLanuchAni());

      //  ShowVideo();
#if UNITY_IOS
        // 手动初始化（动态挂载 GravityEngineAPI 脚本）
        new GameObject("GravityEngine", typeof(GravityEngineAPI));
#endif
        SetLandscape();
       // netCheckCor = StartCoroutine(CheckNetwork());
    }
    private void SetLandscape()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
    }


    void LogOnOff(DeviceInfo info)
    {
        if (info.environment == "prod")
        {
#if UNITY_EDITOR
            Logger.isOn = true;
#else
       Logger.isOn = true;
#endif

        }
        else
        {
            Logger.isOn = true;//打开日志文件
        }
    }
    
    //IEnumerator CheckNetwork()
    //{
    //    while(true) 
    //    {
    //        yield return 2;
    //        if(Application.internetReachability != NetworkReachability.NotReachable)//检查网络是否可达，因为gravity需要网络上报，IOS需要授权
    //        {
    //            new GravityAPI().StartEngine(); //启动引力引擎，并初始化
    //            StopCoroutine(netCheckCor);
    //            netCheckCor = null;
    //        }
         
    //    }
    //}
    public long GetSystemTime() {
        return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
    }
    
    private IEnumerator DelayPlayLanuchAni()
    {
        yield return null;  // 等待一帧,确保组件已完全准备好
        launchUI.PlayLanuchAni(AppLaunchComplete);
    }

    private void ClearBundleCache()
    {
        string tag = "CacheVersion";
        if (PlayerPrefs.HasKey(tag))
        {
            var curVersion = PlayerPrefs.GetString(tag);
            if (!CacheVersion.Equals(curVersion))
            {
                UpdateVersions.ClearAsync();
                PlayerPrefs.SetString(tag,CacheVersion);
            }
        }
        else
        {
            UpdateVersions.ClearAsync();
            PlayerPrefs.SetString(tag,CacheVersion);
        }
        
        PlayerPrefs.Save();
    }

    private void InitUpdateUI()
    {

        var panelName = GetNeedUpdatePanelName();
        if (string.IsNullOrEmpty(panelName))
        {
            Debug.Log("panelName is null");
            return;
        }
        var filePath = PanelLocalPath + "/" + panelName;
        Debug.Log(filePath);
        if (File.Exists(filePath))
        {
            GameObject updateGo = null;
            try
            {
                var ab = AssetBundle.LoadFromFile(filePath);
                updateGo = ab.LoadAsset<GameObject>("AppHotUpdatePanel");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                PlayerPrefs.DeleteKey(PanelTag);
                return;
            }
            if (updateGo != null) LoadingResouce = updateGo;
            else Debug.LogError("[GameStart] InitUpdateUI: AB加载成功但AppHotUpdatePanel资源为null，使用默认面板");
        }
        else
        {
            Debug.Log($"filePath {filePath} is not exist");
            PlayerPrefs.DeleteKey(PanelTag);
        }
        PlayerPrefs.Save();
    }


    private void GetDeviceInfoAndStartHotUpdate()
    {
#if UNITY_EDITOR
        var rspData = new AppDeviceRes();
        rspData.deviceInfo = new DeviceInfo();
        if (DebugSetting.Inst != null)
        {
            rspData.deviceInfo.version = DebugSetting.Inst.version;
            rspData.deviceInfo.environment = DebugSetting.Inst.environment.ToString();
            rspData.deviceInfo.platform = DebugSetting.Inst.mobile.ToString();
        }

        var requestHeader = rspData?.deviceInfo?.headerInfo;
        if (requestHeader != null)
        {
            GlobalConfigManager.Instance.Init(requestHeader);
            GlobalConfigManager.Instance.Refresh();  
        }
        LogOnOff(rspData.deviceInfo);
        StartCoroutine(UpdateResource(rspData));
#else
        MobileAPI.Instance.AddClientRespose("getDevice", (response)=> OnGetBaseInfo(response));
        MobileAPI.Instance.SendMessage("getDevice", "");
#endif
    }

    private void OnGetBaseInfo(string res)
    {
        MobileAPI.Instance.DelClientResponse("getDevice");
        try
        {
            var rspData = JsonConvert.DeserializeObject<AppDeviceRes>(res);
            if (rspData?.deviceInfo != null)
            {
                var requestHeader = rspData.deviceInfo.headerInfo;
                if (requestHeader != null)
                {
                    GlobalConfigManager.Instance.Init(requestHeader);
                    GlobalConfigManager.Instance.Refresh();
                }
                LogOnOff(rspData.deviceInfo);
                StartCoroutine(UpdateResource(rspData));
            }
            else
            {
                Debug.LogError($"[GameStart] OnGetBaseInfo: deviceInfo为null, res={res}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GameStart] OnGetBaseInfo: JSON解析异常 res={res} ex={e.Message}");
        }
    }

    /// <summary>
    /// 热更开始
    /// </summary>
    /// <param name="dev"></param>
    /// <returns></returns>
    private IEnumerator UpdateResource(AppDeviceRes dev)
    {
        var xAsset = GameObject.Find("XAsset");
        var updateVer = xAsset.GetComponent<UpdateVersions>();
        updateVer.onUpdateChanged = OnUpdateVersionChanged;
        updateVer.tempMessageBox.onExitGame = ExitApp;
        updateVer.InitUpdateSetting();

        Debug.Log("[GameStart] 开始 Assets.InitializeAsync...");
        var t0 = GetSystemTime();
        var initReq = Assets.InitializeAsync();
        yield return initReq;
        Debug.Log($"[GameStart] InitializeAsync 完成，耗时={(GetSystemTime() - t0)}ms result={initReq.result} error={initReq.error}");

        Debug.Log("[GameStart] 开始获取热更信息...");
        yield return GetResourcesInfoRequest(dev,GetResourcesInfoSuccess, GetResourcesInfoFail);
        
        if(info == null)
            yield break;
 
        updateUI.UpdateVersion(info.hotUpdateVersion);
        if (info.forceUpdate == 1)
        {
            ShowForceUpdatePanel();
            yield break;
        }
        updateVer.SetInfo(info);
        
#if UNITY_EDITOR
        if (Assets.SimulationMode)
        {
            // 模拟模式：AssetDatabase 直读，跳过热更直接进游戏
            launchUI.PlayLanuchAni(() =>
            {
                updateIsComplete = true;
                HotfixEntry.Start();
            });
        }
        else
        {
            // AB 模式：走和手机一样的完整热更流程
            updateVer.OnStart();
        }
#else
        updateVer.OnStart();
#endif

    }

    public  void ShowVideo()
    {
        //StartCoroutine(DoShowVideo());
    }
    //private IEnumerator DoShowVideo()
    //{
    //    StartCoroutine(VideoTools.DoShowVideo());
 
    //    var currTime = 0.0f;
    //    while (!VideoManager.Inst._vp.isPrepared && currTime < 2)
    //    {
    //        yield return null;
    //        currTime += Time.deltaTime;
    //    }
    //    VideoManager.Inst.Play();
    //}
    public void ShowForceUpdatePanel()
    {
        var forceUpdatePanel = GameObject.Instantiate(ForceUpdatePanelResource,uiCanvas);
        forceUpdatePanel.transform.SetAsLastSibling();
        forceUpdatePanel.SetActive(true);
        var exitGame = forceUpdatePanel.transform.Find("Panel/Bottom/Confirm");
        var exitBtn = exitGame.GetComponent<Button>();
        exitBtn.onClick.RemoveAllListeners();
        exitBtn.onClick.AddListener(OnBtnExitGameClick);
        
#if PACKAGE_TYPE_US
        var titleText = forceUpdatePanel.transform.Find("Panel/Top/Title").GetComponent<Text>();
        var contentText = forceUpdatePanel.transform.Find("Panel/Bottom/Text (Legacy)").GetComponent<Text>();
        var btnText = forceUpdatePanel.transform.Find("Panel/Bottom/Confirm/Text (Legacy)").GetComponent<Text>();
        if (updateUI)
        {
            LanuchLocalizationUtil.SetLocalText(titleText,"新客户端版本");
            LanuchLocalizationUtil.SetLocalText(contentText,"有新的客户端版本可以用，\n请到应用商店进行更新，体验新内容游玩更顺畅");
            LanuchLocalizationUtil.SetLocalText(btnText,"更新");
        }
#endif

    }

    private void OnBtnExitGameClick()
    {
#if UNITY_ANDROID
        MobileAPI.Instance.SendMessage("openNativeStore", "");
#elif UNITY_IOS
        var appStore = "itms-apps://itunes.apple.com/app/apple-store/id6450975322";
#if PACKAGE_TYPE_US
        appStore = "itms-apps://itunes.apple.com/app/apple-store/id1590291415";
#endif
        Application.OpenURL(appStore);
#endif
    }
    
    
    private void OnUpdateVersionChanged(AppUpdateState state, float progress, ulong downloadedBytes, ulong totalBytes, float startTime)
    {
        switch (state)
        {
            case AppUpdateState.GetVersion:
                updateUI.OnGetVersion(progress);
                break;
            case AppUpdateState.DownloadingResource:
                updateUI.OnDownLoadResAndUpdateUI(progress, downloadedBytes, totalBytes, startTime);
                break;
            
            case AppUpdateState.CleanData:
                updateUI.OnCleanData(progress);
                break;
            
            case AppUpdateState.Complete:
                updateIsComplete = true;
                updateUI.OnFinished();
                AppUpdateResComplete();
                break;
        }
    }

    private void OnComplete()
    {
#if UNITY_EDITOR
        HotfixEntry.Start();
        return;
#endif
        Debug.Log("BUD updateVer.OnComplete");
        // 为aot assembly加载原始metadata， 这个代码放aot或者热更新都行。
        // 一旦加载后，如果AOT泛型函数对应native实现不存在，则自动替换为解释模式执行。

        // 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
        // 我们在BuildProcessor里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

        // 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
        // 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误。

        //updateUI.gameObject.SetActive(false);
        //GameObject.Destroy(updateUI.gameObject);

        //GameObject.DestroyImmediate(updateUI);

        int AOTFlag = AOTDllNames.Length;
        for (int i = 0; i < AOTFlag; i++)
        {
            string dllName = AOTDllNames[i];
            string assetName = string.Format("Assets/Arts/HybridCLR/BaseDlls/{0}.bytes", dllName);
            var aotReq = Asset.Load(assetName, typeof(TextAsset));
            OnLoadAOTDllSuccess(assetName, aotReq.asset);
        }
        var dllDownload = Asset.Load("Assets/Arts/HybridCLR/Dlls/HotDllDownload.bytes", typeof(TextAsset));
        TextAsset dllAsset = (TextAsset) dllDownload.asset;
        Assembly hotfixAssembly = Assembly.Load(dllAsset.bytes);
        var hotfixEntry = hotfixAssembly.GetType("HotDllDownload");
        var start = hotfixEntry.GetMethod("LoadHotDll");
        start?.Invoke(null, null);
        StartCoroutine(SaveUpdateAssetBundle());

    }

    private static void OnLoadAOTDllSuccess(string assetName, object asset)
    {
        if (asset == null)
        {
            Debug.LogError($"Unity======= assetName= {assetName}");
        }

        TextAsset dll = (TextAsset) asset;
        byte[] dllBytes = dll.bytes;
        // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
        var err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes, HomologousImageMode.SuperSet);
        Debug.Log($"LoadMetadataForAOTAssembly:{assetName}. ret:{err}");
    }

    private void AppLaunchComplete()
    {
        if (updateIsComplete)
        {
            OnComplete();
        }
        else
        {
            GetDeviceInfoAndStartHotUpdate();
            launchUI.gameObject.SetActive(false);
            updateUI.gameObject.SetActive(true);
            updateUI.PlayBGM();
        }
    }

    private void AppUpdateResComplete()
    {
        if (launchUI.isLanuchFinsh)
        {
            OnComplete();
        }
    }

    IEnumerator GetResourcesInfoRequest(AppDeviceRes dev, Func<string, AppDeviceRes, IEnumerator> successAct, Func<string, AppDeviceRes, IEnumerator> failAct)
    {
        string url = string.Empty;
        if (dev.deviceInfo.environment == "prod")
        {
            url = "https://api.budapp.cn/configuration/hotUpdate";
        }
        else
        {
            url = "https://api-test.budapp.cn/configuration/hotUpdate";
        }
        
#if PACKAGE_TYPE_US
        if (dev.deviceInfo.environment == "prod")
        {
            url = "https://global.joinbudapp.com/configuration/hotUpdate";
        }
        else
        {
            url = "https://global.joinbudapp.com/configuration/hotUpdate";
        }
#endif
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.timeout = 10;
            if (dev != null && dev.deviceInfo != null && !string.IsNullOrEmpty(dev.deviceInfo.uuid))
            {
                webRequest.SetRequestHeader("uuid", dev.deviceInfo.uuid);
            }
            else
            {
#if UNITY_EDITOR
                webRequest.SetRequestHeader("uuid", "040DA13C-7AE5-4439-86C3-327BD57FD42A");
#endif
                Debug.LogError("GetResourcesInfoRequest UUID Is Null");
            }

            webRequest.SetRequestHeader("environment", dev.deviceInfo.environment);
            webRequest.SetRequestHeader("device", "");
            webRequest.SetRequestHeader("platform", "U3D");
            webRequest.SetRequestHeader("quality", "0");//1:Low 0:High
            webRequest.SetRequestHeader("version", dev.deviceInfo.version??"1.0.0");
            webRequest.SetRequestHeader("contentType", "application/json");
            webRequest.SetRequestHeader("feature", "s15-0");
#if UNITY_IOS
                webRequest.SetRequestHeader("mobile", "ios");
#elif UNITY_ANDROID
            webRequest.SetRequestHeader("mobile", "android");
#endif

            yield return webRequest.SendWebRequest();
            if (webRequest.isDone)
            {
                if (webRequest.result == UnityWebRequest.Result.ProtocolError ||
                    webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.DataProcessingError)
                {
                    yield return failAct?.Invoke(webRequest.downloadHandler.text, dev);
                }
                else
                {
                    yield return successAct?.Invoke(webRequest.downloadHandler.text, dev);
                }
            }
            else
            {
                yield return failAct?.Invoke(webRequest.error, dev);
            }
        }
    }

    private class ResourcesInfo
    {
        public int result;
        public ABUpdateInfo data;
        public string rmsg;
      
    }

    private IEnumerator GetResourcesInfoSuccess(string success, AppDeviceRes dev)
    {
        Debug.Log($"获取热更信息{success}");
        var resInfo = JsonConvert.DeserializeObject<ResourcesInfo>(success);
        if (resInfo != null)
        {
            if (resInfo.result == 0)
            {
                if (resInfo.data != null && !string.IsNullOrEmpty(resInfo.data.downloadURL))
                {
                    isBoxWhiteUser = resInfo.data.isBoxWhiteUser;
                    info = resInfo.data;
                    FailCount = 0;
                    yield break;
                }
                else
                {
                    if (resInfo.data == null)
                    {
                        Debug.LogError("获取热更信息成功 但是 resInfo.data = null");
                    }
                    else if (string.IsNullOrEmpty(resInfo.data.downloadURL))
                    {
                        Debug.LogError("获取热更信息成功 但是 downloadURL = null");
                    }
                }
            }
            else
            {
                Debug.LogError("获取热更信息成功 但是 resInfo.result != 0");
            }
        }
        else
        {
            Debug.LogError("获取热更信息成功 但是 resInfo = null");
        }

        yield return GetResourcesInfoFail("", dev);
    }

    private IEnumerator GetResourcesInfoFail(string fail, AppDeviceRes dev)
    {
        Debug.LogError($"获取热更信息错误{fail}");
        if(++FailCount < FailMax)
        {
            yield return GetResourcesInfoRequest(dev, GetResourcesInfoSuccess, GetResourcesInfoFail);
        }
        else
        {   
            Debug.LogError("达到三次重试上限，热更失败。");
            yield return RetryGetResourcesInfoRequest(dev);
        }
    }

    private IEnumerator RetryGetResourcesInfoRequest(AppDeviceRes dev)
    {
        Debug.LogError("获取热更信息错误 - 弹出重试界面");
        //重置失败次数
        FailCount = 0;
        string tipsStr = "获取热更信息失败，是否重试？";
        if(Application.internetReachability == NetworkReachability.NotReachable)
        {
            tipsStr = "网络不太顺畅，请检查网络设置";
        }
#if PACKAGE_TYPE_US
        tipsStr = LanuchLocalizationUtil.GetLocalText(tipsStr);
#endif
        var retry  = MessageBox.Show(tempMessageBox, tipsStr);
        yield return retry;
        if (retry.result == Request.Result.Success)
        {
            yield return GetResourcesInfoRequest(dev, GetResourcesInfoSuccess, GetResourcesInfoFail);
        }
        else
        {
            yield break;
        }
    }

    private bool IsNeedUpdateUI(string remoteName)
    {
        var panelName = GetNeedUpdatePanelName();
        if (string.IsNullOrEmpty(panelName))
        {
            return true;
        }
        return !panelName.Equals(remoteName);
    }

    private string GetNeedUpdatePanelName()
    {
        if (PlayerPrefs.HasKey(PanelTag))
        {
            var panelName = PlayerPrefs.GetString(PanelTag);
            return panelName;
        }
        return string.Empty;
    }

    private IEnumerator SaveUpdateAssetBundle()
    {
        string url = info?.panelURL;
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("www download url is null");
            yield break;
        }
        string fileName = string.Empty;
        try
        {
            Uri uri = new Uri(url);
            fileName = Path.GetFileName(uri.LocalPath);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            yield break;
        }
        if (!IsNeedUpdateUI(fileName))
        {
            yield break;
        }
        Debug.Log("SaveUpdateAssetBundle=url===" + url);
        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            yield return uwr.SendWebRequest();
            if (uwr.error != null)
            {
                Debug.LogError("www download had an error" + uwr.error);
                yield break;
            }
            if (uwr.isDone)
            {
                if (Directory.Exists(PanelLocalPath))
                {
                    Directory.Delete(PanelLocalPath,true);
                }
                Directory.CreateDirectory(PanelLocalPath);
                
                FileInfo fileInfo = new FileInfo(PanelLocalPath + "/" + fileName);
                FileStream fs = fileInfo.Create();
 
                //fs.Write(字节数组, 开始位置, 数据长度);
                fs.Write(uwr.downloadHandler.data, 0, uwr.downloadHandler.data.Length);
 
                fs.Flush();     //文件写入存储到硬盘
                fs.Close();     //关闭文件流对象
                fs.Dispose();   //销毁文件对象

                PlayerPrefs.SetString(PanelTag, fileName);
                PlayerPrefs.Save();
            }
        }
    }

    private void ExitApp()
    {
        MobileAPI.Instance.SendMessage("killApp", "");
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class LanuchLocalizationUtil
{
    private static Dictionary<string, string> LocalDict = new Dictionary<string, string>();
    private static bool IsInit = false;
    private static void Init()
    {
        LocalDict.Clear();
#if PACKAGE_TYPE_US
        LocalDict.Add("加载中...","Loading...");
        LocalDict.Add("获取版本信息","Get version info");
        LocalDict.Add("更新过程中请不要关闭游戏","Please do not close the game during the update");
        LocalDict.Add("游戏资源更新中[{0:N0}MB / {1:N0}MB] 速度:{2:N0}KB/S","Game resources updating [{0:N0}MB / {1:N0}MB] Speed: {2:N0}KB/S");
        LocalDict.Add("解压中，请不要关闭游戏","Unzipping, please do not close the game");
        LocalDict.Add("新客户端版本","New version");
        LocalDict.Add("有新的客户端版本可以用，\n请到应用商店进行更新，体验新内容游玩更顺畅","A new version is available. \nPlease update to the app store to experience smoother gameplay with new content ");
        LocalDict.Add("更新","Update");
        LocalDict.Add("获取热更信息失败，是否重试？","Failed to get hot update information, retry?");
#endif
        IsInit = true;
    }
    
    //没用SetLocalText 此处还没初始化xasset，无法获取多语言配置
    public static void SetLocalText(Text textComp,string localizationKey,params object[] formatArgs)
    {
        if (!IsInit)
        {
            Init();
        }

        string localValue = localizationKey;
        if (LocalDict.ContainsKey(localizationKey))
        {
            localValue = LocalDict[localizationKey];
        }
        
        if (formatArgs == null || formatArgs.Length <= 0)
        {
            textComp.text = localValue;
        }
        else
        {
            textComp.text = string.Format(localValue, formatArgs);
        }
    }

    public static string GetLocalText(string localizationKey,params object[] formatArgs)
    {
        if (string.IsNullOrEmpty(localizationKey))
        {
            return "";
        }
        if (LocalDict != null && LocalDict.ContainsKey(localizationKey))
        {
            string zhValue = LocalDict[localizationKey];
            if (string.IsNullOrEmpty(zhValue))
            {
                return localizationKey;
            }

            if (formatArgs == null || formatArgs.Length == 0)
            {
                return zhValue;
            }
            return string.Format(zhValue, formatArgs);
        }

        if(formatArgs == null || formatArgs.Length == 0)
            return localizationKey;
        return string.Format(localizationKey, formatArgs);
    }

}

public class AppHotUpdatePanel : MonoBehaviour
{
    private Text versionLabel;
    private Text Txt_DownLoadDesc;
    private Text Txt_DownLoadInfo;
    private Transform LoadingSound;
    private ProgressSmooth progressSmooth;
    private Transform logo;
    private float randomProgressBase = -1f; // 用于存储80%-90%之间的随机值N
    private float currentProgress = 0f; // 当前显示的进度
    private float baseAccelerationSpeed = 0.05f; // 基础加速速度


    public void Init()
    {
        versionLabel = this.transform.Find("UIContainer/Version").GetComponent<Text>();
        Txt_DownLoadDesc = this.transform.Find("UIContainer/Desc").GetComponent<Text>();
        Txt_DownLoadInfo = this.transform.Find("UIContainer/DownloadInfo").GetComponent<Text>();
        LoadingSound = this.transform.Find("loadSound");
        progressSmooth = this.transform.Find("UIContainer/Progress").GetComponent<ProgressSmooth>();
        logo = this.transform.Find("UIContainer/Logo");
        progressSmooth.ResetFlag();
        LoadingSound?.SetParent(null);
        GameObject.DontDestroyOnLoad(LoadingSound);

        //播放视频，隐藏背景
        //transform.GetComponent<Image>().enabled = false;
        //transform.Find("BGContainer").gameObject.SetActive(false);

        // 重置进度相关变量
        randomProgressBase = -1f;
        currentProgress = 0f;
        
#if PACKAGE_TYPE_US
        logo.gameObject.SetActive(false);
#endif
        LanuchLocalizationUtil.SetLocalText(Txt_DownLoadDesc,"加载中...");
    }

    public void SetFont(Font textFont)
    {
        versionLabel.font = textFont;
        Txt_DownLoadDesc.font = textFont;
        Txt_DownLoadInfo.font = textFont;
    }
    
    public void PlayBGM()
    {
        LoadingSound.gameObject.SetActive(true);
    }

    public void OnGetVersion(float progress)
    {
        LanuchLocalizationUtil.SetLocalText(Txt_DownLoadDesc,"获取版本信息");
        Txt_DownLoadInfo.text = "";
        progressSmooth.SetValue(progress);
    }
    
    public void OnDownLoadResAndUpdateUI(float progress, ulong downloadedBytes = 0, ulong totalBytes = 0, float startTime = 0)
    {
        LanuchLocalizationUtil.SetLocalText(Txt_DownLoadDesc,"更新过程中请不要关闭游戏");
        if(progress == 0)
        {
            progressSmooth.ResetFlag();
        }
        // 如果是第一次调用，生成80%-90%之间的随机值N
        if (randomProgressBase < 0f)
        {
            randomProgressBase = UnityEngine.Random.Range(0.7f, 0.9f);
        }
        
        // 如果当前进度还没达到目标随机值，则加速到目标值
        if (currentProgress < randomProgressBase)
        {
            // 计算动态加速速度：值越大，加速越快；值越小，加速越慢
            float progressDiff = randomProgressBase - currentProgress;
            float dynamicAccelerationSpeed = baseAccelerationSpeed * (1f + progressDiff * 2f); // 差值越大，加速越快
            
            currentProgress = Mathf.Min(currentProgress + dynamicAccelerationSpeed, randomProgressBase);
        }
        
        // 计算实际显示的进度：N + (已下载资源/总资源) * (100% - N)
        float actualProgress = 0f;
        if (totalBytes > 0)
        {
            float downloadRatio = (float)downloadedBytes / (float)totalBytes;
            float calculatedProgress = currentProgress + downloadRatio * (1.0f - currentProgress);
            
            // 如果实际下载进度比计算出的进度大，则显示实际下载进度
            actualProgress = Mathf.Max(calculatedProgress, downloadRatio);
        }
        else
        {
            actualProgress = currentProgress;
        }
       // Debug.LogError($"updateui OnDownLoadResAndUpdateUI randomProgressBase={randomProgressBase} actualProgress=" + actualProgress);
        progressSmooth.SetValue(actualProgress);

        float elapsedTime = Time.time - startTime;
        float downloadSpeed = downloadedBytes / 1024f / elapsedTime;
        LanuchLocalizationUtil.SetLocalText(Txt_DownLoadInfo,"游戏资源更新中[{0:N0}MB / {1:N0}MB] 速度:{2:N0}KB/S", downloadedBytes / (1024f * 1024f), totalBytes / (1024f * 1024f), downloadSpeed);
    }

    public void OnCleanData(float progress)
    {
        LanuchLocalizationUtil.SetLocalText(Txt_DownLoadDesc,"解压中，请不要关闭游戏");
        Txt_DownLoadInfo.text = "";
        progressSmooth.SetValue(progress);
    }

    public void OnFinished()
    {
        Txt_DownLoadDesc.text = "";
        Txt_DownLoadInfo.text = "";
        progressSmooth.SetValue(1.0f);
    }

    public void UpdateVersion(string version)
    {
        if (!string.IsNullOrEmpty(version))
        {
            versionLabel.text = "v " + version;
        }

    }



#if UNITY_EDITOR
    // 编辑器测试相关变量
    private bool isSimulating = false;
    private float simulationStartTime = 0f;
    private float simulatedDownloadedBytes = 0f;
    private float simulatedTotalBytes = 1000f * 1024f * 102f; // 100MB
    private float simulationSpeed = 2f * 1024f * 1024f; // 2MB/s

    // 编辑器GUI绘制
    private void OnGUI()
    {
        if (!Application.isPlaying) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");

        GUILayout.Label("热更新进度模拟测试", EditorGUIUtility.isProSkin ? GUI.skin.label : GUI.skin.box);

        if (!isSimulating)
        {
            if (GUILayout.Button("开始模拟下载"))
            {
                StartSimulation();
            }
        }
        else
        {
            if (GUILayout.Button("停止模拟"))
            {
                StopSimulation();
            }

            GUILayout.Label($"模拟进度: {simulatedDownloadedBytes / (1024f * 1024f):F1}MB / {simulatedTotalBytes / (1024f * 1024f):F1}MB");
            GUILayout.Label($"当前随机基准值: {randomProgressBase:P1}");
            GUILayout.Label($"当前显示进度: {currentProgress:P1}");
        }

        GUILayout.Space(10);
        GUILayout.Label("下载速度设置:");
        simulationSpeed = GUILayout.HorizontalSlider(simulationSpeed, 0.1f * 1024f * 1024f, 10f * 1024f * 1024f);
        GUILayout.Label($"{(simulationSpeed / (1024f * 1024f)):F1} MB/s");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private void StartSimulation()
    {
        isSimulating = true;
        simulationStartTime = Time.time;
        simulatedDownloadedBytes = 0f;

        // 重置进度相关变量
        randomProgressBase = -1f;
        currentProgress = 0f;

        // 初始化UI
        Init();
        OnGetVersion(0f);
    }

    private void StopSimulation()
    {
        isSimulating = false;
        OnFinished();
    }

    private void Update()
    {
        if (!isSimulating) return;

        // 模拟下载进度
        simulatedDownloadedBytes += simulationSpeed * Time.deltaTime;

        if (simulatedDownloadedBytes >= simulatedTotalBytes)
        {
            simulatedDownloadedBytes = simulatedTotalBytes;
            StopSimulation();
            return;
        }

        // 调用更新UI方法
        OnDownLoadResAndUpdateUI(0f, (ulong)simulatedDownloadedBytes, (ulong)simulatedTotalBytes, simulationStartTime);
    }
#endif
}



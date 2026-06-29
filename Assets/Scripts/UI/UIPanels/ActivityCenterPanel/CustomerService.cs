using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Game.COSXML;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Networking;

public class CustomGlobalConfigData
{
    /// <summary>
    /// 是否在登陆显示审核按钮。 是否展示Demo按钮 0 否1 是
    /// </summary>
    public int isShowDemo;
    public string customerServiceUrl;
    public int[] showChannel;//渠道审核中
}

[Serializable]
public class CustomGlobalConfigRes
{
    public int result;
    public CustomGlobalConfigData data;
    public string rmsg;
}

public class CustomerService : MonoBehaviour
{
    /// <summary>与 Logger 本地文件一致：yyyyMMddHHmmss.log（如 20260326130950.log）。</summary>
    private static readonly Regex s_dateNamedLog = new Regex(@"^\d{14}\.log$", RegexOptions.Compiled);

    [SerializeField] private CButton Btn_Service;
    private static string url = string.Empty;

    void Start()
    {
        Btn_Service.onClick.RemoveAllListeners();
        Btn_Service.onClick.AddListener(OnBtnServiceClick);
        gameObject.SetActive(false);

        var localUrl = GetCustomUrl();
        if (!string.IsNullOrEmpty(localUrl))
        {
            url = localUrl;
        }
        if (!string.IsNullOrEmpty(url))
        {
            gameObject.SetActive(true);
            return;
        }
        //gameObject.SetActive(true);
        CoroutineManager.Inst.StartCoroutine(ConfigoRequest(GetConfigSuccess, GetConfigFail));

        //ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        //req.idList = new List<string>() { "1" };
        //NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
        //    {
        //        ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);

        //        //客服数据
        //        if (!string.IsNullOrEmpty(activityResponse.customerServiceUrl))
        //        {
        //            url = activityResponse.customerServiceUrl;
        //            gameObject.SetActive(true);
        //        }
        //    },
        //    (error) => { Debug.LogError("获取客服地址错误!"); });
    }
    private IEnumerator GetConfigSuccess(string content)
    {
        Debug.Log($"获取配置信息 {content}");
        var resInfo = JsonConvert.DeserializeObject<CustomGlobalConfigRes>(content);
        if (resInfo != null)
        {
            url = resInfo.data.customerServiceUrl;
            if(!string.IsNullOrEmpty(url))
            {
                gameObject.SetActive(true);
            }else
            {
                gameObject.SetActive(false);
            }
          
        }else
        {
       
        }
        yield break;
    }

    private IEnumerator GetConfigFail(string fail)
    {
        gameObject.SetActive(false);
        Debug.LogError("获取客服地址错误!");
        yield break;
    }
    IEnumerator ConfigoRequest(Func<string, IEnumerator> successAct, Func<string, IEnumerator> failAct)
    {
        string url = "https://api.budapp.cn/configuration/appConfig";;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.timeout = 10;
            //foreach (var element in reqeustHeader)
            //{
            //    webRequest.SetRequestHeader(element.Key, element.Value);
            //}

            webRequest.SetRequestHeader("device", "");
            webRequest.SetRequestHeader("platform", "U3D");
            webRequest.SetRequestHeader("quality", "0");//1:Low 0:High
            webRequest.SetRequestHeader("contentType", "application/json");
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
                    yield return failAct?.Invoke(webRequest.result + "-----" + webRequest.error);
                }
                else
                {
                    yield return successAct?.Invoke(webRequest.downloadHandler.text);
                }
            }
            else
            {
                yield return failAct?.Invoke(webRequest.error);
            }
        }
    }
    private string GetCustomUrl()
    {
        if (!DeviceInfoManager.Inst.CheckVersion_1_0_19())
        {
            return "";
        }
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        foreach (var assembly in assemblies)
        {
            if (assembly.FullName.Contains("xasset"))
            {
                Type t = assembly.GetType("GlobalConfigManager");
                if (t != null)
                {
                    var property = t.GetProperty("CustomerServiceUrl", BindingFlags.Public | BindingFlags.Instance);
                    if (property != null && property.PropertyType == typeof(string))
                    {
                        object instance = GlobalConfigManager.Instance;
                        return (string)property.GetValue(instance);
                    }

                    return ""; // 默认值（如果属性不存在）

                }
            }
        }
        return "";
    }

    private void OnBtnServiceClick()
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("[CustomerService] customerServiceUrl empty");
            return;
        }
        OpenCustomerWebView();
        StartCoroutine(UploadLocalLogsThenOpenWebView());
    }

    /// <summary>
    /// 将最外层 Logs 目录下<strong>最新的一个</strong> .log 上传到腾讯云日志桶，完成后打开客服页。
    /// </summary>
    private IEnumerator UploadLocalLogsThenOpenWebView()
    {
        Debug.Log("正在上传本地日志…");
        CosXmlUploadManager.Setup();

        var latestLog = GetLatestLogFilePathUnderOuterLogs();
        if (string.IsNullOrEmpty(latestLog))
        {
            Debug.Log("暂无本地日志文件");
            yield break;
        }

        // Logger 子线程占用日志文件时直接读会 Sharing violation，先以 ReadWrite 共享读快照到临时文件再上传
        if (!TrySnapshotLogForUpload(latestLog, out var uploadPath, out var snapErr))
        {
            Debug.LogError($"[CustomerService] log snapshot failed: {snapErr}");
            yield break;
        }

        try
        {
            var uid =  "guest";
            if(AccountDataManager.Inst != null && AccountDataManager.Inst.UserInfo != null && !string.IsNullOrEmpty(AccountDataManager.Inst.UserInfo.username))
            {
                uid = AccountDataManager.Inst.UserInfo.username;
                if(!string.IsNullOrEmpty(AccountDataManager.Inst.UserInfo.nickname))
                {
                    uid += "_" + DataUtil.RemoveRichTextAndEmoji(AccountDataManager.Inst.UserInfo.nickname);
                }
            }
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = Path.GetFileName(latestLog);
            var cosKey = $"client-log/{uid}/{ts}";

            var done = false;
            string err = null;
            CosXmlUploadManager.UploadFileToClientLogBucket(cosKey, uploadPath, (_, e) =>
            {
                err = e;
                done = true;
            });

            while (!done)
                yield return null;

            if (!string.IsNullOrEmpty(err))
            {
                Debug.LogError($"[CustomerService] log upload failed: {err}");
                Debug.LogError("日志上传失败");
            }
            else
                Debug.Log("日志上传完成");
        }
        finally
        {
            try
            {
                if (!string.IsNullOrEmpty(uploadPath) && File.Exists(uploadPath))
                    File.Delete(uploadPath);
            }
            catch
            {
                // ignored
            }
        }
    }

    /// <summary>
    /// 日志文件可能被 <see cref="LogHandler"/> 占用；用 Read+ReadWrite 共享打开并复制到临时路径，供上传独占读取。
    /// </summary>
    private static bool TrySnapshotLogForUpload(string sourcePath, out string snapshotPath, out string error)
    {
        snapshotPath = null;
        error = null;
        try
        {
            var baseDir = Application.temporaryCachePath;
            if (string.IsNullOrEmpty(baseDir))
                baseDir = Path.GetTempPath();
            snapshotPath = Path.Combine(baseDir, $"bud_customer_service_log_{Guid.NewGuid():N}.log");

            using (var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var dst = new FileStream(snapshotPath, FileMode.Create, FileAccess.Write, FileShare.None))
                src.CopyTo(dst);

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            if (!string.IsNullOrEmpty(snapshotPath))
            {
                try
                {
                    if (File.Exists(snapshotPath))
                        File.Delete(snapshotPath);
                }
                catch
                {
                    // ignored
                }
            }

            snapshotPath = null;
            return false;
        }
    }

    private void OpenCustomerWebView()
    {
        LoggerUtils.LogFormat($"$[WebView] open webView: {url}");
        var jb = new JObject
        {
            ["url"] = url
        };
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.openWebview, JsonConvert.SerializeObject(jb));
    }

    /// <summary>
    /// 生成日志目录（复刻 LogHandler.logPath 的规则，避免在 AOT 环境取不到 LogHandler 类型）。
    /// </summary>
    private static string GetOuterLogsDirectory()
    {
        const string logDirName = "Logs";

        // LogHandler 在非 Editor 下：Application.persistentDataPath + "/Logs"
        // Editor 下：返回相对路径 "Logs"（这里补成绝对路径方便枚举）
        if (!Application.isEditor)
            return Path.Combine(Application.persistentDataPath, logDirName);

        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), logDirName));
    }

    /// <summary>在 <see cref="LogHandler.logPath"/> 对应目录下，文件名为 14 位日期时间的 .log 中最新一份（按文件名时间降序）。</summary>
    private static string GetLatestLogFilePathUnderOuterLogs()
    {
        var logDir = GetOuterLogsDirectory();
        if (!Directory.Exists(logDir))
            return null;

        return Directory.GetFiles(logDir, "*.log", SearchOption.TopDirectoryOnly)
            .Where(p => File.Exists(p) && s_dateNamedLog.IsMatch(Path.GetFileName(p)))
            .OrderByDescending(p => Path.GetFileNameWithoutExtension(p), StringComparer.Ordinal)
            .FirstOrDefault();
    }
}

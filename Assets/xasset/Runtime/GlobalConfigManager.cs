using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

[Serializable]
public class GlobalConfigData
{
    /// <summary>
    /// 是否在登陆显示审核按钮。 是否展示Demo按钮 0 否1 是
    /// </summary>
    public int isShowDemo;
    public string customerServiceUrl;
    public int[] showChannel;//渠道审核中
}

[Serializable]
public class GlobalConfigRes
{
    public int result;
    public GlobalConfigData data;
    public string rmsg;
}

public enum ChannelId
{
    Official = 2,
    Vivo = 3,
    Oppo = 4,
    Honor = 5,
    Huawei = 6,
    Tencent = 7,
    BiliBili = 8,
    GameCenter = 9,
    Xiaomi = 10,
    Douyin = 11,
    KuaiShou = 12,
    ChongChong = 13,
    TapTap = 14,
    HaoyouKuaiBao = 15,
    Unknown = 0
}
/// <summary>
/// 全局配置接口
/// </summary>
public class GlobalConfigManager : MonoBehaviour
{
    private static GlobalConfigManager instance;

    private GlobalConfigManager() { }

    public static GlobalConfigManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindObjectOfType<GlobalConfigManager>();
                if (instance == null)
                {
                    instance = new GameObject("GlobalConfigManager").AddComponent<GlobalConfigManager>();
                    GameObject.DontDestroyOnLoad(instance.gameObject);
                }
            }
            return instance;
        }
    }
    
    private const int FailMax = 3;
    private int FailCount = 0;

    /// <summary>
    /// 当前后端返回数据
    /// </summary>
    private GlobalConfigData ConfigData;
    /// <summary>
    /// 加载完成Action, true 有后端返回数据， false 重试三次均失败
    /// </summary>
    public Action<bool> Complete;

    public bool IsGetConfigData
    {
        get
        {
            return ConfigData != null;
        }
    }

    public bool IsShowAppleAudit
    {
        get
        {
            if (ConfigData == null)
            {
                return false;
            }
            return ConfigData.isShowDemo == 1;
        }
    }
    public bool IsHuaweiAudit
    {
        get
        {
            return IsChannelAudit(ChannelId.Huawei);
        }
    }

    public string CustomerServiceUrl
    {
        get
        {
            if(ConfigData == null)
            {
                return "";
            }
            return ConfigData.customerServiceUrl;
        }
    }

    public bool IsChannelAudit(ChannelId channel)
    {
        if (ConfigData == null || ConfigData.showChannel == null || ConfigData.showChannel.Length == 0)
        {
            return false;
        }
        for (int i = 0; i < ConfigData.showChannel.Length; i++)
        {
            if (ConfigData.showChannel[i] == (int)channel)
            {
                return true;
            }
        }
        return false;
    }

    private Dictionary<string, string> reqeustHeader = new Dictionary<string, string>();
    public void Init(Dictionary<string, string> header)
    {
        if (header == null || header.Keys.Count == 0)
        {
            return;
        }

        reqeustHeader = header;
    }

    /// <summary>
    /// 防止无网启动时，拉取配置失败，玩家没有杀进程，重新联网，下载完资源后卡死，此时需要重新请求一下配置。
    /// </summary>
    public void ReRequst()
    {
        if(FailCount >= FailMax)
        {
            FailCount = 0;
            Refresh();
        }
    }
    /// <summary>
    /// 刷新
    /// </summary>
    public void Refresh()
    {
        StartCoroutine(ConfigoRequest(GetConfigSuccess, GetConfigFail));
    }

    private bool isProdEnvironment
    {
        get
        {
            var key = "environment";
            if (!reqeustHeader.Keys.Contains(key))
            {
                return true;
            }

            return reqeustHeader[key] == "prod";
        }
    }

    private string RequestUrlPath
    {
        get
        {
#if PACKAGE_TYPE_US
            if (isProdEnvironment)
            {
                return "https://global.joinbudapp.com/configuration/appConfig";
            }
            else
            {
                return "https://global.joinbudapp.com/configuration/appConfig";
            }
#else
            if (isProdEnvironment)
            {
                return "https://api.budapp.cn/configuration/appConfig";
            }
            else
            {
                return "https://api-test.budapp.cn/configuration/appConfig";
            }
#endif
        }
    }

    private IEnumerator GetConfigSuccess(string content)
    {
        Debug.Log($"获取配置信息 {content}");
        var resInfo = JsonConvert.DeserializeObject<GlobalConfigRes>(content);
        if (resInfo != null)
        {
            if (resInfo.result == 0)
            {
                ConfigData = resInfo.data;
                FailCount = 0;
                Complete?.Invoke(true);
            }
            else
            {
                FailCount = FailMax;
            }
        }
        yield break;
    }
    
    private IEnumerator GetConfigFail(string fail)
    {
        Debug.LogError($"获取热更信息错误{fail}");
        if(++FailCount < FailMax)
        {
            yield return ConfigoRequest( GetConfigSuccess, GetConfigFail);
        }
        else
        {
            Complete?.Invoke(false);
            Debug.LogError("达到三次重试上限，拉去配置失败。");
        }
    }
    
    IEnumerator ConfigoRequest(Func<string, IEnumerator> successAct, Func<string, IEnumerator> failAct)
    {
        string url = RequestUrlPath;
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            webRequest.timeout = 10;
            foreach (var element in reqeustHeader)
            {
                webRequest.SetRequestHeader(element.Key, element.Value);
            }
            
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
}

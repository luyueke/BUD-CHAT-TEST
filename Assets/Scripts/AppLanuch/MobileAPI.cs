using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 与热更中设备信息需要区分开
/// </summary>
public class AppDeviceRes
{
    public DeviceInfo deviceInfo;
    public bool isShowAppleLogin;
    /// <summary>
    ///  是否显示渠道登录
    /// </summary>
    public bool isShowChannelLogin; 
}

public class DeviceInfo
{
    /// <summary>
    /// 版本号
    /// </summary>
    public string version;

    /// <summary>
    /// 设备uuid
    /// </summary>
    public string uuid;

    /// <summary>
    /// 请求平台, 从端上获取后替换为u3d
    /// </summary>
    public string platform;

    /// <summary>
    /// 当前设备的平台 android / ios
    /// </summary>
    public string mobile;

    /// <summary>
    /// 环境 master / pr / prod
    /// </summary>
    public string environment;


    public Dictionary<string, string> headerInfo
    {
        get
        {
            var header = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(version))
            {
                header.Add("version", version);
            }
            if (!string.IsNullOrEmpty(uuid))
            {
                header.Add("uuid", uuid);
            }
            if (!string.IsNullOrEmpty(platform))
            {
                header.Add("platform", platform);
            }
            if (!string.IsNullOrEmpty(mobile))
            {
                header.Add("mobile", mobile);
            }
            if (!string.IsNullOrEmpty(environment))
            {
                header.Add("environment", environment);
            }

            return header;
        }
    }
}

public class MobileResponse
{
    public int isSuccess;
    public string data;
    public string funcName;
}

public class MobileAPI : MonoBehaviour
{
    private static MobileAPI instance;

    public static MobileAPI Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(typeof(MobileAPI).Name);
                instance = go.AddComponent<MobileAPI>();
            }

            return instance;
        }
    }

    private static AndroidJavaObject javaObject;
    
#if PACKAGE_TYPE_US
    private static readonly string
        androidClassPath = "com.pointone.buddyglobal.feature.unity.view.UnityPlayerActivity";
#else
    private static readonly string
        androidClassPath = "cn.budapp.biyoudideshijie.feature.unity.view.UnityPlayerActivity";
#endif

    public Dictionary<string, UnityAction<string>> onClientRespose = new Dictionary<string, UnityAction<string>>();
    public Dictionary<string, UnityAction<string>> onClientFail = new Dictionary<string, UnityAction<string>>();


    public void AddClientRespose(string key, UnityAction<string> callback)
    {
        if (onClientRespose.ContainsKey(key))
        {
            onClientRespose.Remove(key);
        }

        onClientRespose.Add(key, callback);
    }

    public void DelClientResponse(string key)
    {
        if (onClientRespose.ContainsKey(key))
        {
            onClientRespose.Remove(key);
        }
    }

    public void AddClientFail(string key, UnityAction<string> callback)
    {
        if (onClientFail.ContainsKey(key))
        {
            onClientFail.Remove(key);
        }

        onClientFail.Add(key, callback);
    }

    public void ReceiveMessageFromClient(string msg)
    {
        Debug.Log("ReceiveMessageFromClient msg = " + msg);
        MobileResponse response = JsonConvert.DeserializeObject<MobileResponse>(msg);
        if (response.isSuccess == 1)
        {
            if (onClientRespose.ContainsKey(response.funcName))
            {
                onClientRespose[response.funcName]?.Invoke(response.data);
            }
        }
        else
        {
            if (onClientFail.ContainsKey(response.funcName))
            {
                onClientFail[response.funcName]?.Invoke(response.data);
            }
        }
    }

    public void SendMessage(string funcName, string data)
    {
#if UNITY_ANDROID
        AndroidInterfaceCall(funcName, data);
#elif UNITY_IPHONE
        IOSInterface.sendMessageToClient(funcName, data);
#endif
    }
#if UNITY_ANDROID
    public static void AndroidInterfaceCall(string funcName, string msg)
    {
#if UNITY_STANDARD_BUILD
        return;
#endif
        try
        {
            var jo = GetObject();
            jo.Call(funcName, msg);
        }
        catch(Exception e)
        {
            Debug.LogError($"Unity Error = Failed to invoke Interface Android Call err: {e}");
        }

    }
    
    public static AndroidJavaObject GetObject()
    {
#if UNITY_STANDARD_BUILD
        return null;
#endif
        if (javaObject == null)
        {
            AndroidJavaClass jc = new AndroidJavaClass(androidClassPath);
            javaObject = jc.CallStatic<AndroidJavaObject>("getInstance");
        }
        return javaObject;
    }
#endif
}
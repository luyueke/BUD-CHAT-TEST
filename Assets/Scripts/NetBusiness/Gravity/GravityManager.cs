using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Version = System.Version;

/// <summary>
/// 引力引擎数据上报管理类
/// </summary>
public class GravityManager : GlobalInstance<GravityManager>
{

#if UNITY_ANDROID || UNITY_EDITOR
    static string accessToken = "qD7btmhYsxpk6nFkXcdryfcStaeMi8vz";
    static string clientId = "29546730";
#else
    static string accessToken = "jafNpYVeX4iBC0jhL3scwmWToAtqbc7F";
    static string clientId = "default_placeholder";
#endif


    private Type gravityAPI;
    private Type gravityEngineAPI;
    private bool isFinded = false;

    public void StartEngine()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        clientId = SystemInfo.deviceUniqueIdentifier;
#endif
        Debug.Log($"gravity StartEngine {accessToken},clientId={clientId},version={DeviceInfoManager.Inst.DeviceBaseData.version}");

        if (!Check())
        {
            return;
        }

        MethodInfo method = gravityAPI.GetMethod("StartEngine", new Type[] { typeof(string) , typeof(string) });
        if (method != null)
        {
            Debug.Log($"gravity  nvoke StartEngine");
            method.Invoke(null, new object[] { accessToken, clientId });
        }
    }

    /// <summary>
    /// 无参数的数据上报
    /// </summary>
    /// <param name="eventName">事件名</param>
    public void Track(string eventName)
    {
        #if UNITY_IOS
        Debug.Log($"gravity Track {eventName},version={DeviceInfoManager.Inst.DeviceBaseData.version}");

        if (!Check())
        {
            return;
        }

        MethodInfo method = gravityEngineAPI.GetMethod("Track",new Type[] { typeof(string) });
        if(method != null)
        {
            Debug.Log($"gravity run here,invoke  method, {eventName}");
            method.Invoke(null, new object[] { eventName });
        }
#endif
    }
    /// <summary>
    /// 带参数的数据上报
    /// </summary>
    /// <param name="eventName">事件名</param>
    /// <param name="properties">参数</param>
    public void Track(string eventName, Dictionary<string, object> properties)
    {
        #if UNITY_IOS
        Debug.Log($"gravity Track {eventName},version={DeviceInfoManager.Inst.DeviceBaseData.version}");

        if(!Check()) 
        {
            return;
        }

        MethodInfo method = gravityEngineAPI.GetMethod("Track", new Type[] { typeof(string),typeof(Dictionary<string,object>) });
        if (method != null)
        {
            Debug.Log($"gravity run here,invoke  method, {eventName}");
            method.Invoke(null, new object[] { eventName,properties });
        }
#endif
    }

    public bool Check()
    {
#if UNITY_IOS
        Debug.Log($"gravity check ,version={DeviceInfoManager.Inst.DeviceBaseData.version}");

        if(!CheckVersion()) //低版本没有接入引力引擎，不上报数据
        {
            return false;
        }

        Debug.Log($"gravity run here,get type");
        this.GetGravityAPI();
        if (gravityEngineAPI != null && gravityAPI != null)
        {
            return true;
        }
#endif
        return false;
    }

    /**-----巨量引擎相关 start------- */
    public void BDEventRegister(string method, bool isSuccess)
    {
#if UNITY_IOS
        if (!Check())
        {
            return;
        }

        JObject obj = new JObject()
        {
            ["method"] = method,
            ["isSuccess"] = isSuccess
        };

        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.onBDEventRegister, obj); 
#endif
    }

    public void BDEventV3(string eventName,JObject obj)
    {
#if UNITY_IOS
        if(!Check())
        {
            return;
        }
#if UNITY_ANDROID

        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.onBDEventV3, eventName,obj);
#endif
#endif
    }

    public void BDEventPurchase(string contentType, string contentName, string contentId, int contentNumber, string paymentChannel, string currency, bool isSuccess, int currencyAmount)
    {
#if UNITY_IOS
        if (!Check())
        {
            return;
        }
#if UNITY_ANDROID
        JObject obj = new JObject() {
            ["contentType"] = contentType,
            ["contentName"] = contentName,
            ["contentId"] = contentId,
            ["contentNumber"] = contentNumber,
            ["paymentChannel"] = paymentChannel,
            ["currency"] = currency,
            ["isSuccess"] = isSuccess,
            ["currencyAmount"] = currencyAmount,
        };

        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.onBDEventPurchase, obj);
#endif
#endif
    }


    /**-----巨量引擎相关 end------- */


    /// <summary>
    /// 通过反射，获取引力引擎API类
    /// </summary>
    private void GetGravityAPI()
    {
        if(gravityEngineAPI == null && !isFinded)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
           
            foreach(var assembly in assemblies)
            {
                //Debug.Log("assembly.name=" + assembly.FullName);
                if(assembly.FullName.Contains("GravityEngine"))
                {
                    Type t = assembly.GetType("GravityEngine.GravityEngineAPI");
                    if (t != null)
                    {
                        gravityEngineAPI = t;
                 
                    }
                }
                if(assembly.FullName.Contains("Assembly-CSharp"))
                {
                    Type t = assembly.GetType("GravityAPI");
                    if (t != null)
                    {
                        gravityAPI = t;
                       
                    }
                }
                if(gravityAPI != null && gravityEngineAPI != null)
                {
                    break;
                }
            }
        }
        isFinded = true;
    }

    /// <summary>
    /// 对比检查版本
    /// </summary>
    /// <returns>是否通过</returns>
    private bool CheckVersion()
    {
        if(CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.11") >= 0)
        {
            return true;
        }
        return false;
    }

    private int CompareVersion(string version1, string version2)
    {
        version1 = version1.Trim('v');
        version2 = version2.Trim('v');
        var versionValue1 = new Version(version1);
        var versionValue2 = new Version(version2);
        return versionValue1.CompareTo(versionValue2);
    }
}

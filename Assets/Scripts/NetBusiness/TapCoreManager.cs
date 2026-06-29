using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using TapSDK.Core;
using System;
using System.Reflection;

public class TapCoreManager: GlobalInstance<TapCoreManager>
{
    private const string ClientID = "ewb9eioz0bj5lhauk0";
    private const string Token = "VXvdbRtjeq6x73cSXa8eWVNIUc2FNKesfeHZxLUu";

    private int channelId;

    private Type tapTapEvent;

    /// <summary>
    /// 初始化，在用户同意隐私协议后调用
    /// </summary>
    public void InitAfterIfNeed(int channelId = -1)
    {
        this.channelId = channelId;
        if (!Check()) 
        {
            return;
        }

        //try
        //{
        //    SDKInit();
        //}catch(Exception e)
        //{
        //    Debug.LogError("TapCore Init throw Exception message=" + e.Message);
        //}

    }

    //private void SDKInit()
    //{
    //    // 核心配置
    //    TapTapSdkOptions coreOptions = new TapTapSdkOptions
    //    {
    //        // 客户端 ID，开发者后台获取
    //        clientId = ClientID,
    //        // 客户端令牌，开发者后台获取
    //        clientToken = Token,
    //        // 地区，CN 为国内，Overseas 为海外
    //        region = TapTapRegionType.CN,
    //        // 语言，默认为 Auto，默认情况下，国内为 zh_Hans，海外为 en
    //        preferredLanguage = TapTapLanguageType.zh_Hans,
    //        // 游戏版本号，如果不传则默认读取应用的版本号
    //        gameVersion = DeviceInfoManager.Inst.DeviceBaseData.version,
    //        // CAID，仅国内 iOS
    //        caid = "000-000-0000-00000",
    //        // 是否开启广告商 ID 收集，默认为 false
    //        enableAdvertiserIDCollection = false,
    //        // OAID证书, 仅 Android，用于上报 OAID 仅 [TapTapRegion.CN] 生效
    //        oaidCert = null,
    //        // 是否开启日志，Release 版本请设置为 false
    //        enableLog = true,
    //        // 是否禁用 OAID 反射，默认为 true
    //        disableReflectionOAID = true
    //    };

    //    //数据分析相关配置
    //    TapTapEventOptions eventOptions = new TapTapEventOptions
    //    {
    //        // 渠道，如 AppStore、GooglePlay
    //        channel = "taptap",
    //        // 初始化时传入的自定义参数，会在初始化时上报到 device_login 事件
    //        propertiesJson = "{\"device_login_custom_key\": \"BUD\"}",
    //        // 是否能够覆盖内置参数，默认为 false
    //        overrideBuiltInParameters = false,
    //        // 是否开启自动上报 IAP 事件
    //        enableAutoIAPEvent = true,
    //        // 是否禁用自动上报设备登录事件，默认为 false
    //        disableAutoLogDeviceLogin = false
    //    };


    //    TapTapSdkBaseOptions[] otherOptions = new TapTapSdkBaseOptions[]
    //    {
    //        eventOptions,
    //        // ... 其他模块配置项
    //    };
    //    TapTapSDK.Init(coreOptions, otherOptions);
    //}

    public void Login(string uid)
    {
        if (!Check())
        {
            return;
        }
        if (string.IsNullOrEmpty(uid))
        {
            return;
        }
        //if (tapTapEvent == null)
        //{
        //    Debug.LogWarning("tapCore: TapTapEvent type is null, cannot invoke SetUserID");
        //    return;
        //}
        Debug.Log($"tapCore ,invoke  SetUserID uid= {uid}");
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.taptapSetUserId, uid);

        //string  propertiesJson = "{\"device_login_custom_key\": \"BUD\"}";

        ////TapTapEvent.SetUserID(uid, propertiesJson);

        //MethodInfo method = tapTapEvent.GetMethod("SetUserID", BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) ,typeof(string)}, null);
        //if (method != null)
        //{
        //    try
        //    {
        //        Debug.Log($"tapCore ,invoke  SetUserID uid= {uid}");
        //        method.Invoke(null, new object[] { uid,propertiesJson });
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogError($"tapCore: Failed to invoke SetUserID, error: {e.Message}");
        //    }
        //}
        //else
        //{
        //    Debug.LogWarning("tapCore: SetUserID method not found");
        //}
    }

    public void ClearUser()
    {
        if (!Check())
        {
            return;
        }
        //if (tapTapEvent == null)
        //{
        //    Debug.LogWarning("tapCore: TapTapEvent type is null, cannot invoke ClearUser");
        //    return;
        //}
        //  TapTapEvent.ClearUser();
        Debug.Log($"tapCore ,invoke  ClearUser ");
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.taptapClearUser, "");


        //MethodInfo method = tapTapEvent.GetMethod("ClearUser", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
        //if (method != null)
        //{
        //    try
        //    {
        //        Debug.Log($"tapCore ,invoke  ClearUser ");
        //        method.Invoke(null, null);
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.LogError($"tapCore: Failed to invoke ClearUser, error: {e.Message}");
        //    }
        //}
        //else
        //{
        //    Debug.LogWarning("tapCore: ClearUser method not found");
        //}

    }

    private bool Check()
    {
        if (!CheckVersion()) //低版本，无此SDK，跳过
        {
            return false;
        }
        if (channelId != 14) //只有taptap渠道才需要初始化
        {
            return false;
        }

        return true;
        //if (tapTapEvent == null )
        //{
        //    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        //    foreach (var assembly in assemblies)
        //    {
        //        if (assembly.FullName.Contains("TapSDK.Core.Runtime"))
        //        {
        //            Type t = assembly.GetType("TapSDK.Core.TapTapEvent");
        //            if (t != null)
        //            {
        //                tapTapEvent = t;
        //            }
        //        }
        //    }
        //}

        //return tapTapEvent != null;
    }
    private bool CheckVersion()
    {
        if (CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.17") >= 0)
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


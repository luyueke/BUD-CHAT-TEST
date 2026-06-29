using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class DeviceRes
{
    public DeviceData deviceInfo;
    public OldUserInfo oldUserInfo;
    public bool isShowAppleLogin;
    /// <summary>
    /// 是否显示taptap登陆，unity热更不好确定sdk支持版本。从客户端拿
    /// </summary>
    public int isShowTaptapLogin;
    /// <summary>
    ///  是否显示渠道登录
    /// </summary>
    public bool isShowChannelLogin;
}

public class OldUserInfo
{
    public string uid;
    public string token;
}

[Serializable]
public class DeviceData
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

    ///设备型号
    public string generation;

    public string lang;//语言码
    
    public Dictionary<string, string> HttpRequestHeader()
    {
        platform = "u3d";
#if PACKAGE_TYPE_US
        //TODO:@Jaywill 海外服暂时写死语言码
        lang = LangCode.en.ToString();
#endif
        var result = new Dictionary<string, string>();

        var fieldInfos = GetType().GetFields();
        foreach (var fieldInfo in fieldInfos)
        {
            var fieldValue = fieldInfo.GetValue(this);
            if (fieldValue == default)
            {
                continue;
            }
            result[fieldInfo.Name] = fieldValue.ToString();
        }
        return result;
    }

    public GameEnvironment currentEnvironment()
    {
        if (string.IsNullOrEmpty(environment))
        {
            return GameEnvironment.PROD;
        }

        if (environment == "master")
        {
            return GameEnvironment.MASTER;
        }
        else if (environment == "pr")
        {
            return GameEnvironment.ALPHA;
        }
        else if (environment == "dev")
        {
            return GameEnvironment.DEV;
        }

        return GameEnvironment.PROD;
    }

    public int CompareVersion(string version1, string version2)
    {
        version1 = version1.Trim('v');
        version2 = version2.Trim('v');
        var versionValue1 = new Version(version1);
        var versionValue2 = new Version(version2);
        return versionValue1.CompareTo(versionValue2);
    }

    public int CompareVersion(string oldVersion)
    {
        string version1 = version.Trim('v');
        string version2 = oldVersion.Trim('v');
        var versionValue1 = new Version(version1);
        var versionValue2 = new Version(version2);
        return versionValue1.CompareTo(versionValue2);
    }
}

public enum GameEnvironment
{
    DEV = 0,
    MASTER = 1,
    ALPHA = 2,
    PROD  = 3,
}

public class DeviceInfoManager : GlobalInstance<DeviceInfoManager>
{
    public Action InfoDidUpdate;

    private bool _isShowAppleLogin = false;
    private bool _isShowChannelLogin = false;
    private bool _isShowTaptap = false;

    /// <summary>
    /// 是否显示apple login
    /// </summary>
    public bool ShowAppleLogin
    {
        get { return _isShowAppleLogin; }
    }

    /// <summary>
    /// 是否显示taptap登陆， 国服 S5 添加，之前的不显示
    /// </summary>
    public bool ShowTaptapLogin
    {
        get
        {
            return _isShowTaptap;
        }
    }

    public bool ShowChannelLogin
    {
        get
        {
            return _isShowChannelLogin;
        }
    }

    private GameEnvironment _currentEnvironment = GameEnvironment.PROD;

    /// <summary>
    /// 当前程序运行环境
    /// </summary>
    public GameEnvironment Environment
    {
        get { return _currentEnvironment; }
    }

    private DeviceData _deviceData;

    public DeviceData DeviceBaseData
    {
        get { return _deviceData; }
    }

    private OldUserInfo _oldUserInfo;

    public OldUserInfo OldUserInfo
    {
        get { return _oldUserInfo; }
    }

    public void Init()
    {
        RefreshDeviceInfo();
    }

    public void RefreshDeviceInfo(Action UpdateAction = null)
    {
        _deviceData = defaultRes().deviceInfo;
        InfoDidUpdate = UpdateAction;
#if UNITY_EDITOR
        var defaultStr = JsonConvert.SerializeObject(defaultRes());
        onGetBaseInfo(defaultStr);
        return;
#endif
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.GetBaseInfo,
            (response) => onGetBaseInfo(response));
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.GetBaseInfo, "");
    }

    private void onGetBaseInfo(string res)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.GetBaseInfo);

        var rspData = JsonConvert.DeserializeObject<DeviceRes>(res);
        if (rspData != null)
        {
            _deviceData = rspData.deviceInfo;
            _isShowAppleLogin = rspData.isShowAppleLogin;
            _isShowTaptap = rspData.isShowTaptapLogin == 1;
            _isShowChannelLogin = rspData.isShowChannelLogin;
            _currentEnvironment = rspData.deviceInfo.currentEnvironment();
            _oldUserInfo = rspData.oldUserInfo;
        }

        InfoDidUpdate?.Invoke();
    }

    public bool IsOldUsUser()
    {
        return _oldUserInfo != null && !string.IsNullOrEmpty(_oldUserInfo.uid) &&
                !string.IsNullOrEmpty(_oldUserInfo.token);
    }

    public void RemoveOldUserInfo()
    {
        _oldUserInfo = null;
    }

    /// <summary>
    /// 模拟器数据
    /// </summary>
    /// <returns></returns>

    private static DeviceRes defaultRes()
    {
        DeviceData device = new DeviceData();
        device.environment = "prod";
        device.version = "1.0.0";
        device.uuid = "1815301222400552960";
        device.platform = "u3d";
        device.mobile = "ios";

#if UNITY_EDITOR
        {
            // device.environment = DebugSetting.Inst.environment.ToString().ToLower();
            // device.version = DebugSetting.Inst.version;
            // device.uuid = "unityDefaultUUID";
            // device.platform = "u3d";
            // device.mobile = DebugSetting.Inst.mobile.ToString().ToLower();
        }
#endif
        
        var resData = new DeviceRes();
        resData.deviceInfo = device;
        resData.isShowAppleLogin = true;
        return resData;
    }

    public bool CheckVersion_is_1_0_14()
    {
        if (CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.14") == 0)
        {
            return true;
        }
        return false;
    }

    public bool CheckVersion_1_0_14()
    {
        if (CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.14") >= 0)
        {
            return true;
        }
        return false;
    }

    public bool CheckVersion_1_0_18()
    {
        if (CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.18") >= 0)
        {
            return true;
        }
        return false;
    }
    public bool CheckVersion_1_0_19()
    {
#if UNITY_EDITOR
        return true;
#endif
        if (CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.19") >= 0)
        {
            return true;
        }
        return false;
    }

    public bool CheckVersion_1_0_20()
    {
#if UNITY_EDITOR
        return true;
#endif
        if (CompareVersion(DeviceInfoManager.Inst.DeviceBaseData.version, "1.0.20") >= 0)
        {
            return true;
        }
        return false;
    }
    public bool CheckVersion_1_0_20_Update()
    {
//#if UNITY_EDITOR
//        return true;
//#endif
       if(string.IsNullOrEmpty(xasset.Assets.HotUpdateVersion))
        {
            return false;
        }
        if (CompareVersion(xasset.Assets.HotUpdateVersion, "1.0.20") >= 0)
        {
            return true;
        }
        return false;
    }
 
    private int CompareVersion(string version1, string version2)
    {
        version1 = version1.Trim('v');
        version2 = version2.Trim('v');
        var versionValue1 = new System.Version(version1);
        var versionValue2 = new System.Version(version2);
        return versionValue1.CompareTo(versionValue2);
    }
}

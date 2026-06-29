using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using ThinkingData.Analytics;

public class AnalyticsManager: GlobalInstance<AnalyticsManager>
{ 
#if PACKAGE_TYPE_US
    private const string APPID = "ecfb996b20b8445bbf9d6afa2b95d7a3";
#else
    private const string APPID = "2b932dafe1b64561b63937fe5577d3da";
#endif
   
    private const string SERVER = "https://global-receiver-ta.thinkingdata.cn";
    private static List<string> budOrderIds = new List<string>();
    /// <summary>
    /// 初始化，在用户同意隐私协议后调用
    /// </summary>
    public void InitAfterIfNeed(int channelId = -1)
    {
        // TDAnalytics.Init(APPID, SERVER);
        // Dictionary<string, object> superProperties = new Dictionary<string, object>();
        // superProperties["channel"] = GetChannelName(channelId);
        // TDAnalytics.SetSuperProperties(superProperties);
        // TDAutoTrackEventType trackEventType = TDAutoTrackEventType.AppStart | TDAutoTrackEventType.AppEnd |
        //                                       TDAutoTrackEventType.AppInstall;
        // TDAnalytics.EnableAutoTrack(trackEventType);

        // try
        // {    
        //     TDAnalytics.CalibrateTimeWithNtp("time.apple.com");
        // }
        // catch (System.Exception e)
        // {
        //     Debug.LogError("TDAnalytics.CalibrateTimeWithNtp " + e.StackTrace);
        // }
    }

    private string GetChannelName(int channelId)
    {
        switch (channelId)
        {
            case -1:
                return "ios";
            case 2 :
                return "官服";
            case 3:
                return "vivo";
            case 4:
                return "oppo";
            case 5:
                return "honor";
            case 6:
                return "huawei";
            case 7:
                return "tencent";
            case 8:
                return "bilibili";
            case 9:
                return "4399";
            case 10:
                return "xiaomi";
            case 11:
                return "douyin";
            case 12:
                return "kuaishou";
            case 13:
                return "cc";
            case 14:
                return "taptap";
            default:
                return "unknown";
        }
    }

    /// <summary>
    /// 登陆后调用，必须调用。
    /// </summary>
    /// <param name="uid"></param>
    public void Login(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            return;
        }
    }

    public string DeviceId
    {
        get
        {
            return "";
        }
    }

    public void Logout()
    {
    }

    /// <summary>
    /// 设置用户信息
    /// </summary>
    /// <param name="properties"></param>
    public void UserSet(Dictionary<string, object> properties)
    {
    }
    
    /// <summary>
    /// 数据上报
    /// </summary>
    /// <param name="eventName">事件名</param>
    /// <param name="properties">参数</param>
    public void Track(string eventName, Dictionary<string, object> properties = null)
    {
#if UNITY_IOS
        if (eventName == AnalyticsEventName.TOP_UP_SUCCESS)
        {
            if(properties.TryGetValue("priceNum",out object num) && num is float num1 && num1 > 0)
            {
                properties.Add("$pay_amount", (int)(num1 * 100)); //转化为分
            }
            if (properties.TryGetValue("budOrderId", out object id))
            {
                properties.Add("$order_id", id);
            }
            if (properties.TryGetValue("method", out object m))
            {
                properties.Add("$pay_method", m);
            }
            
            GravityManager.Inst.Track("$PayEvent", properties);
        }
#endif
    }

    //public void UpdateOpenId(string openId)
    //{
    //    var dict = GetSuperProperties();
    //    dict.Add("open_id", openId ?? "");
    //    SetSuperProperties(dict);
    //}

    public void SetSuperProperties(Dictionary<string, object> superProperties) {
    }
    public Dictionary<string, object> GetSuperProperties()
    {
        var dict = new Dictionary<string, object>();
        if(dict == null)
        {
#if !UNITY_EDITOR
            Debug.LogError("数数获取不到公共属性");
#endif
            return new Dictionary<string, object>();
        }
        return dict;
    }
    public bool ContainOrderID(string budOrderID)
    {
        if (string.IsNullOrEmpty(budOrderID))
        {
            return false;
        }

        if (!budOrderIds.Contains(budOrderID))
        {
            budOrderIds.Add(budOrderID);
            return false;
        }
        return true;
    }
}

public class AnalyticsEventName
{
    /// <summary>
    /// 进入登陆页面
    /// </summary>
    public const string LANDINGPAGEVIEW = "LANDING_PAGE_VIEW";
    
    
    //进入新玩家登录
    public const string New_USER_LOGIN_IN_START = "New_USER_LOGIN_IN_START";
    
    //新玩家登录成功
    public const string New_USER_LOGIN_IN_SUCCESS = "New_USER_LOGIN_IN_SUCCESS";
    
    //所有玩家进入服务端登录
    public const string USER_LOGIN_IN = "USER_LOGIN_IN";
    
    /// <summary>
    /// 进入选性别页面
    /// </summary>
    public const string GENDERPAGEVIEW = "GENDER_PAGE_VIEW";
    
    /// <summary>
    /// 老玩家进入大厅
    /// </summary>
    public const string OLDUSER_GOTO_GAMEHALL = "OLDUSER_GOTO_GAMEHALL";
    
    /// <summary>
    /// 到大厅，注册完成
    /// </summary>
    public const string FINISHSIGNUP = "FINISH_SIGN_UP";

    /// <summary>
    /// 支付
    /// </summary>
    public const string TOP_UP_SUCCESS = "TOP_UP_SUCCESS";

    public const string AIYANDERE_DATA = "AIYANDERE_DATA";

    public const string AIBUDDY_CHAT = "AIBUDDY_CHAT";

    public const string AIHOSPITAL_DATA = "AIHOSPITAL_DATA";
    public const string AILARP = "AILARP";
    public const string GamePage = "GamePage";
    public const string AILARPGameHall = "AILARPGameHall";
    public const string UGCGameHall = "UGCGameHall";

    public const string LimitedTimePackage_Click = "LimitedTimePackage_Click";
    public const string LimitedTimePackage_Show = "LimitedTimePackage_Show";
    public const string LimitedTimePackage_Buy = "LimitedTimePackage_Buy";

    public const string LOGIN_GAMEHALL_Enter = "login_gamehall_enter";
}
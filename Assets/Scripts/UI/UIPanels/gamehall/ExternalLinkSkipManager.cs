using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class ExternalLinkSkipManager : GlobalInstance<ExternalLinkSkipManager>
{
    // 苹果appstore的活动跳转s7的母包里我们排上这个需求，需要跳转到赛季盲盒（活动1）+ 病娇游戏（活动2） 的跳转
    public enum UniversalLinkType
    {
        Unknown, // 未知
        SeasonBlindBox, // S7 赛季盲盒
        YandereGame, // s7 病娇游戏
    }

    private Action<UniversalLinkType> SkipAction;

    public UniversalLinkType ActivityType(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return UniversalLinkType.Unknown;
        }

        if (value == "SeasonBlindBox")
        {
            return UniversalLinkType.SeasonBlindBox;
        }
        else if (value == "YandereGame")
        {
            return UniversalLinkType.YandereGame;
        }

        return UniversalLinkType.Unknown;
    }

    public void RegisterUniversalLinkListener(Action<UniversalLinkType> skipAction)
    {
#if UNITY_IOS
        this.SkipAction = skipAction;
        LoggerUtils.Log("[UniversalLink] RegisterUniversalLinkListener");
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.universalLinkActivityInfo,
            HandleUniversalLinkActivity);
#endif
    }

    public void GetCurrentUniversalLinkInfo()
    {
#if UNITY_IOS
        MobileInterface.Instance.SendMessage(MobileInterfaceDefine.universalLinkActivityInfo);
#endif
        
#if UNITY_EDITOR
        // mockData();
#endif
    }

    private void mockData()
    {
        var activeInfo = new UniversalLinkActivityRes();
        activeInfo.lastPathComponent = "appStore";
        activeInfo.queryItems = new List<ActivityQueryItem>()
        {
            new ActivityQueryItem()
            {
                key = "id",
                value = "SeasonBlindBox"
            }
        };
        
        var jb = new JObject()
        {
            ["isSuccess"] = 1,
            ["funcName"] = "universalLinkActivityInfo",
            ["data"] = JsonConvert.SerializeObject(activeInfo)
        };
        
        var dataStr = JsonConvert.SerializeObject(jb);
        MobileInterface.Instance.ReceiveMessageFromClient(dataStr);
    }

    private void HandleUniversalLinkActivity(string msg)
    {
        LoggerUtils.Log($"[UniversalLink] recive message {msg}");
        if (string.IsNullOrEmpty(msg))
        {
            return;
        }
        LoggerUtils.Log($"[UniversalLink] recive message {msg}");

        UniversalLinkActivityRes getchannelIdResponse = JsonConvert.DeserializeObject<UniversalLinkActivityRes>(msg);
        if (getchannelIdResponse == null)
        {
            LoggerUtils.Log($"[UniversalLink] DeserializeObject faile");
            return;
        }
        
        LoggerUtils.Log($"[UniversalLink] DeserializeObject object {getchannelIdResponse}");

        var lastPath = getchannelIdResponse?.lastPathComponent;
        var infos = getchannelIdResponse?.queryItems;
        if (infos != null)
        {
            string key = "id";
            var queryItem = infos.Find(x => x.key == key);
            if (queryItem != null)
            {
                var activity = ActivityType(queryItem.value);
                LoggerUtils.Log($"[UniversalLink] SkipAction {activity}");
                SkipAction?.Invoke(activity);
            }
        }
    }
}

[Serializable]
public class UniversalLinkActivityRes
{
    /// 活动的component, 后面可以用来判断什么类型的跳转来源
    public string lastPathComponent;
    /// <summary>
    /// 透传的 query 信息
    /// </summary>
    public List<ActivityQueryItem> queryItems;
}

public class ActivityQueryItem
{
    public String key;
    public string value;
}

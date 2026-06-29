using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GravityEngine;
using GravitySDK.PC.Constant;



public class GravityAPI 
{


    public static void StartEngine(string accessToken, string clientId)
    {
        // 启动引力引擎
        GravityEngineAPI.StartGravityEngine(accessToken, clientId, GravityEngineAPI.SDKRunMode.NORMAL);

        // 原生app开启自动采集，并设置自定属性
        GravityEngineAPI.EnableAutoTrack(AUTO_TRACK_EVENTS.APP_ALL, new Dictionary<string, object>()
        {
            {"auto_track_key", "auto_track_value"} // 静态属性
        });

        Initialize(clientId);
    }

    public static void Initialize( string clientId)
    {
        Debug.Log("gravity initialize ");
#if UNITY_IOS && !UNITY_EDITOR
            // iOS原生应用注册
            GravityEngineAPI.InitializeIOS(false, "", "", true, "appstore", new InitializeCallbackImpl());
#else
        GravityEngineAPI.Initialize(clientId, "bud", 1, "", true, new InitializeCallbackImpl());
#endif
    }

    public static void ResetClientID(string newClientId)
    {
        GravityEngineAPI.ResetClientID(newClientId, new ResetClientIdCallbackImpl());
    }

    public static void DryRunEventWithCallback(string traceId, Dictionary<string, object> pParams)
    {
        GravityEngineAPI.DryRunEventWithCallback(traceId, pParams, new QueryDryRunCallbackImpl());
    }
}

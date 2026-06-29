using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AndroidInterface : GameInstance<AndroidInterface>
{
    static AndroidJavaObject javaObject;
#if PACKAGE_TYPE_US
    static readonly string androidClassPath = "com.pointone.buddyglobal.feature.unity.view.UnityPlayerActivity";
#else
    static readonly string androidClassPath = "cn.budapp.biyoudideshijie.feature.unity.view.UnityPlayerActivity";
#endif
    
    public static void Call(string funcName, string msg)
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
            LoggerUtils.Log($"Unity Error = Failed to invoke Interface Android Call err: {e}");
        }

    }

    // ½« JObject ×ª»»Îª AndroidJavaObject
    public static AndroidJavaObject ConvertJObjectToJava(JObject jsonObj)
    {
        string jsonString = jsonObj.ToString();
        using (AndroidJavaClass jsonClass = new AndroidJavaClass("org.json.JSONObject"))
        {
            return jsonClass.CallStatic<AndroidJavaObject>("new", jsonString);
        }
    }


    public static void Call(string funcName, JObject obj)
    {
#if UNITY_STANDARD_BUILD
            return;
#endif
        try
        {
            var jo = GetObject();
            
            jo.Call(funcName, ConvertJObjectToJava(obj));
        }
        catch (Exception e)
        {
            LoggerUtils.Log($"Unity Error = Failed to invoke Interface Android Call err: {e}");
        }
    }

    public static void Call(string funcName,string str, string obj)
    {
#if UNITY_STANDARD_BUILD
        return;
#endif
        try
        {
            var jo = GetObject();
            jo.Call(funcName,str, obj);
        }
        catch (Exception e)
        {
            LoggerUtils.Log($"Unity Error = Failed to invoke Interface Android Call err: {e}");
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

    public override void Release()
    {
        base.Release();
        javaObject = null;
    }
}

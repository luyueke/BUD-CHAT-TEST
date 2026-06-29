using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class ClientResponse
{
    public int isSuccess;
    public string data;
    public string funcName;
}

public class MobileInterface : MonoBehaviour
{
    private static MobileInterface instance;

    public static MobileInterface Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject(typeof(MobileInterface).Name);
                instance = go.AddComponent<MobileInterface>();
            }
            return instance;
        }
    }

    public static bool isInit = false;
    
    [HideInInspector]
    public Dictionary<string, UnityAction<string>> onClientRespose = new Dictionary<string, UnityAction<string>>();
    public Dictionary<string, UnityAction<string>> onClientFail = new Dictionary<string, UnityAction<string>>();
    
    public void Awake()
    {
        instance = this;
        isInit = true;
        DontDestroyOnLoad(gameObject);
    }
    private void OnDestroy()
    {
        instance = null;
        isInit = false;
    }
    
    public void AddClientRespose(string key, UnityAction<string> callback)
    {
        Debug.Log($"MobileInterface AddClientRespose key={key}");
        if (onClientRespose.ContainsKey(key))
        {
            onClientRespose.Remove(key);
        }
        onClientRespose.Add(key, callback);
    }

    public void DelClientResponse(string key)
    {
        Debug.Log($"MobileInterface DelClientResponse key={key}");
        if (onClientRespose.ContainsKey(key))
        {
            onClientRespose.Remove(key);
        }
    }
    
    public void AddClientFail(string key, UnityAction<string> callback)
    {
        DelClientFail(key);
        onClientFail.Add(key, callback);
    }
    
    public void DelClientFail(string key)
    {
        if (onClientFail.ContainsKey(key))
        {
            onClientFail.Remove(key);
        }
    }
    
    public void ReceiveMessageFromClient(string msg)
    {
        Debug.Log("ReceiveMessageFromClient msg = "+ msg);
        ClientResponse response = JsonConvert.DeserializeObject<ClientResponse>(msg);
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
        AndroidInterface.Call(funcName, data);
#elif UNITY_IPHONE
        Debug.LogError("打印一下ios发送数据输出:" + funcName + "      " + data);
        IOSInterface.sendMessageToClient(funcName, data);
#endif
    }

    public void SendMessage(string funcName, JObject data)
    {
#if UNITY_ANDROID
        AndroidInterface.Call(funcName, data.ToString());
#elif UNITY_IPHONE
        IOSInterface.sendMessageToClient(funcName, data.ToString());
#endif
    }

    public void SendMessage(string funcName, string str, JObject data)
    {
#if UNITY_ANDROID
        AndroidInterface.Call(funcName, str, data.ToString());
#elif UNITY_IPHONE
        IOSInterface.sendMessageToClient(funcName, data.ToString());
#endif
    }

    public Action<string> OpenDebugKeyboardUIAction;
    public Action<string> OpenDebugKeyboardWithEmoteUIAction;


    public void ShowKeyboard(string info)
    {
#if UNITY_EDITOR
        OpenDebugKeyboardUIAction?.Invoke(info);
        return;
#endif
        SendMessage(MobileInterfaceDefine.showKeyboard, info);
    }

    public void ShowEmoteKeyboard(string info,string npcID)
    {
#if UNITY_EDITOR
        OpenDebugKeyboardWithEmoteUIAction?.Invoke(npcID);
        return;
#endif
        SendMessage(MobileInterfaceDefine.showKeyboard, info);
    }
    
    public void OpenSystemAlbum(string info)
    {
        SendMessage(MobileInterfaceDefine.openSystemAlbum, info);
    }

    public void SaveMediaToLocal(string info)
    {
        SendMessage(MobileInterfaceDefine.saveMediaToLocal, info);
    }
    public void RefreshIconBadgeNumber(string info)
    {

        SendMessage(MobileInterfaceDefine.refreshIconBadgeNumber, info);
    }

    public void IsVivoStartFromGameCenter()
    {
#if UNITY_EDITOR || UNITY_IOS
        return;
#endif
        SendMessage(MobileInterfaceDefine.isStartFromVivoGameCenter, "");
    }

    public void JumpToVivoGameCenter()
    {
#if UNITY_EDITOR || UNITY_IOS
        return;
#endif
        SendMessage(MobileInterfaceDefine.jumpVivoGameCenter, "");
    }
}

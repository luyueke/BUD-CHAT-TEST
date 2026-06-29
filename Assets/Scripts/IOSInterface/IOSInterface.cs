using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class IOSInterface
{
#if UNITY_EDITOR
    public static void sendMessageToClient(string key,string msg){
        Debug.LogError($"sendMessageToClient {key}");
    }
#elif UNITY_IPHONE
     [DllImport("__Internal")]
     public static extern void sendMessageToClient(string key,string msg);
#endif

}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Message;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using System.Buffers;
using BestHTTP.WebSocket;
namespace Game
{
    public class RoomWsSystem
    {
        private static RoomWsSystem _instance;
        public static RoomWsSystem Inst
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new RoomWsSystem();
                }
                return _instance;
            }
        }

        private bool isConnecting = false;
        private string address = "";
        private WebSocket webSocket;


        public bool IsConnected => webSocket != null && webSocket.IsOpen;
        bool bHadReceiveHello = false;

        Coroutine coroutine;

        public void ConnectToWebSocket()
        {
            if (isConnecting)
            {
                Debug.LogWarning("WebSocket 正在连接中，请稍候...");
                return;
            }

            try
            {
                bHadReceiveHello = false;
                {
                    address = "wss://kiboo.pointonecreate.com/ws/conversation";
                }
                isConnecting = true;

                Debug.Log($"正在连接到 BestHTTP WebSocket: {address}");

                // 创建新的WebSocket实例
                webSocket = new WebSocket(new Uri(address));

                // 设置自定义头参数
                webSocket.OnInternalRequestCreated += OnInternalRequestCreated;

                // 订阅 WebSocket 事件
                webSocket.OnOpen += OnOpen;
                webSocket.OnMessage += OnMessageRecv;
                webSocket.OnClosed += OnClosed;
                webSocket.OnError += OnError;

                // 开始连接到服务器
                webSocket.Open();
            }
            catch (Exception e)
            {
                Debug.LogError($"WebSocket 连接初始化失败: {e.Message}");
            }
        }

        void OnInternalRequestCreated(WebSocket ws, BestHTTP.HTTPRequest request)
        {
            var tokenInfo = NetworkManager.Inst.GetHttpTokenInfo();
            foreach (var token in tokenInfo)
            {
                Debug.Log("WebSocket 头参数: " + token.Key + " " + token.Value);
                request.SetHeader(token.Key, token.Value);
            }


            Debug.Log("WebSocket 自定义头参数已设置");
        }

        void OnOpen(WebSocket ws)
        {
            Debug.Log("✅ BestHTTP WebSocket 连接已建立");
            isConnecting = false;

            ConnectSuccess();
            //心跳开启
            webSocket.StartPingThread = true;
            webSocket.PingFrequency = 3000;
            webSocket.CloseAfterNoMessage = TimeSpan.FromSeconds(60);

        }

        public void SendMessage2Server(string message)
        {
            if (webSocket != null && webSocket.IsOpen)
            {
                webSocket.Send(message);
            }
            else
            {
                Debug.LogWarning("WebSocket 未连接，无法发送消息");
            }
        }
        void ConnectSuccess()
        {
            Debug.Log("WebSocket 连接成功");
            MessageHelper.Broadcast(MessageName.RoomWsConnectStateChange, true);
        }

        void OnMessageRecv(WebSocket ws, string message)
        {
            DateTime now = DateTime.Now;

            try
            {
                // if (message == "response.output_audio.start")
                // {
                //     MessageHelper.Broadcast(MessageName.RealTimeAISpeakBegin);
                //     Debug.Log("RoomOpenAIEvent:AI开始说话");
                // }
                // else if (message == "response.output_audio.done")
                // {
                //     //这里要开始检测声音结束
                //     if (RoomManager.Inst.roomType == 1 && !bHadReceiveHello)
                //     {
                //         //第一次收到ai说话消息，开麦
                //         RoomManager.Inst.SendRoomSwitch(1);
                //         bHadReceiveHello = true;
                //     }
                //     // MessageHelper.Broadcast(MessageName.RealTimeAISpeakEnd);
                //     Debug.Log("RoomOpenAIEvent:AI结束说话");
                // }
                // else if (message == "input_audio_buffer.speech_started")
                // {
                //     MessageHelper.Broadcast(MessageName.RealTimePlayerSpeakBegin);
                //     Debug.Log("RoomOpenAIEvent:我开始说话");
                // }
                // else if (message == "input_audio_buffer.speech_stopped")
                // {
                //     MessageHelper.Broadcast(MessageName.RealTimePlayerSpeakEnd);
                //     Debug.Log("RoomOpenAIEvent:我结束说话");
                // }
            }
            catch (System.Exception e)
            {
                Debug.LogError("RoomTalkState err:" + e.Message);
            }
        }

        public void CloseWebSocket()
        {
            if (webSocket != null)
            {
                webSocket.Close();
            }
        }



        void OnClosed(WebSocket ws, ushort code, string message)
        {
            Debug.LogError($"🔌 BestHTTP WebSocket 连接已关闭，代码: {code}, 消息: {message}");
            isConnecting = false;
            webSocket = null;
            if (coroutine != null)
            {
                CoroutineManager.Inst.StopCoroutine(coroutine);
                coroutine = null;
            }

            if (this == null)
            {
                return;
            }
        }

        void OnError(WebSocket ws, string ex)
        {
            Debug.LogError($"❌ BestHTTP WebSocket 错误: {ex ?? "Unknown error"}");
            isConnecting = false;
            webSocket = null;
        }

    }


}
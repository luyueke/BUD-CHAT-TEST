using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Game;
using Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
public class RoomManagerKiboo : RoomManager
{
    public bool checkEnterRoomFailed = true; //是否检查进入房间失败
    bool isEnterRoomFailed = false; //当次进入房间是否失败 (连续收到两次state变化  第一次2~3 第二次3~2 提示连接失败)
    int receiveRoomStateCount = 0; //收到房间状态变化的次数
    protected override void ConnectWebSocket()
    {
        RoomWsKibooSystem.Inst.ConnectToWebSocket();
    }
    protected override void AddClientRespose()
    {
        RoomWsKibooSystem.Inst.onWebSocketConnectStateChange += OnRoomWsConnectStateChange;
    }

    protected override void DelClientRespose()
    {
        RoomWsKibooSystem.Inst.onWebSocketConnectStateChange -= OnRoomWsConnectStateChange;
    }

    void OnRoomWsConnectStateChange(bool isConnected)
    {
        if (!isFirstWebSocketConnected)
        {
            isFirstWebSocketConnected = true;
            EnterRoom();
        }
        //重连待补充
    }
    protected override void OnRoomStateChange(string data)
    {
        Debug.Log("RoomStateChange:" + data);
        try
        {
            RoomStateData roomStateData = JsonConvert.DeserializeObject<RoomStateData>(data);
            Debug.Log("RoomStateChange roomStateData:" + roomStateData.roomState);

            if (checkEnterRoomFailed)
            {
                if (receiveRoomStateCount == 0)
                {
                    if (roomStateData.fromRoomState == 2 && roomStateData.roomState == 3)
                    {
                        //异常情况时 第一次是2~3
                        receiveRoomStateCount++;
                    }
                    else
                    {
                        checkEnterRoomFailed = false;
                    }
                }
                else if (receiveRoomStateCount == 1)
                {
                    if (roomStateData.fromRoomState == 3 && roomStateData.roomState == 2)
                    {
                        //异常情况时 第二次是2~3
                        isEnterRoomFailed = true;
                        // MessageHelper.Broadcast(MessageName.RoomEnterRoomMustFailed);
                    }
                    checkEnterRoomFailed = false;
                }
            }
            if (isEnterRoomFailed)
            {
                // return;
            }
            curRoomState = (int)roomStateData.roomState;
            if ((int)roomStateData.roomState == 1)
            {
                UpdateStatus("ai进入房间");
            }
            else if ((int)roomStateData.roomState == 2)
            {
                UpdateStatus("房间断线");
            }
            else if ((int)roomStateData.roomState == 3)
            {
                UpdateStatus("房间连接中");
                MessageHelper.Broadcast(MessageName.RoomConnectStateChange, RoomConnectState.Connecting);
            }
            else if ((int)roomStateData.roomState == 4)
            {
                UpdateStatus("房间重新连接");
                MessageHelper.Broadcast(MessageName.RoomConnectStateChange, RoomConnectState.Connecting);
            }
            else if ((int)roomStateData.roomState == 5)
            {
                UpdateStatus("房间连接成功");
                SendRoomSwitch(Sound);
                MessageHelper.Broadcast(MessageName.RoomConnectStateChange, RoomConnectState.Connected);
            }
            else if ((int)roomStateData.roomState == 6)
            {
                UpdateStatus("房间连接失败");
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError("RoomStateChange err:" + e.Message);
        }

    }

    protected override void OnRoomNotifyVolume(string data)
    {
        float.TryParse(data, out volume);
    }

    public override void SendMsgToSever(string msg)
    {
        RoomWsKibooSystem.Inst.SendChatMessageServer(msg);
    }
    protected override void OnRoomChatTopic(string data)
    {
        Debug.Log("RoomManager OnRoomChatTopic: " + data);
        // try
        // {
        //     if (string.IsNullOrEmpty(data))
        //     {
        //         return;
        //     }
        //     MessageHelper.Broadcast(MessageName.NewAddChatMessage, data, false);  //这里都是ai的
        //     Debug.Log("RoomChatTopic roomChatTopicData:" + data);
        // }
        // catch (System.Exception e)
        // {
        //     Debug.LogError("RoomChatTopic err:" + e.Message);
        // }
    }

    protected override void OnDestroy()
    {
        RoomWsKibooSystem.Inst.CloseWebSocket();
        base.OnDestroy();
    }

    protected override void OnRoomOpenAIEvent(string data)
    {
        //     DateTime now = DateTime.Now;
        //     try
        //     {
        //         if (data == "response.output_audio.start")
        //         {
        //             MessageHelper.Broadcast(MessageName.RealTimeAISpeakBegin);
        //             Debug.Log("RoomOpenAIEvent:AI开始说话");
        //             UpdateStatus("AI开始说话");
        //         }
        //         // else if (data == "response.output_audio.done")
        //         else if (data == "AIDidStopSpeech")
        //         {
        //             if (roomType == 1 && !bHadReceiveHello)
        //             {
        //                 //第一次收到ai说话消息，开麦
        //                 SendRoomSwitch(1);
        //                 bHadReceiveHello = true;
        //             }
        //             MessageHelper.Broadcast(MessageName.RealTimeAISpeakEnd);
        //             Debug.Log("RoomOpenAIEvent:AI结束说话");
        //             UpdateStatus("AI结束说话");
        //         }
        //         else if (data == "input_audio_buffer.speech_started")
        //         {
        //             MessageHelper.Broadcast(MessageName.RealTimePlayerSpeakBegin);
        //             Debug.Log("RoomOpenAIEvent:我开始说话");
        //             UpdateStatus("我开始说话");
        //         }
        //         else if (data == "input_audio_buffer.speech_stopped")
        //         {
        //             MessageHelper.Broadcast(MessageName.RealTimePlayerSpeakEnd);
        //             Debug.Log("RoomOpenAIEvent:我结束说话");
        //             UpdateStatus("我结束说话");
        //         }
        //     }
        //     catch (System.Exception e)
        //     {
        //         Debug.LogError("RoomTalkState err:" + e.Message);
        //     }
    }

}


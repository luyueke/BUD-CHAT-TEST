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
public class RoomManager : MonoBehaviour
{
    public int roomType;  // 0纯文本 1纯语音 2文本和语音
    protected string _serverUrl = "wss://live.pointonecreate.com";

    protected string _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NjIxMzY5MDgsImlkZW50aXR5IjoidHd3LTEiLCJpc3MiOiJ0ZXN0a2V5IiwibmJmIjoxNzU4NTM2OTA4LCJzdWIiOiJ0d3ctMSIsInZpZGVvIjp7InJvb20iOiIxMjMiLCJyb29tSm9pbiI6dHJ1ZX19.a6yvNY4BWCdonjGKFreybqxKWF16mAq1hGQNdpNjRBE";

    public Text statueTxt;


    [HideInInspector] public float volume;


    public static RoomManager Inst;

    int voiceType = 0; // 0官方 1傲娇 2阳光 3温柔   默认是官方的

    bool _hadToken = false; // 是否已经获取到token

    protected bool bHadReceiveHello = false; // 是否已经收到hello消息  进入房间后，先闭麦，根据房间类型，收到一次文本或讲话信息后，才开麦
    [HideInInspector] public bool bBanModifyMicStatus = false; // 是否禁止修改麦克风状态
    int _sound = 0; // 0闭麦 1开麦
    public int Sound
    {
        set
        {
            _sound = value;
            MessageHelper.Broadcast(MessageName.LivekitRoomSoundChange,value);
        }
        get
        {
            return _sound;
        }
    }

    public int curRoomState = 0;

    #region WebSocket
    protected bool isFirstWebSocketConnected = false;

    #endregion
    [HideInInspector] public int curChatMode = -1; //当前聊天模式 -1 不发这个字段   0 聊天 1 测评 2 塔罗牌

    Coroutine checkMailSayingOverCoroutine;

    void Awake()
    {
    }

    void OnEnable()
    {
        Inst = this;
        bHadReceiveHello = false;
        AddClientRespose();
        ConnectWebSocket();
    }
    protected virtual void OnDisable()
    {
        CloseRoom();
    }

    protected virtual void ConnectWebSocket()
    {
    }

    protected virtual void EnterRoom()
    {
        UpdateStatus("获取token中..");
        // ChatSystem.Inst.LiveTokenReq((suc) =>
        // {
        //     Debug.Log("ChatSystem.Inst.LiveTokenReq suc " + suc);
        //     if (suc)
        //     {
        //         if (this == null || this.gameObject.activeInHierarchy == false)
        //         {
        //             Debug.Log("RoomManager EnterRoom this is null or gameObject is not active");
        //             return;
        //         }
        //         if (roomType == 0)
        //         {
        //             MessageHelper.Broadcast(MessageName.RoomConnectStateChange, RoomConnectState.Connected);
        //             return;
        //         }
        //         UpdateStatus("获取到token,连接中..");
        //         // 初始化音频源
        //         _serverUrl = ChatSystem.Inst.liveTokenData.url;
        //         _token = ChatSystem.Inst.liveTokenData.token;
        //         JObject json = new JObject();
        //         json.Add("token", _token);
        //         json.Add("url", _serverUrl);
        //         Debug.Log("RoomManager EnterRoom");
        //         bBanModifyMicStatus = false;
        //         SendRoomSwitch(0); //进入前闭麦
        //         bBanModifyMicStatus = true; 
        //         MobileInterface.Instance.EnterRoom(json.ToString());
        //         _hadToken = true;
        //         InitRoom(voiceType);
        //     }
        // }, roomType, curChatMode);
    }

    public virtual void checkSayHelloAndOpenMic()
    {
        checkMailSayingOverCoroutine = StartCoroutine(checkSayHelloAndOpenMicCoroutine());
    }

    public virtual void StopCheckMailSayingOver()
    {
        if (checkMailSayingOverCoroutine != null)
        {
            StopCoroutine(checkMailSayingOverCoroutine);
        }
        checkMailSayingOverCoroutine = null;
    }

    /// <summary>
    /// 有些场景是没有第一句话打招呼的 需要处理跳过第一句话打招呼 并开麦
    /// </summary>
    public virtual void HandleJumpFirtWord()
    {
    }

    IEnumerator checkSayHelloAndOpenMicCoroutine()
    {
        yield return new WaitForSeconds(3f);
        if (!bHadReceiveHello)
        {
            SendRoomSwitch(1);
            bHadReceiveHello = true;
        }
    }

    protected virtual void AddClientRespose()
    {
    }

    public void InitRoom(int voiceType)
    {
        this.voiceType = voiceType;
        Debug.Log("RoomManager InitRoom voiceType " + voiceType);
        if (!_hadToken)
        {
            return;
        }
        // MobileInterface.Instance.InitRoom(voiceType);
    }


    /// <summary>
    /// 发送消息到服务器
    /// </summary>
    /// <param name="msg">消息内容</param>
    // public void SendMsgToSever(string msg)
    // {
    //     MobileInterface.Instance.SendRoomTxtInput(msg);
    // }

    public virtual void SendMsgToSever(string msg)
    {

    }


    public virtual void SendRoomSwitch(int isOpen) //0 关闭 1 打开
    {
        if (bBanModifyMicStatus)
        {
            Debug.Log("RoomManager SendRoomSwitch 禁止修改麦克风");
            return;
        }



        Debug.Log("RoomManager SendRoomSwitch isOpen: " + isOpen);
        Sound = isOpen;
        JObject json = new JObject();
        json.Add("micSwitch", isOpen);
        // MobileInterface.Instance.SendRoomSwitch(json.ToString());
    }

    protected virtual void OnRoomStateChange(string data)
    {


    }

    protected virtual void OnRoomOpenAIEvent(string data)
    {

    }

    protected virtual void OnRoomChatTopic(string data)
    {
    }



    protected virtual void OnRoomNotifyVolume(string data)
    {
    }

    public virtual void CloseRoom()
    {
        Debug.Log("RoomManager CloseRoom");
        // MobileInterface.Instance.ExitRoom("");
        DelClientRespose();
    }

    protected virtual void OnDestroy()
    {
        CloseRoom();
    }

    protected virtual void DelClientRespose()
    {
    }



    protected virtual void UpdateStatus(string message)
    {
        if (statueTxt != null)
        {
            // statueTxt.text = message;
            statueTxt.text = "";
        }
    }
}

public class RoomStateData
{
    public int roomState;
    public int fromRoomState;
}

public enum RoomState
{
    EnterRoomSuccess = 1, //ai进入房间成功
    RoomDisconnected = 2, //房间断开连接
    RoomConnecting = 3, //房间连接中
    RoomReConnecting = 4, //房间重新连接
    RoomRonnected = 5, //房间连接成功
    RoomConnectFailed = 6, //房间连接失败(EnterRoom事件失败的时候,"data":{"roomState":6,"fromRoomState":xx,"token":xx,"errMsg":xx})

}

public class RoomOpenAIEventData
{
    public string event_id;
    public string type;   //"response.output_audio.started" "response.output_audio.done" "input_audio_buffer.speech_started" "input_audio_buffer.speech_stopped"
    public string response_id;
}

public class RoomTxtChatData
{
    public string id;
    public long timestamp; //毫秒时间戳
    public string message; //消息内容
    public bool ignoreLegacy;
}

//测评
public class AssessmentEmailData
{
    public string assessmentType; //测评类型
    public string content; //测评内容
}




public enum RoomConnectState
{
    Connecting = 0, //连接中
    Connected = 1, //连接成功
}


public enum RoomChatWsType
{
    AIEvent = 1,
    ChatTopic = 2,
    CustomTopic = 3,
}
public class RoomChatWsMessage
{
    public int type; //1:AIEvent 2:ChatTopic 3:CustomTopic
    public string data;
}
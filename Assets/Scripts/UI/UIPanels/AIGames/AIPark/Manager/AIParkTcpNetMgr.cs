using Message;
using Network;
using Network.Tcp.Core;
using UIAgent;
using UnityEngine;
using Pb.Base;
using Pb.Game;
using Pb.Map;
using GameSync.Manager;
using AIGame.Base;
using System.Collections.Generic;
using GameData.GameSync;
using Pb.Game;
using System;
using GameData.BaseInfo;
using Network.Tcp.Core;
using Newtonsoft.Json;
using System.Linq;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Props.PropsBehaviours;
using static AIGame.Base.AIParkUtils;
using UI.UIPanels.ProfilePanel;
using Basic.Utils;
public class AIParkTcpNetMgr
{
    public enum ReconnectType
    {
        None,
        SelectEvent,
        NextScene,
    }
    public enum WaitDataType
    {
        None,
        Wait_SelectEvent_Data,
        Wait_NextScene_Data,
    }
    static AIParkTcpNetMgr _instance;
    public static AIParkTcpNetMgr Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new AIParkTcpNetMgr();
            }
            return _instance;
        }
    }


    public Dictionary<int, (bool, bool)> reqSceneDataDict; //场景数据 请求与返回状态

    public bool bHadSelectEvent = false; //是否已经选择过事件
    public string curSelectEvent = "";

    public int PreNextSceneIdx = 1;
    public ReconnectType reconnectType = ReconnectType.None;
    public int reconnectSceneIdx = -1;
    public ParkEndType parkEndType = ParkEndType.Uncomplete;
    public WaitDataType waitDataType = WaitDataType.None;
    long waitNextDataTime = 0;
    BudTimer checkReconnectTimer = null;
    public void Init()
    {
        reqSceneDataDict ??= new();
        reqSceneDataDict.Clear();
        reqSceneDataDict.Add(1, (true, true));
        bHadSelectEvent = false;
        curSelectEvent = "";
        PreNextSceneIdx = 1;
        reconnectType = ReconnectType.None;
        reconnectSceneIdx = -1;
        parkEndType = ParkEndType.Uncomplete;
        waitNextDataTime = 0;
        waitDataType = WaitDataType.None;
        NetworkManager.Inst.AddMessageListener(NetMsg.MSG_S_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, OnRecvParkSyncNetData);
        MessageHelper.AddListener<string, int, int>(MessageName.OnSyncNpcLocation, OnSyncNpcLocation);
        MessageHelper.AddListener(MessageName.TcpLoginSuccess, OnTcpLoginSuccess);
    }

    public void Release()
    {
        NetworkManager.Inst.RemoveMessageListener(NetMsg.MSG_S_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, OnRecvParkSyncNetData);
        MessageHelper.RemoveListener<string, int, int>(MessageName.OnSyncNpcLocation, OnSyncNpcLocation);
        MessageHelper.RemoveListener(MessageName.TcpLoginSuccess, OnTcpLoginSuccess);
    }

    private void OnSyncNpcLocation(string npcRoleType, int location, int action)
    {
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.LocationSync;
        netData.SceneIndex = AIParkUtils.Inst.ParkCustomData.SceneIndex;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        netData.Location.Add(npcRoleType, location);
        Debug.Log("乐园消息AIParkTcpNetMgr LocationSync 7 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);
    }

    void OnTcpLoginSuccess()
    {
        Debug.Log("乐园重连成功 reconnectType = " + reconnectType);
        //重连回来
        if (reconnectType == ReconnectType.SelectEvent)
        {
            Debug.Log("乐园掉线 重连做出选择:" + curSelectEvent);
            SendAIParkSyncReq_SelectedEvent(curSelectEvent);
        }
        else if (reconnectType == ReconnectType.NextScene)
        {
            Debug.Log("乐园掉线 重连请求下一幕:" + reconnectSceneIdx);
            SendAIParkSyncReq_NextScene(reconnectSceneIdx);
        }
    }

    private void OnRecvParkSyncNetData(byte[] bytes)
    {
        AIGameAmusementParkSyncReply aIGameAmusementParkSyncReply = AIGameAmusementParkSyncReply.Parser.ParseFrom(bytes);
        LoggerUtils.Log("收到乐园消息同步数据:" + JsonConvert.SerializeObject(aIGameAmusementParkSyncReply));

        Pb.Game.AIGameAmusementParkSyncReply.Types.SyncEnum rspSynEnum = aIGameAmusementParkSyncReply.SyncType;
        switch (rspSynEnum)
        {
            case Pb.Game.AIGameAmusementParkSyncReply.Types.SyncEnum.EndSummary:
                //结局
                if (aIGameAmusementParkSyncReply.NextScene != null)
                {
                    //最后一幕的讨论 及总结
                    AIParkUtils.Inst.ParkCustomData.isSummaryScene = true;
                    AIParkUtils.Inst.ParkGameData.actions?.Clear();
                    AIParkUtils.Inst.ParkGameData.actions.AddRange(aIGameAmusementParkSyncReply.NextActions);
                    HandleNextScene(aIGameAmusementParkSyncReply, true);
                }
                HandleSelectEvent(aIGameAmusementParkSyncReply);
                waitDataType = WaitDataType.None;
                break;
            case Pb.Game.AIGameAmusementParkSyncReply.Types.SyncEnum.NewInteract:
                //新交互
                if (aIGameAmusementParkSyncReply.NextScene != null)
                {
                    //新事件
                    if (!AIParkUtils.Inst.ParkCustomData.isSummaryScene)
                    {
                        AIParkUtils.Inst.ParkGameData.actions?.Clear();
                        AIParkUtils.Inst.ParkGameData.actions.AddRange(aIGameAmusementParkSyncReply.NextActions);
                    }
                    HandleNextScene(aIGameAmusementParkSyncReply, false);
                    waitDataType = WaitDataType.None;
                }
                break;
            case Pb.Game.AIGameAmusementParkSyncReply.Types.SyncEnum.GlobalToast:
                //飘字
                AIParkUtils.Inst.ParkGameData.Toast = aIGameAmusementParkSyncReply.Toast;
                AIGameController.Inst.GetCurAIGame<AIParkGame>().ShowGlobalToast(AIParkUtils.Inst.ParkGameData.Toast);
                break;
            default:
                break;
        }
        if (aIGameAmusementParkSyncReply.NextActions?.Count > 0)
        {
            for (int i = 0; i < aIGameAmusementParkSyncReply.NextActions.Count; i++)
            {
                var nextAction = aIGameAmusementParkSyncReply.NextActions[i];
                AIParkUtils.Inst.ParkGameData.AddNewHistory(nextAction);
            }
        }
    }


    void HandleSelectEvent(AIGameAmusementParkSyncReply aIGameAmusementParkSyncReply)
    {
        AIParkUtils.Inst.ParkGameData.Summary = aIGameAmusementParkSyncReply.Summary;
        AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
        aiGame.OnGameEndRsp();
    }

    void HandleNextScene(AIGameAmusementParkSyncReply aIGameAmusementParkSyncReply, bool isSummary)
    {
        AIParkUtils.Inst.ParkGameData.CompatibleStage();
        var sceneIdx = PreNextSceneIdx;
        Debug.Log("乐园HandleNextScene isSummary = " + isSummary);
        if (isSummary)
        {
            reqSceneDataDict[sceneIdx] = (true, true);
            AIParkUtils.Inst.ParkGameData.summaryEvent = aIGameAmusementParkSyncReply.NextScene;
        }
        else
        {
            reqSceneDataDict[sceneIdx] = (true, true);
            AIParkUtils.Inst.ParkGameData.events[sceneIdx - 1] = aIGameAmusementParkSyncReply.NextScene;
        }
        Debug.Log("乐园HandleNextScene sceneIdx = " + sceneIdx);
        Debug.Log("乐园HandleNextScene AIParkUtils.Inst.ParkGameData.events.Count = " + AIParkUtils.Inst.ParkGameData.events.Count);
        // if (sceneIdx > AIParkUtils.Inst.ParkGameData.events.Count)
        // {
        //     AIParkUtils.Inst.ParkGameData.summaryEvent = aIGameAmusementParkSyncReply.NextScene;
        // }
        // else
        // {
        //     reqSceneDataDict[sceneIdx] = (true, true);
        //     AIParkUtils.Inst.ParkGameData.events.Add(aIGameAmusementParkSyncReply.NextScene);
        // }

        AIParkGame aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
        aiGame.OnRspNextSceneData();

        AIParkUtils.Inst.ParkGameData.EventSummaryList.Add(aIGameAmusementParkSyncReply.Summary);
    }



    void TestTriggerData(AIGameAmusementParkSyncReply aIGameAmusementParkSyncReply)
    {


    }
    /// <summary>
    /// 发送NPCNPC交互请求
    /// </summary>
    /// <param name="participants"></param>
    /// <param name="location"></param>
    /// <param name="action"></param> <summary>
    /// 
    /// </summary>
    /// <param name="participants"></param>
    /// <param name="location"></param>
    /// <param name="action"></param>
    public void SendAIParkSyncReq_NPCNPC(List<string> participants, int location, int action, Action callback)
    {
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.InteractNpcNpc;
        Pb.Game.History history = new Pb.Game.History();
        history.Participants.AddRange(participants.ToArray());
        history.Location = location;
        history.Action = action;
        netData.Interact = history;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息AIParkTcpNetMgr InteractNpcNpc 1 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);
    }

    public void SendAIParkSyncReq_NPCPlayer(string participants, int location, int action, Action callback)
    {
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.InteractNpcPlayer;
        Pb.Game.History history = new Pb.Game.History();
        // history.Participants.AddRange(participants.ToArray());
        history.Location = location;
        history.Action = action;

        // History lastHistory = AIParkUtils.Inst.ParkGameData.GetParticipantsLastHistory(participants);
        // netData.Interact = lastHistory;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息AIParkTcpNetMgr InteractNpcPlayer 2 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);
    }

    public int GetSelectEventSceneIndex()
    {
        for (int i = 0; i < AIParkUtils.Inst.ParkGameData.events.Count; i++)
        {
            if (AIParkUtils.Inst.ParkGameData.events[i].EventType == 1)
            {
                return i + 1;
            }
        }
        return AIParkUtils.Inst.ParkGameData.events.Count + 1;
    }

    public void SelectEventSetPreNextSceneIdx()
    {
        for (int i = 0; i < AIParkUtils.Inst.ParkGameData.events.Count; i++)
        {
            if (AIParkUtils.Inst.ParkGameData.events[i].EventType == 1)
            {
                PreNextSceneIdx = i + 2;
                return;
            }
        }
        PreNextSceneIdx = AIParkUtils.Inst.ParkGameData.events.Count + 1;
    }
    /// <summary>
    /// 选择事件
    /// </summary>
    /// <param name="eventId"></param> <summary>
    /// 
    /// </summary>
    /// <param name="eventId"></param>
    public void SendAIParkSyncReq_SelectedEvent(string selectedEvent)
    {
        if (bHadSelectEvent)
        {
            return;
        }
        bHadSelectEvent = true;
        curSelectEvent = selectedEvent;
        SelectEventSetPreNextSceneIdx();
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.SelectEvent;
        netData.SelectedEvent = selectedEvent;
        netData.SceneIndex = GetSelectEventSceneIndex();
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息AIParkTcpNetMgr SelectEvent 3 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);
        if (!NetworkManager.Inst.IsConnect())
        {
            bHadSelectEvent = false;
            reconnectType = ReconnectType.SelectEvent;
            LoggerUtils.Log("乐园掉线 保存选择:" + selectedEvent);
        }
        else
        {
            LoggerUtils.Log("乐园 保存选择:" + selectedEvent);
            waitNextDataTime = GameUtils.GetTimeStamp();
            reconnectType = ReconnectType.None;
            waitDataType = WaitDataType.Wait_SelectEvent_Data;
        }
    }

    public bool CheckHadNextSceneData(int sceneIndex)
    {
        if (!reqSceneDataDict.ContainsKey(sceneIndex))
        {
            return false;
        }
        return reqSceneDataDict[sceneIndex].Item1 && reqSceneDataDict[sceneIndex].Item2;
    }

    public void TestReconnect()
    {
        // NetworkManager.Inst.Disconnect();
        NetworkManager.Inst.OnConnectLost();
        // NetworkManager.Inst.Reconnect(true);
    }

    public void CheckReconnectState()
    {
        Debug.Log("回到前台 乐园消息AIParkTcpNetMgr CheckReconnectState 检查状态:"+NetworkManager.Inst.IsConnect());
        if (NetworkManager.Inst.IsConnect())
        {
            return;
        }
        //离线了
        Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了");
        TimerManager.Inst.Stop(checkReconnectTimer);
        if (waitDataType == WaitDataType.Wait_NextScene_Data)
        {
            Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待场景数据");
            if (reqSceneDataDict.ContainsKey(PreNextSceneIdx))
            {
                Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待场景数据 存在");
                if (reqSceneDataDict[PreNextSceneIdx].Item1 && !reqSceneDataDict[PreNextSceneIdx].Item2)
                {
                    Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待场景数据 存在 且 未返回");
                    var waitTime = GameUtils.GetTimeStamp() - waitNextDataTime > 30 ? 6 : (30 - (GameUtils.GetTimeStamp() - waitNextDataTime));
                    checkReconnectTimer = TimerManager.Inst.RunOnce("AIParkTcpNetMgr_CheckReconnectState", waitTime, () =>
                    {
                        if (waitDataType == WaitDataType.Wait_NextScene_Data)
                        {
                            Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待场景数据 存在 且 未返回 仍在等待");
                            if (NetworkManager.Inst.IsConnect())
                            {
                                Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待场景数据 存在 且 未返回 尝试重新发送");
                                SendAIParkSyncReq_NextScene(PreNextSceneIdx);
                            }
                            else
                            {
                                Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待场景数据 存在 且 未返回 重新检查");
                                CheckReconnectState();
                            }
                        }
                    });
                }
            }
        }
        else if (waitDataType == WaitDataType.Wait_SelectEvent_Data)
        {
            Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待选择事件");
            var waitTime = GameUtils.GetTimeStamp() - waitNextDataTime > 30 ? 6 : (30 - (GameUtils.GetTimeStamp() - waitNextDataTime));
            checkReconnectTimer = TimerManager.Inst.RunOnce("AIParkTcpNetMgr_CheckReconnectState", waitTime, () =>
            {
                Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待选择事件 重新检查");
                if (waitDataType == WaitDataType.Wait_SelectEvent_Data)
                {
                    Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待选择事件 重新检查 仍在等待");
                    if (NetworkManager.Inst.IsConnect())
                    {
                        Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待选择事件 重新检查 尝试重新发送");
                        SendAIParkSyncReq_SelectedEvent(curSelectEvent);
                    }
                    else
                    {
                        Debug.Log("乐园消息AIParkTcpNetMgr CheckReconnectState 离线了 等待选择事件 重新检查 重新检查");
                        CheckReconnectState();
                    }
                }

            });
        }

    }

    /// <summary>
    /// 切换下一幕
    /// </summary>
    /// <param name="sceneIndex"></param>
    public void SendAIParkSyncReq_NextScene(int sceneIndex)
    {
        PreNextSceneIdx = sceneIndex;
        reqSceneDataDict[sceneIndex] = (true, false);
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.NextScene;
        netData.SceneIndex = sceneIndex;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        LoggerUtils.Log("乐园消息AIParkTcpNetMgr NextScene 4 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);
        if (!NetworkManager.Inst.IsConnect())
        {
            LoggerUtils.Log("乐园掉线 保存场景:" + sceneIndex);
            reconnectType = ReconnectType.NextScene;
            reconnectSceneIdx = sceneIndex;
        }
        else
        {
            LoggerUtils.Log("乐园 保存场景:" + sceneIndex);
            waitNextDataTime = GameUtils.GetTimeStamp();
            reconnectType = ReconnectType.None;
            waitDataType = WaitDataType.Wait_NextScene_Data;
        }
    }

    public void SendAIParkSyncReq_InferAction(string participants)
    {
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.InferAction;
        History lastHistory = AIParkUtils.Inst.ParkGameData.GetParticipantsLastHistory(participants);
        netData.Interact = lastHistory;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息AIParkTcpNetMgr InferAction 5 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);

    }
    public void SendAIParkSyncReq_StartPushAction()
    {
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.StartPushAction;
        netData.SceneIndex = AIParkUtils.Inst.ParkCustomData.SceneIndex;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息AIParkTcpNetMgr StartPushAction 6 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);

    }
    /// <summary>
    /// 位置同步(本地的行动发生变化后,通知后端)
    /// </summary>


    public void TestSendAIParkSyncReq_InferAction()
    {
        SendAIParkSyncReq_InferAction("101");
    }

    public void TestSendAIParkSyncReq_NpcNpc()
    {
        List<string> participants = new List<string>() { "101" };
        int location = ((int)LocationType.SlideSlides);
        int actionStr = ((int)ActionType.SlideSlides);
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.InteractNpcNpc;
        Pb.Game.History history = new Pb.Game.History();
        history.Participants.AddRange(participants.ToArray());
        history.Location = location;
        history.Action = actionStr;
        netData.Interact = history;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息test AIParkTcpNetMgr 1 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);
    }

    public void TestSendAIParkSyncReq_NpcPlayer()
    {
        List<string> participants = new List<string>() { "101", "102" };
        int location = ((int)LocationType.SlideSlides);
        int actionStr = ((int)ActionType.SlideSlides);
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.InteractNpcPlayer;
        Pb.Game.History history = new Pb.Game.History();
        history.Participants.AddRange(participants.ToArray());
        history.Location = location;
        history.Action = actionStr;
        netData.Interact = history;
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息test AIParkTcpNetMgr 2 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);
    }

    public void TestSendAIParkSyncReq_SelectedEvent()
    {
        AIGameAmusementParkSyncRequest netData = new AIGameAmusementParkSyncRequest();
        netData.SyncType = Pb.Game.AIGameAmusementParkSyncRequest.Types.SyncEnum.SelectEvent;
        netData.SelectedEvent = "八音盒的发条人偶";
        netData.SessionId = AIParkUtils.Inst.ParkGameData.sessionId;
        Debug.Log("乐园消息test AIParkTcpNetMgr 3 Send-->" + JsonConvert.SerializeObject(netData));
        NetworkManager.Inst.SendCustomCmd(NetCmd.C_CMD_AI_GAME_AMUSEMENT_PARK_SYNC, netData);

    }

    public void TestNpcHistory()
    {
        History history = new History();
        history.Participants.Add("101");
        history.Location = ((int)LocationType.SlideSlides);
        history.Action = ((int)LocationType.SlideSlides);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = "101",
            Content = "101说话内容",
        });
        AIPark_CharacterManager.Inst.InsertHistory("101", history);
    }


    public void TestNewHistoryList()
    {
        var locaType = LocationType.TrojanHorse;
        var actionType = ActionType.TrojanHorse;
        List<History> historysss = new();
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Elise).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Casper).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Pio).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Teddy).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Vivien).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Rowland).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());

        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes?.Clear();
        historysss.Add(history);


        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Elise).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Elise).ToString(),
            Content = "1.Elise说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Casper).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Casper).ToString(),
            Content = "2.Casper说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Pio).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Pio).ToString(),
            Content = "3.Pio说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Teddy).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Teddy).ToString(),
            Content = "4.Teddy说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Vivien).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Vivien).ToString(),
            Content = "5.Vivien说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Rowland).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Rowland).ToString(),
            Content = "6.Rowland说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Tilia).ToString(),
            Content = "7.Tilia说话内容",
        });
        historysss.Add(history);
        AIParkUtils.Inst.ParkGameData.ParseNewHistoryList(historysss);
    }



    public void TestInsertNewHistoryList()
    {
        var locaType = LocationType.TrojanHorse;
        var actionType = ActionType.TrojanHorse;
        List<History> historysss = new();
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Elise).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Casper).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Pio).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Teddy).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Vivien).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Rowland).ToString());
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());

        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes?.Clear();
        historysss.Add(history);


        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Elise).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Elise).ToString(),
            Content = "1.Elise插入说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Casper).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Casper).ToString(),
            Content = "2.Casper插入说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Pio).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Pio).ToString(),
            Content = "3.Pio插入说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Teddy).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Teddy).ToString(),
            Content = "4.Teddy插入说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Vivien).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Vivien).ToString(),
            Content = "5.Vivien插入说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Rowland).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Rowland).ToString(),
            Content = "6.Rowland插入说话内容",
        });
        historysss.Add(history);

        history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)locaType);
        history.Action = ((int)actionType);
        history.Quotes.Add(new AmusementAIQuoteLine()
        {
            Speaker = ((int)ParkNpcRoleType.Tilia).ToString(),
            Content = "7.Tilia插入说话内容",
        });
        historysss.Add(history);
        AIParkUtils.Inst.ParkGameData.ParseNewHistoryList(historysss);
    }


    public void TestSelfEnterSeeSaw()
    {

    }

    public void testt1()
    {
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Elise).ToString());
        history.Location = ((int)LocationType.SeeSaw);
        history.Action = ((int)ActionType.SeeSaw);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
    }
    //后端推他去跷跷板
    public void testt4()
    {
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)LocationType.TrojanHorse);
        history.Action = ((int)ActionType.TrojanHorse);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
    }
    public void testt5()
    {
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)LocationType.Swinging);
        history.Action = ((int)ActionType.None);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
    }
    public void testt6()
    {
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)LocationType.Swinging);
        history.Action = ((int)ActionType.Swinging);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
    }
    public void testt7()
    {
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)LocationType.SeeSaw);
        history.Action = ((int)ActionType.SeeSaw);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
    }
    public void testt8()
    {
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)LocationType.SeeSaw);
        history.Action = ((int)ActionType.None);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
    }
    //先让去荡秋千 再打断去跷跷板
    //去荡秋千的路上 玩家先把秋千抢了
    public void testt2()
    {

        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)LocationType.Swinging);
        history.Action = ((int)ActionType.Swinging);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
    }

    public void testt3()
    {
        History history = new History();
        history.Participants.Add(((int)ParkNpcRoleType.Tilia).ToString());
        history.Location = ((int)LocationType.Swinging);
        history.Action = ((int)ActionType.Idle);
        AIParkUtils.Inst.ParkGameData.AddNewHistory(history);
        //去做跷跷板
        // AIParkPropsManager.Inst.CheckDoCustomAction(ParkNpcRoleType.Tilia, 100);
    }

}

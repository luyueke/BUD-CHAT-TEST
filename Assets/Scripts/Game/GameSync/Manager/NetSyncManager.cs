using System.Collections.Generic;
using System;
using Google.Protobuf;
using Pb.Base;
using Pb.Game;
using NetEngine;
using Google.Protobuf.WellKnownTypes;
using NetEngine.src;
using GameData.Config;
using GameSync.Manager;
using GameData.GameSync;
using Message;

public class NetSyncManager : GameInstance<NetSyncManager>
{
    private const string TAG = "NetSyncManager";

    private Dictionary<SubCmdType, Func<Any, IMessage>> SyncCmdRegisterDict = new Dictionary<SubCmdType, Func<Any, IMessage>>();
    private Dictionary<SubCmdType,Action<CommonSyncClientData>> NetSyncCallback = new Dictionary<SubCmdType, Action<CommonSyncClientData>>();
    private Dictionary<SubCmdType,Action<CommonSyncClientData>> NetSyncRoomCallback = new Dictionary<SubCmdType, Action<CommonSyncClientData>>();
    private Dictionary<SubCmdType,Action<ErrorCode>> ErrorCodeCallback = new Dictionary<SubCmdType, Action<ErrorCode>>();
    private Dictionary<SubCmdType,List<IMessage>> _propDataDict = new Dictionary<SubCmdType,List<IMessage>>();
    private Dictionary<SubCmdType,Action<string,List<IMessage>>> NetMapInfoCallback = new Dictionary<SubCmdType, Action<string,List<IMessage>>>();


    public NetSyncManager()
    {
        SyncCmdRegisterDict.Add(SubCmdType.SwitchBtn,(data)=>convert(data,new SwitchBtnNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.SensorBox,(data)=>convert(data,new SensorBoxNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.Emote, (data) => convert(data, new EmoteNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.Chat, (data) => convert(data, new ChatNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.ChangeImage, (data) => convert(data, new ChangeImageNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.ConflictStatus,(data) => convert(data, new PlayerConflictStatus()));
        SyncCmdRegisterDict.Add(SubCmdType.MusicalBst,(data) => convert(data,new MusicalNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.MusicalOp,(data) => convert(data,new MusicalStatus()));
        SyncCmdRegisterDict.Add(SubCmdType.InteractPanel, data => convert(data, new InteractPanelNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.HiddenPet, data => convert(data, new HiddenPetNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.SetBan, data => convert(data, new SetBanNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.CallBuddy, data => convert(data, new CallBuddyNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.ParkSync, data => convert(data, new AIGameAmusementParkSyncReply()));
        SyncCmdRegisterDict.Add(SubCmdType.CallVehicle, data => convert(data, new CallVehicleNetData()));
        SyncCmdRegisterDict.Add(SubCmdType.CallTheatre, data => convert(data, new CallTheatreNetData()));
    }

    public override void Release()
    {
        base.Release();

    }

    public void InitRoomBst()
    {
        Global.Room.CommonSyncBstListener += OnRecvRoomPacket;
        Global.Map.CommonSyncBstListener += OnRecvMapPacket;
    }

    /// <summary>
    /// 发送数据，广播所有地图
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="packet"></param> <summary>
    public void SendAllRoom(SubCmdType cmd, IMessage packet)
    {
        Send(cmd,SyncArea.AllRoom,packet);
    }

    /// <summary>
    /// 发送数据，只广播地图
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="packet"></param> <summary>
    public void Send(SubCmdType cmd, IMessage packet)
    {
        Send(cmd,SyncArea.OnlyMap,packet);
    }

    public void Send(SubCmdType cmd,SyncArea syncArea,IMessage packet)
    {
        if (Global.Map == null || Global.Room == null) 
        {
            LoggerUtils.Log("当前房间信息为 null, 发送本地同步消息");
            // 创建本地同步消息
            CommonSyncClientData localSyncData = new CommonSyncClientData
            {
                MapId = string.Empty,
                PalyerId = Player.Id,
                Body = packet
            };
            
            // 通过消息系统广播本地同步消息
            MessageHelper.Broadcast(MessageName.LocalNetworkSync, cmd, localSyncData);
            return;
        }

        // 原有的网络同步逻辑
        CommonSyncReq commonSyncReq = new CommonSyncReq();
        commonSyncReq.SubCmd = cmd;
        commonSyncReq.PlayerId = Player.Id;
        commonSyncReq.MapId = Global.Map.ClientData.MapId;
        commonSyncReq.SyncArea = syncArea;
        commonSyncReq.PropData = Any.Pack(packet);

        LoggerUtils.Log($"{TAG} OnSendMapPacket cmd:{cmd} body:{packet}");
        Core.Room?.SendCommonSync(commonSyncReq,(ResponseEvent eve)=>{
            if(ErrorCodeCallback.ContainsKey(cmd))
            {
                ErrorCodeCallback[cmd].Invoke(eve.Code);
            }
        });
    }

    public void OnRecvMapPacket(CommonSyncBst commonSyncBst)
    {
        LoggerUtils.Log($"{TAG} OnRecvMapPacket cmd:{commonSyncBst.SubCmd}");
        SubCmdType cmd = commonSyncBst.SubCmd;
        if (NetSyncCallback.ContainsKey(cmd))
        {
            var packet = convertByCmd(cmd,commonSyncBst.PropData);
            if(packet != null)
            {
                ClientManager.Inst?.RunOnMainThread(()=>{
                    CommonSyncClientData clientData = new CommonSyncClientData();
                    clientData.MapId = commonSyncBst.MapId;
                    clientData.PalyerId = commonSyncBst.PlayerId;
                    clientData.Body = packet;
                    if (NetSyncCallback.ContainsKey(cmd) && NetSyncCallback[cmd] != null)//防止回调的时候监听已移除
                    {
                        NetSyncCallback[cmd].Invoke(clientData);
                    }
                });
            }
        }
    }

    public void OnRecvRoomPacket(CommonSyncBst commonSyncBst)
    {
        LoggerUtils.Log($"{TAG} OnRecvRoomPacket cmd:{commonSyncBst.SubCmd}");
        SubCmdType cmd = commonSyncBst.SubCmd;
        if (NetSyncRoomCallback.ContainsKey(cmd))
        {
            var packet = convertByCmd(cmd,commonSyncBst.PropData);
            if(packet != null)
            {
                ClientManager.Inst.RunOnMainThread(()=>{
                    CommonSyncClientData clientData = new CommonSyncClientData();
                    clientData.MapId = commonSyncBst.MapId;
                    clientData.PalyerId = commonSyncBst.PlayerId;
                    clientData.Body = packet;
                    NetSyncRoomCallback[cmd]?.Invoke(clientData);
                });

            }
        }
    }


    public void OnMapInfoSync(MapInfoRsp mapInfoData)
    {
        _propDataDict.Clear();
        var propItems = mapInfoData.PropItems;
        for (int i = 0; i < propItems.Count; i++)
        {
            var propItem = propItems[i];
            var cmd = propItem.SubCmd;
            var packet = convertByCmd(cmd,propItem.PropData);
            if(packet != null)
            {
                if(!_propDataDict.ContainsKey(cmd))
                {
                    _propDataDict[cmd] = new List<IMessage>();
                }
                _propDataDict[cmd].Add(packet);
            }
        }

        foreach(var cmd in _propDataDict.Keys)
        {
            if(NetMapInfoCallback.ContainsKey(cmd))
            {
                NetMapInfoCallback[cmd].Invoke(mapInfoData.MapId,_propDataDict[cmd]);
            }
        }
    }


    private IMessage convertByCmd(SubCmdType cmd,Any data)
    {
        if (SyncCmdRegisterDict.ContainsKey(cmd))
        {
            var func = SyncCmdRegisterDict[cmd];
            var packet = func(data);
            return packet;
        }
        return null;
    }


    private IMessage convert(Any data,IMessage tmp)
    {
       tmp.MergeFrom(data.Value);
       return tmp;
    }


    /// <summary>
    /// 添加监听地图内道具广播
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    public void AddBroadcastListener(SubCmdType cmd,Action<CommonSyncClientData> callback)
    {
        if(!NetSyncCallback.ContainsKey(cmd))
        {
            Action<CommonSyncClientData> listener = null;
            NetSyncCallback.Add(cmd,listener);
        }
        NetSyncCallback[cmd] += callback;
    }


    /// <summary>
    /// 移除监听地图内道具广播
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    public void RemoveBroadcastListener(SubCmdType cmd,Action<CommonSyncClientData> callback)
    {
        if(NetSyncCallback.ContainsKey(cmd))
        {
            NetSyncCallback[cmd] -= callback;
        }
    }


    /// <summary>
    /// 添加监听房间内道具广播
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    public void AddAllRoomBroadcastListener(SubCmdType cmd,Action<CommonSyncClientData> callback)
    {
        if(!NetSyncRoomCallback.ContainsKey(cmd))
        {
            Action<CommonSyncClientData> listener = null;
            NetSyncRoomCallback.Add(cmd,listener);
        }
        NetSyncRoomCallback[cmd] += callback;
    }


    /// <summary>
    /// 移除监听房间内道具广播
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    public void RemoveAllRoomBroadcastListener(SubCmdType cmd,Action<CommonSyncClientData> callback)
    {
        if(NetSyncRoomCallback.ContainsKey(cmd))
        {
            NetSyncRoomCallback[cmd] -= callback;
        }
    }


    /// <summary>
    /// 添加登录或重连时地图内道具信息
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    public void AddMapInfoListener(SubCmdType cmd,Action<string,List<IMessage>> callback)
    {
        if(!NetMapInfoCallback.ContainsKey(cmd))
        {
            Action<string,List<IMessage>> listener = null;
            NetMapInfoCallback.Add(cmd,listener);
        }
        NetMapInfoCallback[cmd] += callback;
    }

    /// <summary>
    /// 移除登录或重连时地图内道具信息
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>

    public void RemoveMapInfoListener(SubCmdType cmd,Action<string,List<IMessage>> callback)
    {
        if(NetMapInfoCallback.ContainsKey(cmd))
        {
            NetMapInfoCallback[cmd] -= callback;
        }
    }


    /// <summary>
    /// 添加监听道具回包错误码
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    public void AddErrorCodeListener(SubCmdType cmd,Action<ErrorCode> callback)
    {
        if(!ErrorCodeCallback.ContainsKey(cmd))
        {
            Action<ErrorCode> listener = null;
            ErrorCodeCallback.Add(cmd,listener);
        }
        ErrorCodeCallback[cmd] += callback;
    }


    /// <summary>
    /// 移除监听道具回包错误码
    /// </summary>
    /// <param name="cmd"></param>
    /// <param name="callback"></param>
    public void RemoveErrorCodeListener(SubCmdType cmd,Action<ErrorCode> callback)
    {
        if(ErrorCodeCallback.ContainsKey(cmd))
        {
            ErrorCodeCallback[cmd] -= callback;
        }
    }
}

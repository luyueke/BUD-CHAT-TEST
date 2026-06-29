using System.Threading;
using System;
using System.Collections.Concurrent;
using NetEngine.src.Net;
using NetEngine.src.Sender;
using NetEngine.src.Util;
using NetEngine.src.Util.Def;
using Pb.Base;

namespace NetEngine.src {
    public enum ENetworkType
    {
        Common=1,
        LockStep=2,
    }
    public class Sdk {
        /// <summary>
        /// 唯一实例对象
        /// </summary>
        private static Sdk instance = null;

        private SocketClient _socket1 = null;
        private SocketClient _socket2 = null;
        private FrameSender _frameSender = null;
        private Action<ResponseEvent> _initRspCallback;
        public int networkGroupType = 0;
       
        public Sdk (GameInfoPara gameInfo, ConfigPara config) {
            if (Instance != null) return;
            Instance = this;
            networkGroupType = (int)ENetworkType.Common | (int)ENetworkType.LockStep;
            // 合并游戏信息
            GameInfo.Assign (gameInfo);
            Config.Assign (config);
            RequestHeader.GameId = gameInfo.GameId;
            RequestHeader.PlayerId = gameInfo.OpenId;
        }
       
        /// <summary>
        /// 唯一实例对象
        /// </summary>
        public static Sdk Instance {
            get => instance;
            set => instance = value;
        }
        public static BstCallbacks BstCallbacks { get; } = new BstCallbacks ();

        public static ErrCode ErrCode { get; set; }
        public static void Uninit ()
        {
            Sdk.Instance = null;
            
        }

        public static void UpdateSdk () {
            Instance._frameSender = Core.FrameSender;
            Instance._socket1 = Core.Socket1;
            Instance._socket2 = Core.Socket2;
        }

        public void OnUpdate(float deltaTime)
        {
        }

        public void ClearResponse () {
            BstCallbacks.ClearCallbacks();
        }

        public SocketClient GetSocket (ConnectionType type) {
            switch (type) {
                case ConnectionType.Common:
                    return _socket1;
                case ConnectionType.Frame:
                    return _socket2;
                default:
                    return null;
            }
        }

        public void Init (Action<ResponseEvent> callback) {
            // 初始化成功修改playerid
            this._initRspCallback = callback;
            Core.InitSdk ();
        }

        /**
         * sdk 初始化回调
         */
        public void InitRsp (ResponseEvent eve) {
            this._initRspCallback (eve);
        }

        public bool IsInited () {
            return SdkStatus.IsInited ();
        }

        public void CloseConnect(ConnectionType socketType)
        {
            Core.CloseConnnect(socketType);
        }

        public void OpenConnect(ConnectionType socketType)
        {
            Core.OpenConnnect(socketType);
        }
       
        /// <summary>
        /// 加入房间 
        /// </summary>
        public static void JoinRoom (JoinRoomPara para, string roomId, Action<ResponseEvent> callback) {
            // var req = new JoinRoomReq {
            //     PlayerInfo = new PlayerInfo {
            //         Name = para.PlayerInfo.Name,
            //         CustomProfile = para.PlayerInfo.CustomProfile,
            //         CustomPlayerStatus = para.PlayerInfo.CustomPlayerStatus,
            //         Id = RequestHeader.PlayerId,
            //         ImageChosenDataJson = para.PlayerInfo.ImageChosenDataJson,
            //     },
            //     TeamId = "0",
            //     JoinType = JoinRoomType.CommonJoin,
            //     RoomId = roomId ?? "",
            //     SessionId = para.SessionId
            // };
            // Core.Room.JoinRoom (req, callback);
        }

        // 加入团队房间
        public static void JoinTeamRoom (JoinTeamRoomPara para, string roomId, Action<ResponseEvent> callback) {
            // Debugger.Log ("join team room para: {0} {1} {2}", para.teamId, JoinRoomType.CommonJoin, para.roomId);
            // var req = new JoinRoomReq {
            //     PlayerInfo = new PlayerInfo {
            //         Name = para.PlayerInfo.Name,
            //         CustomProfile = para.PlayerInfo.CustomProfile,
            //         CustomPlayerStatus = para.PlayerInfo.CustomPlayerStatus,
            //         Id = RequestHeader.PlayerId,
            //         ImageChosenDataJson = para.PlayerInfo.ImageChosenDataJson,
            //     },
            //     TeamId = para.TeamId,
            //     JoinType = JoinRoomType.CommonJoin,
            //     RoomId = roomId ?? ""
            // };
            // Core.Room.JoinRoom (req, callback);
        }
        
        /// <summary>
        /// 加入房间 
        /// </summary>
        public static void EnterRoom (EnterRoomPara para, Action<ResponseEvent> callback) {
            // var req = new EnterRoomReq {
            //     RoomName = para.RoomName,
            //     RoomType = para.RoomType,
            //     MaxPlayers = para.MaxPlayers,
            //     IsPrivate = para.IsPrivate,
            //     CustomProperties = para.CustomProperties,
            //     PlayerInfo = new PlayerInfo {
            //         Name = para.PlayerInfo.Name,
            //         CustomProfile = para.PlayerInfo.CustomProfile,
            //         CustomPlayerStatus = para.PlayerInfo.CustomPlayerStatus,
            //         Id = RequestHeader.PlayerId,
            //         ImageChosenDataJson = para.PlayerInfo.ImageChosenDataJson,
            //         Lang = para.PlayerInfo.Lang,
            //         Locale = para.PlayerInfo.Locale,
            //         WalletAddr = para.PlayerInfo.WalletAddr,
            //     },
            //     SessionId = para.SessionId,
            //     RoomId = para.RoomId,
            //     GameData = para.GameData,
            //     MaxBlood = para.MaxBlood,
            //     MapId = para.MapId,
            // };
            // Core.Room.EnterRoom (req, callback);
        }

        // 退出房间
        public static void LeaveRoom (LeaveRoomPara para, Action<ResponseEvent> callback) {
            // var req = new LeaveRoomReq {
            //     PlayerId = para.PlayerId,
            //     RoomId = para.RoomId
            // };
            // Core.Room.LeaveRoom (req, callback);
        }

        // 解散房间
        public static void DismissRoom (Action<ResponseEvent> callback) {
            // var req = new DismissRoomReq ();
            // Core.Room.DismissRoom (req, callback);
        }

        // 移除房间内玩家
        public static void RemovePlayer (RemovePlayerPara para, Action<ResponseEvent> callback) {
            // var req = new RemovePlayerReq {
            //     RemovePlayerId = para.RemovePlayerId
            // };
            // Core.Room.RemoveUser (req, callback);
        }

        /// <summary>
        /// 获取房间信息
        /// </summary>
        public static void GetRoomByRoomId (GetRoomByRoomIdPara getRoomByRoomIdPara, Action<ResponseEvent> callback) {
            // var para = new GetRoomByRoomIdReq {
            //     RoomId = getRoomByRoomIdPara.RoomId,
            //     SessionId = getRoomByRoomIdPara.SessionId,
            //     PlayerId = getRoomByRoomIdPara.PlayerId
            // };
            // Core.Room.GetRoomByRoomId (para, callback);
        }

        public void ClientSendToServer (SendToGameSvrPara para, string roomId, Action<ResponseEvent> callback)
        {
            // var req = new SendToGameSvrReq {
            //     PlayerId = RequestHeader.PlayerId,
            //     RoomId = roomId,
            //     Data = JsonConvert.SerializeObject(para.Data),
            // };
            
            // Core.Room.ClientSendToServer(req, callback);
        }

        // 设置帧同步房间
        public bool SetFrameRoom (RoomInfoRsp roomInfo) {
            // if (roomInfo?.Players == null) return false;
            //if (roomInfo.PlayerList.All (info => !info.Id.Equals (RequestHeader.PlayerId))) return false;
            // if (roomSyncType==ERoomSyncType.LockStep)
            // {
                // _frameSender.SetFrameRoom (roomInfo);
            // }

            // if (roomSyncType==ERoomSyncType.KeyFrame)
            // {
                // keyFrameSender.SetFrameRoom(roomInfo);
            // }
           
            return true;
        }

        public void ConnectFrameSocket(Action<ResponseEvent> callback)
        {
            _socket2.Url = Config.Url + ":" + Port.TcpRelayPort2;
                
                Debugger.Log("_socket2 go to Connect:" + _socket2.Url);
                if (!_socket2.IsSocketStatus("connect"))
                {          
                    _socket2.EventOnceHandlers.Clear();
                    _socket2.OnceEvent("connect", (SocketEvent e) =>
                    {
                        Debugger.Log("_socket2 Connect:" + _socket2.Url);
                       // 清空事件列表
                       _socket2.EventOnceHandlers.Clear();
                        _socket2.SetScocketStatus(SocketState.Open);
                    });
                    // StartFrame();
                    _socket2.OnceEvent("connectClose", (SocketEvent e) =>
                    {
                        Debugger.Log("STARTFRAMESYNC fail at SocketEventType.connectClose");
                        _socket2.EventOnceHandlers.Clear();
                        StartFrameSyncFailRsp(new ResponseEvent(ErrorCode.EcSdkSocketError, null, null),
                            callback);
                            

                    });
                    _socket2.OnceEvent("connectError", (SocketEvent e) =>
                    {
                        Debugger.Log("STARTFRAMESYNC fail at SocketEventType.connectError");
                        _socket2.EventOnceHandlers.Clear();
                        SdkUtil.UnityLog("###StartFrameSync Connect:connectError");
                        StartFrameSyncFailRsp(new ResponseEvent(ErrorCode.EcSdkSocketError, null, null),
                            callback);
                    });
                     _socket2.ConnectSocketTask("SDK startFrameSync");
                }
        }

        // 开始帧同步
        public void StartFrameSync (Action<ResponseEvent> callback) {
            var roomInfo = _frameSender?.RoomInfo;
            if (roomInfo == null) {
                // StartFrameSyncFailRsp (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom), callback);
                return;
            }

            void StartFrame () {
                // var req = new StartFrameSyncReq { };
                // req.RoomId = roomInfo?.Id;
                // req.PlayerId = Player.Id; 
                // _frameSender.StartFrameSync(req, callback);
                //_frameSender.CheckLogin (eve => {
                //    if (eve.Code == ErrCode.EcOk) {
                //        Debugger.Log ("STARTFRAMESYNC start {0}", GameInfo.GameId);
                //        var req = new StartFrameSyncReq { };
                //        _frameSender.StartFrameSync (req, callback);
                //    } else {
                //        Debugger.Log ("STARTFRAMESYNC fail at CheckLogin, seq= {0}, code={1} {2}", eve.Seq, eve.Code,
                //            roomInfo);
                //        StartFrameSyncFailRsp (
                //            new ResponseEvent (ErrCode.EcSdkNoCheckLogin, "CheckLogin失败, seq=" + eve.Seq, null, null),
                //            callback);
                //    }
                //}, "sdk startFrame");
            }

            // _socket2.CloseSocketTask(Connect, null);
            // if(_socket1.IsSocketStatus("connect"))
            // {
            //     Connect();
            // }else{
            //     _socket2.CloseSocketTaskAndClear(Connect,null);
            // }
            Connect();

            void Connect()
            {
                //_socket2.Url = Config.Url + ":" + Port.GetRelayPort();
                _socket2.Url = Config.Url + ":" + Port.TcpRelayPort2;
                
                Debugger.Log("_socket2 go to Connect:" + _socket2.Url);
                if (!_socket2.IsSocketStatus("connect"))
                {          
                    _socket2.EventOnceHandlers.Clear();
                    _socket2.OnceEvent("connect", (SocketEvent e) =>
                    {
                        Debugger.Log("_socket2 Connect:" + _socket2.Url);
                       // 清空事件列表
                       SdkUtil.UnityLog("###StartFrameSync _socket2:connect:"+ _socket2.Url);
                       _socket2.EventOnceHandlers.Clear();
                        _socket2.SetScocketStatus(SocketState.Open);
                        StartFrame();
                    });
                    // StartFrame();
                    _socket2.OnceEvent("connectClose", (SocketEvent e) =>
                    {
                        Debugger.Log("STARTFRAMESYNC fail at SocketEventType.connectClose");
                        SdkUtil.UnityLog("###StartFrameSync Connect:connectClose");
                        _socket2.EventOnceHandlers.Clear();
                        // StartFrameSyncFailRsp(new ResponseEvent(ErrCode.EcSdkSocketError, "Socket错误", null, null),
                        // StartFrameSyncFailRsp(new ResponseEvent(ErrCode.EcSdkSocketError, null, null),
                        //     callback);
                            

                    });
                    _socket2.OnceEvent("connectError", (SocketEvent e) =>
                    {
                        Debugger.Log("STARTFRAMESYNC fail at SocketEventType.connectError");
                        _socket2.EventOnceHandlers.Clear();
                        SdkUtil.UnityLog("###StartFrameSync Connect:connectError");
                        // StartFrameSyncFailRsp(new ResponseEvent(ErrCode.EcSdkSocketError, "Socket错误", null, null),
                        // StartFrameSyncFailRsp(new ResponseEvent(ErrCode.EcSdkSocketError, null, null),
                        //     callback);
                    });

                     _socket2.ConnectSocketTask("SDK startFrameSync");
                }
                else
                {
                    StartFrame();
                }
            }
        }


        private static void StartFrameSyncFailRsp (ResponseEvent eve, Action<ResponseEvent> callback) {
            callback?.Invoke (eve);
        }

        // 禁止帧同步
        public void StopFrameSync (Action<ResponseEvent> callback) {
            var roomInfo = _frameSender.RoomInfo;
            if (roomInfo == null) {
                // callback?.Invoke (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom, "未找到帧同步房间，请确认", "", null));
                // callback?.Invoke (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom,  "", null));
                return;
            }
            // var req = new StopFrameSyncReq { };
            // req.RoomId = roomInfo?.Id;
            // req.PlayerId = Player.Id;
            // _frameSender.StopFrameSync (req, (eve) => {
            //     if (eve.Code == ErrCode.EcOk) callback?.Invoke (eve);
            // });
        }

        // 发送帧同步数据
        // public void SendFrame (SendFramePara para, Action<ResponseEvent> callback) {
        //     var roomInfo = _frameSender?.RoomInfo;
        //     if (roomInfo == null) {
        //         callback?.Invoke (new ResponseEvent (ErrorCode.EcRoomPlayerNotInRoom, "", null));
        //         return;
        //     }

        //     var req = new SendFrameReq {
        //         RoomCode = roomInfo.RoomCode,
        //         Item = new FrameItem {
        //             PlayerId = RequestHeader.PlayerId,
        //             Timestamp = Convert.ToUInt64 (SdkUtil.GetCurrentTimeMilliseconds ())
        //         }
        //     };
        //     _frameSender.SendFrame (req, callback);
        // }
        // public void SendKeyFrame (SendKeyFrameReq req, Action<ResponseEvent> callback) {
        //     // var roomInfo = keyFrameSender?.RoomInfo;
        //     if (!Core.Socket3.IsSocketStatus("connect"))
        //     {
        //         callback?.Invoke (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom, "未找到帧同步房间，请确认", "", null));
        //         return;
        //     }
        //     keyFrameSender.SendFrame (req, callback);
        // }

        // public void SendEmpty2Server(EmptyMsgReq req, Action<ResponseEvent> callback)
        // {
        //     if (!Core.Socket3.IsSocketStatus("connect"))
        //     {
        //         callback?.Invoke (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom, "未找到帧同步房间，请确认", "", null));
        //         return;
        //     }
        //     keyFrameSender.SendEmpty2Server(req,callback);
        // }
        // public void SendSnapshot2Server(SendSnapshotReq req, Action<ResponseEvent> callback)
        // {
        //     if (!Core.Socket3.IsSocketStatus("connect"))
        //     {
        //         callback?.Invoke (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom, "未找到帧同步房间，请确认", "", null));
        //         return;
        //     }
        //     keyFrameSender.SendSnapshot (req, callback);
        // }

        // public void StopKeyFrame()
        // {
        //     StopKeyFrameSyncReq req=new StopKeyFrameSyncReq();
        //     req.PlayerId = RequestHeader.PlayerId;
        //     req.RoomId = Global.Room.RoomInfo.Id;
        //     keyFrameSender.SendStopKeyFrameSync(req,null);
        //     
        // }
        // 请求补帧
        // public void RequestFrame (RequestFramePara para, Action<ResponseEvent> callback) {
        //     var roomInfo = _frameSender?.RoomInfo;
        //     if (roomInfo == null) {
        //         // callback?.Invoke (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom, "未找到帧同步房间，请确认", "", null));
        //         callback?.Invoke (new ResponseEvent (ErrCode.EcRoomPlayerNotInRoom, "", null));
        //         return;
        //     }
        //     if (para.BeginFrameId < 0 || para.EndFrameId < 0) {
        //         // callback?.Invoke (new ResponseEvent (ErrCode.EcParamsInvalid, "非法参数，请确认", "", null));
        //         callback?.Invoke (new ResponseEvent (ErrCode.EcParamsInvalid, "", null));
        //         return;
        //     }
        //
        //     ////////////// 批量补帧 //////////////
        //     const int maxFrameNum = 2000 - 1;
        //     const bool supportPartial = true;
        //     string roomId = roomInfo.RoomCode;
        //
        //     long beginFrameId = para.BeginFrameId;
        //     long endFrameId = Math.Min(beginFrameId + maxFrameNum, para.EndFrameId);
        //
        //     List<Frame> frames = new List<Frame>();
        //     Action<ResponseEvent> cb = null;
        //
        //     cb = (responseEvent) =>
        //     {
        //         if (responseEvent.Code != 0)
        //         {
        //             callback?.Invoke(responseEvent);
        //             return;
        //         }
        //
        //         RequestFrameRsp requestFrameRsp = (RequestFrameRsp) responseEvent.Data;
        //
        //         frames.AddRange(requestFrameRsp.Frames);
        //
        //         if (requestFrameRsp.IsPartial && frames.Count > 0)
        //         {
        //             Frame lastFrame = frames[frames.Count - 1];
        //             endFrameId = (long) lastFrame.Id;
        //         }
        //
        //         if (endFrameId < para.EndFrameId)
        //         {
        //             beginFrameId = endFrameId + 1;
        //             endFrameId = Math.Min(beginFrameId + maxFrameNum, para.EndFrameId);
        //
        //             var tmpReq = new RequestFrameReq
        //             {
        //                 RoomId = roomId,
        //                 BeginFrameId = Convert.ToUInt64(beginFrameId),
        //                 EndFrameId = Convert.ToUInt64(endFrameId),
        //                 SupportPartial = supportPartial,
        //             };
        //
        //             _frameSender.RequestFrame(tmpReq, cb);
        //
        //             return;
        //         }
        //
        //         // 补帧结束
        //         requestFrameRsp.IsPartial = false;
        //         requestFrameRsp.Frames.Clear();
        //         requestFrameRsp.Frames.AddRange(frames);
        //
        //         callback?.Invoke(responseEvent);
        //         return;
        //     };
        //
        //     var req = new RequestFrameReq
        //     {
        //         RoomId = roomId,
        //         BeginFrameId = Convert.ToUInt64(beginFrameId),
        //         EndFrameId = Convert.ToUInt64(endFrameId),
        //         SupportPartial = supportPartial,
        //     };
        //
        //     _frameSender.RequestFrame(req, cb);
        // }

        // 房间内发送信息
        public static void SendToClient(SendToClientPara para, string roomId, Action<ResponseEvent> callback)
        {
            // 如果玩家列表为空，直接回调成功
            if (para.RecvPlayerList.Count == 0)
            {
                callback?.Invoke(new ResponseEvent(ErrCode.EcOk, "", null));
                return;
            }

            // var req = new SendToClientReq
            // {
            //     PlayerId = para.PlayerId,
            //     RoomId = roomId,
            //     Msg = para.Msg,
            // };
            // req.RecvPlayerList.AddRange(para.RecvPlayerList);
            // Core.Sender.SendMessage(req, callback);
        }

        // 发自定义服务消息
        public void SendToGameSvr (SendToGameSvrPara para, string roomId, Action<ResponseEvent> callback) {
//             var req = new SendToGameSvrReq {
//                 PlayerId = RequestHeader.PlayerId,
//                 RoomId = roomId,
// #if UNITY_5_3_OR_NEWER
//                 Data = JsonUtility.ToJson (para.Data),
// #else
//                 Data = JsonConvert.SerializeObject(para.Data),
// #endif
//             };
//             _frameSender.SendMessageExt (req, callback);
        }


        //获取当前线程数
        public static int GetThreadCount()
        {
            int MaxWorkerThreads, miot, AvailableWorkerThreads, aiot;  
            //获得最大的线程数量  
            ThreadPool.GetMaxThreads(out MaxWorkerThreads, out miot);  
            
            AvailableWorkerThreads = aiot = 0;  
            //获得可用的线程数量  
            ThreadPool.GetAvailableThreads(out AvailableWorkerThreads, out aiot);  
            //返回线程池中活动的线程数  
            return MaxWorkerThreads - AvailableWorkerThreads;
        }

        public static long GetSystemTime() {
            return (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }
    }
}

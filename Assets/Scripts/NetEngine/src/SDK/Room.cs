/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-09-14 10:16:19
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-18 19:03:11
 * @ Description: 客户端的房间管理
 */

using System;
using System.Linq;
using Google.Protobuf;
using NetEngine.src.SDK;
using NetEngine.src.Broadcast;
using NetEngine.src;
using NetEngine.src.Util.Def;
using Pb.Base;
using NetEngine.src.Util;
using GameData.GameSync;

namespace NetEngine
{
    /********************************* SDK Room对象 *********************************/
    public class Room : RoomBroadcastHandler
    {
        public RoomBroadcast RoomBroadcast { get; set; }

        public RoomInfoRsp ClientData { get; private set; } // 同步服务器的当前房间的信息

        /// <summary>
        /// 在第一次GetGameServer成功之后，客户端的房间就创建了
        /// </summary>
        public Room(string roomCode, GetGameServerReq gameServerInfo) : base()
        {
            ClientData = new RoomInfoRsp();
            ClientData.RoomCode = roomCode;
            ClientData.RoomType = (RoomType)gameServerInfo.roomType;
            ClientData.RoomSubType = (RoomSubType)gameServerInfo.roomSubType;
            RoomBroadcast = new RoomBroadcast(this);
        }

        /// <summary>
        /// 房间信息更新接口
        /// onUpdate 表明 Room 实例的 roomInfo 信息发生变化，这种变化原因包括各种房间操作、房间广播、本地网络状态变化等。
        /// 开发者可以在该接口中更新游戏画面，或者使用 networkState 属性判断网络状态。
        /// </summary>
        /// <param name="room"></param>
        public Action<Room,string,ResponseEvent> OnUpdate = (room,Tag, eve) => { };

        /// <summary>
        /// 获取客户端本地 SDK 网络状态
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool GetNetworkState(ConnectionType type)
        {
            try {
                var socket = Sdk.Instance.GetSocket(type);
                // return socket != null && socket.IsSocketStatus ("connect") && Core.Pinger1.IsResposne();
                //修改双心跳后不依赖Pinger1做判定
                return socket != null && socket.IsSocketStatus("connect");
            } catch (Exception e) {

                LoggerUtils.LogError("GetNetworkState error: " + e.Message);
                return false;
            }

        }

        public void SendFrame(SendFramePara para, Action<ResponseEvent> callback)
        {
            // this.RoomUtil.ActiveFrame();
            // Sdk.Instance.SendFrame(para, (eve) =>
            // {
            //     if (eve.Data != null)
            //     {
            //         var rsp = (SendFrameRsp)eve.Data;
            //         eve.Data = rsp;
            //     }

            //     callback?.Invoke(eve);
            // });
        }
        /**
         * @doc Room.requestFrame
         * @name 请求补帧
         * @description 调用结果将在 callback 中异步返回。
         * @param {SDKType.RequestFramePara} requestFramePara  请求补帧参数
         * @param {SDKType.ReqCallback<SDKType.RequestFrameRsp>} callback  响应回调函数
         * @returns {void}
         */
        // public void RequestFrame(RequestFramePara para, Action<ResponseEvent> callback)
        // {
        //     this.RoomUtil.ActiveFrame();
        //
        //     void Eve(ResponseEvent eve)
        //     {
        //         // Debugger.Log("request frame rsp");
        //         if (eve.Data != null)
        //         {
        //             var rsp = (RequestFrameRsp)eve.Data;
        //             var frames = new List<Frame>();
        //             foreach (var item in rsp.Frames)
        //             {
        //                 var frame = new Frame
        //                             {
        //                                 // Id = item.Id,
        //                                 // Ext = item.Ext,
        //                                 // Time = Convert.ToInt64(SdkUtil.GetCurrentTimeSeconds()),
        //                                 // RoomId = RoomInfo.Id,
        //                                 // IsReplay = true
        //                             };
        //                 frame.Items.AddRange(item.Items);
        //                 frames.Add(frame);
        //             }
        //
        //             rsp.Frames.Clear();
        //             rsp.Frames.AddRange(frames);
        //             eve.Data = rsp;
        //         }
        //
        //         callback?.Invoke(eve);
        //     }
        //
        //     Sdk.Instance.RequestFrame(para, Eve);
        // }

        public void RetryAutoRequestFrame()
        {
            RoomBroadcast.FrameBroadcast.RetryFill(this);
        }

        // public void SendToClient(SendToClientPara para, Action<ResponseEvent> callback)
        // {
        //     var recvPlayerList = para.RecvPlayerList;
        //     switch (para.RecvType)
        //     {
        //         case RecvType.RoomAll:
        //         {
        //             // 发给所有玩家
        //             recvPlayerList.AddRange(RoomInfo.Players.Select(info => info.Uid));
        //             break;
        //         }
        //         case RecvType.RoomOthers:
        //         {
        //             // 不包含自己的其他玩家
        //             recvPlayerList.AddRange(from info in RoomInfo.Players
        //                                     where !info.Uid.Equals(RequestHeader.PlayerId)
        //                                     select info.Uid);
        //             break;
        //         }
        //         case RecvType.RoomSome:
        //             break;
        //         default:
        //         {
        //             // callback?.Invoke(new ResponseEvent(ErrCode.EcParamsInvalid, "参数错误，消息接收者类型无效", "", null));
        //             callback?.Invoke(new ResponseEvent(ErrCode.EcParamsInvalid, "", null));
        //             return;
        //         }
        //     }

        //     var callbackPara = new SendToClientPara
        //                        {
        //                            RecvPlayerList = recvPlayerList,
        //                            Msg = para.Msg
        //                        };
        //     Sdk.SendToClient(para, RoomInfo.RoomCode, callback);
        // }

        // public void SendToGameSvr(SendToGameSvrPara para, Action<ResponseEvent> callback)
        // {
        //     Sdk.Instance.SendToGameSvr(para, RoomInfo.RoomCode, callback);
        // }

        //区别于之前的SendToGameSvr接口，新版用Room的WebSocket发送
        // public void ClientSendToServer(SendToGameSvrPara para, Action<ResponseEvent> callback)
        // {
        //     Sdk.Instance.ClientSendToServer(para, RoomInfo.RoomCode, callback);
        // }
    }
}

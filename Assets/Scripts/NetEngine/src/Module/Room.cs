using System;
using Pb.Base;
using NetEngine.src.Net;
using NetEngine.src.Util;

namespace NetEngine.src.Room
{
    public class Room : BaseNetUtil
    {
        public Room(BstCallbacks bstCallbacks) : base(bstCallbacks)
        {
            // 注册广播
            this.SetBroadcastHandler(ClientSendServerCmd.EPushPlayerEnter, this.OnPlayerEnter);
            this.SetBroadcastHandler(ClientSendServerCmd.EPushPlayerLeave, this.OnPlayerLeave);
            this.SetBroadcastHandler(ClientSendServerCmd.EPushFrameData, this.OnBstFrameData);
            this.SetBroadcastHandler(ClientSendServerCmd.EPushPlayerSyncProp, this.OnCommonSyncBst);
        }

        ///////////////////////////////// 请求 //////////////////////////////////

        // 进入房间
        public string EnterRoom(EnterRoomReq para, Action<ResponseEvent> callback)
        {
            const int subcmd = (int)ClientSendServerCmd.ECmdRoomEnterReq;
            var response = new NetResponseCallback(CommonResponse);
            var seq = this.Send(para, subcmd, response, callback);
            return seq;
        }

        // 离开房间
        public string LeaveRoom(LeaveRoomReq para, Action<ResponseEvent> callback)
        {
            const int subcmd = (int)ClientSendServerCmd.ECmdRoomLeaveReq;
            var response = new NetResponseCallback(CommonResponse);
            var seq = this.Send(para, subcmd, response, callback);
            return seq;
        }

        /// <summary>
        /// 同步房间信息
        /// </summary>
        public string SyncRoomInfo(string roomCode, Action<ResponseEvent> callback)
        {
            var para = new RoomInfoReq()
            {
                RoomCode = roomCode,
            };
            const int subcmd = (int)ClientSendServerCmd.ECmdRoomInfoReq;
            var response = new NetResponseCallback(CommonResponse);
            var seq = this.Send(para, subcmd, response, callback);
            return seq;
        }

        /// <summary>
        /// 同步当前地图信息
        /// </summary>
        public string SyncMapInfo(string mapId, string roomCode,string playerId, Action<ResponseEvent> callback)
        {
            var para = new Pb.Base.MapInfoReq()
            {
                MapId = mapId,
                RoomCode = roomCode,
                PlayerId = playerId
            };
            const int subcmd = (int)ClientSendServerCmd.ECmdMapInfoReq;
            var response = new NetResponseCallback(CommonResponse);
            var seq = this.Send(para, subcmd, response, callback);
            return seq;
        }


        public string SendCommonSync(CommonSyncReq syncReq, Action<ResponseEvent> callback)
        {
            const int subcmd = (int)ClientSendServerCmd.ECmdRoomSyncPropReq;
            var response = new NetResponseCallback(CommonResponse);
            var seq = this.Send(syncReq, subcmd, response, callback);
            return seq;
        }


        ///////////////////////////////// 响应 //////////////////////////////////

        private void CommonResponse(bool send, DecodeRspResult res, Action<ResponseEvent> callback)
        {
            var rspPacket = res.Packet;
            var eve = new ResponseEvent(rspPacket.Code, rspPacket.Seq, res.Body);
            callback?.Invoke(eve);
            return;
        }

        ////////////////////////////////////// 广播  /////////////////////////////////////////

        private void OnPlayerEnter(DecodeRspResult bst, string seq)
        {
            var eve = new BroadcastEvent(bst.Body, seq);
            this.bstCallbacks.InnerRoomBst.OnPlayerEnter(eve);
        }

        private void OnPlayerLeave(DecodeRspResult bst, string seq)
        {
            var eve = new BroadcastEvent(bst.Body, seq);
            this.bstCallbacks.InnerRoomBst.OnPlayerLeave(eve);
        }


        private void OnBstFrameData(DecodeRspResult bst, string seq)
        {
            var eve = new BroadcastEvent(bst.Body, seq);
            this.bstCallbacks.InnerRoomBst.OnBstFrameData(eve);
        }

        private void OnCommonSyncBst(DecodeRspResult bst, string seq)
        {
            var eve = new BroadcastEvent(bst.Body, seq);
            this.bstCallbacks.InnerRoomBst.OnCommonSyncBst(eve);
        }
    }
}
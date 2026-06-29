using System;
using System.Collections.Generic;
using Pb.Base;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.TestTools;
using GooglePB = global::Google.Protobuf;

namespace NetEngine.src.Util
{
    public struct DecodeRspResult
    {
        public DecodeRspResult(ServerSendClientRsp packet, object data) : this()
        {
            Packet = packet;
            Body = data;
        }

        public ServerSendClientRsp Packet { get; set; }

        public bool IsBst {get;set;} // true: 广播， false: 单播

        public object Body { get; set; }

        public override string ToString()
        {
            string str = "{\"Seq\": \"" + this.Packet.Seq +
                         "\", \"Data\": " + this.Body?.ToString()
                         + "}";
            return str;
        }
    }

    public class Pb
    {
        public static Dictionary<int, Func<ByteString, object>>
            rspDic = new Dictionary<int, Func<ByteString, object>>();

        public static Dictionary<int, Func<ByteString, object>>
            bstDic = new Dictionary<int, Func<ByteString, object>>();

        public static void Init()
        {
            if (rspDic.Count + bstDic.Count != 0)
            {
                return;
            }

            // 设置解包方法
            ///////////////////////////// 响应 /////////////////////////////
            rspDic.Add((int)ClientSendServerCmd.ECmdRoomEnterReq,
                (data) => convert(data, new EnterRoomRsp()));
            rspDic.Add((int)ClientSendServerCmd.ECmdRoomLeaveReq,
                (data) => convert(data, new LeaveRoomRsp()));
            rspDic.Add((int)ClientSendServerCmd.ECmdRoomInfoReq,
                (data) => convert(data, new RoomInfoRsp()));
            rspDic.Add((int)ClientSendServerCmd.ECmdMapInfoReq,
                (data) => convert(data, new MapInfoRsp()));
            rspDic.Add((int)ClientSendServerCmd.ECmdHeartBeatReq,
                (data) => convert(data, new HeartBeatRsp()));
            rspDic.Add((int)ClientSendServerCmd.ECmdRoomSyncPropReq,
                (data) => convert(data, new CommonSyncRsp()));

            ///////////////////////////// 广播 /////////////////////////////
            bstDic.Add((int)ClientSendServerCmd.EPushPlayerEnter,
                (data) => convert(data, new PlayerEnterBst()));
            bstDic.Add((int)ClientSendServerCmd.EPushPlayerLeave,
                (data) => convert(data, new PlayerLeaveBst()));
            bstDic.Add((int)ClientSendServerCmd.EPushFrameData,
                (data) => convert(data, new RecvFrameBst()));
            bstDic.Add((int)ClientSendServerCmd.EPushPlayerSyncProp,
                (data) => convert(data, new CommonSyncBst()));
        }

        private static object convert(ByteString data, GooglePB::IMessage tmp)
        {
            tmp.MergeFrom((ByteString)data);
            return tmp;
        }

        public static byte[] EncodeReq(ClientSendServerReq packet, GooglePB::IMessage data)
        {
            packet.Body = data.ToByteString();
            return packet.ToByteArray();
        }

        public static DecodeRspResult DecodeServerData(byte[] data, Func<string, int> getReqCmd)
        {
            var packet = new ServerSendClientRsp();
            packet.MergeFrom(data);
            bool isBst = bstDic.ContainsKey((int)packet.Cmd);
            object rsp = null;
            if (isBst)
            {
                // 广播
                Func<ByteString, object> func = null;
                bstDic.TryGetValue((int)packet.Cmd, out func);
                if (func != null)
                {
                    rsp = func(packet.Body);
                }

                #if UNITY_EDITOR
                    if (!bstDic.ContainsKey((int)packet.Cmd))
                    {
                        Debug.LogError($"请在[NetEngine.src.Util.PB]中注册广播[CMD={packet.Cmd}]的解析方法.");
                    }
                #endif
            } else {
                // 单播
                int cmd = getReqCmd(packet.Seq);
                if (cmd > 0 && rspDic.ContainsKey(cmd) && packet.Body != null)
                {
                    Func<ByteString, object> func = null;
                    rspDic.TryGetValue(cmd, out func);

                    if (func != null)
                    {
                        rsp = func(packet.Body);
                    }
                }

                #if UNITY_EDITOR
                    if (!rspDic.ContainsKey(cmd))
                    {
                        Debug.LogError($"请在[NetEngine.src.Util.PB]中注册单播[CMD={packet.Cmd}]的解析方法.");
                    }
                #endif
            }

            var rspResult = new DecodeRspResult
            {
                Packet = packet,
                Body = rsp,
                IsBst = isBst
            };

            return rspResult;
        }
    }
}
// MsgData.cs
// Create by xiaojl Mar/15/2023
// 消息数据

using System;
using System.IO;

namespace Network.Tcp.Core
{
    // 消息头部, 待整理
    internal class MsgHeader
    {
        public const UInt16 HeaderLength = 6;
    }

    // 消息数据
    internal class MsgData
    {
        public UInt16 Cmd { get; private set; }
        public byte[] Body { get; private set; }

        public MsgData(UInt16 cmd, byte[] body)
        {
            Cmd = cmd;
            Body = body;
        }

        public byte[] Encode()
        {
            var data = new byte[MsgHeader.HeaderLength + Body.Length];
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryWriter bw = new BinaryWriter(ms);
                bw.Write(Cmd);
                bw.Write(Body.Length);
                bw.Write(Body);
                return data;
            }
        }
    }
}
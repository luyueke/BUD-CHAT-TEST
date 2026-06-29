using Pb.Base;
using NetEngine.src.Util;

namespace NetEngine.src.Net
{
    public class NetServer : Net
    {
        public NetServer()
        {
        }

        public byte[] BuildData(byte[] data)
        {
            const byte pre = (byte)MessageDataTag.ServerPre;
            const byte end = (byte)MessageDataTag.ServerEnd;
            return BuildData(pre, data, end);
        }

        // 处理接受的广播消息
        public static void HandleMessage(DecodeRspResult repResult)
        {
            BroadcastCallback handler = null;
            BroadcastHandlers.TryGetValue(repResult.Packet.Cmd, out handler);
            
            handler?.Invoke(repResult, repResult.Packet.Seq);
        }

        // 设置广播回调
        public void SetBroadcastHandler(ClientSendServerCmd type, BroadcastCallback handler)
        {
            BroadcastHandlers.TryAdd(type, handler);
            bdhandlers.TryAdd(type, null);
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using Pb.Base;
using NetEngine.src.Util;
using NetEngine.src.Util.Def; // using System.Timers;


namespace NetEngine.src.Net
{
    public struct MessageWrapper
    {
        public byte Pre { get; set; }

        public byte End { get; set; }

        public byte[] Body { get; set; }
    };

    public class SendQueueValue
    {
        public Action<ErrorCode, string> sendFail = (ErrorCode errCode, string errMsg) => { };

        public Action sendSuccess = () => { };
        public Action resend = () => { };
        public Action remove = () => { };
        public Action<DecodeRspResult> response;

        public DateTime Time { get; set; }

        public bool IsSocketSend { get; set; }

        public int Cmd { get; set; }
    };

    public enum MessageDataTag : byte
    {
        ClientPre = 0x02,
        ClientEnd = 0x03,

        // 接收请求 - 发送响应
        ServerPre = 0x28,
        ServerEnd = 0x29,
    }

    public delegate void NetResponseCallback(bool send, DecodeRspResult result, Action<ResponseEvent> callback);

    public delegate void BroadcastCallback(DecodeRspResult bstResult, string seq);
    // public delegate void EventHandler(object sender, SocketEvent e);
    // public delegate void EventHandler(object sender, SocketEvent e, Action<byte[]> handleResponse);

    public class Net : IDisposable
    {
        protected static readonly ConcurrentDictionary<string, SendQueueValue> SendQueue =
            new ConcurrentDictionary<string, SendQueueValue>();

        protected static readonly ConcurrentDictionary<ClientSendServerCmd, BroadcastCallback> BroadcastHandlers =
            new ConcurrentDictionary<ClientSendServerCmd, BroadcastCallback>();

        private static readonly Timer Timer = new Timer();

        protected readonly ConcurrentDictionary<ClientSendServerCmd, Object> bdhandlers =
            new ConcurrentDictionary<ClientSendServerCmd, Object>();

        // 该实例对象的发送队列
        private readonly ConcurrentDictionary<string, Object> _queue;

        // public KCPSocket socket;
        // private QAppProtoErrCode ErrCode;
        private Action<DecodeRspResult> _handleResponse;
        private Action<DecodeRspResult> _handleBroadcast;

        public SocketClient SocketClient { get; set; }

        private int queueLimit = 1000;

        // 循环检测 sendQueue 中的消息发送
        public static void StartQueueLoop()
        {
            Timer.SetTimer(CheckSendQueue, Config.ResendInterval);
        }

        private static readonly Action CheckSendQueue = () =>
        {
            foreach (var val in SendQueue.Select(kv => kv.Value))
            {
                if (DateTime.Now.Subtract(val.Time).TotalMilliseconds > Config.ResendTimeout)
                {
                    ErrorCode code = ErrorCode.EcSdkResTimeout;
                    var msg = "";
                    // if (UserStatus.IsStatus(UserStatus.StatusType.Login))
                    // {
                    //     code = (int)ProtoErrCode.EcSdkResTimeout;
                    // }
                    // else
                    // {
                    //     // if (UserStatus.GetErrCode() == (int)ProtoErrCode.EcOk)
                    //     // {
                    //     //     code = (int)ProtoErrCode.EcSdkNoLogin;
                    //     //     msg = "登录失败";
                    //     // }
                    //     // else
                    //     // {
                    //     //     code = UserStatus.GetErrCode();
                    //     //     msg = "登录失败，" + UserStatus.GetErrMsg();
                    //     // }
                    // }

                    val.sendFail(code, msg);
                }
                else
                {
                    if (!val.IsSocketSend && DateTime.Now.Subtract(val.Time).TotalMilliseconds > Config.ResendInterval)
                    {
                        val.resend();
                    }
                }
            }
        };

        // 停止检测消息发送, 清空全部消息
        public static void StopQueueLoop()
        {
            Timer.Stop();
            foreach (var val in SendQueue.Select(kv => kv.Value))
            {
                val.remove();
            }

            SendQueue.Clear();
        }

        protected Net()
        {
            SocketClient = null;
            _queue = new ConcurrentDictionary<string, Object>();
        }

        // 绑定 socket 对象
        public bool BindSocket(SocketClient socketClient, Action<DecodeRspResult> handleResponse, Action<DecodeRspResult> handleBroadcast)
        {
            if (this.SocketClient != null || socketClient == null) return false;
            this.SocketClient = socketClient;

            this._handleResponse = handleResponse;
            this._handleBroadcast = handleBroadcast;

            if (this.SocketClient.IsMsgBind == false)
            {
                this.SocketClient.OnEvent("message", OnMessageEvent);
            }

            return true;
        }

        private void OnMessageEvent(SocketEvent socketEvent)
        {
            if (socketEvent.Data.Length == 0) return;
            var resData = socketEvent.Data;
            var msgWrap = UnpackBody(socketEvent.Data);

            int getCmdFunc(string _seq)
            {
                SendQueueValue _val = null;
                SendQueue.TryGetValue(_seq + "", out _val);

                if (_val == null)
                {
                    return -1;
                }
                return _val.Cmd;
            }
            
            var serData = Util.Pb.DecodeServerData(msgWrap.Body, getCmdFunc);
            // 单播和广播都走一个通知 (通过CMD判断)
            switch (msgWrap.Pre)
            {
                case (byte)MessageDataTag.ClientPre when (byte)MessageDataTag.ClientEnd == msgWrap.End:
                    if (serData.IsBst)
                    {
                        _handleBroadcast(serData);
                        // Debugger.LogWithSockId(SocketClient.Id, $"[Broadcast][CMD={serData.Packet.Cmd}] {serData.ToString()}");
                    }else {
                        _handleResponse(serData);
                         // Debugger.LogWithSockId(SocketClient.Id, $"[Response][CMD={serData.Packet.Cmd}] {serData.ToString()}");
                    }
                    break;
                case (byte)MessageDataTag.ServerPre when (byte)MessageDataTag.ServerEnd == msgWrap.End:
                    break;
            }
        }

        public void UnbindSocket()
        {
            SocketClient = null;
            this.ClearQueue();
            this.ClearBdHandlers();
        }

        // 构建请求数据
        protected static byte[] BuildData(byte pre, byte[] body, byte end)
        {
            var uintValue = (uint)(body.Length);
            // Debugger.Log("Build data body length: {0} {1}", body.Length, uintValue);

            var uintBytes = BitConverter.GetBytes(uintValue);
            Array.Reverse(uintBytes);
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(pre);
                writer.Write(uintBytes);
                writer.Write(body);
                writer.Write(end);
                return memory.ToArray();
            }
        }

        // 解析消息数据
        private static MessageWrapper UnpackBody(byte[] data)
        {
            using (var memory = new MemoryStream(data))
            using (var reader = new BinaryReader(memory))
            {
                var msg = new MessageWrapper { Pre = reader.ReadByte() };
                var pkgLenBytes = reader.ReadBytes(4);
                msg.Body = reader.ReadBytes(data.Length - 6);
                msg.End = reader.ReadByte();
                // Debugger.Log("{0}, {1}", msg.Pre, msg.End);
                return msg;
            }
        }

        // 清空该实例对象的消息队列
        public void ClearQueue()
        {
            var keys = this._queue.Keys;

            foreach (var seq in keys)
            {
                SendQueue.TryRemove(seq, out SendQueueValue s);
            }

            this._queue.Clear();
        }

        // 清空该实例对象的广播回调
        private void ClearBdHandlers()
        {
            var keys = this.bdhandlers.Keys;

            foreach (var type in keys)
            {
                BroadcastHandlers.TryRemove(type, out BroadcastCallback s);
            }

            bdhandlers.Clear();
        }

        // 向请求队列中添加记录
        protected void AddSendQueue(string seq, SendQueueValue value)
        {
            SendQueue.TryAdd(seq, value);
            this._queue.TryAdd(seq, null);
            if (_queue.Count > queueLimit)
            {
                DeleteSendQueue(_queue.First().Key);
            }
        }

        // 在请求队列中删除记录
        public void DeleteSendQueue(string seq)
        {
            SendQueue.TryRemove(seq, out SendQueueValue s);
            this._queue.TryRemove(seq, out Object q);
        }

        // 处理请求的响应错误码
        private bool HandleErrCode()
        {
            return false;
        }

        // 调用 Socket 发送消息
        protected string Send(byte[] data, string seq, ClientSendServerCmd subcmd)
        {
            var readyCode = GetReadyCode(subcmd);
            if (readyCode != 0)
            {
                HandleSendFail(seq, readyCode);
            }
            else if (data.Length > 1016 && SocketClient.Id == 1)
            {
                HandleSendFail(seq, ErrorCode.EcSdkRelayDataExceedLimited);
            }
            else
            {
                SocketClient.Send(data,
                    (code) => HandleSendFail(seq, code),
                    () => HandleSendSuccess(seq)
                );
            }

            return seq;
        }

        // 发送失败 Callback
        private void HandleSendFail(string seq, ErrorCode code)
        {
            SendQueueValue val = null;
            SendQueue.TryGetValue(seq + "", out val);
            if (val == null) return;

            // 处理 wssocket 帧长度超过 856B
            if (code == ErrorCode.EcSdkRelayDataExceedLimited ||
                DateTime.Now.Subtract(val.Time).TotalMilliseconds > Config.ResendTimeout)
            {
                var sendCode = UserStatus.GetErrCode() != ErrorCode.Ok ? UserStatus.GetErrCode() : code;
                val.sendFail(sendCode, null);
                return;
            }

            switch (code)
            {
                case ErrorCode.EcSdkUninit:
                    // 没有初始化
                    val.sendFail(code, null);
                    break;
                case ErrorCode.EcSdkNoLogin:
                    // 没登录
                    SocketClient.Emit("autoAuth", new SocketEvent());
                    return;
                case ErrorCode.EcSdkNoCheckLogin:
                {
                    // 没checklogin
                    SocketClient.Emit("autoAuth", new SocketEvent());
                    return;
                }
            }

            return;
        }

        // 发送成功 Callback
        private static void HandleSendSuccess(string seq)
        {
            SendQueueValue val = null;
            SendQueue.TryGetValue(seq + "", out val);
            if (seq == "" || val == null) return;
            val.sendSuccess();
        }

        private ErrorCode GetReadyCode(ClientSendServerCmd subcmd)
        {
            if (!SdkStatus.IsInited())
            {
                // 发送失败: 没有初始化 (login不需要初始化)
                var info = new PlayerInfo
                           {
                               Uid = ""
                           };
                GamePlayerInfo.SetInfo(info);
                UserStatus.SetStatus(UserStatus.StatusType.Logout);
                return ErrorCode.EcSdkUninit;
            }

            // 检测 socket
            if (SocketClient == null || string.IsNullOrEmpty(SocketClient.Url))
                return ErrorCode.EcSdkSendFail;

            // 帧同步链接，不再依赖socket1链接上
            if (SocketClient.Id == (int)ConnectionType.Common && !UserStatus.IsStatus(UserStatus.StatusType.Login))
                return ErrorCode.EcSdkNoLogin;

            if (SocketClient.Id == (int)ConnectionType.Frame && !CheckLoginStatus.IsChecked() &&
                (subcmd == ClientSendServerCmd.ECmdFrmaeSendReq 
                 // ||subcmd == ClientSendServerCmd.ECmdRelayRequestFrameReq 
                 ||subcmd == ClientSendServerCmd.ECmdHeartBeatReq 
                 // || ubcmd == ClientSendServerCmd.ECmdRelayClientSendtoGamesvrReq
                 ))
                return ErrorCode.EcSdkNoCheckLogin;

            // 发送消息
            return ErrorCode.Ok;
        }

        public void Dispose()
        {
        }
    }
}
using System;
using Pb.Base;
using NetEngine.src.Net;
using NetEngine.src.Util;
using NetEngine.src.Util.Def;

namespace NetEngine.src.Ping
{
    public class Pinger : BaseNetUtil
    {
        private const int _maxPingRetry = 4;
        
        private int Timeout
        {
            get
            {
                // if (this.Id == (int) ConnectionType.Common && Config.EnableUdp) return Config.PingTimeout / 2;
                // return Config.PingTimeout;

                return Config.PingTimeout;
            }
        }

        public enum StateEnum
        {
            Resposne,
            Timeout
        };

        public StateEnum State { get; set; } = StateEnum.Resposne;

        public bool IsResposne()
        {
            return State == StateEnum.Resposne;
        }

        public bool IsTimeout()
        {
            return State == StateEnum.Timeout;
        }

        public Timer PingTimer { get; set; } = new Util.Timer();

        public Timer PongTimer { get; set; } = new Util.Timer();

        public string CurrentSeq { get; set; } = "";

        public int Id { get; }

        public string routedId = "";

        public static int MaxPingRetry => _maxPingRetry;

        public int Retry { get; set; } = MaxPingRetry;

        public Pinger(BstCallbacks bstCallbacks, int id, string routeId) : base(bstCallbacks)
        {
            Debugger.Log($"pinger id={id}");
            this.Id = id;
            this.routedId = routeId;
        }

        ///////////////////////////////// PONG //////////////////////////////////
        public void Ping(Action<ResponseEvent> callback)
        {
            //if (string.IsNullOrEmpty (RequestHeader.AuthKey)) {
            //    return;
            //}
            var startTime = DateTime.Now;
            var routeId =routedId;
            var conType = GetConnectionType(Id);
            var body = new HeartBeatReq
            {
                ConType = conType,
                RouteId = routeId,
                PlayerId = Player.Id
            };
            PingTimer.Stop();
            void PongResposne(bool send, DecodeRspResult result, Action<ResponseEvent> cb)
            {
                Debugger.Log($"PongResposne {Id}  {send}");
                this.HandlePong(send, result, startTime);
            }

            var seq = this.Send (body, (int) ClientSendServerCmd.ECmdHeartBeatReq, PongResposne, callback);
            
            Debugger.Log("Ping {0} {1}", this.Id, seq);
            
            CurrentSeq = seq;
            this.PongTimer.SetTimer (() => HandlePongTimeout (seq), this.Timeout);

            this.client.SocketClient.Emit("pingSend", new SocketEvent());
        }

        public ConnectionType GetConnectionType(int id)
        {
            ConnectionType conType = ConnectionType.Common;
            switch (id)
            {
                case 0:
                    conType= ConnectionType.Common;
                    break;
                case 1:
                    conType= ConnectionType.Frame;
                    break;
                // case 2:
                //     conType = ConnectionType.KeyFrame;
                //     break;
            }

            return conType;
        }
        public void Stop()
        {
            PingTimer.Close();
            PongTimer.Close();
        }

        ///////////////////////////////// PONG //////////////////////////////////
        private void HandlePong(bool send, DecodeRspResult res, DateTime startTime)
        {
            PongTimer.Stop();

            Debugger.Log("Pong {0} {1} {2}", this.Id, res.Packet.Seq, send);

            if (!send)
            {
                this.HandlePongTimeout(res.Packet.Seq);
                return;
            }

            this.Retry = MaxPingRetry;
            // 清空发送队列
            this.client.ClearQueue();

            // 心跳的错误码单独处理
            var errCode = res.Packet.Code;

            // 上报心跳时延
            // if (this.Id == 1 && errCode == ErrCode.EcOk)
            // {
            //     EventUpload.PushPingEvent(
            //         new PingEventParam(Convert.ToInt64((DateTime.Now - startTime).TotalMilliseconds)));
            // }
            
            State = StateEnum.Resposne;
            this.client.SocketClient.Emit("pongResposne", new SocketEvent());

            this.PingTimer.SetTimer(() => this.Ping(null), this.Timeout);
        }

        //////////////////////////////// TIMEOUT ////////////////////////////////
        private void HandlePongTimeout(string seq)
        {
            Debugger.Log("HandlePongTimeout {0} {1}  {2}", this.Id, seq,this.client.SocketClient.Url);

            State = StateEnum.Timeout;

            // this.PongTimer.Stop();
            this.client.DeleteSendQueue(seq);
            this.Retry--;
            if (!seq.Equals(this.CurrentSeq)) return;
            if (this.client.SocketClient == null) return;

            // 针对 KCP 的逻辑
            // if (this.Id == (int) ConnectionType.Relay && Config.EnableUdp) {
            // //if (this.Id == (int) ConnectionType.Common && Config.EnableUdp) {
            //     if (this.Retry >= 0) {
            //         // 重试
            //         this.PingTimer.SetTimer (() => this.Ping (null), this.Timeout);
            //         this.client.Socket.Emit("pongTimeout", new SocketEvent());
            //         return;
            //     } else {
            //         this.Retry = MaxPingRetry;
            //     }
            // }
            // else
            // {
            //     this.client.Socket.Emit("pongTimeout", new SocketEvent());
            // }
            //双心跳
            if (Retry <= 0)
            {
                this.client.SocketClient.Emit("pongTimeout", new SocketEvent());
                this.client.ClearQueue();
                this.client.SocketClient.ConnectNewSocketTask(this.client.SocketClient.Url);
            }
            else
            {
                this.Ping(null);
            }
        }
    }
}
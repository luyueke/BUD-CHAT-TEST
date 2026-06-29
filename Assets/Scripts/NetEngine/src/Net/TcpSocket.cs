using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Pb.Base;
using NetEngine.src.Util;
using NetEngine.src.Util.Def;

namespace NetEngine.src.Net
{
    public class TcpSocket : BaseSocket
    {
        private readonly TcpSocketClient tcpSocketClient;
        private int port;
        private string url;
        private EndPoint endPoint;

        public TcpSocket(string url)
        {
            this.url = url.ToLower().Replace("wss://", "").Replace("ws://", "");
            var str = url.Split(':');
            this.port = Convert.ToInt32(str.Length > 1 ? url.Split(':')[1] : Port.GetRelayPort().ToString());
            this.url = str[0];
            var address = Dns.GetHostAddresses(this.url)[0];

            tcpSocketClient = new TcpSocketClient(this, address.MapToIPv4().ToString(), port);
        }
        public override void Send(byte[] data, Action<ErrorCode> fail, Action success)
        {
            tcpSocketClient.Send(data, fail, success);
        }

        public override void Connect()
        {
            ReadyState = SocketState.Connecting;
            //NoDelay参数设置为true
            tcpSocketClient.OptionNoDelay = true;
            if (!tcpSocketClient.ConnectAsync())
            {
                Debugger.Log($"Connect failed");
                ReadyState = SocketState.Closed;
            }
        }

        public void OnOpen()
        {
            this.ReadyState = SocketState.Open;
            base.onOpen();
        }

        public void OnClose()
        {
            ReadyState = SocketState.Closed;
            base.onClose();
        }

        public void OnMessage(SocketEvent ev)
        {
            base.onMessage(ev);
        }

        public void OnError(SocketEvent ev)
        {
            if (ReadyState == SocketState.Connecting)
                base.onError(ev);
        }
        

        public override void Close(Action success, Action fail)
        {
            ReadyState = SocketState.Closing;
            tcpSocketClient.Close(success, fail);
        }
    }
    
    public class TcpSocketClient : NetCoreServer.TcpClient
    {
        private readonly TcpSocket task;
        private byte[] lastRemainData = Array.Empty<byte>();
        private const int PACKLIMIT = 1 << 20;
        
        protected class Message
        {
            public byte[] data;
            public Int32 length;

            public Message()
            {
                length = 0;
                data = Array.Empty<byte>();
            }
            public bool IsComplete()
            {
                //  原始字符串头尾一个字节，长度四个字节
                return data.Length == length + 6;
            }

            public bool IsEmpty()
            {
                return data.Length == 0;
            }
        }
        
        public void Send(byte[] data, Action<ErrorCode> fail, Action success)
        {
            try
            {
                if (!this.SendAsync(data))
                {
                    fail?.Invoke(ErrorCode.EcSdkSendFail);
                    return;
                }
                // Debugger.Log ("data length send {0}", data.Length);
                success?.Invoke();
            }
            catch (Exception e)
            {
                // Debugger.Log ("send error {0}", e, ToString ());
                fail?.Invoke(ErrorCode.EcSdkSendFail);
            }
        }

        public void Close(Action success, Action fail)
        {
            Debugger.Log("###websocket new task close beigin：" + "   socket hashcode:" + Socket.GetHashCode());
            Task.Run(() =>
            {
                DisconnectAndStop();
                if (SdkUtil.IsDebug)
                {
                    int threadCount = Sdk.GetThreadCount();
                    Debugger.Log("###websocket new task close 线程ID:" + Thread.CurrentThread.ManagedThreadId +
                                     "   socket hashcode:" + this.Socket.GetHashCode() + "  threadCount:" + threadCount);
                }

                task.OnClose();
                success?.Invoke();
            });
        }

        public TcpSocketClient(TcpSocket task, string address, int port) : base(address, port)
        {
            this.task = task;
        }

        public void DisconnectAndStop()
        {
            DisconnectAsync();
            while (IsConnected)
                Thread.Yield();
        }

        protected override void OnConnected()
        {
            Debugger.Log($"TCP client connected a new session with Id {Id} endpoint {this.Endpoint}");
            task.OnOpen();
        }

        protected override void OnDisconnected()
        {
            Debugger.Log($"TCP client disconnected a session with Id {Id} endpoint {this.Endpoint}");
            task.OnClose();
        }

        protected override void OnReceived(byte[] buffer, long offset, long size)
        {
            byte[] tmp = new byte[lastRemainData.Length + size];
            Array.Copy(lastRemainData, 0, tmp, 0, lastRemainData.Length);
            Array.Copy(buffer, offset, tmp, lastRemainData.Length, size);
            offset = 0;
            var message = tryGetMessage(tmp, (int)offset, tmp.Length);
            while (message.IsComplete())
            {
                var eve = new SocketEvent
                {
                    Data = message.data
                };
                task.OnMessage(eve);
                offset += message.length + 6;
                message = tryGetMessage(tmp, (int)offset, tmp.Length);
            }

            lastRemainData = message.data;
        }

        protected Message tryGetMessage(byte[] data, int startPos, int size)
        {
            if (startPos >= data.Length)
            {
                return new Message { length = 0, data = new byte[0] };
            }

            if (size < startPos + 6)
            {
                return new Message{length = 0, data = data.Skip(startPos).Take(size - startPos).ToArray()};
            }

            var length = BitConverter.ToInt32(data, 1 + startPos);
            length = IPAddress.NetworkToHostOrder(length);
            if (length > PACKLIMIT)
            {
                throw new Exception("packet length over limit");
            }
            if (data[startPos] != 0x02 && data[startPos] != 0x28)
            {
                throw new Exception("data is invalid");
            }

            if (length + startPos + 6 > size)
            {
                return new Message{length = length, data = data.Skip(startPos).Take(size - startPos).ToArray()};
            }

            if (data[startPos + length +5] != 0x03 && data[startPos + length +5] != 0x29)
            {
                throw new Exception("data is invalid");
            }
            return new Message{length = length, data = data.Skip(startPos).Take(length +6).ToArray()};
        }

        
        protected override void OnError(SocketError error)
        {
            Debugger.Log($"TCP client caught an error with code {error} endPoint {Endpoint}");
            try
            {
                var eve = new SocketEvent
                {
                    Msg = error.ToString()
                };
                task.OnError(eve);
            }
            catch (Exception exce)
            {
                Debugger.Log("Bud:TcpClientSocket OnError:" + exce.ToString());
            }
        }
    }
}
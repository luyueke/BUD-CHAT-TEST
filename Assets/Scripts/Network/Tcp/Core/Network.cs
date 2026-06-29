// Network.cs
// Create by xiaojl Mar/15/2023
// 网络模块实现

using System;
using System.Net.Sockets;

namespace Network.Tcp.Core
{
    internal class Network
    {
        public Action<NetworkStatus> OnNetworkEvent;
        public Action<MsgData> OnNetworkMessage;

        // 常量定义
        private const int SEND_TIMEOUT         = 5;          // 5s
        private const int RECEIVE_TIMEOUT      = 5;          // 5s
        private const int DEFAULT_RECVBUF_SIZE = 512 * 1024; // 512k

        // 接收数据缓冲区
        private byte[] _recvBuffer = new byte[DEFAULT_RECVBUF_SIZE];

        private Socket _socket = null;

        public void Connect(string ip, int port)
        {
            // 创建socket
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.SendTimeout = SEND_TIMEOUT;
            _socket.ReceiveTimeout = RECEIVE_TIMEOUT;
            _socket.NoDelay = true;

            try
            {
                // 开始连接服务器
                _socket.BeginConnect(ip, port, new AsyncCallback(OnConnectSuccess), null);

                LoggerUtils.Log("Network: open.");
            }
            catch
            {
                Disconnect();
            }
        }

        public void Disconnect()
        {
            if (_socket != null)
            {
                if (_socket.Connected)
                    _socket.Close();

                _socket = null;
            }

            LoggerUtils.Log("Network: close.");
        }

        public void Send(MsgData data)
        {
            try
            {
                // 发送数据到服务器
                _socket.Send(data.Encode());
            }
            catch
            {
                OnConnectLost();
            }
        }

        private void OnConnectSuccess(IAsyncResult cookie)
        {
            if (_socket == null)
            {
                OnConnectLost();
                return;
            }
            try
            {
                // 服务器连接成功
                _socket.EndConnect(cookie);

                // 开始接收数据
                _socket.BeginReceive(_recvBuffer, 0, DEFAULT_RECVBUF_SIZE, SocketFlags.None, new AsyncCallback(OnMessageArrived), null);

                // 派发服务器连接成功事件
                GameThreadQueue.Dispatch(() =>
                {
                    OnNetworkEvent?.Invoke(NetworkStatus.Connect);
                });
            }
            catch
            {
                OnConnectLost();
            }
        }

        private void OnMessageArrived(IAsyncResult cookie)
        {
            if (_socket == null)
            {
                OnConnectLost();
                return;
            }
            try
            {
                int length = _socket.EndReceive(cookie);
                if (length != 0)
                {
                    var buffer = new NetRecvBuffer();

                    // 获取上次未处理完的数据
                    var uncomplete = cookie.AsyncState as byte[];
                    if (uncomplete != null)
                    {
                        // 将上次未处理完的数据重新写入缓冲区
                        buffer.WriteBytes(uncomplete, 0, uncomplete.Length);
                    }

                    // 将本次接收到的数据写入缓冲区
                    buffer.WriteBytes(_recvBuffer, 0, length);

                    // 尝试从缓冲区中读取一条完整消息
                    MsgData data = null;
                    while ((data = buffer.TryDecode()) != null)
                    {
                        // 派发服务器消息到达事件
                        MsgData msg = data;
                        GameThreadQueue.Dispatch(() =>
                        {
                            OnNetworkMessage?.Invoke(msg);
                        });
                    }

                    // 将未处理完的数据存放在state参数中，并开始下一次异步接收数据
                    _socket.BeginReceive(_recvBuffer, 0, DEFAULT_RECVBUF_SIZE, SocketFlags.None, new AsyncCallback(OnMessageArrived), buffer.UnCompelete);
                }
                else
                {
                    OnConnectLost();
                }
            }
            catch
            {
                OnConnectLost();
            }
        }

        public void OnConnectLost()
        {
            // socket 已关闭（被 TcpNetworkMgr.OnConnectLost 主动 Disconnect 后，
            // 旧的异步回调仍可能触发此方法），直接返回防止重复派发 Disconnect 事件
            if (_socket == null) return;

            // 断开连接
            Disconnect();
            LoggerUtils.Log("Network: disconnect.");

            GameThreadQueue.Dispatch(() =>
            {
                OnNetworkEvent?.Invoke(NetworkStatus.Disconnect);
            });
        }
    }
}
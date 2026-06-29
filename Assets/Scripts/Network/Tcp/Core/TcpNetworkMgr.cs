// NetworkManager.cs
// Create by xiaojl Mar/15/2023
// 网络管理器

using System;
using Google.Protobuf;
using UIAgent;
using UnityEngine.SceneManagement;

namespace Network.Tcp.Core
{
    // 网络状态
    public enum NetworkStatus
    {
        Disconnect,
        Connect,
        Connecting,
    }

    internal partial class TcpNetworkMgr
    {
        private Network _net = null;
        private NetworkStatus _status = NetworkStatus.Disconnect;
        
                
        // 重连
        private bool _reconnectEnabled = true;
        private int _reconnectMaxCount = 3;
        private int _reconnectTimeout = 5;
        private BudTimer _timer;

        public void Init()
        {
            _net = new Network();
            _net.OnNetworkEvent += OnEvent;
            _net.OnNetworkMessage += OnMessage;

            InitDispatcher();
            InitKeepAlive();
        }

        public void Destroy()
        {
            DestroyKeepAlive();
            DestroyDispatcher();

            _net.Disconnect();
            _net.OnNetworkMessage = null;
            _net.OnNetworkEvent = null;
            _net = null;
        }

        public bool IsDisconnect()
        {
            return _status == NetworkStatus.Disconnect;
        }

        public bool IsConnect()
        {
            return _status == NetworkStatus.Connect;
        }

        public void Connect(string ip, int port)
        {
            // 当前必须处于disconnect状态
            if (IsDisconnect() == false)
                return;

            _net.Connect(ip, port);

            // 标记当前处于连接中
            _status = NetworkStatus.Connecting;
            LoggerUtils.Log("TcpNetworkMgr: Connect _status="+_status);
        }

        public void OnConnectLost()
        {
            // 标记当前处于已掉线
            _status = NetworkStatus.Disconnect;
            LoggerUtils.Log("TcpNetworkMgr: OnConnectLost _status="+_status);

            // 断开心跳
            EnableKeepAlive(false);

            // 仅关闭 socket，不走 _net.OnConnectLost()：
            // 后者会把 Disconnect 事件 Dispatch 到下一帧，届时 _status 可能已被新连接改为
            // Connecting，导致后续 Connect 事件被 OnEvent 中的状态检查丢弃。
            _net?.Disconnect();

            // 同步派发断线事件（此方法始终在主线程执行）
            DispatchEvent(NetworkStatus.Disconnect);
        }

        public void Send(NetCmd cmd, IMessage data)
        {
            // Socket未连接
            if (IsConnect() == false)
                return;

            var msgData = new MsgData((UInt16)cmd, data.ToByteArray());
            _net.Send(msgData);
        }

        private void OnEvent(NetworkStatus status)
        {
            // 连接成功
            if (status == NetworkStatus.Connect)
            {
                // Socket在异步连接成功前已关闭
                if (_status != NetworkStatus.Connecting)
                    return;

                // 改变状态
                _status = NetworkStatus.Connect;
                LoggerUtils.Log("TcpNetworkMgr: OnEvent _status="+_status);
                // 可以开启重连
                _reconnectEnabled = true;
            }
            // 连接失败
            else if (status == NetworkStatus.Disconnect)
            {
                _status = NetworkStatus.Disconnect;
                LoggerUtils.Log("TcpNetworkMgr: OnEvent _status="+_status);
            }

            // 分发网络事件
            DispatchEvent(_status);
        }

        private void OnMessage(MsgData data)
        {
            var cmd = (NetMsg)data.Cmd;
            var body = data.Body;

            // 分发消息函数
            DispatchMessage(cmd, body);
        }
        
        // 断线重连
        public void Reconnect(bool ForceReconnect = false)
        {
            if (!IsDisconnect())
                return;

            if (ForceReconnect)
            {
                _reconnectEnabled = true;
            }

            if (! _reconnectEnabled)
                return;

            TimerManager.Inst.Stop(_timer);

            _reconnectEnabled = false;

            var _reconnectCount = 0;
            _timer = TimerManager.Inst.RunDisposable("Reconnect", 0, _reconnectTimeout, _reconnectMaxCount + 1, () =>
            {
                ++_reconnectCount;

                if (IsConnect())
                {
                    // 重连成功，停掉计时器
                    TimerManager.Inst.Stop(_timer);
                    return;
                }

                if (_reconnectCount > _reconnectMaxCount)
                {
                    TimerManager.Inst.Stop(_timer);
                    if (SceneManager.GetActiveScene().name == "GameHall")
                    {
                        UIAgentManager.Inst.OpenPanel(PanelId.ReconnectTcpPanel);
                    }
                    return;
                }

                LoggerUtils.Log($"Network: start reconnect({_reconnectCount})");
                TcpLoginManager.Instance.Reconnect();
            });
        }
    }
}
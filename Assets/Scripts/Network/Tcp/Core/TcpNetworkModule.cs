using System;
using Google.Protobuf;
using Network.Tcp.Core;
using UnityEngine;

namespace Network.Tcp
{
    internal class TcpNetworkModule : INetworkModule
    {
        private TcpNetworkMgr _tcpMgr;

        public void Init()
        {
            _tcpMgr = new TcpNetworkMgr();
            TcpBootstrap.Inst.Init(_tcpMgr);
            TcpBootstrap.Inst.gameObject.DontDestroy();
        }

        public void Release()
        {
            if (GameObject.Find("TcpBootstrap"))
            {
                TcpBootstrap.Inst.DestroySelf();
            }
        }

        public void ConnectServer(string ip, int port)
        {
            if (_tcpMgr == null) return;
            if (_tcpMgr.IsDisconnect() == false)
                _tcpMgr.OnConnectLost();

            // 连接服务器
            LoggerUtils.Log("[TCP]Network: start connect");
            _tcpMgr.Connect(ip, port);
        }

        public void ReConnect(bool ForceReconnect = false)
        {
            LoggerUtils.Log("[TCP]Network: start reconnect");
            _tcpMgr?.Reconnect(ForceReconnect);
        }

        public void Send(NetCmd cmd, IMessage data)
        {
            _tcpMgr?.Send(cmd, data);
        }

        public void OnConnectLost()
        {
            _tcpMgr?.OnConnectLost();
        }        
        
        public void EnableKeepAlive(bool enabled)
        {
            _tcpMgr?.EnableKeepAlive(enabled);
        }

        public void AddMessageListener(NetMsg msg, MsgListener listener)
        {
            _tcpMgr?.AddHook(msg, listener);
        }

        public void RemoveMessageListener(NetMsg msg, MsgListener listener)
        {
            _tcpMgr?.RemoveHook(msg, listener);
        }

        public void AddNetStatusEventListener(Action<NetworkStatus> listener)
        {
            _tcpMgr?.AddEventHook(listener);
        }

        public void RemoveNetStatusEventListener(Action<NetworkStatus> listener)
        {
            _tcpMgr?.RemoveEventHook(listener);
        }

        public void SendPing()
        {
            long now = TimeUtils.NowSeconds();
            _tcpMgr?.SendPing(now);
        }

        public bool IsDisconnect()
        {
            if (_tcpMgr != null)
            {
                return _tcpMgr.IsDisconnect();
            }
            return true;
        }

        public bool IsConnect()
        {
            if (_tcpMgr != null)
            {
                return _tcpMgr.IsConnect();
            }
            return false;
        }

        public void SendCustomCmd(NetCmd cmd, IMessage data){
            _tcpMgr?.SendCustomCmd(cmd, data);
        }
    }
}
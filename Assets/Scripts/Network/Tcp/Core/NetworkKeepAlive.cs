// NetworkKeepAlive.cs
// Create by xiaojl Mar/15/2023
// 心跳模块

using System;
using System.Threading;
using Network.Message;

namespace Network.Tcp.Core
{
    internal partial class TcpNetworkMgr
    {
        private const int KEEP_ALIVE_INTERVAL = 30; // 30s 发送心跳间隔
        private const int KEEP_ALIVE_TIMEOUT = 5; // 5s

        private readonly Ping ping = new Ping();

        private EventWaitHandle _singnal = new EventWaitHandle(false, EventResetMode.AutoReset);
        private Thread _thread;

        private bool _enabled = false;
        private bool _disposed = false;
        private bool _isInit = false;

        private long _lastPingTime;
        private long _lastPongTime;
        private bool _waitPong = false;

        private int _timeoutCount;
        private int _timeoutMaxCount = 3;

        private void InitKeepAlive()
        {
            _isInit = true;
            _thread = new Thread(KeepAliveDaemon);
            _thread.Start();

            AddHook(NetMsg.MSG_PONG, RecvPong);
        }

        private void DestroyKeepAlive()
        {
            RemoveHook(NetMsg.MSG_PONG, RecvPong);

            _disposed = true;
#if UNITY_EDITOR
            _singnal.Close();
#else
            _singnal.WaitOne();
#endif
            _singnal.Dispose();
        }

        public void EnableKeepAlive(bool enabled)
        {
            _enabled = enabled;

            if (enabled)
            {
                var now = TimeUtils.NowSeconds();
                this._lastPingTime = now;
                this._lastPongTime = now;

                CheckThreadState();
            }
        }

        public void CheckThreadState()
        {
            if (_isInit)
            {
                if (!_thread.IsAlive)
                {
                    _thread = new Thread(KeepAliveDaemon);
                    _thread.Start();
                }
            }
        }

        private void KeepAliveDaemon()
        {
            try
            {
                while (!_disposed)
                {
                    try
                    {
                        if (_enabled)
                        {
                            if (_waitPong && _timeoutCount >= _timeoutMaxCount)
                            {
                                _timeoutCount = 0;
                                _enabled = false;
                                _waitPong = false;
                                // 派发到主线程，与 Network.cs 中 socket 异步事件的处理方式保持一致
                                GameThreadQueue.Dispatch(OnConnectLost);
                                return;
                            }

                            var now = TimeUtils.NowSeconds();
                            if (now - _lastPingTime >= KEEP_ALIVE_INTERVAL)
                            {
                                ++_timeoutCount;
                                _lastPongTime = now;
                                SendPing(now);
                            }
                        }

                        Thread.Sleep(1000);
                    }
                    catch (Exception e)
                    {
                        LoggerUtils.LogError($"Network: keepalive exception({e})");
                    }
                }
            }
            finally
            {
                // 无论线程以何种方式退出（_disposed=true 或超时 return），都必须发信号
                // 否则 DestroyKeepAlive 的 WaitOne 可能永久阻塞
                _singnal?.Set();
            }
        }

        public void SendPing(long now)
        {
            _waitPong = true;
            this._lastPingTime = now;

            Send(NetCmd.CMD_PING, ping);

            LoggerUtils.Log($"Network: keepalive send ping(timestamp = {now}).");
        }

        private void RecvPong(byte[] bytes)
        {
            var now = TimeUtils.NowSeconds();

            _waitPong = false;
            this._lastPongTime = now;

            _timeoutCount = 0;

            LoggerUtils.Log($"Network: keepalive recv pong(timestamp = {now}).");
        }
    }
}
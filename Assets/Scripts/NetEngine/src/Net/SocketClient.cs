
using System;
using System.Collections.Concurrent;
using System.Linq;
using Pb.Base;
using NetEngine.src.Util;
using NetEngine.src.Util.Def;

//using UnityEngine;

namespace NetEngine.src.Net
{
    public class SocketClient
    {
        private readonly ConcurrentDictionary<string, Action<SocketEvent>> _eventHandlers =
            new ConcurrentDictionary<string, Action<SocketEvent>>();

        private BaseSocket _baseSocket;

        private int _status;
        private readonly bool _enableUdp;

        // private Action<byte[], int> emit;

        public string Url { get; set; }

        public ConcurrentDictionary<string, Action<SocketEvent>> EventOnceHandlers { get; } =
            new ConcurrentDictionary<string, Action<SocketEvent>>();

        public int Id { get; }

        public bool IsMsgBind { get; set; } = false;

        public bool ForceClose { get; set; } = false;

        public readonly Util.Timer ReconnectTimer = new Util.Timer();

        public int ReconnectTimes { get; set; }

        public Action<byte[], int> Handler { get; set; }

        public SocketClient(int id, bool enableUdp, string url)
        {
            this.Id = id;
            this.Url = url;
            this._enableUdp = enableUdp;
            ReconnectTimes = 0;
        }

        private void OpenSocketTask(string tag)
        {
            Debugger.Log($"OpenSocketTask Tag: {tag}");
            if (string.IsNullOrEmpty(this.Url)) throw new Exception("Socket.url = " + this.Url);
            if (!IsSocketStatus("connect") && !IsSocketStatus("close"))
            {
                ReconnectTimer.SetTimer(() => OpenSocketTask("open"), Config.ReconnectInterval);
            }

            if (!IsSocketStatus("close")) return;

            ReconnectTimes++;
            // 重连超时：关闭连接，不会继续重连
            if (ReconnectTimes > Config.ReconnectMaxTimes)
            {
                ReconnectTimes = 0;
                return;
            }

            ReconnectTimer.Stop();
            ForceClose = false;

            if (_enableUdp && Config.EnableUdp)
            {
                this._baseSocket = new KcpSocket(Url, _enableUdp);
            }
            else
            {
                this._baseSocket = new TcpSocket(Url);
            }

            this._baseSocket.onOpen = HandleSocketOpen;
            this._baseSocket.onClose = HandleSocketClose;
            this._baseSocket.onError = HandleSocketError;
            this._baseSocket.onMessage = HandleSocketMessage;

            this._baseSocket.Connect();
        }

        public void ConnectSocketTask(string tag)
        {
            if (!IsSocketStatus("connect") && ReconnectTimes > 0 && ReconnectTimes < Config.ReconnectMaxTimes)
            {
                // 还在重连
                Debugger.Log(" 帧 ConnectSocketTask 还在重连 tag:" + tag);
                return;
            }

            ReconnectTimes = 0;
            OpenSocketTask(tag + " connect");
        }

        public void ConnectNewSocketTask(string url)
        {
            // 关闭 socketTask，利用 connect 方法实例化新的 socketTask
            this.Url = url;
            ReconnectTimes = 0;

            void NewConnect()
            {
                ConnectSocketTask("connectNewSocket");
            }

            CloseSocketTask(NewConnect, NewConnect);
        }

        public void CloseSocketTaskAndClear(Action success, Action fail)
        {
            ReconnectTimes = 0;
            CloseSocketTask(success, fail);
        }

        /////////////////////////////////   关闭与销毁   //////////////////////////////////
        public void CloseSocketTask(Action success, Action fail)
        {
            // 强关
            this.ForceClose = true;
            if (this._baseSocket == null)
            {
                success?.Invoke();
                EmitCloseStatus();
                return;
            }

            this._baseSocket.Close(
                // Success Action
                () =>
                {
                    this._baseSocket = null;
                    success?.Invoke();
                },
                // Fail Action
                () =>
                {
                    this._baseSocket = null;
                    fail?.Invoke();
                }
            );
        }

        public void DestroySocketTask()
        {
            CloseSocketTask();
            _eventHandlers.Clear();
            IsMsgBind = false;
        }

        public void CloseSocketTask()
        {
            ReconnectTimer.Stop();
            ReconnectTimer.Close();

            // 不需要这个判断
            // if (!IsSocketStatus("close"))
            // {
            // }
            CloseSocketTask(null, null);
        }

        private void HandleSocketOpen()
        {
            ReconnectTimes = 0;
            EmitConnectStatus();
            ReconnectTimer.Stop();
        }

        private void HandleSocketClose()
        {
            EmitCloseStatus();

            if (this.ForceClose == true)
            {
                ReconnectTimer.Stop();
                ReconnectTimer.Close();
                return;
            }

            ReconnectTimer.SetTimer(() => OpenSocketTask("close"), Config.ReconnectInterval);
        }

        private void HandleSocketMessage(SocketEvent e)
        {
            var eve = new SocketEvent
            {
                Msg = "socket message",
                Data = e.Data
            };
            Emit("message", eve);
        }

        private void HandleSocketError(SocketEvent errMsg)
        {
            var eve = new SocketEvent
            {
                Msg = "socket connectError",
                Data = errMsg.Data
            };
            Emit("connectError", eve);
            //Debug.Log("Bud: OpenSocketTask HandleSocketError");


            if (this.ForceClose == true)
            {
                ReconnectTimer.Stop();
                ReconnectTimer.Close();
                return;
            }

            ReconnectTimer.SetTimer(() => OpenSocketTask("error"), Config.ReconnectInterval);
        }

        private void OnTimedOpen(object source, System.Timers.ElapsedEventArgs e)
        {
            OpenSocketTask("close");
        }

        public void OnEvent(string tag, Action<SocketEvent> socketEvent)
        {
            this._eventHandlers.TryAdd(tag, socketEvent);
            if (tag == "message")
            {
                this.IsMsgBind = true;
            }
        }

        // Once Event Listener: Remove listener when event executed 
        public void OnceEvent(string tag, Action<SocketEvent> socketEvent)
        {
            this.EventOnceHandlers.TryAdd(tag, socketEvent);
        }

        public void SetScocketStatus(SocketState status)
        {
            if (_baseSocket != null)
            {
                _baseSocket.ReadyState = status;
            }
        }

        ///////////////////////////////     状态判断     //////////////////////////////////
        public bool IsSocketStatus(string status)
        {
            switch (status)
            {
                case "connect":
                    if (_baseSocket == null || _baseSocket.ReadyState != SocketState.Open)
                        break;
                    return true;
                case "connecting":
                    if (_baseSocket == null || _baseSocket.ReadyState != SocketState.Connecting)
                        break;
                    return true;
                case "close":
                    if (_baseSocket == null) return true;
                    if (_baseSocket != null && _baseSocket.ReadyState != SocketState.Closed)
                        break;
                    return true;
                case "closing":
                    if (_baseSocket == null || _baseSocket.ReadyState != SocketState.Closing)
                        break;
                    return true;
            }

            return false;
        }

        public void Emit(string tag, SocketEvent socketEvent)
        {
            if (socketEvent != null) socketEvent.Tag = tag;
            foreach (var key in _eventHandlers.Keys.Where(key => key.Equals(tag) || key.Equals("*")))
            {
                _eventHandlers[key].Invoke(socketEvent);
            }

            foreach (var key in EventOnceHandlers.Keys.Where(key => key.Equals(tag)))
            {
                //jaywill -修复一次性广播无效问题-20220413-22:33
                // _eventHandlers[key].Invoke (socketEvent);
                EventOnceHandlers[key].Invoke(socketEvent);
                Action<SocketEvent> eve;
                EventOnceHandlers.TryRemove(tag, out eve);
            }
        }

        private static void Reconnect()
        {
        }

        private void EmitConnectStatus()
        {
            Debugger.Log($"EmitConnectStatus = {Id}");
            Emit("connect", new SocketEvent("socket is connected"));
        }

        private void EmitCloseStatus()
        {
            Emit("connectClose", new SocketEvent("socket is closed"));
        }

        ///////////////////////////////// 消息发送相关方法 //////////////////////////////////
        public void Send(byte[] data, Action<ErrorCode> sendFail, Action sendSuccess)
        {
            if (!IsSocketStatus("connect"))
            {
                sendFail(ErrorCode.EcSdkSocketClose);
                Reconnect();
                return;
            }

            _baseSocket.Send(data, sendFail, sendSuccess);
        }
    }
}
using System;
using System.Collections.Generic;
using Google.Protobuf;
using Network.Http;
using Network.Tcp;
using Network.Tcp.Core;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Network
{
    public enum NetworkModuleType
    {
        Tcp,
        Http
    }

    public class NetworkManager : GlobalInstance<NetworkManager>
    {
        private Dictionary<NetworkModuleType, INetworkModule> _modules;
        public string HotUpdateVersion = string.Empty;
        public void Init()
        {
            _modules ??= new Dictionary<NetworkModuleType, INetworkModule>();
            _modules.Clear();
            _modules.Add(NetworkModuleType.Tcp, new TcpNetworkModule());
            _modules.Add(NetworkModuleType.Http, new HttpNetworkModule());

            foreach (var m in _modules.Values)
            {
                m?.Init();
            }
        }

        public override void Release()
        {
            base.Release();
            foreach (var m in _modules.Values)
            {
                m?.Release();
            }

            _modules.Clear();
        }

        private INetworkModule GetModule(NetworkModuleType type)
        {
            if (_modules == null)
                return null;

            return _modules.TryGetValue(type, out var module) ? module : null;
        }

        #region HTTP

        private HttpNetworkModule GetHttpModule()
        {
            return GetModule(NetworkModuleType.Http) as HttpNetworkModule;
        }

        public void SetHttpTokenInfo(Dictionary<string, string> tokenInfo)
        {
            var currentTokenInfo = GetHttpModule().TokenInfo;
            foreach (var keyValuePair in tokenInfo)
            {
                currentTokenInfo[keyValuePair.Key] = keyValuePair.Value;
            }

            GetHttpModule().TokenInfo = currentTokenInfo;

            LoggerUtils.Log(
                $"<color=#96F65E>SetHttpTokenInfo TokenInfo</color>: {JsonConvert.SerializeObject(currentTokenInfo)}");
        }

        public void SetHttpRequestUrl(string requestUrl)
        {
            GetHttpModule().RequestUrl = requestUrl;
        }

        public void SetHttpUrl(GameEnvironment environment)
        {
            var baseUrl = "https://api-test.budapp.cn";
            switch (environment)
            {
                case GameEnvironment.DEV:
                case GameEnvironment.MASTER:
                    baseUrl = "https://api-test.budapp.cn";
                    break;
                case GameEnvironment.ALPHA:
                    baseUrl = "https://api-test.budapp.cn";
                    break;
                case GameEnvironment.PROD:
                    baseUrl = "https://api.budapp.cn";
                    break;
            }
            
#if PACKAGE_TYPE_US
            baseUrl = "https://global.joinbudapp.com";
            switch (environment)
            {
                case GameEnvironment.DEV:
                case GameEnvironment.MASTER:
                case GameEnvironment.ALPHA:
                    baseUrl = "https://global.joinbudapp.com";
                    break;
                case GameEnvironment.PROD:
                    baseUrl = "https://global.joinbudapp.com";
                    break;
            }
#endif

            SetHttpRequestUrl(baseUrl);
        }

        public void SetHotUpdateVersion(string version)
        {
            HotUpdateVersion = version;
            GetHttpModule().HotUpdateVersion = version;
        }

        public void SendHttpRequest(string path, HttpMethod requestType, string paramStr, UnityAction<string> onReceive,
            UnityAction<string> onFail,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0)
        {
            GetHttpModule()?.MakeHttpRequest(path, requestType, paramStr, onReceive, onFail, sHeader, timeOut,
                retryCount);
        }
        
        
        public void SendHttpRequestOnStream(string path, HttpMethod requestType, string paramStr, UnityAction<string> onReceive,
            UnityAction onClose,
            RequestHeader sHeader = null, float timeOut = 0)
        {
            GetHttpModule()?.MakeHttpRequestOnStream(path, requestType, paramStr, onReceive, onClose, sHeader, timeOut);
        }
        

        public void SendHttpRequest<T>(string path, HttpMethod requestType, object args, UnityAction<T> onReceive,
            UnityAction<HttpResponseRawData> onFail, GameObject owner,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0)
        {
            GetHttpModule()?.MakeHttpRequest(path, requestType, args, onReceive, onFail, owner, sHeader, timeOut,
                retryCount);
        }

        public void SendHttpRequest<T>(string path, HttpMethod requestType, object args, UnityAction<T> onReceive,
            UnityAction<HttpResponseRawData> onFail,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0, bool self = true, bool autoHandError = true)
        {
            GetHttpModule()?.MakeHttpRequest(path, requestType, args, onReceive, onFail, sHeader, timeOut, retryCount, self, autoHandError);
        }

        public void SendHttpRequestByCache(string path, HttpMethod requestType, string paramStr,
            UnityAction<string> onReceive,
            UnityAction<string> onFail,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0)
        {
            GetHttpModule()?.MakeHttpRequest(path, requestType, paramStr, onReceive, onFail, sHeader, timeOut,
                retryCount);
        }

        #endregion

        #region TCP

        private TcpNetworkModule GetTcpModule()
        {
            return GetModule(NetworkModuleType.Tcp) as TcpNetworkModule;
        }

        public void ConnectServer(string ip, int port)
        {
            GetTcpModule()?.ConnectServer(ip, port);
        }

        public void Reconnect(bool ForceReconnect = false)
        {
            GetTcpModule()?.ReConnect(ForceReconnect);
        }

        public void OnConnectLost()
        {
            GetTcpModule()?.OnConnectLost();
        }

        public void EnableKeepAlive(bool enabled)
        {
            GetTcpModule()?.EnableKeepAlive(enabled);
        }

        public void SendMessage(NetCmd cmd, IMessage data)
        {
            GetTcpModule()?.Send(cmd, data);
        }

        public void AddMessageListener(NetMsg msg, MsgListener listener)
        {
            GetTcpModule()?.AddMessageListener(msg, listener);
        }

        public void RemoveMessageListener(NetMsg msg, MsgListener listener)
        {
            GetTcpModule()?.RemoveMessageListener(msg, listener);
        }

        public void AddNetStatusEventListener(Action<NetworkStatus> listener)
        {
            GetTcpModule()?.AddNetStatusEventListener(listener);
        }

        public void RemoveNetStatusEventListener(Action<NetworkStatus> listener)
        {
            GetTcpModule()?.RemoveNetStatusEventListener(listener);
        }

        public void SendPing()
        {
            GetTcpModule()?.SendPing();
        }

        public bool IsDisconnect()
        {
            if (GetTcpModule() != null)
            {
                return GetTcpModule().IsDisconnect();
            }
            return true;
        }
        #endregion

        public bool IsConnect() 
        {
            if (GetTcpModule() != null)
            {
                return GetTcpModule().IsConnect();
            }
            return false;
        }
        public void SendCustomCmd(NetCmd cmd, IMessage data){
            GetTcpModule()?.SendCustomCmd(cmd, data);
        }

         public Dictionary<string, string> GetHttpTokenInfo()
        {
            return GetHttpModule().TokenInfo;
        }
    }
}

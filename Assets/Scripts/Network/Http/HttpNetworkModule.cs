using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BestHTTP;
using Game.Config;
using GameData;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Network.Http
{
    internal class HttpNetworkModule : INetworkModule
    {
        #region 外部可进行设置

        public Dictionary<string, string> TokenInfo = new Dictionary<string, string>();
        public string RequestUrl = string.Empty;
        public string HotUpdateVersion = string.Empty;

        #endregion

        private const string DefaultRequestUrl = "https://api-test.budapp.cn";

        private const int DefaultTimeout = 15;

        private UnityAction<string> OnStreamMessage;
        private UnityAction OnStreamClose;
        public const int RETRY_SCOPE = 1;

        private HttpErrorCodeHandler _errorCodeHandler;

        public void Init()
        {
            _errorCodeHandler = new HttpErrorCodeHandler();
        }

        public void Release()
        {
            HTTPManager.OnQuit();
        }

        private string GetRequestUrl()
        {
            var url = DefaultRequestUrl;
            if (!string.IsNullOrEmpty(RequestUrl))
            {
                url = RequestUrl;
            }

            return url;
        }

        private HTTPRequest CreatePostHttpRequest(ref string url, HttpRequestData requestData,
            OnRequestFinishedDelegate callBack, float timeOut = 0)
        {
            url += requestData.path;
            var request = new HTTPRequest(new Uri(url), HTTPMethods.Post, callBack);
            request.RawData = Encoding.UTF8.GetBytes(requestData.paramStr);
            request.Timeout = timeOut <= 0 ? TimeSpan.FromSeconds(DefaultTimeout) : TimeSpan.FromSeconds(timeOut);
            return request;
        }

        private HTTPRequest CreateGetHttpRequest(ref string url, HttpRequestData requestData,
            OnRequestFinishedDelegate callBack, float timeOut = 0)
        {
            if (!string.IsNullOrEmpty(requestData.paramStr))
            {
                var jObject = JObject.Parse(requestData.paramStr);
                IEnumerable<string> nameValues = jObject
                    .Properties()
                    .Select(x => $"{x.Name}={x.Value}");
                url += requestData.path + "?" + string.Join("&", nameValues);
            }
            else
            {
                url += requestData.path;
            }

            var request = new HTTPRequest(new Uri(url), HTTPMethods.Get, callBack);
            request.Timeout = timeOut <= 0 ? TimeSpan.FromSeconds(DefaultTimeout) : TimeSpan.FromSeconds(timeOut);
            return request;
        }

//         private void AddNewHttpRequestHeader(BestHTTP.HTTPRequest request, RequestHeader sHeader)
//         {
//             #region basic header
//             foreach (var keyValuePair in TokenInfo)
//             {
//                 if (request.HasHeader(keyValuePair.Key))
//                 {
//                     request.SetHeader(keyValuePair.Key, keyValuePair.Value);
//                 }
//                 else
//                 {
//                     request.AddHeader(keyValuePair.Key, keyValuePair.Value);
//                 }
//             }
//
//             request.AddHeader(HeaderDefine.contentType, HeaderDefine.applicationJson);
//
// #if UNITY_EDITOR
//             if (DebugSetting.Inst)
//                 request.AddHeader(HeaderDefine.hotUpdateVersion, DebugSetting.Inst.HotUpdateVersion);
// #else
//             request.AddHeader(HeaderDefine.hotUpdateVersion, HotUpdateVersion);
// #endif
//
//             // request.AddHeader(HeaderDefine.userRole, newUser);
//
//             if (Application.platform == RuntimePlatform.Android ||
//                 (Application.platform == RuntimePlatform.WindowsEditor ||
//                  Application.platform == RuntimePlatform.OSXEditor))
//             {
//                 // 编辑器状态也下默认视为android
//                 request.SetHeader(HeaderDefine.mobile, HeaderDefine.android);
//             }
//             else if (Application.platform == RuntimePlatform.IPhonePlayer)
//             {
//                 request.SetHeader(HeaderDefine.mobile, HeaderDefine.ios);
//             }
//
//             #endregion
//
//             //TODO:内部灰度请求
//             request.SetHeader(HeaderDefine.feature, HeaderDefine.CUR_FEATURE);
//         }

        private void AddHttpRequestHeader(HTTPRequest request, RequestHeader sHeader)
        {
            #region basic header

            foreach (var keyValuePair in TokenInfo)
            {
                if (request.HasHeader(keyValuePair.Key))
                {
                    request.SetHeader(keyValuePair.Key, keyValuePair.Value);
                }
                else
                {
                    request.AddHeader(keyValuePair.Key, keyValuePair.Value);
                }
            }

            request.AddHeader(HeaderDefine.contentType, HeaderDefine.applicationJson);

#if UNITY_EDITOR
            if(DebugSetting.Inst)
                request.AddHeader(HeaderDefine.hotUpdateVersion, DebugSetting.Inst.HotUpdateVersion);
#else
            request.AddHeader(HeaderDefine.hotUpdateVersion, HotUpdateVersion);
#endif

            // request.AddHeader(HeaderDefine.userRole, newUser);

            if (Application.platform == RuntimePlatform.Android ||
                (Application.platform == RuntimePlatform.WindowsEditor ||
                 Application.platform == RuntimePlatform.OSXEditor))
            {
                // 编辑器状态也下默认视为android
                request.SetHeader(HeaderDefine.mobile, HeaderDefine.android);
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                request.SetHeader(HeaderDefine.mobile, HeaderDefine.ios);
            }

            #endregion


            // 自定义header
            if (sHeader != null)
            {
                var fieldInfos = sHeader.GetType().GetFields();
                foreach (var fieldInfo in fieldInfos)
                {
                    var fieldValue = fieldInfo.GetValue(sHeader);
                    if (fieldValue == default)
                    {
                        continue;
                    }

                    if (request.HasHeader(fieldInfo.Name))
                    {
                        request.SetHeader(fieldInfo.Name, fieldValue.ToString());
                    }
                    else
                    {
                        request.AddHeader(fieldInfo.Name, fieldValue.ToString());
                    }
                }
            }
            //TODO:内部灰度请求
            request.SetHeader(HeaderDefine.feature, HeaderDefine.CUR_FEATURE);
        }

        public void MakeHttpRequestOnStream(string path, HttpMethod requestType, string paramStr,
            UnityAction<string> onReceive,
            UnityAction onMsgClose,
            RequestHeader sHeader = null, float timeOut = 0)
        {
            // Http超时判定
            if (timeOut > 0)
            {
                TimerManager.Inst.RunOnce(nameof(MakeHttpRequest), timeOut, () =>
                {
                    LoggerUtils.LogError("[MakeHttpRequest] http error- Rsp TimeOut !! path = " + path);
                    OnStreamClose?.Invoke();
                    OnStreamMessage = null;
                    OnStreamClose = null;
                });
            }

            OnStreamClose = onMsgClose;
            OnStreamMessage = onReceive;
            var sseUrl = GetRequestUrl() + path;
            var eventSource = new BestHTTP.ServerSentEvents.EventSource(new System.Uri(sseUrl));
            eventSource.InternalRequest.MethodType =
                requestType == HttpMethod.POST ? BestHTTP.HTTPMethods.Post :BestHTTP.HTTPMethods.Get;
            eventSource.InternalRequest.MaxRetries = requestType == HttpMethod.GET ? 1 : 0;
            Debug.Log(paramStr);
            eventSource.InternalRequest.RawData = Encoding.UTF8.GetBytes(paramStr);
            AddHttpRequestHeader(eventSource.InternalRequest, sHeader);
            eventSource.OnOpen += OnOpen;
            eventSource.OnMessage += OnMessage;
            eventSource.OnClosed += OnClose;
            eventSource.Open();
        }

        void OnOpen(BestHTTP.ServerSentEvents.EventSource eventSource)
        {
            Debug.Log("[MakeHttpRequest] SSE connection opened.");
        }

        void OnMessage(BestHTTP.ServerSentEvents.EventSource eventSource, BestHTTP.ServerSentEvents.Message message)
        {
            Debug.Log($"[MakeHttpRequest] Received SSE message: {message.Data}");
            OnStreamMessage?.Invoke(message.Data);
        }

        void OnClose(BestHTTP.ServerSentEvents.EventSource eventSource)
        {
            OnStreamClose?.Invoke();
            Debug.Log($"[MakeHttpRequest] SSE Close");
        }



        public void MakeHttpRequest(string path, HttpMethod requestType, string paramStr, UnityAction<string> onReceive,
            UnityAction<string> onFail,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0)
        {
            MakeHttpRequest(path, requestType, paramStr, (obj) => onReceive?.Invoke(obj?.ToString()), onFail, sHeader,
                timeOut, retryCount, true);
        }

        public void MakeHttpRequest<T>(string path, HttpMethod requestType, object args, UnityAction<T> onReceive,
            UnityAction<HttpResponseRawData> onFail, GameObject owner,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0)
        {
            MakeHttpRequest<T>(path, requestType, args, (obj) =>
            {
                if (owner != null)
                {
                    onReceive?.Invoke(obj);
                }
                else
                {
                    LoggerUtils.Log($"[MakeHttpRequest] HttpResponse callBack request owner == null:" + path);
                }
            }, (err) =>
            {
                if (owner != null)
                {
                    onFail?.Invoke(err);
                }
                else
                {
                    LoggerUtils.Log($"[MakeHttpRequest] HttpResponse err request owner == null" + path);
                }
            }, sHeader, timeOut, retryCount);
        }

        public void MakeHttpRequest<T>(string path, HttpMethod requestType, object args, UnityAction<T> onReceive,
            UnityAction<HttpResponseRawData> onFail,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0, bool self = true, bool autoHandError = true)
        {
            string paramStr = null;
            if (args != null)
            {
                if (args is string value)
                {
                    paramStr = value;
                }
                else
                {
                    var settings = new JsonSerializerSettings {
                        NullValueHandling = NullValueHandling.Ignore,
                    };
                    paramStr = JsonConvert.SerializeObject(args, settings);
                }
            }

            MakeHttpRequest(path, requestType, paramStr, (obj) =>
                {
                    if (obj == null) {
                        onReceive?.Invoke(default);
                    } else {
                        onReceive?.Invoke(
                            ((JObject)obj).ToObject<T>());
                    }
                }, (errValue)=> {
                    try
                    {
                        var err = JsonConvert.DeserializeObject<HttpResponseRawData>(errValue);
                        onFail?.Invoke(err);
                    }
                    catch (System.Exception ex)
                    {
                        LoggerUtils.LogError(ex.Message + "MakeHttpRequest Error:" + ex.StackTrace);
                        onFail?.Invoke(new HttpResponseRawData { result = -1, rmsg = ex.Message });
                    }
                }, sHeader,
                timeOut, retryCount, self, autoHandError);
        }


        /// <summary>
        /// 带重试的http接口，默认重试3次
        /// 触发重试时机：
        /// 1-定时器检测到超时
        /// 2-http req/rsp未完成（已拿到回包result则不重试）
        /// </summary>
        private void MakeHttpRequest(string path, HttpMethod requestType, string paramStr,
            UnityAction<object> onReceive, UnityAction<string> onFail,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0, bool self = true, bool autoHandError = true)
        {
            var isFinish = false;
            var url = GetRequestUrl();
            BudTimer timer = null;

            // 重试
            bool DoRetryRequest(ref int retryCountPara, HTTPRequest request, HTTPResponse resp)
            {
                if (retryCountPara <= RETRY_SCOPE) return false;
                retryCountPara--;

                if (request != null && resp != null)
                {
                    LoggerUtils.LogError(
                        $"[MakeHttpRequest] Retrying, retryCount = {retryCount}, HttpPath = {path}, http error request.State:{request.State}, resp :{resp.IsSuccess}");
                }

                MakeHttpRequest(path, requestType, paramStr, onReceive, onFail, sHeader, timeOut, retryCount, self, autoHandError);
                return true;
            }

            HttpRequestData requestData = new HttpRequestData
            {
                path = path,
                paramStr = paramStr
            };

            // 请求回调
            OnRequestFinishedDelegate callBack = (HTTPRequest req, HTTPResponse resp) =>
            {
                if (isFinish) return;
                isFinish = true;
                TimerManager.Inst.Stop(timer);

                HttpResponseRawData responseDataRawData = new HttpResponseRawData();

                LoggerUtils.Log($"[MakeHttpRequest] HttpResponse callBack request.State:{req.State}");
                if (req.State != HTTPRequestStates.Finished || resp == null)
                {
                    if (!DoRetryRequest(ref retryCount, req, resp))
                    {
                        responseDataRawData.result = -1;
                        responseDataRawData.rmsg = "http error request.State:" + req.State;
                        LoggerUtils.LogError(
                            $"[MakeHttpRequest] http error, retryCount = {retryCount}, HttpPath = {path}, Rmsg = {responseDataRawData.rmsg}");
                        onFail?.Invoke(JsonConvert.SerializeObject(responseDataRawData));
                    }

                    return;
                }

                if (!resp.IsSuccess)
                {
                    var reason =
                        $"[MakeHttpRequest] http error:StatusCode: {resp.StatusCode},  Message:{resp.Message},  data: {resp.DataAsText}";
                    if (!DoRetryRequest(ref retryCount, req, resp))
                    {
                        responseDataRawData.result = -1;
                        responseDataRawData.rmsg = reason;
                        LoggerUtils.LogError(
                            $"[MakeHttpRequest] http error- retryCount = {retryCount}, HttpPath = {path}, Rmsg = {reason}");
                        onFail?.Invoke(JsonConvert.SerializeObject(responseDataRawData));
                    }

                    return;
                }

                // 字节流返回则没有HttpResponseRaw结构
#if UNITY_EDITOR
                LoggerUtils.Log($"<color=#0D7CEA>[MakeHttpRequest] http callBack Path:{path}</color> :StatusCode: {resp.StatusCode},  Message:{resp.Message},  Data: {resp.DataAsText}");
#endif
                responseDataRawData = JsonConvert.DeserializeObject<HttpResponseRawData>(resp.DataAsText);

                //服务器内部错误返回，不重试，直接弹Toast
                if (responseDataRawData != null && responseDataRawData.result != 0 && responseDataRawData.result < 1000)
                {
                    _errorCodeHandler.HandleErrorCodeResult(responseDataRawData);
                    onFail?.Invoke(resp.DataAsText);
                    return;
                }

                if (responseDataRawData.result != 0)
                {
                    if (!DoRetryRequest(ref retryCount, req, resp))
                    {

                        LoggerUtils.LogError("[MakeHttpRequest] http error-" + "HttpPath = " + path + " Rmsg = " + resp.DataAsText);
                        //根据后端错误码弹toast
                        if (autoHandError)
                        {
                            _errorCodeHandler.HandleErrorCodeResult(responseDataRawData);
                        }

                        onFail?.Invoke(resp.DataAsText);
                    }

                    return;
                }

                global::Message.MessageHelper.Broadcast(global::Message.MessageName.AvaterDatabaseUpdate, resp.DataAsText);

                //请求成功回调
                onReceive?.Invoke(responseDataRawData.data);
            };

            // Http超时判定, bestHttp在断网后会有可能不返回timeout,这里用budTimer实现
            if (timeOut > 0)
            {
                timer = TimerManager.Inst.RunOnce(nameof(MakeHttpRequest), timeOut, () =>
                {
                    if (isFinish) return;
                    var responseDataRaw = new HttpResponseRawData()
                    {
                        result = -1,
                        rmsg = "MakeHttpRequest Rsp TimeOut",
                    };
                    LoggerUtils.LogError("[MakeHttpRequest] http error- Rsp TimeOut !! path = " + path);
                    onFail?.Invoke(JsonConvert.SerializeObject(responseDataRaw));
                    isFinish = true;
                    callBack = null;
                });
            }

            // 创建http request
            var request = requestType == HttpMethod.POST
                ? CreatePostHttpRequest(ref url, requestData, callBack, timeOut)
                : CreateGetHttpRequest(ref url, requestData, callBack, timeOut);
            AddHttpRequestHeader(request, sHeader);
            // oh ! green!
            LoggerUtils.Log(
                $"<color=#96F65E>[MakeHttpRequest] send request url:{url}</color> \n paramStr:{requestData.paramStr}");
            LoggerUtils.Log(
                $"<color=#96F65E>[MakeHttpRequest] send request url:{url}</color> \n headStr:{JsonConvert.SerializeObject(request.Headers)}");
            // 发送request
            request.Send();
        }
    }
}

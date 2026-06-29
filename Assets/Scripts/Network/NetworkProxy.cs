using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameData;
using Network.Http;
using UnityEngine;
using UnityEngine.Events;

namespace Network
{
    public static class HttpNetCommand
    {
        public const string GameOfficialRecommend = "10001";
        public const string GameSectionRecommend = "10002";
        public const string GameSectionInfoRecommend = "10003";
        public const string ClothSectionRecommend = "10004";
        public const string ClothSectionInfoRecommend = "10005";
        public const string UGCPropSectionRecommend = "10006";
        public const string UGCPropSectionInfoRecommend = "10007";
        public const string MatSectionRecommend = "10008";
        public const string MatSectionInfoRecommend = "10009";
    }

    public class NetworkProxy
    {
        /// <summary>
        /// 服务端请求代理，初始化默认调用onReceive回调
        /// </summary>
        public static void SendHttpRequest(string httpUrl, HttpMethod requestType, string paramStr,
            UnityAction<string> onReceive,
            UnityAction<string> onFail, NetCacheEvent cache = null,
            RequestHeader sHeader = null, float timeOut = 0, int retryCount = 0)
        {
            string content = string.Empty;
            NetCacheKey cacheKey = new()
            {
                httpUrl = httpUrl,
                filterStr = paramStr
            };
            LoggerUtils.Log($"httpUrl==={httpUrl}  paramStr==={paramStr}");
            if (cache != null)
            {
                var block = DataPool.Inst.GetHttpData(cacheKey);
                if (block != null && !string.IsNullOrEmpty(block.content))
                {
                    content = block.content;
                    onReceive?.Invoke(content);
                }
            }
            NetworkManager.Inst.SendHttpRequestByCache(httpUrl, requestType, paramStr, (reqContent) =>
                {
                    if (cache != null)
                    {
                        if (string.IsNullOrEmpty(content))
                        {
                            if (!string.IsNullOrEmpty(reqContent))
                            {
                                DataPool.Inst.AddHttpData(cacheKey, reqContent);
                                onReceive?.Invoke(reqContent);
                            }
                            else
                            {
                                string err = httpUrl + "请求内容为Null";
                                LoggerUtils.LogError(err);
                                onFail?.Invoke(err);
                            }
                        }
                        else
                        {
                            if (!content.Equals(reqContent))
                            {
                                DataPool.Inst.AddHttpData(cacheKey, reqContent);
                                cache.OnChange?.Invoke(reqContent);
                            }
                        }
                    }
                    else
                    {
                        onReceive?.Invoke(reqContent);
                    }
                }, err=>
                {
                    LoggerUtils.LogError(httpUrl+"请求出错"+err);
                    onFail?.Invoke(err);
                }, sHeader, timeOut,
                retryCount);
        }
    }
}

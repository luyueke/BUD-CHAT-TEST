using System;
using System.Collections;
using System.Collections.Generic;
using GravityEngine;
using GravityEngine.Utils;
using GravitySDK.PC.Storage;
using GravitySDK.PC.TaskManager;
using GravitySDK.PC.Utils;
#if GRAVITY_OPPO_GAME_MODE
using QGMiniGame;
#endif
using UnityEngine;
using UnityEngine.Networking;
#if GRAVITY_WECHAT_GAME_MODE || GRAVITY_BILIBILI_GAME_MODE || GRAVITY_MEITUAN_GAME_MODE
using WeChatWASM;
#elif GRAVITY_BYTEDANCE_GAME_MODE
using StarkSDKSpace;
#elif GRAVITY_BYTEDANCE_TT_GAME_MODE
using TTSDK;
#elif GRAVITY_KUAISHOU_GAME_MODE
using com.kwai.mini.game;
using com.kwai.mini.game.config;
#elif GRAVITY_KUAISHOU_WEBGL_GAME_MODE
using KSWASM;
#elif GRAVITY_OPPO_GAME_MODE
using QGMiniGame;
#elif GRAVITY_HUAWEI_GAME_MODE
using HWWASM;
#elif GRAVITY_ALIPAY_GAME_MODE
using AlipaySdk;
#endif

namespace GravitySDK.PC.GravityTurbo
{
    public static class GravityHelper
    {
        private const string GravityHost = "https://api.gravity-engine.com";
        private const string dev_host = "https://event-test-backend.gravity-engine.com";
        private static string _accessToken;
        private static string _clientID;
        private static string _channel;
        private static int _silentPeriod = 7;
        private static bool _enableSilentPeriod;
        private static bool _isTencentDryRunModeEnabled = false;
        private static bool _isForceUseParams = false;

        private static void GlobalCheck()
        {
            if (string.IsNullOrEmpty(_accessToken))
            {
                throw new ArgumentException("accessToken must be required");
            }

            if (string.IsNullOrEmpty(_clientID))
            {
                throw new ArgumentException("clientId must be required");
            }
        }

        /// <summary>
        /// 初始化GravityHelper SDK必须参数（每次启动都需要调用）
        /// </summary>
        /// <param name="accessToken">项目通行证，在：网站后台-->管理中心-->应用列表中找到Access Token列 复制（首次使用可能需要先新增应用）</param> 
        /// <param name="clientId">用户唯一标识，如微信小程序/小游戏的openid、Android ID、iOS的IDFA、或业务侧自行生成的唯一用户ID均可</param>
        /// <param name="channel">用户渠道</param>
        public static void InitSDK(string accessToken, string clientId, string channel)
        {
            _accessToken = accessToken;
            _clientID = clientId;
            _channel = channel;
            GlobalCheck();
            GravitySDKLogger.Print("GravityHelper init success");
        }
        
        static Dictionary<string, string> ConvertToDictionary(Dictionary<string, object> sourceDict)
        {
            var targetDict = new Dictionary<string, string>();
            
            if (sourceDict == null) return targetDict;
            foreach (var kvp in sourceDict)
            {
                // 尝试将值转换为字符串
                string stringValue = kvp.Value?.ToString();
                targetDict[kvp.Key] = stringValue;
            }

            return targetDict;
        }

        public static void PreInit()
        {
            try
            {
                string latestCacheTimestampStr =
                    (string) GravitySDKFile.GetData("latest_cache_timestamp", typeof(string));
                long currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                if (!string.IsNullOrEmpty(latestCacheTimestampStr))
                {
                    long latestCacheTimestamp = Convert.ToInt64(latestCacheTimestampStr);
                    if (currentTimestamp - latestCacheTimestamp < 24 * 60 * 60)
                    {
                        // 一天内，则不需要多次缓存，直接return
                        Debug.Log("not need to re cache within 1 day");
                        return;
                    }    
                }
#if GRAVITY_WECHAT_GAME_MODE || GRAVITY_BILIBILI_GAME_MODE || GRAVITY_MEITUAN_GAME_MODE
                LaunchOptionsGame launchOptionsSync = WX.GetLaunchOptionsSync();
                var launchQuery = launchOptionsSync.query;
                var launchScene = launchOptionsSync.scene;
    #elif GRAVITY_BYTEDANCE_GAME_MODE
                LaunchOption launchOptionsSync = StarkSDK.API.GetLaunchOptionsSync();
                var launchQuery = new Dictionary<string, string>();
                var launchScene = "";
                if (launchOptionsSync != null)
                {
                    launchQuery = launchOptionsSync.Query;
                    launchScene = launchOptionsSync.Scene;
                }
    #elif GRAVITY_BYTEDANCE_TT_GAME_MODE
                LaunchOption launchOptionsSync = TT.GetLaunchOptionsSync();
                var launchQuery = new Dictionary<string, string>();
                var launchScene = "";
                if (launchOptionsSync != null)
                {
                    launchQuery = launchOptionsSync.Query;
                    launchScene = launchOptionsSync.Scene;   
                }
    #elif GRAVITY_KUAISHOU_GAME_MODE
#if KUAISHOU_BELOW_121_VERSION
            KSOutLaunchOption launchOption = KSConfig.kSOutLaunchOption;
#else 
            KSOutLaunchOption launchOption = KS.GetLaunchOptionSync();
#endif
                var launchQuery = launchOption.query;
                var launchScene = launchOption.from;
    #elif GRAVITY_KUAISHOU_WEBGL_GAME_MODE
                LaunchOptions launchOption = KS.GetLaunchOptionSync();
                var launchQuery = launchOption.query;
                var launchScene = launchOption.from;
    #elif GRAVITY_ALIPAY_GAME_MODE
                Dictionary<string, string> launchQuery = new Dictionary<string, string>();
                var launchScene = "";
                
                AlipaySDK.API.GetLaunchOptions((result) =>
                {
                    // TEST  {"query":{"key1":"value1","key2":"value2"},"referrerInfo":{}}
                    // result = "{\"query\":{\"key1\":\"value1\",\"key2\":\"value2\"},\"referrerInfo\":{}}";
                    Debug.Log("alipay launch info is " + result);
                    if (result!=null)
                    {
                        Dictionary<string, object> launchOptionDict = GE_MiniJson.Deserialize(result);
                        if (launchOptionDict.TryGetValue("query", out var queryStr))
                        {
                            Dictionary<string, object> queryDict = (Dictionary<string, object>) queryStr;
                            launchQuery = ConvertToDictionary(queryDict);
                            Debug.Log("alipay query got");
                            // finally
                            var launchQueryStr = GE_MiniJson.Serialize(launchQuery);
                            GravitySDKFile.SaveData("query_cache", "" + launchQueryStr);
                            GravitySDKFile.SaveData("scene_cache", "" + launchScene);
                            GravitySDKFile.SaveData("latest_cache_timestamp", "" + currentTimestamp);
                        }
                        else
                        {
                            Debug.Log("alipay query is null");
                        }
                    }
                    else
                    {
                        Debug.Log("alipay launch option is null");
                    }
                    // 异步的需要return，针对支付宝小游戏，暂时没有改造
                    return;
                });
    #elif GRAVITY_OPPO_GAME_MODE
                Dictionary<string, string> launchQuery = new Dictionary<string, string>();
                var launchScene = "";
                var launchStr =  QG.GetEnterOptionsSync();
                Debug.Log("oppo quickgame launch info is " + launchStr);
                if (launchStr!=null)
                {
                    Dictionary<string, object> launchOptionDict = GE_MiniJson.Deserialize(launchStr);
                    if (launchOptionDict.TryGetValue("query", out var queryStr))
                    {
                        Dictionary<string, object> queryDict = (Dictionary<string, object>) queryStr;
                        launchQuery = ConvertToDictionary(queryDict);
                        Debug.Log("oppo quickgame query got");
                    }
                    else
                    {
                        Debug.Log("oppo quickgame query is null");
                    }
                }
    #elif GRAVITY_HUAWEI_GAME_MODE
                Dictionary<string, string> launchQuery = new Dictionary<string, string>();
                var launchScene = "";
                GetLaunchOptionsSyncResult result = QG.GetLaunchOptionsSync();
                Debug.Log("huawei quickgame launch info is " + result);
                if (result!=null)
                {
                    Dictionary<string, object> queryDict = GE_MiniJson.Deserialize(result.query);
                    launchQuery = ConvertToDictionary(queryDict);
                    Debug.Log("huawei quickgame query got");
                }
#elif GRAVITY_XIAOMI_GAME_MODE
                Dictionary<string, string> launchQuery = new Dictionary<string, string>();
                var launchScene = "";
                bool success;
                if (SyncHelper.GetLaunchInfoSync(out success, out launchQuery, out launchScene))
                {
                    Debug.Log($"Success: {success}, Scene: {launchScene}, Query: {launchQuery?.ContainsKey("type")}");
                }
                else
                {
                    Debug.Log("Failed to get launch info.");
                }
#elif GRAVITY_INNER_DEBUG
                Dictionary<string, string> launchQuery = new Dictionary<string, string>();
                launchQuery["a"] = "b";
                var launchScene = "1000"; 
    #else
                Dictionary<string, string> launchQuery = new Dictionary<string, string>();
                var launchScene = "";
    #endif
                // finally
                var launchQueryStr = GE_MiniJson.Serialize(launchQuery);
                GravitySDKFile.SaveData("query_cache", "" + launchQueryStr);
                GravitySDKFile.SaveData("scene_cache", "" + launchScene);
                GravitySDKFile.SaveData("latest_cache_timestamp", "" + currentTimestamp);
            }
            catch (Exception e)
            {
                Debug.Log("preinit error");
                Debug.Log(e);
            }
        }
        
        public static bool IsLaunchCacheValid()
        {
            string latestCacheTimestampStr =
                (string) GravitySDKFile.GetData("latest_cache_timestamp", typeof(string));
            long currentTimestamp = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            if (!string.IsNullOrEmpty(latestCacheTimestampStr))
            {
                long latestCacheTimestamp = Convert.ToInt64(latestCacheTimestampStr);
                if (currentTimestamp - latestCacheTimestamp < 24 * 60 * 60)
                {
                    // 一天之内，cache有效
                    Debug.Log("cache valid " + currentTimestamp + " " + latestCacheTimestamp + " " +
                              (currentTimestamp - latestCacheTimestamp));
                    return true;
                } 
            }
            // 如果超过了1天，或者当前缓存被清空，则cache无效
            Debug.Log("cache invalid " + currentTimestamp + " " + latestCacheTimestampStr);
            return false;
        }

        public static void TestLaunchParams()
        {
            Dictionary<string, string> wxLaunchQuery = new Dictionary<string, string>();
            string launchScene = "";
            if (wxLaunchQuery == null || wxLaunchQuery.Count <= 0)
            {
                // 先判断当前cache是否有效
                if (IsLaunchCacheValid())
                {
                    // cache有效，尝试获取cache
                    Dictionary<string, object> cacheQuery = new Dictionary<string, object>();
                    var cacheQueryStr = (string) GravitySDKFile.GetData("query_cache", typeof(string));
                    if (cacheQueryStr != null)
                    {
                        cacheQuery = GE_MiniJson.Deserialize(cacheQueryStr);
                        wxLaunchQuery = ConvertToDictionary(cacheQuery);
                        wxLaunchQuery["gravity_launch_cache"] = "1";
                    }
                }
            }
            if (string.IsNullOrEmpty(launchScene))
            {
                // 判断cache是否有效
                if (GravityHelper.IsLaunchCacheValid())
                {
                    // 尝试获取当前cache
                    var cacheSceneStr = (string) GravitySDKFile.GetData("scene_cache", typeof(string));
                    if (!string.IsNullOrEmpty(cacheSceneStr))
                    {
                        // cache有值，使用之
                        launchScene = cacheSceneStr;
                    }
                }
            }
            GravitySDKFile.DeleteData("query_cache");
            GravitySDKFile.DeleteData("scene_cache");
            GravitySDKFile.DeleteData("latest_cache_timestamp");
            foreach (var kvp in wxLaunchQuery)
            {
                Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
            }
            Debug.Log(launchScene);
        }

        public static void Initialize(string clientId, string name, int version, string openId, Dictionary<string, string> wxLaunchQuery, bool enableSyncAttribution,
            IInitializeCallback initializeCallback, UnityWebRequestMgr.Callback callback)
        {
            // check params
            GlobalCheck();
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("name must be required");
            }

            // 尝试在wxLaunchQuery为空的时候，尝试获取原来的缓存数据
            try
            {
                if (wxLaunchQuery == null || wxLaunchQuery.Count <= 0)
                {
                    // 先判断当前cache是否有效
                    if (IsLaunchCacheValid())
                    {
                        // cache有效，尝试获取cache
                        Dictionary<string, object> cacheQuery = new Dictionary<string, object>();
                        var cacheQueryStr = (string) GravitySDKFile.GetData("query_cache", typeof(string));
                        if (cacheQueryStr != null)
                        {
                            cacheQuery = GE_MiniJson.Deserialize(cacheQueryStr);
                            wxLaunchQuery = ConvertToDictionary(cacheQuery);
                            wxLaunchQuery["gravity_launch_cache"] = "1";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("get cache query warning");
                Debug.Log(e);
            }

#if GRAVITY_ALIPAY_GAME_MODE
            if (wxLaunchQuery != null && wxLaunchQuery.TryGetValue("channel", out string queryChannel))
            {
                _channel = queryChannel;
            }
#endif

            var currentClientId = SetOrGetClientId(clientId);
            var registerRequestDir = new Dictionary<string, object>()
            {
                {"client_id", currentClientId},
                {"name", name},
                {"channel", _channel},
                {"version", version},
                {"wx_openid", openId},
                {"wx_unionid", ""},
                {"ad_data", wxLaunchQuery},
                {"need_return_attribution", enableSyncAttribution},
            };
#if GRAVITY_OPPO_GAME_MODE
            string deviceId = "";
            QG.GetDeviceId(
                (success) =>
                {
                    deviceId = success.data.deviceId; //设备唯一标识
                },
                (fail) =>
                {
                    Debug.Log("QG.GetDeviceId fail = " + JsonUtility.ToJson(fail));
                },
                (complete) =>
                {
                    Debug.Log("gravity-engine current deviceId is " + deviceId);
                    QG.GetSystemInfo((msg) =>
                        {
                            string brand = msg.data.brand; // 手机品牌
                            string language = msg.data.language; // 系统语言
                            string model = msg.data.model; // 手机型号
                            string platformVersionName = msg.data.platformVersionName; // 客户端平台
                            string platformVersionCode = msg.data.platformVersionCode; // Version
                            string screenHeight = msg.data.screenHeight; // 屏幕高度
                            string screenWidth = msg.data.screenWidth; // 屏幕宽度
                            string system = msg.data.system; // 系统版本
                            string COREVersion = msg.data.COREVersion; // 版本号
                            
                            var deviceInfo = new Dictionary<string, string>()
                            {
                                {"os_name", "android"},
                                {"android_id", deviceId},
                                {"imei", deviceId},
                                {"oaid", deviceId},
                                {"rom", platformVersionName},
                                {"rom_version", platformVersionCode},
                                {"brand", brand},
                                {"model", model},
                                {"android_version", COREVersion}
                            };
                            registerRequestDir["device_info"] = deviceInfo;
                            RequestInitialize(initializeCallback, callback, currentClientId, registerRequestDir);
                        },
                        (err) =>
                        {
                            Debug.Log("QG.GetSystemInfo fail = " + JsonUtility.ToJson(err));
                            var deviceInfo = new Dictionary<string, string>()
                            {
                                {"os_name", "android"},
                                {"android_id", deviceId},
                                {"imei", deviceId},
                                {"oaid", deviceId},
                            };
                            registerRequestDir["device_info"] = deviceInfo;
                            RequestInitialize(initializeCallback, callback, currentClientId, registerRequestDir);
                        });
                });
#else
            RequestInitialize(initializeCallback, callback, currentClientId, registerRequestDir);
#endif
        }

        private static void RequestInitialize(IInitializeCallback initializeCallback, UnityWebRequestMgr.Callback callback, string currentClientId,
            Dictionary<string, object> registerRequestDir)
        {
            var body = GE_MiniJson.Serialize(registerRequestDir);
            var url = GravityHost + "/event_center/api/v1/user/initialize/?access_token=" + _accessToken +
                      "&client_id=" + currentClientId;
            Debug.Log("url is " + url);
            Debug.Log("body is " + body);
            UnityWebRequestMgr.Instance.Post(url, registerRequestDir, (request =>
                {
                    string responseText = request.downloadHandler.text;
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    GravitySDKLogger.Print("response is " + responseText);
                    if (res != null)
                    {
                        if (res.TryGetValue("code", out var re))
                        {
                            int code = Convert.ToInt32(re);
                            if (code == 0)
                            {
                                if (res.TryGetValue("data", out object dataObj))
                                {
                                    initializeCallback?.onSuccess((Dictionary<string, object>) dataObj);
                                }
                                else
                                {
                                    initializeCallback?.onSuccess(new Dictionary<string, object>());
                                }
                                return;
                            }
                        }
                    }

                    initializeCallback?.onFailed("code is not 0, failed with msg " + res?["msg"]);
                }), callback);
        }
        
        
        public static void GetOpenId(string sessionCode, string accessToken, IGetOpenIdCallback getOpenIdCallback)
        {
#if GRAVITY_WECHAT_GAME_MODE
            string platformStr = "wx";
#elif GRAVITY_BYTEDANCE_GAME_MODE || GRAVITY_BYTEDANCE_TT_GAME_MODE
            string platformStr = "dy";
#elif GRAVITY_KUAISHOU_GAME_MODE || GRAVITY_KUAISHOU_WEBGL_GAME_MODE
            string platformStr = "ks";
#elif GRAVITY_BILIBILI_GAME_MODE
            string platformStr = "bili";
#else
            string platformStr = null;
#endif
            if (getOpenIdCallback == null)
            {
                GravitySDKLogger.Print("Callback error");
                return;
            }
            if (platformStr == null)
            {
                getOpenIdCallback.onFailed("平台类型错误，请检查是否正确添加全局宏参数：GRAVITY_WECHAT_GAME_MODE、GRAVITY_BYTEDANCE_GAME_MODE、GRAVITY_BYTEDANCE_TT_GAME_MODE、GRAVITY_KUAISHOU_GAME_MODE、GRAVITY_KUAISHOU_WEBGL_GAME_MODE、GRAVITY_BILIBILI_GAME_MODE");
            }
            else
            {
                var requestDir = new Dictionary<string, object>()
                {
                    {"code", sessionCode},
                };
                UnityWebRequestMgr.Instance.Post(
                    GravityHost + "/event_center/api/v1/base/"+ platformStr + "/code2Session/?access_token=" + accessToken, requestDir,
                (request =>
                {
                    string responseText = request.downloadHandler.text;
                    // string responseText = "{\n  \"data\": {\n    \"resp\": {\n      \"session_key\": \"wHVdBZh5CmqKTl7zTnloSg==\",\n      \"openid\": \"oeleK67L8sN8MIqAkZa2fkPftyvs\",\n      \"unionid\": \"oaNSjv-v854AXW_1W41MZv3sSwKU\"\n    }\n  },\n  \"extra\": {},\n  \"code\": 0,\n  \"msg\": \"成功\"\n}";
                    
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    Debug.Log("response is " + responseText);
                    if (res != null)
                    {
                        if (res.TryGetValue("code", out var re))
                        {
                            int code = Convert.ToInt32(re);
                            if (code == 0)
                            {
                                if (res.TryGetValue("data", out object dataObj))
                                {
                                    Dictionary<string, object> dataDict = (Dictionary<string, object>) dataObj;
                                    // foreach (var kvp in dataDict)
                                    // {
                                    //     Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
                                    // }
                                    if (dataDict.TryGetValue("resp", out object respObj))
                                    {
                                        // Dictionary<string, object> respDict = (Dictionary<string, object>) respObj;
                                        // foreach (var kvp in respDict)
                                        // {
                                        //     Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
                                        // }
                                        getOpenIdCallback?.onSuccess((Dictionary<string, object>) respObj);
                                    }
                                    else
                                    {
                                        getOpenIdCallback?.onSuccess(new Dictionary<string, object>());
                                    }
                                }
                                else
                                {
                                    getOpenIdCallback?.onSuccess(new Dictionary<string, object>());
                                }
                                return;
                            }
                        }
                    }

                    getOpenIdCallback?.onFailed("code is not 0, failed with msg " + res?["msg"]);
                }));
            }
        }
        
        public static String GetAccessToken()
        {
            return _accessToken;
        }

        public static String GetClientId()
        {
            return _clientID;
        }
        
        /// <summary>
        /// 如果传入的currentClientId不为空，则set到_clientID，否则直接返回当前的_clientId
        /// </summary>
        /// <param name="currentClientId"></param>
        /// <returns></returns>
        public static String SetOrGetClientId(string currentClientId)
        {
            if (!GravitySDKUtil.IsEmptyString(currentClientId))
            {
                _clientID = currentClientId;
            }
            return _clientID;
        }

        public static void SetClientId(string clientId)
        {
            _clientID = clientId;
        }
        
        public static void SetSilentPeriod(int silentPeriod)
        {
            _silentPeriod = silentPeriod;
            _enableSilentPeriod = true;
        }
        
        public static String GetChannel()
        {
            return _channel;
        }

        public static bool IsDryRunModeEnabled()
        {
            return _isTencentDryRunModeEnabled;
        }

        public static bool UseParams()
        {
            return _isForceUseParams;
        }

        public static void SetDryRunModeStatus(bool isEnabled)
        {
            _isTencentDryRunModeEnabled = isEnabled;
            _isForceUseParams = true;
        }

        /// <summary>
        /// 只回传traceId对应的事件
        /// </summary>
        /// <param name="traceId"></param>
        /// <param name="otherParams"></param>
        /// <param name="queryDryRunCallback"></param>
        /// <param name="waitTime"></param>
        /// <param name="retryCount"></param>
        /// <param name="maxRetryInt"></param>
        public static void QueryDryRunInfo(string traceId, Dictionary<string, object> otherParams, IQueryDryRunCallback queryDryRunCallback, int waitTime = 1, int retryCount = 0, int maxRetryInt = 0)
        {
            UnityWebRequestMgr.Instance.Get(
                    GravityHost + "/event_center/api/v1/event/postback_info/?access_token=" + _accessToken + "&client_id=" + SetOrGetClientId("") + "&trace_id=" + traceId,
                (request =>
                {
                    string responseText = request.downloadHandler.text;
                    // string responseText = "{\n  \"data\": {\n    \"postback_list\": [\n      {\n        \"trace_id\": \"8d7493023ad4690c2567fbb06bcf54f7\",\n        \"action\": \"pay\",\n        \"postback_value\": 30,\n        \"click_company\": \"tencent\",\n        \"timestamp\": 1680998892068\n      }\n    ]\n  },\n  \"extra\": {},\n  \"code\": 0,\n  \"msg\": \"成功\"\n}";
                    
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    Debug.Log("response is " + retryCount + " " + responseText);
                    if (res != null)
                    {
                        if (res.TryGetValue("code", out var re))
                        {
                            int code = Convert.ToInt32(re);
                            if (code == 0)
                            {
                                if (res.TryGetValue("data", out object dataObj))
                                {
                                    Dictionary<string, object> dataDict = (Dictionary<string, object>) dataObj;
                                    // foreach (var kvp in dataDict)
                                    // {
                                    //     Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
                                    // }
                                    if (maxRetryInt == 0)
                                    {
                                        if (dataDict.TryGetValue("retry", out var maxRetry))
                                        {
                                            maxRetryInt = Convert.ToInt32(maxRetry);
                                        }
                                    }
                                    if (dataDict.TryGetValue("postback_list", out object respObj))
                                    {
                                        Debug.Log(respObj);
                                        List<object> respList = (List<object>) Convert.ChangeType(respObj, typeof(List<object>));
                                        if (respList.Count == 0)
                                        {
                                            // 没有查到数据，可能需要retry
                                            if (maxRetryInt > retryCount)
                                            {
                                                Debug.Log("retry " + retryCount + " maxCount " + maxRetryInt + " wait time " + waitTime);
                                                QueryDryRunInfo(traceId, otherParams, queryDryRunCallback, waitTime + 1, retryCount + 1, maxRetryInt);
                                                return;
                                            }
                                            else
                                            {
                                                queryDryRunCallback?.onEmpty();
                                            }
                                        }
                                        
                                        List<Dictionary<string, string>> postBackList = new List<Dictionary<string, string>>();
                                        foreach (var item in respList)
                                        {
                                            Dictionary<string, object> postbackDict = (Dictionary<string, object>) item;
                                            // foreach (var eachValue in postbackDict)
                                            // {
                                            //     Debug.Log("key " + eachValue.Key + " : " + eachValue.Value?.ToString() + " " + eachValue);
                                            // }
                                            string action = (string) postbackDict["action"];
                                            string clickCompany = (string) postbackDict["click_company"];
                                            var postback = new Dictionary<string, string>()
                                            {
                                                {"trace_id", traceId},
                                                {"action", action}
                                            };
                                            if (action.Equals("pay"))
                                            {
                                                int postbackValueInt = Convert.ToInt32(postbackDict["postback_value"]);

                                                Debug.Log("pay " + postbackValueInt);
                                                // 回调出去，给媒体上报
                                                queryDryRunCallback?.onTrackPay(postbackValueInt, clickCompany, otherParams);
                                                postBackList.Add(postback);
                                            } else if (action.Equals("key_active"))
                                            {
                                                Debug.Log("key_active");
                                                // 回调出去，给媒体上报
                                                queryDryRunCallback?.onTrackKeyActive(clickCompany, otherParams);
                                                postBackList.Add(postback);
                                            }
                                        }

                                        if (postBackList.Count > 0)
                                        {
                                            Debug.Log("need to send postback count " + postBackList.Count);
                                            SendDryRunInfo(postBackList, null);
                                        }
                                    }
                                    else
                                    {
                                        queryDryRunCallback?.onEmpty();
                                    }
                                }
                                else
                                {
                                    queryDryRunCallback?.onEmpty();
                                }
                                return;
                            }
                        }
                    }

                    queryDryRunCallback?.onFailed("code is not 0, failed with msg " + res?["msg"]);
                }), waitTime);
        }

        private static void SendDryRunInfo(List<Dictionary<string, string>> postBackList, ISendDryRunCallback sendDryRunCallback)
        {
            var requestDir = new Dictionary<string, object>()
            {
                {"postback_list", postBackList},
            };
            var body = GE_MiniJson.Serialize(requestDir);
            Debug.Log("request body is " + body);
            UnityWebRequestMgr.Instance.Post(
                    GravityHost + "/event_center/api/v1/event/postback_info/?access_token=" + _accessToken + "&client_id=" + SetOrGetClientId(""), requestDir,
                (request =>
                {
                    string responseText = request.downloadHandler.text;
                    // string responseText = "{\n  \"data\": {\n    \"postback_list\": [\n      {\n        \"trace_id\": \"8d7493023ad4690c2567fbb06bcf54f7\",\n        \"action\": \"pay\",\n        \"postback_value\": 30,\n        \"click_company\": \"tencent\",\n        \"timestamp\": 1680998892068\n      }\n    ]\n  },\n  \"extra\": {},\n  \"code\": 0,\n  \"msg\": \"成功\"\n}";
                    
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    Debug.Log("response is " + responseText);
                    if (res != null)
                    {
                        if (res.TryGetValue("code", out var re))
                        {
                            int code = Convert.ToInt32(re);
                            if (code == 0)
                            {
                                Debug.Log("send dry run info success");
                                sendDryRunCallback?.onSuccess();
                                return;
                            }
                        }
                    }
                    Debug.Log("code is not 0, failed with msg " + res?["msg"]);
                    sendDryRunCallback?.onFailed("code is not 0, failed with msg " + res?["msg"]);
                }));
        }
        
        private static void SetupUserInfo(string action)
        {
            List<Dictionary<string, string>> postBackList = new List<Dictionary<string, string>>();
            var postback = new Dictionary<string, string>()
            {
                {"action", action}
            };
            postBackList.Add(postback);
            var requestDir = new Dictionary<string, object>()
            {
                {"postback_list", postBackList},
            };
            var body = GE_MiniJson.Serialize(requestDir);
            Debug.Log("request body is " + body);
            UnityWebRequestMgr.Instance.Post(
                    GravityHost + "/event_center/api/v1/event/tencent_sdk/?access_token=" + _accessToken + "&client_id=" + SetOrGetClientId(""), requestDir,
                (request =>
                {
                    string responseText = request.downloadHandler.text;
                    
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    Debug.Log("response is " + responseText);
                }));
        }
        
        // 腾讯SDK首次注册、沉默唤醒回传
        public static void TencentSDKRegisterTrack()
        {
            if (_silentPeriod <= 0 && _enableSilentPeriod)
            {
                GE_Log.e("silentPeriod must be set, not bellow zero.");
                return;
            }

            var tencentSDKLockObj = GravitySDKFile.GetData("tencent_sdk_lock", typeof(int));
            if (tencentSDKLockObj != null)
            {
                var tencentSDKLock = (int) tencentSDKLockObj;
                if (tencentSDKLock != 0)
                {
                    Debug.Log("tencent_sdk_lock is locked");
                    return;
                }
            }
            // lock
            GravitySDKFile.SaveData("tencent_sdk_lock", 1);
            UnityWebRequestMgr.Instance.Get(
                    GravityHost + "/event_center/api/v1/event/tencent_sdk/?access_token=" + _accessToken + "&client_id=" + SetOrGetClientId("") + "&backFlowDay=" + _silentPeriod,
                (request =>
                {
                    string responseText = request.downloadHandler.text;
                    // string responseText = "{\n  \"data\": {\n    \"postback_list\": [\n      {\n        \"trace_id\": \"8d7493023ad4690c2567fbb06bcf54f7\",\n        \"action\": \"pay\",\n        \"postback_value\": 30,\n        \"click_company\": \"tencent\",\n        \"timestamp\": 1680998892068\n      }\n    ]\n  },\n  \"extra\": {},\n  \"code\": 0,\n  \"msg\": \"成功\"\n}";
                    
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    Debug.Log("response is " + responseText);
                    if (res != null)
                    {
                        if (res.TryGetValue("code", out var re))
                        {
                            int code = Convert.ToInt32(re);
                            if (code == 0)
                            {
                                if (res.TryGetValue("data", out object dataObj))
                                {
                                    Dictionary<string, object> dataDict = (Dictionary<string, object>) dataObj;
                                    // foreach (var kvp in dataDict)
                                    // {
                                    //     Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
                                    // }
                                    if (dataDict.TryGetValue("action", out object action))
                                    {
                                        Debug.Log(action);
#if GRAVITY_WECHAT_GAME_MODE && ENABLE_TENCENT_SDK_TRACK
                                        // 判断action，并且执行回调
                                        if (action.Equals("register"))
                                        {
                                            // 首次注册
                                            DnSdkHelper.onRegister();
                                            Debug.Log("tencent sdk onRegister");
                                            SetupUserInfo("register");
                                        } else if (action.Equals("re_active") && _enableSilentPeriod)
                                        {
                                            // 沉默唤醒
                                            DnSdkHelper.onReActive(_silentPeriod);
                                            Debug.Log("tencent sdk onReActive " + _silentPeriod);
                                            SetupUserInfo("re_active");
                                        }
#endif
                                    }
                                    else
                                    {
                                        Debug.Log("action not valid");
                                    }
                                }
                                else
                                {
                                    Debug.Log("data not valid");
                                }
                                return;
                            }
                        }
                    }

                    Debug.Log("response is null");
                    GravitySDKFile.SaveData("tencent_sdk_lock", 0);
                }));
        }
        
        public static void QueryAppConfigInfo(bool useLocalParams)
        {
            if (useLocalParams)
            {
                // 为true表明通过SetDryRunModeStatus手动代码控制过演练模式的开关，此时不需要再额外请求接口来判定是否需要开启演练模式
                if (IsDryRunModeEnabled())
                {
                    TryDryRunMode();
                }
                else
                {
                    TencentSDKRegisterTrack();
                }
                return;
            }
            UnityWebRequestMgr.Instance.Get(
                GravityHost + "/event_center/api/v1/base/appconf/?conf_type=dryrun_mode&access_token=" + _accessToken,
                (request =>
                {
                    string responseText = request.downloadHandler.text;
                    // string responseText = "{\n  \"data\": {\n    \"postback_list\": [\n      {\n        \"trace_id\": \"8d7493023ad4690c2567fbb06bcf54f7\",\n        \"action\": \"pay\",\n        \"postback_value\": 30,\n        \"click_company\": \"tencent\",\n        \"timestamp\": 1680998892068\n      }\n    ]\n  },\n  \"extra\": {},\n  \"code\": 0,\n  \"msg\": \"成功\"\n}";
                    
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    Debug.Log("response is " + responseText);
                    if (res != null)
                    {
                        if (res.TryGetValue("code", out var re))
                        {
                            int code = Convert.ToInt32(re);
                            if (code == 0)
                            {
                                if (res.TryGetValue("data", out object dataObj))
                                {
                                    Dictionary<string, object> dataDict = (Dictionary<string, object>) dataObj;
                                    // foreach (var kvp in dataDict)
                                    // {
                                    //     Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
                                    // }
                                    if (dataDict.TryGetValue("dryrun_mode", out object respObj))
                                    {
                                        Debug.Log(respObj);
                                        Dictionary<string, object> dryRunModeDic = (Dictionary<string, object>) respObj;
                                        foreach (var kvp in dryRunModeDic)
                                        {
                                            Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
                                        }
                                        if (dryRunModeDic.TryGetValue("tencent", out object tencentValue))
                                        {
                                            Debug.Log(tencentValue);
                                            int v = Convert.ToInt32(tencentValue);
                                            if (v != 0)
                                            {
                                                Debug.Log("tencent mode enabled");
                                                _isTencentDryRunModeEnabled = true;
                                                TryDryRunMode();
                                            }
                                            else
                                            {
                                                TencentSDKRegisterTrack();
                                            }
                                        }
                                        else
                                        {
                                            TencentSDKRegisterTrack();
                                        }
                                    }
                                    else
                                    {
                                        TencentSDKRegisterTrack();
                                    }
                                }
                                else
                                {
                                    TencentSDKRegisterTrack();
                                }
                            }
                            else
                            {
                                TencentSDKRegisterTrack();
                            }
                        }
                        else
                        {
                            TencentSDKRegisterTrack();
                        }
                    }
                    else
                    {
                        TencentSDKRegisterTrack();
                    }
                }));
        }
        
        public static void TryDryRunMode(int waitTime = 1, int retryCount = 0, int maxRetryInt = 0, string specificAction = null)
        {
            UnityWebRequestMgr.Instance.Get(
                GravityHost + "/event_center/api/v1/event/postback_info/?access_token=" + _accessToken + "&client_id=" + SetOrGetClientId(""),
                (request =>
                {
                    string responseText = request.downloadHandler.text;
                    // string responseText = "{\n  \"data\": {\n      \"postback_list\": [\n        {\n          \"trace_id\": \"your_order_id1\",\n          \"action\": \"pay\",\n          \"postback_value\": 10,\n          \"timestamp\": 1733371985420,\n          \"click_company\": \"tencent\"\n      },{\n          \"trace_id\": \"your_trace_id2\",\n          \"action\": \"create_role\",\n          \"postback_value\": 0,\n          \"role_name\": \"法师\",\n          \"timestamp\": 1733371985420,\n          \"click_company\": \"tencent\"\n      },\n        {\n          \"trace_id\": \"your_trace_id3\",\n          \"action\": \"tutorial_finish\",\n          \"postback_value\": 0,\n          \"role_name\": \"\",\n          \"timestamp\": 1733371985420,\n          \"click_company\": \"tencent\"\n      },{\n          \"trace_id\": \"your_trace_id4\",\n          \"action\": \"re_active\",\n          \"postback_value\": 0,\n          \"role_name\": \"\",\n          \"re_active_day\":10,\n          \"timestamp\": 1733371985420,\n          \"click_company\": \"tencent\"\n      }\n      ],\n      \"click_company\": \"tencent\",\n      \"retry\": 1\n  },\n  \"extra\": {},\n  \"code\": 0,\n  \"msg\": \"\\u6210\\u529f\"\n}";
                    
                    Dictionary<string, object> res = GE_MiniJson.Deserialize(responseText);
                    Debug.Log("response is " + retryCount + " " + responseText);
                    if (res != null)
                    {
                        if (res.TryGetValue("code", out var re))
                        {
                            int code = Convert.ToInt32(re);
                            if (code == 0)
                            {
                                if (res.TryGetValue("data", out object dataObj))
                                {
                                    Dictionary<string, object> dataDict = (Dictionary<string, object>) dataObj;
                                    // foreach (var kvp in dataDict)
                                    // {
                                    //     Debug.Log("key " + kvp.Key + " : " + kvp.Value?.ToString());
                                    // }
                                    if (maxRetryInt == 0)
                                    {
                                        if (dataDict.TryGetValue("retry", out var maxRetry))
                                        {
                                            maxRetryInt = Convert.ToInt32(maxRetry);
                                        }
                                    }

                                    if (dataDict.TryGetValue("postback_list", out object respObj))
                                    {
                                        Debug.Log(respObj);
                                        List<object> respList = (List<object>) Convert.ChangeType(respObj, typeof(List<object>));
                                        if (respList.Count == 0)
                                        {
                                            // 没有查到数据，可能需要retry
                                            if (maxRetryInt > retryCount)
                                            {
                                                Debug.Log("retry " + retryCount + " maxCount " + maxRetryInt + " wait time " + waitTime);
                                                TryDryRunMode(waitTime + 1, retryCount + 1, maxRetryInt);
                                            }
                                            return;
                                        }
                                        if (dataDict.TryGetValue("click_company", out var clickCompany))
                                        {
                                            if (!clickCompany.Equals("tencent"))
                                            {
                                                return;
                                            }
                                        }
                                        List<Dictionary<string, string>> postBackList = new List<Dictionary<string, string>>();
                                        foreach (var item in respList)
                                        {
                                            Dictionary<string, object> postbackDict = (Dictionary<string, object>) item;
                                            // foreach (var eachValue in postbackDict)
                                            // {
                                            //     Debug.Log("key " + eachValue.Key + " : " + eachValue.Value?.ToString() + " " + eachValue);
                                            // }
                                            string traceId = (string) postbackDict["trace_id"];
                                            string action = (string) postbackDict["action"];
                                            if (specificAction != null && !specificAction.Equals(action))
                                            {
                                                Debug.Log("本次DryRun不处理Action：" + action);
                                                continue;
                                            }
                                            if (action.Equals("pay"))
                                            {
                                                int postbackValueInt = Convert.ToInt32(postbackDict["postback_value"]);

                                                Debug.Log("pay " + postbackValueInt);
#if GRAVITY_WECHAT_GAME_MODE && ENABLE_TENCENT_SDK_TRACK
                                                // 付费
                                                DnSdkHelper.onPurchase(postbackValueInt);
                                                Debug.Log("tencent sdk onPurchase " + postbackValueInt);
#endif
                                            } else if (action.Equals("create_role"))
                                            {
                                                string role_name = "";
                                                if (postbackDict.ContainsKey("role_name"))
                                                {
                                                    role_name = (string) postbackDict["role_name"];
                                                }
                                                Debug.Log("create_role " + role_name);
                                                GravityEngineAPI.TrackMPCreateRole(role_name);
                                            } else if (action.Equals("tutorial_finish"))
                                            {
                                                Debug.Log("tutorial_finish");
                                                GravityEngineAPI.TrackMPTutorialFinish();
                                            } else if (action.Equals("register"))
                                            {
                                                Debug.Log("register");
#if GRAVITY_WECHAT_GAME_MODE && ENABLE_TENCENT_SDK_TRACK
                                                // 首次注册
                                                DnSdkHelper.onRegister();
                                                Debug.Log("tencent sdk onRegister");
#endif
                                            } else if (action.Equals("re_active"))
                                            {
                                                int re_active_day = 0;
                                                if (postbackDict.ContainsKey("re_active_day"))
                                                {
                                                    re_active_day = Convert.ToInt32(postbackDict["re_active_day"]);
                                                }
                                                Debug.Log("re_active " + re_active_day);
#if GRAVITY_WECHAT_GAME_MODE && ENABLE_TENCENT_SDK_TRACK
                                                // 沉默唤醒
                                                DnSdkHelper.onReActive(re_active_day);
                                                Debug.Log("tencent sdk onReActive " + re_active_day);
#endif
                                            }
                                            var postback = new Dictionary<string, string>()
                                            {
                                                {"trace_id",traceId},
                                                {"action", action}
                                            };
                                            postBackList.Add(postback);
                                        }

                                        if (postBackList.Count > 0)
                                        {
                                            Debug.Log("need to send postback count " + postBackList.Count);
                                            SendDryRunInfo(postBackList, null);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }), waitTime);
        }
    }
}
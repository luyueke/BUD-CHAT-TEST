using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using Basic.Utils;
using Game.Database;
using GameData.Base;
using GameData.Manager;
using GameData.UGCData;
using Message;
using NetBusiness.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using xasset;
using Object = UnityEngine.Object;

namespace Game.Store
{
    /// <summary>
    /// 背包的应用数据类
    /// </summary>
    public static class AssetsDataManager
    {
        private static List<AssetsDataHandler> allHandler = new();
        internal static List<string> FreeList = new List<string>()
        {
            "10300008","10300011","10300012","10300013","10300023","10300024","10300026","10300027",
            "10400157","10400172","10400190","10400001","10400002","10400003","10400197",
            "10600027","10600029","10600038","10600039","10600040","10600041","10600042","10600047","10600048","10600050",
            "10800011","10800015","10800020","10800021","10800028","10800029","10800001","10800037","10800040","10800047","10800052","10800002","10800055","10800056",
            "11100016","11100017","11100018","11100019","11100020","11100021",
            "11300052","11300053","11300103","11300105","11300106","11300107","11300108","11300109","11300110","11300111",
            "11800001",
            "11900001","11900002",
            "12000002",
            "12100002",
            "12300001","12300003","12300004","12300005","12300006","12300007","12300008","12300009","12300010","12300002",
            "40300104","40300103","40300101","40200053","40100069","40100050","40100049","40100002","40100001",
            "40600340","40600341","40600342","40800353","40800354","40800355","40800361", "40400452",
            "72200001", "70400003", "70400004", "70400007","72500001","72500002","72500003","70900002","72600007","71300001","70600004","70600005","70600007","71100001","71100002","71100003","71100004","71100005","71200002","71200003","71900001",
            "40900001"
        };

        private static Dictionary<string, RecommendItemData> ugcDataCache = new Dictionary<string, RecommendItemData>();
        private static Product.StoreData storeData;

        public static Product.StoreData StoreData => storeData;

        public static Action<int> BuyAction;

        public static List<ShapeThemeProp> ShapeThemePropList;

        public static List<ShapeBannerData> ShapeBannerDataList;

        public static void Init()
        {
            allHandler.Clear();
            MessageHelper.RemoveListener<List<InventoryData>>(MessageName.AvaterDatabaseOnDataChange, OnOwnedChanged);
            MessageHelper.RemoveListener(MessageName.AvaterDatabaseIsReady, ServerData);
            MessageHelper.RemoveListener(MessageName.AvaterDatabaseOnDataRebuild, ServerData);

            MessageHelper.AddListener<List<InventoryData>>(MessageName.AvaterDatabaseOnDataChange, OnOwnedChanged);
            MessageHelper.AddListener(MessageName.AvaterDatabaseIsReady, ServerData);
            MessageHelper.AddListener(MessageName.AvaterDatabaseOnDataRebuild, ServerData);

            LoadStoreData();
            LoadShapeStoreData();
        }

        private static string GetProductJsonPath()
        {
            string productJsonUrl = "Assets/Arts/Config/StoreConfig/product.json";
#if PACKAGE_TYPE_US
            productJsonUrl = "Assets/Arts/Config/StoreConfig/product_us.json";
#endif
            return productJsonUrl;
        }

        public static void LoadStoreData()
        {
            string productJsonPath = GetProductJsonPath();
            var storeRequest = Asset.Load(productJsonPath, typeof(TextAsset));
            if (storeRequest == null)
            {
                Debug.LogError("商城数据不存在");
            }
            else
            {
                var textAsset = storeRequest.asset as TextAsset;
                if (storeRequest.result == Request.Result.Success && textAsset != null)
                {
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(textAsset.text);
                        storeData = Product.StoreData.Parser.ParseFrom(bytes);
                    }
                    catch
                    {
                        LoggerUtils.LogError("本地商城数据解析失败。字符串长度:" + textAsset.text.Length);
                    }
                }
                else
                {
                    LoggerUtils.LogError("本地商城数据加载失败");
                }
            }

            if (storeData == null)
            {
                storeData = new Product.StoreData();
            }
        }

        // 服务器配置表修改了
        private static void ServerData()
        {
            foreach (var handler in allHandler)
            {
                handler.SafeInitData();
                handler.OnDataChangeInvoke(new AssetsData[0]);
            }
        }

        public static T GetData<T>() where T : AssetsDataHandler, new()
        {
            T handler = (T)allHandler.Find(h =>
            {
                return h.GetType() == typeof(T);
            });

            if (handler != null) return handler as T;

            handler = new T();
            allHandler.Add(handler);
            return handler;
        }

        /// <summary>
        /// 获取促销商品数据。(运营配置相关数据，不走json)
        /// </summary>
        public static void LoadShapeStoreData()
        {
            JObject galleryClaim = new JObject
            {
                ["uid"] = AccountDataManager.Inst.Uid      //(int)PGCGameType.AIHospital,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.storeShape,
                HttpMethod.GET,
                JsonConvert.SerializeObject(galleryClaim),
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<ShapeStoreData>(arg0);
                    if (data != null && data.list != null)
                    {
                        ShapeThemePropList = new List<ShapeThemeProp>();
                        foreach(var item in data.list)
                        {
                            ShapeThemePropList.Add(item);
                        }
                    }
                    else
                    {
                        LoggerUtils.Log("storeShape解析失败!");
                    }
                }, onFail: arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                });

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.shapeBanner,
                HttpMethod.GET,
                JsonConvert.SerializeObject(galleryClaim),
                onReceive: arg0 =>
                {
                    var data = JsonConvert.DeserializeObject<ShapeStoreBanner>(arg0);
                    if (data != null && data.list != null)
                    {
                        ShapeBannerDataList = new List<ShapeBannerData>();
                        foreach (var item in data.list)
                        {
                            ShapeBannerDataList.Add(item);
                        }
                    }
                    else
                    {
                        LoggerUtils.Log("shapeBanner解析失败!");
                    }
                }, onFail: arg0 =>
                {
                    LoggerUtils.LogError(HttpUrlDefine.TokenData + "请求出错" + arg0);
                });
        }

        #region 请求皮肤详情
        private static Dictionary<string, Action<RecommendItemData>> requestUgcActions = new();
        private static Dictionary<string, Action<RecommendItemData>> requestingUgcActions = new();

        public static void GetUgcInfo(string ugcId, Action<RecommendItemData> callback)
        {
            if (ugcDataCache.ContainsKey(ugcId))
            {
                callback?.Invoke(ugcDataCache[ugcId]);
                return;
            }
            if (requestUgcActions.Count == 0) CoroutineManager.Inst.StartCoroutine(BatchRequestUgcInfo());

            if (requestUgcActions.ContainsKey(ugcId)) requestUgcActions[ugcId] += callback;
            else if (requestingUgcActions.ContainsKey(ugcId)) requestingUgcActions[ugcId] += callback;
            else requestUgcActions[ugcId] = callback;
        }

        public static IEnumerator BatchRequestUgcInfo()
        {
            yield return new WaitForEndOfFrame();
            while (requestUgcActions.Count > 0)
            {
                var ugcIds = "";
                var dict = new Dictionary<string, Action<RecommendItemData>>();
                foreach (var kv in requestUgcActions)
                {
                    ugcIds = string.IsNullOrEmpty(ugcIds) ? kv.Key : ugcIds + "," + kv.Key;
                    dict[kv.Key] = kv.Value;
                    requestingUgcActions[kv.Key] = kv.Value;
                    if (dict.Count >= 50) break;
                }
                foreach (var kv in dict) requestUgcActions.Remove(kv.Key);

                JObject req = new JObject() { ["idList"] = ugcIds, };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetClothesBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchDetailRsp>(content);

                    if (rsps.skinList == null) return;

                    foreach (var rsp in rsps.skinList)
                    {
                        if (rsp == null || rsp.skinInfo == null) continue;
                        RecommendItemData ugcData = new RecommendItemData()
                        {
                            UgcInfo = rsp.skinInfo,
                            creatorInfo = rsp.creator,
                            interactInfo = rsp.interactInfo
                        };
                        var ugcId = rsp.skinInfo.id;
                        ugcDataCache[ugcId] = ugcData;
                    }

                    foreach (var kv in dict)
                    {
                        if (requestingUgcActions.ContainsKey(kv.Key) && ugcDataCache.ContainsKey(kv.Key))
                        {
                            requestingUgcActions[kv.Key]?.Invoke(ugcDataCache[kv.Key]);
                        }

                        requestingUgcActions.Remove(kv.Key);
                    }
                },
                (content) =>
                {
                    foreach (var kv in dict)
                    {
                        requestingUgcActions.Remove(kv.Key);
                    }
                });
            }
        }
        #endregion

        #region 请求乐谱详情
        private static Dictionary<string, Action<bool, RecommendItemData>> requestMusicScoreActions = new();
        private static Dictionary<string, Action<bool, RecommendItemData>> requestingMusicScoreActions = new();

        public static void GetMusicScoreInfo(string ugcId, Action<bool, RecommendItemData> callback)
        {
            if (ugcDataCache.ContainsKey(ugcId))
            {
                callback?.Invoke(true, ugcDataCache[ugcId]);
                return;
            }

            if (requestMusicScoreActions.Count == 0) CoroutineManager.Inst.StartCoroutine(BatchRequestMusicScoreInfo());

            if (requestMusicScoreActions.ContainsKey(ugcId)) requestMusicScoreActions[ugcId] += callback;
            else if (requestingMusicScoreActions.ContainsKey(ugcId)) requestingMusicScoreActions[ugcId] += callback;
            else requestMusicScoreActions[ugcId] = callback;
        }

        public static IEnumerator BatchRequestMusicScoreInfo()
        {
            yield return new WaitForEndOfFrame();
            while (requestMusicScoreActions.Count > 0)
            {
                var ugcIds = "";
                var dict = new Dictionary<string, Action<bool, RecommendItemData>>();
                foreach (var kv in requestMusicScoreActions)
                {
                    ugcIds = string.IsNullOrEmpty(ugcIds) ? kv.Key : ugcIds + "," + kv.Key;
                    dict[kv.Key] = kv.Value;
                    requestingMusicScoreActions[kv.Key] = kv.Value;
                    if (dict.Count >= 50) break;
                }
                foreach (var kv in dict) requestMusicScoreActions.Remove(kv.Key);

                JObject req = new JObject() { ["idList"] = ugcIds, };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetMusicScoreBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchMusicScoreDetailRsp>(content);

                    if (rsps.musicScoreList == null) return;

                    foreach (var rsp in rsps.musicScoreList)
                    {
                        if (rsp == null || rsp.musicScoreInfo == null) continue;
                        RecommendItemData ugcData = new RecommendItemData()
                        {
                            UgcInfo = rsp.musicScoreInfo,
                            creatorInfo = rsp.creator,
                            interactInfo = rsp.interactInfo
                        };
                        var ugcId = rsp.musicScoreInfo.id;
                        ugcDataCache[ugcId] = ugcData;
                    }

                    foreach (var kv in dict)
                    {
                        if (requestingMusicScoreActions.ContainsKey(kv.Key) && ugcDataCache.ContainsKey(kv.Key))
                        {
                            requestingMusicScoreActions[kv.Key]?.Invoke(true, ugcDataCache[kv.Key]);
                        }
                        else
                        {
                            requestingMusicScoreActions[kv.Key]?.Invoke(false, null);
                        }

                        requestingMusicScoreActions.Remove(kv.Key);
                    }
                },
                (content) =>
                {
                    foreach (var kv in dict)
                    {
                        requestingMusicScoreActions.Remove(kv.Key);
                    }
                });
            }
        }
        #endregion


        #region 请求姿势详情
        private static Dictionary<string, Action<bool, RecommendItemData>> requestPoseActions = new();
        private static Dictionary<string, Action<bool, RecommendItemData>> requestingPoseActions = new();

        public static void GetPoseInfo(string ugcId, Action<bool, RecommendItemData> callback)
        {
            if (ugcDataCache.ContainsKey(ugcId))
            {
                callback?.Invoke(true, ugcDataCache[ugcId]);
                return;
            }
            if (requestPoseActions.Count == 0) CoroutineManager.Inst.StartCoroutine(BatchRequestPoseInfo());
            if (requestPoseActions.ContainsKey(ugcId)) requestPoseActions[ugcId] += callback;
            else if (requestingPoseActions.ContainsKey(ugcId)) requestingPoseActions[ugcId] += callback;
            else requestPoseActions[ugcId] = callback;
        }

        public static IEnumerator BatchRequestPoseInfo()
        {
            yield return new WaitForEndOfFrame();
            while (requestPoseActions.Count > 0)
            {
                var ugcIds = "";
                var dict = new Dictionary<string, Action<bool, RecommendItemData>>();
                foreach (var kv in requestPoseActions)
                {
                    ugcIds = string.IsNullOrEmpty(ugcIds) ? kv.Key : ugcIds + "," + kv.Key;
                    dict[kv.Key] = kv.Value;
                    requestingPoseActions[kv.Key] = kv.Value;
                    if (dict.Count >= 50) break;
                }
                foreach (var kv in dict) requestPoseActions.Remove(kv.Key);

                JObject req = new JObject() { ["idList"] = ugcIds, };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetPoseBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchPoseDetailRsp>(content);

                    if (rsps.poseList == null) return;

                    foreach (var rsp in rsps.poseList)
                    {
                        if (rsp == null || rsp.poseInfo == null) continue;
                        RecommendItemData ugcData = new RecommendItemData()
                        {
                            UgcInfo = rsp.poseInfo,
                            creatorInfo = rsp.creator,
                            interactInfo = rsp.interactInfo
                        };
                        var ugcId = rsp.poseInfo.id;
                        ugcDataCache[ugcId] = ugcData;
                    }

                    foreach (var kv in dict)
                    {
                        if (requestingPoseActions.ContainsKey(kv.Key) && ugcDataCache.ContainsKey(kv.Key))
                        {
                            requestingPoseActions[kv.Key]?.Invoke(true, ugcDataCache[kv.Key]);
                        }
                        else
                        {
                            requestingPoseActions[kv.Key]?.Invoke(false, null);
                        }

                        requestingPoseActions.Remove(kv.Key);
                    }
                },
                (content) =>
                {
                    foreach (var kv in dict)
                    {
                        requestingPoseActions.Remove(kv.Key);
                    }
                });
            }
        }
        #endregion


        #region 请求Ugc动画详情
        private static Dictionary<string, Action<bool, RecommendItemData>> requestUgcAnimActions = new();
        private static Dictionary<string, Action<bool, RecommendItemData>> requestingUgcAnimActions = new();

        public static void GetUgcAnimInfo(string ugcId, Action<bool, RecommendItemData> callback)
        {
            if (ugcDataCache.ContainsKey(ugcId))
            {
                callback?.Invoke(true, ugcDataCache[ugcId]);
                return;
            }
            if (requestUgcAnimActions.Count == 0) CoroutineManager.Inst.StartCoroutine(BatchRequestUgcAnimInfo());
            if (requestUgcAnimActions.ContainsKey(ugcId)) requestUgcAnimActions[ugcId] += callback;
            else if (requestingUgcAnimActions.ContainsKey(ugcId)) requestingUgcAnimActions[ugcId] += callback;
            else requestUgcAnimActions[ugcId] = callback;
        }

        public static IEnumerator BatchRequestUgcAnimInfo()
        {
            yield return new WaitForEndOfFrame();
            while (requestUgcAnimActions.Count > 0)
            {
                var ugcIds = "";
                var dict = new Dictionary<string, Action<bool, RecommendItemData>>();
                foreach (var kv in requestUgcAnimActions)
                {
                    ugcIds = string.IsNullOrEmpty(ugcIds) ? kv.Key : ugcIds + "," + kv.Key;
                    dict[kv.Key] = kv.Value;
                    requestingUgcAnimActions[kv.Key] = kv.Value;
                    if (dict.Count >= 50) break;
                }
                foreach (var kv in dict) requestUgcAnimActions.Remove(kv.Key);

                JObject req = new JObject() { ["idList"] = ugcIds, };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.batchAnimInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchUgcAnimDetailRsp>(content);

                    if (rsps.animationList == null) return;

                    foreach (var rsp in rsps.animationList)
                    {
                        if (rsp == null || rsp.animInfo == null) continue;
                        RecommendItemData ugcData = new RecommendItemData()
                        {
                            UgcInfo = rsp.animInfo,
                            creatorInfo = rsp.creator,
                            interactInfo = rsp.interactInfo
                        };
                        var ugcId = rsp.animInfo.id;
                        ugcDataCache[ugcId] = ugcData;
                    }

                    foreach (var kv in dict)
                    {
                        if (requestingUgcAnimActions.ContainsKey(kv.Key) && ugcDataCache.ContainsKey(kv.Key))
                        {
                            requestingUgcAnimActions[kv.Key]?.Invoke(true, ugcDataCache[kv.Key]);
                        }
                        else
                        {
                            requestingUgcAnimActions[kv.Key]?.Invoke(false, null);
                        }

                        requestingUgcAnimActions.Remove(kv.Key);
                    }
                },
                (content) =>
                {
                    foreach (var kv in dict)
                    {
                        requestingUgcAnimActions.Remove(kv.Key);
                    }
                });
            }
        }
        #endregion

        #region 请求Ugc车辆详情

        private static Dictionary<string, Action<bool, RecommendItemData>> requestUgcVehicleActions = new();
        private static Dictionary<string, Action<bool, RecommendItemData>> requestingUgcVehicleActions = new();

        public static void GetUgcVehicleInfo(string ugcId, Action<bool, RecommendItemData> callback)
        {
            if (ugcDataCache.ContainsKey(ugcId))
            {
                callback?.Invoke(true, ugcDataCache[ugcId]);
                return;
            }
            if (requestUgcVehicleActions.Count == 0) CoroutineManager.Inst.StartCoroutine(BatchRequestUgcVehicleInfo());

            if (requestUgcVehicleActions.ContainsKey(ugcId)) requestUgcVehicleActions[ugcId] += callback;
            else if (requestingUgcVehicleActions.ContainsKey(ugcId)) requestingUgcVehicleActions[ugcId] += callback;
            else requestUgcVehicleActions[ugcId] = callback;
        }

        public static IEnumerator BatchRequestUgcVehicleInfo()
        {
            yield return new WaitForEndOfFrame();
            while (requestUgcVehicleActions.Count > 0)
            {
                var ugcIds = "";
                var dict = new Dictionary<string, Action<bool, RecommendItemData>>();
                foreach (var kv in requestUgcVehicleActions)
                {
                    ugcIds = string.IsNullOrEmpty(ugcIds) ? kv.Key : ugcIds + "," + kv.Key;
                    dict[kv.Key] = kv.Value;
                    requestingUgcVehicleActions[kv.Key] = kv.Value;
                    if (dict.Count >= 50) break;
                }
                foreach (var kv in dict) requestUgcVehicleActions.Remove(kv.Key);

                JObject req = new JObject() { ["idList"] = ugcIds, };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.batchVehicleInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchUgcVehicleDetailRsp>(content);
                    if (rsps.vehicleList == null) return;

                    foreach (var rsp in rsps.vehicleList)
                    {
                        if (rsp == null || rsp.vehicleInfo == null) continue;
                        var ugcId = rsp.vehicleInfo.id;
                        // creatorInfo 以 /recommend/ugc/sectionInfoV2 返回的为准（避免被 batchVehicleInfo 覆盖）
                        if (ugcDataCache.TryGetValue(ugcId, out var cacheData) && cacheData != null)
                        {
                            cacheData.UgcInfo = rsp.vehicleInfo;
                            if (cacheData.creatorInfo == null) cacheData.creatorInfo = rsp.creator; // 兜底
                            if (rsp.interactInfo != null) cacheData.interactInfo = rsp.interactInfo;
                            LoggerUtils.LogError(cacheData.creatorInfo.ToString());
                        }
                        else
                        {
                            ugcDataCache[ugcId] = new RecommendItemData()
                            {
                                UgcInfo = rsp.vehicleInfo,
                                creatorInfo = rsp.creator,
                                interactInfo = rsp.interactInfo
                            };
                        }
                    }

                    foreach (var kv in dict)
                    {
                        if (requestingUgcVehicleActions.ContainsKey(kv.Key) && ugcDataCache.ContainsKey(kv.Key))
                        {
                            requestingUgcVehicleActions[kv.Key]?.Invoke(true, ugcDataCache[kv.Key]);
                        }
                        else
                        {
                            requestingUgcVehicleActions[kv.Key]?.Invoke(false, null);
                        }

                        requestingUgcVehicleActions.Remove(kv.Key);
                    }
                },
                (content) =>
                {
                    foreach (var kv in dict)
                    {
                        requestingUgcVehicleActions.Remove(kv.Key);
                    }
                });
            }
        }
        #endregion

        #region 请求演员详情
        private static Dictionary<string, Action<bool, RecommendItemData>> requestActorActions = new();
        private static Dictionary<string, Action<bool, RecommendItemData>> requestingActorActions = new();

        public static void GetActorInfo(string ugcId, Action<bool, RecommendItemData> callback)
        {
            if (ugcDataCache.ContainsKey(ugcId) && ugcDataCache[ugcId].theatreAvatarInfo != null)
            {
                callback?.Invoke(true, ugcDataCache[ugcId]);
                return;
            }
            if (requestActorActions.Count == 0) CoroutineManager.Inst.StartCoroutine(BatchRequestActorInfo());
            if (requestActorActions.ContainsKey(ugcId)) requestActorActions[ugcId] += callback;
            else if (requestingActorActions.ContainsKey(ugcId)) requestingActorActions[ugcId] += callback;
            else requestActorActions[ugcId] = callback;
        }

        public static IEnumerator BatchRequestActorInfo()
        {
            yield return new WaitForEndOfFrame();
            while (requestActorActions.Count > 0)
            {
                var ugcIds = "";
                var dict = new Dictionary<string, Action<bool, RecommendItemData>>();
                foreach (var kv in requestActorActions)
                {
                    ugcIds = string.IsNullOrEmpty(ugcIds) ? kv.Key : ugcIds + "," + kv.Key;
                    dict[kv.Key] = kv.Value;
                    requestingActorActions[kv.Key] = kv.Value;
                    if (dict.Count >= 50) break;
                }
                foreach (var kv in dict) requestActorActions.Remove(kv.Key);

                JObject req = new JObject() { ["idList"] = ugcIds };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActorBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchActorDetailRsp>(content);
                    if (rsps.actorList == null) return;
                    foreach (var rsp in rsps.actorList)
                    {
                        if (rsp == null || rsp.actorInfo == null) continue;
                        RecommendItemData ugcData = new RecommendItemData()
                        {
                            UgcInfo = rsp.actorInfo,
                            creatorInfo = rsp.creator,
                            interactInfo = rsp.interactInfo
                        };
                        ugcDataCache[rsp.actorInfo.id] = ugcData;
                    }
                    foreach (var kv in dict)
                    {
                        if (requestingActorActions.ContainsKey(kv.Key) && ugcDataCache.ContainsKey(kv.Key))
                            requestingActorActions[kv.Key]?.Invoke(true, ugcDataCache[kv.Key]);
                        else
                            requestingActorActions[kv.Key]?.Invoke(false, null);
                        requestingActorActions.Remove(kv.Key);
                    }
                },
                (content) =>
                {
                    foreach (var kv in dict) requestingActorActions.Remove(kv.Key);
                });
            }
        }
        #endregion

        #region 请求剧本详情
        private static Dictionary<string, Action<bool, RecommendItemData>> requestTheatreActions = new();
        private static Dictionary<string, Action<bool, RecommendItemData>> requestingTheatreActions = new();

        public static void GetTheatreInfo(string ugcId, Action<bool, RecommendItemData> callback)
        {
            if (ugcDataCache.ContainsKey(ugcId) && ugcDataCache[ugcId].theatreInfo != null)
            {
                callback?.Invoke(true, ugcDataCache[ugcId]);
                return;
            }
            if (requestTheatreActions.Count == 0) CoroutineManager.Inst.StartCoroutine(BatchRequestTheatreInfo());
            if (requestTheatreActions.ContainsKey(ugcId)) requestTheatreActions[ugcId] += callback;
            else if (requestingTheatreActions.ContainsKey(ugcId)) requestingTheatreActions[ugcId] += callback;
            else requestTheatreActions[ugcId] = callback;
        }

        public static IEnumerator BatchRequestTheatreInfo()
        {
            yield return new WaitForEndOfFrame();
            while (requestTheatreActions.Count > 0)
            {
                var ugcIds = "";
                var dict = new Dictionary<string, Action<bool, RecommendItemData>>();
                foreach (var kv in requestTheatreActions)
                {
                    ugcIds = string.IsNullOrEmpty(ugcIds) ? kv.Key : ugcIds + "," + kv.Key;
                    dict[kv.Key] = kv.Value;
                    requestingTheatreActions[kv.Key] = kv.Value;
                    if (dict.Count >= 50) break;
                }
                foreach (var kv in dict) requestTheatreActions.Remove(kv.Key);
                
                JObject req = new JObject() { ["idList"] = ugcIds };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TheatreBatchInfo, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    var rsps = JsonConvert.DeserializeObject<BatchTheatreDetailRsp>(content);
                    if (rsps.theatreList == null)
                    {
                        foreach (var kv in dict)
                        {
                            requestingTheatreActions[kv.Key]?.Invoke(false, null);
                            requestingTheatreActions.Remove(kv.Key);
                        }
                        return;
                    }
                    foreach (var rsp in rsps.theatreList)
                    {
                        if (rsp == null || rsp.theaterInfo == null) continue;
                        RecommendItemData ugcData = new RecommendItemData()
                        {
                            UgcInfo = rsp.theaterInfo,
                            creatorInfo = rsp.creator,
                            interactInfo = rsp.interactInfo
                        };
                        ugcDataCache[rsp.theaterInfo.id] = ugcData;
                    }
                    foreach (var kv in dict)
                    {
                        if (requestingTheatreActions.ContainsKey(kv.Key) && ugcDataCache.ContainsKey(kv.Key))
                            requestingTheatreActions[kv.Key]?.Invoke(true, ugcDataCache[kv.Key]);
                        else
                            requestingTheatreActions[kv.Key]?.Invoke(false, null);
                        requestingTheatreActions.Remove(kv.Key);
                    }
                },
                (content) =>
                {
                    foreach (var kv in dict) requestingTheatreActions.Remove(kv.Key);
                });
            }
        }
        #endregion

        public static void AddUgcCache(List<RecommendItemData> cache)
        {
            if (cache == null || cache.Count == 0) return;
            cache.ForEach(e =>
            {
                var ugcInfo = e?.UgcInfo;
                var ugcId = ugcInfo?.id;
                if (string.IsNullOrEmpty(ugcId)) return;

                if (ugcDataCache.TryGetValue(ugcId, out var cacheData) && cacheData != null)
                {
                    // sectionInfoV2 的 creatorInfo 需要能覆盖/补全已有缓存（例如先请求详情再进列表）
                    if (e.creatorInfo != null) cacheData.creatorInfo = e.creatorInfo;
                    if (e.interactInfo != null) cacheData.interactInfo = e.interactInfo;

                    // 补齐分页接口的字段，避免缓存里只有详情对象导致后续依赖 ugcType/ugcData 的逻辑异常
                    if (string.IsNullOrEmpty(cacheData.ugcData) && !string.IsNullOrEmpty(e.ugcData))
                    {
                        cacheData.ugcData = e.ugcData;
                        cacheData.ugcType = e.ugcType;
                    }
                    if (string.IsNullOrEmpty(cacheData.ugcId) && !string.IsNullOrEmpty(e.ugcId))
                    {
                        cacheData.ugcId = e.ugcId;
                    }
                }
                else
                {
                    ugcDataCache.Add(ugcId, e);
                }
            });
        }

        public static void ClearBagUgcData()
        {
            ugcDataCache.Clear();
        }

        public static void OnOwnedChanged(List<InventoryData> datas)
        {
            OwnedChange(datas.ToArray());
        }

        private static void OwnedChange(InventoryData[] newData)
        {
            foreach (var handler in allHandler)
            {
                handler.UpdateData(newData);
            }
        }

        public static void OnAllOwnedRefresh()
        {
            ServerData();
        }

        public static bool IsFreeAssets(string pgcId)
        {
            return FreeList.Contains(pgcId);
        }

        public static bool IsOwned(string pgcId)
        {
            if (IsFreeAssets(pgcId)) return true;
            var inventoryData = BagDatabase.Inst.Select(pgcId);
            return inventoryData != null && inventoryData.OwnedNum > 0;
        }

        public static void BuyGoods(GoodsData goodsData, Action<bool, string, int> callback, int themeId = 0)
        {
            if (goodsData == null || goodsData.ButtonType != ButtonType.Assets)
            {
                callback?.Invoke(false, "数据异常", 0);
                return;
            }

            switch (goodsData.GoodsType)
            {
                case GoodsType.SingleUgc:
                case GoodsType.BundleUgc:
                    BuyUgc(goodsData.Id, goodsData.Price.CurrencyType, (int)(Mathf.Ceil(goodsData.Price.Value)), callback);
                    break;
                case GoodsType.SinglePgc:
                    BuyPgc(goodsData.Id, goodsData.Price.CurrencyType, (int)(Mathf.Ceil(goodsData.Price.Value)), callback, themeId);
                    break;
            }
        }

        public static void BuyUgc(string ugcId, CurrencyType currencyType, int price, Action<bool, string, int> callback)
        {
            var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount((CurrencyType)currencyType);

            if (count < price)
            {
                callback?.Invoke(false, "余额不足", price - count);
                // 余额不足
                return;
            }


            JObject req = new JObject()
            {
                ["ugcId"] = ugcId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyUgcPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(true, _, 0);
                var rsp = JsonConvert.DeserializeObject<ServerBagUpdateDataRsp>(_);
                BuyAction?.Invoke(rsp.popupType);
            }, (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(false, _, 0);
            });
        }

        public static void BuyUgc(List<AssetsBuyParam> BuyParam, Action<bool, string, int> callback)
        {
            if (BuyParam == null || BuyParam.Count <= 0)
            {
                callback?.Invoke(false, "", 0);
                return;
            }


            var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount((CurrencyType)BuyParam[0].currencyType);
            var price = 0;
            var ls = new List<string>();
            foreach (var item in BuyParam)
            {
                price += item.price;
                ls.Add(item.ugcInfo.id);
            }

            if (count < price)
            {
                callback?.Invoke(false, "余额不足", price - count);
                // 余额不足
                return;
            }

            var req = new Req();
            req.ugcIds = ls;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyUgcPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(true, _, 0);
                var rsp = JsonConvert.DeserializeObject<ServerBagUpdateDataRsp>(_);
                BuyAction?.Invoke(rsp.popupType);
            }, (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(false, _, 0);
            });
        }

        public class Req
        {
            public List<string> ugcIds;
        }

        public static void BuyPgc(string pgcId, CurrencyType currencyType, int price, Action<bool, string, int> callback, int themeId = 0)
        {
            var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount((CurrencyType)currencyType);

            if (count < price)
            {
                callback?.Invoke(false, "余额不足", price - count);
                // 余额不足
                return;
            }
            JObject req ;
            if (themeId == 0)
            {

                req = new JObject()
                {
                    ["pgcId"] = pgcId,
                };
            }
            else
            {
                req = new JObject()
                {
                    ["pgcId"] = pgcId,
                    ["salesthemeid"] = themeId,
                };
            }

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyPgcPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(true, _, 0);
            }, (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(false, _, 0);
            });
        }

        public static void GetBanReward(string ugcId, Action<bool> callback)
        {
            JObject req = new JObject()
            {
                ["ugcId"] = ugcId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BanCompensation, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(true);
            }, (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(false);
            });
        }

        public static void BuyOc(string productId, int slotType, CurrencyType currencyType, int price, Action<bool, string, int> callback)
        {
            var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount((CurrencyType)currencyType);

            if (count < price)
            {
                callback?.Invoke(false, "余额不足", price - count);
                // 余额不足
                return;
            }

            JObject req = new JObject()
            {
                ["productType"] = 5,
                ["productId"] = productId,
                ["slotType"] = slotType,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(true, _, 0);
            }, (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(false, _, 0);
            });
        }


        public static void BuyAIRes(string productId, int aiProductType, CurrencyType currencyType, int price, Action<bool, AIBuyResourceResult, int> callback, Action updateFree, Action<int> noMoney)
        {
            var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount((CurrencyType)currencyType);

            if (count < price)
            {
                noMoney?.Invoke(price - count);
                // 余额不足
                return;
            }

            JObject req = new JObject()
            {
                ["productType"] = 12,
                ["productId"] = productId,
                ["aiProductType"] = aiProductType,
            };
            NetworkManager.Inst.SendHttpRequest<AIBuyResourceResult>(HttpUrlDefine.BuyProductPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                callback?.Invoke(true, _, 0);
            }, (rep) =>
            {
                if (rep != null && rep.result == 401)
                {
                    updateFree?.Invoke();
                }
                else
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    callback?.Invoke(false, null, 0);
                }
            });
        }

        public static int GetUgcStyle(GoodsData goodsData)
        {
            if (goodsData.Assets.Count > 0)
            {
                var asset = goodsData.Assets[0];
                if (asset.UgcInfo != null && asset.UgcInfo.skinInfo != null)
                {
                    return asset.UgcInfo.skinInfo.ugcStyle;
                }
            }
            return 0;
        }

        public static void ClearNewTip(GoodsData goodsData)
        {
            if (goodsData == null) return;
            if (goodsData.Assets == null) return;

            var req = new RedDotCleanDataList()
            {
                list = new List<RedDotCleanData>()
                {
                    new RedDotCleanData()
                    {
                        bizList = new List<Biz>(),
                        type = 1,
                        all = false,
                    }
                }
            };


            var ops = new List<BagDatabase.OperationData>();
            if (goodsData.GoodsType == GoodsType.BundleUgc)
            {
                // 套装只清本体的 inv（bundle id），子部件各自点击时独立清理
                var inv = goodsData.Assets?.Count > 0 ? goodsData.Assets[0].InventoryData : null;
                if (inv != null && inv.IsNew)
                {
                    var newData = inv.Clone();
                    newData.IsNew = false;
                    ops.Add(new BagDatabase.OperationData() { InventoryData = newData, Operation = BagDatabase.Operation.Modify });
                    req.list[0].bizList.Add(new Biz() { id = inv.Id, subType = inv.ClassType });
                }
            }
            else
            {
                foreach (var assets in goodsData.Assets)
                {
                    if (assets.InventoryData != null && assets.InventoryData.IsNew)
                    {
                        var newData = assets.InventoryData.Clone();
                        newData.IsNew = false;
                        ops.Add(new BagDatabase.OperationData()
                        {
                            InventoryData = newData,
                            Operation = BagDatabase.Operation.Modify
                        });
                        req.list[0].bizList.Add(new Biz() { id = assets.Id, subType = assets.InventoryData.ClassType });
                    }
                }
            }


            if (ops.Count > 0)
            {
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.cleanRedDot, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
                {
                    BagDatabase.Inst.ClientOperation(ops);
                }, (_) =>
                {
                    BagDatabase.Inst.ClientOperation(ops);
                });

            }
        }

        public static void BuyGoodsUseVoucher(GoodsData goodsData, Action<bool, string, int> callback)
        {
            if (goodsData == null || goodsData.ButtonType != ButtonType.Assets)
            {
                callback?.Invoke(false, "数据异常", 0);
                return;
            }

            switch (goodsData.GoodsType)
            {
                case GoodsType.SingleUgc:
                    BuyUgcUseVoucher(goodsData.Id, callback);
                    break;
                case GoodsType.SinglePgc:
                    BuyPgcUserVocher(goodsData.Id, callback);
                    break;
            }
        }

        public static void BuyUgcUseVoucher(string ugcId, Action<bool, string, int> callback)
        {
            //var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.UGCSkinTicket);

            //if (count <= 0)
            //{
            //    callback?.Invoke(false, "皮肤卷不足", 0);
            //    // 余额不足
            //    return;
            //}


            //JObject req = new JObject()
            //{
            //    ["ugcId"] = ugcId,
            //    ["useVoucher"] = true
            //};
            //NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyUgcPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            //{
            //    AccountDataManager.Inst.BalanceInfo.Refresh();
            //    callback?.Invoke(true, _, 0);
            //}, (_) =>
            //{
            //    callback?.Invoke(false, _, 0);
            //});
        }

        public static void BuyPgcUserVocher(string pgcId, Action<bool, string, int> callback)
        {
            //var count = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.UGCSkinTicket);

            //if (count <= 0)
            //{
            //    callback?.Invoke(false, "皮肤卷不足", 0);
            //    // 余额不足
            //    return;
            //}


            //JObject req = new JObject()
            //{
            //    ["pgcId"] = pgcId,
            //    ["useVoucher"] = true
            //};
            //NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.BuyPgcPay, HttpMethod.POST, JsonConvert.SerializeObject(req), (_) =>
            //{
            //    AccountDataManager.Inst.BalanceInfo.Refresh();
            //    callback?.Invoke(true, _, 0);
            //}, (_) =>
            //{
            //    callback?.Invoke(false, _, 0);
            //});
        }

        /// <summary>
        /// 判断是否使用券
        /// </summary>
        /// <param name="goodsData"></param>
        /// <returns></returns>
        public static bool CheckUseTicket(GoodsData goodsData)
        {
            if (goodsData.Price == null) return false;
            switch (goodsData.subType)
            {
                case 0:
                case 1008://姿势和乐谱不能用卷
                    return false;
                case 1007: //动作卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityAnimationTicket) <= 0
                        || goodsData.Price.Value > 200) return false;
                    return true;
                case 24: //乐器卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityInstrumentTicket) <= 0
                        || goodsData.Price.Value > 200) return false;
                    return true;
                case 1011: //载具卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityVehicleTicket) <= 0
                        || goodsData.Price.Value > 200) return false;
                    return true;
                case 1015: //载具卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunityTheaterTicket) <= 0
                        || goodsData.Price.Value > 350) return false;
                    return true;
                default: //其余都算皮肤卷
                    if (TokenDataManager.Inst.Data.GetToken(CurrencyType.CommunitySkinTicket) <= 0
                        || goodsData.Price.Value > 60) return false;
                    return true;
            }
        }

        public static Func<bool> IsMonthCardActive;
        public static Func<float> GetDiscountRate;
        /// <summary>
        /// 算粉币的折后价格
        /// </summary>
        /// <param name="goodsData"></param>
        /// <returns></returns>
        public static float GetDiscountPinkCoin(GoodsData goodsData){
            if(goodsData.Price == null) return 0;
            if(goodsData.Price.CurrencyType != CurrencyType.PinkCoin) return goodsData.Price.Value;
            if(goodsData.Price.Value <= 0) return goodsData.Price.Value;
            if(IsMonthCardActive?.Invoke() ?? false){
                return goodsData.Price.Value * (GetDiscountRate?.Invoke() ?? 1);
            }
            return goodsData.Price.Value;
        }
    }

    public class StoreResourceRsp
    {
        public int resourceVersion;
        public string resourceData;
    }

    public class RedDotCleanDataList
    {
        public List<RedDotCleanData> list;
    }

    public class RedDotCleanData
    {
        public List<Biz> bizList;
        public int type;
        public bool all;
    }

    public class Biz
    {
        public string id;
        public int subType;
    }

    public enum OtherClass
    {
        Oc = 1,
        NewUserFeatured = 2, //新增
        BUDSeries = 3,
        LikeList = 4,
        SpecialSkin = 5,
        UgcTheme = 6,
        Suit = 7,
        ShoppingCart = 8,
    }

    public class AIBuyResourceResult
    {
        public int curAILimitProductCnt;
    }

    public class AssetsBuyParam
    {
        public UgcBaseInfo ugcInfo;
        public int subType;
        public CurrencyType currencyType;
        public int price;
    }

    public class ShapeStoreData
    {

        public List<ShapeThemeProp> list;
    }

    public class ShapeThemeProp
    {
        public int themeId;
        public string themeName;
        public int sort;
        public string backgroundColor;
        public string backgroundIconList;
        public string resourceBackgroundColor;
        public string titleUrl;
        public int discount;
        public long startTime;
        public long endTime;
        public string version;
        public string textColorList;
        public List<int> products;

        private List<string> backgroundIconsList;
        public List<string> GetbackgroundIconList()
        {
            if (backgroundIconsList != null)
            {
                return backgroundIconsList;
            }
            backgroundIconsList = new List<string>();
            char[] delimiter = new char[] { ',' };
            string[] parts = backgroundIconList.Split(delimiter);
            foreach(var str in parts)
            {
                backgroundIconsList.Add(str);
            }
            return backgroundIconsList;
        }

        private List<string> textColorsList;

        public List<string> GetTextColorList()
        {
            if (textColorsList != null)
            {
                return textColorsList;
            }

            textColorsList = new List<string>();
            char[] delimiter = new char[] { ',' };
            string[] parts = textColorList.Split(delimiter);
            foreach (var str in parts)
            {
                textColorsList.Add(str);
            }
            return textColorsList;
        }

    }

    public class ShapeStoreBanner
    {
        public List<ShapeBannerData> list;
    }

    public class ShapeBannerData
    {
        public int id;
        public string banner_url;
        public string skip_data;
        public int sort;
        public long start_time;
        public long end_time;
        public string version;
        public string banner_cfg;

        public BannerSkipData bannerSkipData;

        public BannerSkipData GetBannerSkip()
        {
            if (bannerSkipData != null)
            {
                return bannerSkipData;
            }
            bannerSkipData = JsonConvert.DeserializeObject<BannerSkipData>(skip_data);
            return bannerSkipData;
        }
    }

    public class BannerSkipData
    {
        public int bannerType;
        public string id;
    }

}

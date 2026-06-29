using Es;
using Game.Database;
using GameData.PgcData;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using GameData.Base;
using GameData.BaseInfo;
using GameData;
using Product;

namespace Game.Store
{
    public class SendGiftUgcSceneHandler : AssetsDataHandler
    {
        protected Dictionary<int, List<UgcSectionData>> sectionsDict;
        protected Dictionary<int, UGCDataRequest> ugcDataDict;
        protected Dictionary<string, GoodsData> goodsDict;

        public override void InitData()
        {
            sectionsDict = new();
            dict = new();
            ugcDataDict = new();
            goodsDict = new();
        }

        internal virtual UGCDataRequest GetUgcDataRequest(int classType, int ugcType, int recommendId = 0)
        {
            if (ugcDataDict.ContainsKey(classType)) return ugcDataDict[classType];

            var dataRequest = new UGCDataRequest(ugcType, recommendId, classType == (int)OtherClass.LikeList);
            ugcDataDict.Add(classType, dataRequest);
            return dataRequest;
        }

        public virtual Action SearchGoodsData(int classType, string searchKey, Action<string, bool, List<GoodsData>> action)
        {
            var url = HttpUrlDefine.SearchUgc;
            var handler = new HttpPageRequestHandle<UgcAvatarPageUseData>(url, JsonConvert.SerializeObject(new JObject() { ["searchWord"] = searchKey}), HttpMethod.GET);
            List<GoodsData> goodsDatas = new() { };
            AddGoodsData(ref goodsDatas, classType, handler.GetAllData());
            handler.AddSuccessAction((handlerData) =>
            {
                AddGoodsData(ref goodsDatas, classType, new List<UgcAvatarPageUseData>() { handlerData });
                action?.Invoke(searchKey, handler.IsEnd, goodsDatas);
            });
            handler.AddFailAction((HttpResponseRawData) =>
            {
                action?.Invoke(searchKey, true, goodsDatas);
            });

            handler.Start();
            return handler.Next;
        }

        internal void AddGoodsData(ref List<GoodsData> goodsDatas, int classType, List<UgcAvatarPageUseData> handlers)
        {
            if (handlers == null) return;
            var hash = new HashSet<string>();
            foreach (var goodsData in goodsDatas) hash.Add(goodsData.Id);
            for (int i = 0, C = handlers.Count; i < C; i++)
            {
                var handler = handlers[i];
                if (handler.list == null) continue;
                AssetsDataManager.AddUgcCache(handler.list);
                for (int j = 0, C1 = handler.list.Count; j < C1; j++)
                {
                    var data = handler.list[j];
                    // 去重
                    if (data != null && !hash.Contains(data.ugcId))
                    {
                        hash.Add(data.ugcId);
                        goodsDatas.Add(CreateGoodsData(classType, data));
                    }
                }
            }
        }

        private GoodsData CreateGoodsData(int classType, RecommendItemData data)
        {
            GoodsData goodsData;
            var ugcId = data.UgcInfo.id;
            if (goodsDict.ContainsKey(ugcId)) return goodsDict[ugcId];
            goodsData = new GoodsData();
            if(data.skinInfo!=null)
            {
                goodsData.subType = data.skinInfo.subType;
            }
            goodsData.Id = data.UgcInfo.id;
            goodsData.GoodsType = GoodsType.SingleUgc;
            goodsData.ButtonType = ButtonType.Assets;
            goodsData.Name = data.UgcInfo.name;
            goodsData.GiftType = GiftType.Ugc;
            goodsData.IsGiftScene = true;
            goodsData.SourceData = new SourceData()
            {
                Source = Source.Ugc
            };

            if (data.ugcType == UgcType.Clothes)
            {
                if (data.skinInfo.subType == (int)AvatarSubType.Bundle)
                {
                    goodsData.GoodsType = GoodsType.BundleUgc;
                    goodsData.Assets = CreateUgcBundleAssetsData(classType, data);
                    goodsData.UgcBundleInfo = data;
                }
                else
                {
                    goodsData.Assets = new List<AssetsData>()
                    {
                        CreateUgcAssetsData(classType, data)
                    };
                }
            }
            else if (data.ugcType == UgcType.MusicScore)
            {
                goodsData.CantWear = true;
                goodsData.UseTips = LocalizationManager.Inst.GetLocalizedText("乐谱可以在地图中配合乐器使用");
                goodsData.Assets = new List<AssetsData>()
                {
                    CreateMusicScoreAssetsData(classType, data)
                };
            }
            else if (data.ugcType == UgcType.Anim)
            {
                goodsData.CantWear = true;
                goodsData.GoodsType = GoodsType.SingleUgc;
                goodsData.Assets = new List<AssetsData>()
                {
                    CreateUgcAnimAssetsData(classType, data)
                };
            }
            else if (data.ugcType == UgcType.Pose)
            {
                goodsData.CantWear = true;
                goodsData.GoodsType = GoodsType.SingleUgc;
                goodsData.Assets = new List<AssetsData>()
                {
                    CreateUgcPoseAssetsData(classType, data)
                };
            } else if (data.ugcType == UgcType.Prop)
            {
                goodsData.CantWear = true;
                goodsData.GoodsType = GoodsType.SingleUgc;
                goodsData.Assets = new List<AssetsData>()
                {
                    CreatePropAssetsData(classType, data)
                };
            } else if (data.ugcType == UgcType.Material)
            {
                goodsData.CantWear = true;
                goodsData.GoodsType = GoodsType.SingleUgc;
                goodsData.Assets = new List<AssetsData>()
                {
                    CreateMaterialAssetsData(classType, data)
                };
            } else if (data.ugcType == UgcType.MusicTone)
            {
                goodsData.CantWear = true;
                goodsData.GoodsType = GoodsType.SingleUgc;
                goodsData.Assets = new List<AssetsData>()
                {
                    CreateMusicToneAssetsData(classType, data)
                };
            }
            else if (data.ugcType == UgcType.AINpc)
            {
                goodsData.CantWear = true;
                goodsData.GoodsType = GoodsType.SingleUgc;
                goodsData.Assets = new List<AssetsData>()
                {
                    CreateNpcAssetsData(classType, data)
                };
            }
            else if(data.ugcType == UgcType.UgcVehicle)
            {
                goodsData.CantWear = true;
                goodsData.GoodsType = GoodsType.SingleUgc;
                goodsData.Assets = new List<AssetsData>()
                {
                   CreateUgcVehicleAssetsData(classType, data)
                };
            }

            if (data.ugcType == 0 && data.ugcInfo != null)
            {
                goodsData.Assets = new List<AssetsData>()
                {
                    CreateUgcAssetsData(classType, data)
                };
            }

            goodsData.Update();
            goodsDict.Add(ugcId, goodsData);
            return goodsData;
        }

        private List<AssetsData> CreateUgcBundleAssetsData(int classType, RecommendItemData data)
        {
            var list = new List<AssetsData>();
            var info = data.skinInfo;
            if (info.bundleItems == null) return list;
            foreach (var subInfoStr in info.bundleItems)
            {
                var subInfo = JsonConvert.DeserializeObject<SkinInfo>(subInfoStr);
                list.Add(CreateUgcAssetsData(classType, new RecommendItemData()
                {
                    ugcType = (UgcType)subInfo.subType,
                    UgcInfo = subInfo
                }));
            }
            return list;
        }

        protected override void BeforeChangeInvoke(List<AssetsData> changed)
        {
            foreach (var kv in goodsDict)
            {
                var goodsData = kv.Value;
                goodsData.Update();
            }
        }

        protected virtual UGCAssetsData CreateUgcAssetsData(int classType, RecommendItemData data)
        {
            UGCAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.UgcInfo.name;
            assetsData.ResourceType = ResourceType.UgcAvatar;
            assetsData.AvatarSubType = (AvatarSubType)data.skinInfo.subType;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.skinInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.skinInfo.paymentInfo.currencyType,
                    Value = data.skinInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }
        
        protected virtual UGCAssetsData CreateMusicToneAssetsData(int classType, RecommendItemData data)
        {
            UGCAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.UgcInfo.name;
            assetsData.ResourceType = ResourceType.UgcAvatar;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.toneInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.toneInfo.paymentInfo.currencyType,
                    Value = data.toneInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        protected virtual UGCAssetsData CreateUgcVehicleAssetsData(int classType, RecommendItemData data)
        {
            UgcVehicleAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UgcVehicleAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.UgcInfo.name;
            assetsData.ResourceType = ResourceType.UgcVehicle;
            assetsData.VehicleSubType =(VehicleSubType) data.vehicleInfo.vehicleType ;
            assetsData.UgcInfo = data;

            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.vehicleInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.vehicleInfo.paymentInfo.currencyType,                    
                    Value = data.vehicleInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            return assetsData;
        }


        protected virtual UGCAssetsData CreateNpcAssetsData(int classType, RecommendItemData data)
        {
            UGCAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.UgcInfo.name;
            assetsData.ResourceType = ResourceType.UgcAvatar;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.npc.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.npc.paymentInfo.currencyType,
                    Value = data.npc.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }
        
        protected virtual UGCAssetsData CreatePropAssetsData(int classType, RecommendItemData data)
        {
            UGCAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.UgcInfo.name;
            assetsData.ResourceType = ResourceType.UgcAvatar;
            // assetsData.AvatarSubType = (AvatarSubType)data.skinInfo.subType;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.propInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.propInfo.paymentInfo.currencyType,
                    Value = data.propInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }
        
        protected virtual UGCAssetsData CreateMaterialAssetsData(int classType, RecommendItemData data)
        {
            UGCAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UGCAssetsData)dict[ugcId];

            assetsData = new UGCAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.UgcInfo.name;
            assetsData.ResourceType = ResourceType.UgcAvatar;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.materialInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.materialInfo.paymentInfo.currencyType,
                    Value = data.materialInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        private MusicScoreAssetsData CreateMusicScoreAssetsData(int classType, RecommendItemData data)
        {
            MusicScoreAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (MusicScoreAssetsData)dict[ugcId];

            assetsData = new MusicScoreAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.musicScoreInfo.name;
            assetsData.ResourceType = ResourceType.MusicScore;
            assetsData.MusicScoreSubType = MusicScoreSubType.GeneralMusicScore;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.musicScoreInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.musicScoreInfo.paymentInfo.currencyType,
                    Value = data.musicScoreInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        private UgcAnimAssetsData CreateUgcAnimAssetsData(int classType, RecommendItemData data)
        {
            UgcAnimAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UgcAnimAssetsData)dict[ugcId];

            assetsData = new UgcAnimAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.animInfo.name;
            assetsData.ResourceType = ResourceType.UgcEmote;
            assetsData.UgcAnimSubType = (UgcAnimSubType)data.animInfo.animType;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.animInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.animInfo.paymentInfo.currencyType,
                    Value = data.animInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        private UgcPoseAssetsData CreateUgcPoseAssetsData(int classType, RecommendItemData data)
        {
            UgcPoseAssetsData assetsData;
            var ugcId = data.UgcInfo.id;
            if (dict.ContainsKey(ugcId)) return (UgcPoseAssetsData)dict[ugcId];

            assetsData = new UgcPoseAssetsData();
            assetsData.Id = ugcId;
            assetsData.Name = data.poseInfo.name;
            assetsData.ResourceType = ResourceType.UgcPose;
            assetsData.UgcPoseSubType = (UgcPoseSubType)data.poseInfo.poseType;
            assetsData.UgcInfo = data;
            assetsData.InventoryData = BagDatabase.Inst.Select(ugcId);
            if (data.poseInfo.paymentInfo != null)
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = (CurrencyType)data.poseInfo.paymentInfo.currencyType,
                    Value = data.poseInfo.paymentInfo.price
                };
            }
            else
            {
                assetsData.Value = new CurrencyData()
                {
                    CurrencyType = CurrencyType.None,
                    Value = 0
                };
            }

            dict.Add(ugcId, assetsData);
            return assetsData;
        }

        public void ClearAllCache()
        {
            ugcDataDict.Clear();
            sectionsDict.Clear();
            goodsDict.Clear();
        }
    }

    public class SendGiftUgcSectionData
    {
        public string sectionId;
        public string sectionName;
        public SendGiftSectionConfig sectionCfg;
    }

    public class SendGiftSectionConfig
    {
        public string backgroundUrl;
        public string backgroundColor;
        public string selectColor;
        public string textColor;
        public string lineColor;
        public string backgroundConfigColor;
    }

    public class SendGiftSectionListRsp
    {
        public List<SendGiftUgcSectionData> list;
    }

    public class SendGiftHttpRequestHandle<T>
    {
        protected string m_Url;
        protected HttpMethod m_ReqMethod;
        protected string m_ParamStr;

        protected Func<T, T> m_FilterFunc;
        protected Action<T> m_SuccessAction;
        protected Action<HttpResponseRawData> m_FailAction;
        protected List<int> reqQueue = new List<int>();
        protected const int REQ_FLAG = 1;
        protected const int CANCEL_FLAG = 0;

        public SendGiftHttpRequestHandle(string url, string paramStr = "{}", HttpMethod reqMethod = HttpMethod.GET)
        {
            this.m_Url = url;
            this.m_ReqMethod = reqMethod;
            this.m_ParamStr = paramStr;
        }

        public virtual void Start()
        {
            OnStart();
            Request(m_Url, m_ReqMethod, m_ParamStr);
        }

        public virtual void Start(string paramStr)
        {
            OnStart();
            this.m_ParamStr = paramStr;
            Request(m_Url, m_ReqMethod, paramStr);
        }

        protected virtual void OnStart()
        {

        }

        protected virtual int DeQueueReqStack()
        {
            DebugReqQueue();
            if (reqQueue.Count > 0)
            {
                var firstSeq = reqQueue.First();
                reqQueue.RemoveAt(0);
                return firstSeq;
            }

            return -1;
        }

        protected void DebugReqQueue()
        {
#if UNITY_EDITOR
            var debugStr = "";
            reqQueue.ForEach(x => debugStr += " " + x);
            LoggerUtils.Log($"HttpRequestHandle.DebugReqQueue {debugStr}");
#endif
        }

        protected virtual void Request(string url, HttpMethod reqType, string paramStr)
        {
            reqQueue.Add(REQ_FLAG);

            LoggerUtils.Log($"HttpRequestHandle.Req Param Url={url} ReqType={(HttpMethod)reqType} ParamStr={paramStr}");
            NetworkManager.Inst.SendHttpRequest(url, reqType, paramStr, (content) =>
            { OnRequestReceived(url, content); }, OnRequestReceivedFailed);
        }

        protected virtual void OnRequestReceived(string url, string content)
        {
            T data = JsonConvert.DeserializeObject<T>(content);

            if (m_FilterFunc != null)
                data = m_FilterFunc(data);

            if (DeQueueReqStack() == REQ_FLAG)
            {
                LoggerUtils.Log($"HttpRequestHandle.Req Success Data={content}");
                OnRequestSuccess(data);
            }
        }

        protected virtual void OnRequestReceivedFailed(string error)
        {
            HttpResponseRawData failRepData = JsonConvert.DeserializeObject<HttpResponseRawData>(error);
            DeQueueReqStack();
            LoggerUtils.LogError($"HttpRequestHandle.Req Error Error={error}");
            OnRequestFail(failRepData);
        }

        protected virtual void OnRequestSuccess(T data)
        {
            m_SuccessAction?.Invoke(data);
        }

        protected virtual void OnRequestFail(HttpResponseRawData failRepData)
        {
            m_FailAction?.Invoke(failRepData);
        }

        /// <summary>
        /// 数据过滤的处理
        /// </summary>
        public SendGiftHttpRequestHandle<T> AddDataFilter(Func<T, T> filter)
        {
            m_FilterFunc += filter;
            return this;
        }

        /// <summary>
        /// 添加成功回调
        /// </summary>
        public SendGiftHttpRequestHandle<T> AddSuccessAction(Action<T> success)
        {
            m_SuccessAction += success;
            return this;
        }

        /// <summary>
        /// 添加失败回调
        /// </summary>
        public SendGiftHttpRequestHandle<T> AddFailAction(Action<HttpResponseRawData> fail)
        {
            m_FailAction += fail;
            return this;
        }

        /// <summary>
        /// 取消请求
        /// </summary>
        public void Cancel()
        {
            for (int i = 0; i < reqQueue.Count; i++)
            {
                reqQueue[i] = CANCEL_FLAG;// 取消标志位
            }
            m_SuccessAction = null;
            m_FailAction = null;
            m_FilterFunc = null;
        }
    }

    public class SendGiftHttpPageUseData : HttpPageBaseData
    {
        public bool isFirst;
        public int isEnd;
    }

    public class SendGiftRecommendItemData
    {
        public string ugcId;
        public UgcType ugcType;
        public string ugcData;
        // 老接口字段，后续废弃
        public SkinInfo ugcInfo;
        public BaseCreator creatorInfo;
        public BaseInteractInfo interactInfo;

        [JsonIgnore]
        private UgcBaseInfo _ugcInfo;
        [JsonIgnore]
        public UgcBaseInfo UgcInfo
        {
            get
            {
                if (_ugcInfo != null) return _ugcInfo;
                if (string.IsNullOrEmpty(ugcData)) return ugcInfo;
                switch (ugcType)
                {
                    case UgcType.Map:
                        _ugcInfo = JsonConvert.DeserializeObject<MapInfo>(ugcData);
                        break;
                    case UgcType.Material:
                        _ugcInfo = JsonConvert.DeserializeObject<MaterialInfo>(ugcData);
                        break;
                    case UgcType.Clothes:
                        _ugcInfo = JsonConvert.DeserializeObject<SkinInfo>(ugcData);
                        break;
                    case UgcType.MusicScore:
                        _ugcInfo = JsonConvert.DeserializeObject<MusicScoreInfo>(ugcData);
                        break;
                    case UgcType.Prop:
                        _ugcInfo = JsonConvert.DeserializeObject<PropInfo>(ugcData);
                        break;
                    case UgcType.MusicTone:
                        _ugcInfo = JsonConvert.DeserializeObject<ToneInfo>(ugcData);
                        break;
                    case UgcType.Pose:
                        _ugcInfo = JsonConvert.DeserializeObject<PoseInfo>(ugcData);
                        break;
                    case UgcType.Anim:
                        _ugcInfo = JsonConvert.DeserializeObject<AnimInfo>(ugcData);
                        break;
                    case UgcType.AnimMusic:
                        _ugcInfo = JsonConvert.DeserializeObject<AnimMusicInfo>(ugcData);
                        break;
                    case UgcType.AINpc:
                        _ugcInfo = JsonConvert.DeserializeObject<AINpcInfo>(ugcData);
                        break;
                    case UgcType.UgcVehicle:
                        _ugcInfo = JsonConvert.DeserializeObject<VehicleInfo>(ugcData);
                        break;
                    default:
                        _ugcInfo = JsonConvert.DeserializeObject<UgcBaseInfo>(ugcData);
                        break;
                }

                return _ugcInfo;
            }

            set
            {
                _ugcInfo = value;
            }
        }

        public SkinInfo skinInfo
        {
            get
            {
                return UgcInfo as SkinInfo;
            }
        }

        public MusicScoreInfo musicScoreInfo
        {
            get
            {
                return UgcInfo as MusicScoreInfo;
            }
        }
        
        public PoseInfo poseInfo
        {
            get
            {
                return UgcInfo as PoseInfo;
            }
        }
        
        public AnimInfo animInfo
        {
            get
            {
                return UgcInfo as AnimInfo;
            }
        }
    }

    public class SendGiftUgcAvatarPageUseData : HttpPageUseData
    {
        public List<RecommendItemData> list;
    }

    /// <summary>
    /// 分页请求
    /// </summary>
    public class SendGiftHttpPageRequestHandle<T> : HttpRequestHandle<T> where T : HttpPageUseData
    {
        private string m_Cookie = "";
        private bool m_IsEnd = false;
        private bool m_IsFirst = true;
        private bool m_IsRequesting = false;
        private List<T> m_DataCenter = new List<T>();
        public bool IsEmpty => m_IsEnd && m_DataCenter.Count == 0;
        public bool IsEnd => m_IsEnd;

        public SendGiftHttpPageRequestHandle(string url, string paramStr = "{}", HttpMethod reqMethod = HttpMethod.GET) : base(url, paramStr, reqMethod)
        {

        }

        protected override void OnStart()
        {
            m_Cookie = "";
            m_IsEnd = false;
            m_IsFirst = true;
            m_IsRequesting = false;
            m_DataCenter.Clear();

            base.OnStart();
        }

        public void Reset()
        {
            m_Cookie = "";
            m_IsEnd = false;
            m_IsFirst = true;
            m_IsRequesting = false;
            m_DataCenter.Clear();
        }

        public void Next()
        {
            if (m_IsEnd) return;

            Request(m_Url, m_ReqMethod, m_ParamStr);
        }

        public List<T> GetAllData()
        {
            return m_DataCenter;
        }

        protected override void Request(string url, HttpMethod reqType, string paramStr)
        {
            if (m_IsRequesting) return;

            m_IsRequesting = true;
            if (string.IsNullOrEmpty(paramStr)) paramStr = "{}";
            var jb = JsonConvert.DeserializeObject<JObject>(paramStr);
            if (jb.ContainsKey("cookie"))
            {
                jb["cookie"] = m_Cookie;
            }
            else
            {
                jb.Add("cookie", m_Cookie);
            }
            string newParamStr = JsonConvert.SerializeObject(jb);

            base.Request(url, reqType, newParamStr);
        }

        protected override void OnRequestSuccess(T data)
        {
            data.isFirst = m_IsFirst;
            m_IsEnd = data.IsEnd == 1 || data.isEnd == 1;
            m_Cookie = data.cookie; // 分页的数据
            m_DataCenter.Add(data);
            m_IsRequesting = false;
            m_IsFirst = false;
            base.OnRequestSuccess(data);
        }

        protected override void OnRequestFail(HttpResponseRawData failRepData)
        {
            base.OnRequestFail(failRepData);
            m_IsRequesting = false;
        }
    }
}

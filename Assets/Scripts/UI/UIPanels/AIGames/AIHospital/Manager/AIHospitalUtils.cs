using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsManagers;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace AIGame.Base
{
    public class AIHospitalUtils : GlobalInstance<AIHospitalUtils>
    {
        public const string AIHospital_Offical_Bgm = ""; //AIHospital_Offical_Bgm
        public HospitalGameDataRsp HospitalGameData = null;
        private MapInfo officalMapInfo = new MapInfo() { id = "PGC" };
        private int Conversation_Count = 0;
        private int SingleEmote_Count = 0;
        private int DoubleEmote_Count = 0;
        private int LinkEmote_Count = 0;

        public AIHospitalUtils()
        {
            MessageHelper.RemoveListener<bool>(MessageName.SetOperationPanelEnable, SetOperationPanelEnable);
            MessageHelper.AddListener<bool>(MessageName.SetOperationPanelEnable, SetOperationPanelEnable);
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener<bool>(MessageName.SetOperationPanelEnable, SetOperationPanelEnable);
        }

        public MapInfo GetDefMapInfo()
        {
            var mapInfo = new MapInfo();

            mapInfo.name = "逃离废弃医院";
            mapInfo.cover = "https://u3d-business-data-1318932159.cos.ap-beijing.myqcloud.com/AIGame/AI_Hospital/UGC_MapCover/AIHospital_Def_Cover3.png";

            var gameSetting = new GameSetting();
            gameSetting.limitDuration = 900;
            gameSetting.timeLimited = 1;
            gameSetting.limitHp = 3;
            gameSetting.bgMusicUrl = "";

            mapInfo.gameSetting = gameSetting;

            var aiGameConfig = new AIGameConfig();
            aiGameConfig.winRegulatorAmount = 1;
            aiGameConfig.themeColor = "#404040";
            aiGameConfig.plot = "";
            aiGameConfig.purchasedRegulatorAmount = 0;
            aiGameConfig.purchasedFugitiveAmount = 0;
            aiGameConfig.hospitalNPCs = new List<HospitalNPCData>();

            mapInfo.gameSetting.aIGameConfig = aiGameConfig;

            mapInfo.gameType = (int)GameType.AIGame;

            return mapInfo;
        }

        public void EnterOfficalHospitalGame()
        {
            PreStartReq req = new()
            {
                gameId = (int) PGCGameType.AIHospital,
                onSuccess = (content) =>
                {
                    if(string.IsNullOrEmpty(content))
                        return;
            
                    HospitalGameData = JsonConvert.DeserializeObject<HospitalGameDataRsp>(content);
                    EnterHospitalGameByMapInfo(officalMapInfo);
                },
                onFail = (error) =>
                {
                    LoggerUtils.LogError("OnGetAIPreReqFail error = " + error);
                },
            };
            UIManager.Inst.OpenPanel<AIGameLoadingPanel>(PanelId.AIGameLoadingPanel, req);
        }

        public void EnterUgcHospitalGame(string mapId)
        {
            JObject req = new JObject()
            {
                ["id"] = mapId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
                (content) =>
                {
                    if(string.IsNullOrEmpty(content))
                        return;
                    
                    UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
                    var mapInfo = rspData.mapInfo;
                    
                    PreStartReq req = new()
                    {
                        gameId = (int) PGCGameType.AIHospital,
                        mapId = mapInfo.id,
                        onSuccess = (content) =>
                        {
                            if(string.IsNullOrEmpty(content))
                                return;
            
                            HospitalGameData = JsonConvert.DeserializeObject<HospitalGameDataRsp>(content);
                            EnterHospitalGameByMapInfo(mapInfo);
                        },
                        onFail = (error) =>
                        {
                            LoggerUtils.LogError("OnGetAIPreReqFail error = " + error);
                        },
                    };
                    UIManager.Inst.OpenPanel<AIGameLoadingPanel>(PanelId.AIGameLoadingPanel, req);
                }, (error) =>
                {
                    LoggerUtils.LogError("EnterHospitalGameByMapId Fail");
                });
        }

        public void EnterHospitalGameByMapInfo(MapInfo mapInfo)
        {
            Conversation_Count = 0;
            SingleEmote_Count = 0;
            DoubleEmote_Count = 0;
            LinkEmote_Count = 0;
            GameController.StartAIGame(mapInfo, EnterGameModel.AIHospital, true, "AI_Hospital");
            // GameController.StartAIGame(mapInfo, EnterGameModel.AIPark, true, "AI_Park");
        }
        
        //购买卡槽
        private const string AIGameRegulatorSlot = "AIGameRegulatorSlot_1_1";
        private const string AIGameFugitiveSlot = "AIGameFugitiveSlot_1_1";
        public void BuyAIHospitalSlot(string mapId, HospitalNPCType type, int productType, Action onBuySuccess, Action onBuyFail)
        {
            var req = new JObject()
            {
                ["productType"] = productType,
                ["productId"] = type == HospitalNPCType.Provost ? AIGameRegulatorSlot : AIGameFugitiveSlot,
                ["ugcId"] = mapId
            };
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.PayProductByGem,
                HttpMethod.POST,
                JsonConvert.SerializeObject(req),
                onReceive: msg =>
                {
                    AccountDataManager.Inst.BalanceInfo.Refresh();
                    onBuySuccess?.Invoke();
                }, onFail: arg0 =>
                {
                    onBuyFail?.Invoke();
                });
        }

        public void SetOperationPanelEnable(bool isEnable)
        {
            var GuestPanel = UIManager.Inst.FindPanel<AIHospitalGuestPanel>(WindowId.GuestWindow, PanelId.AIHospitalGuestPanel);
            if (GuestPanel != null)
            {
                GuestPanel.gameObject.SetActive(isEnable);
            }

            var UIOPPanel = UIManager.Inst.FindPanel<UIOperationOnWorldPanel>(WindowId.GuestWindow, PanelId.UIOperationOnWorldPanel);
            if (UIOPPanel != null)
            {
                UIOPPanel.gameObject.SetActive(isEnable);
            }
        }

        public void TrackAnalyticsData(int duration, HospitalEndType endType, HospitalTaskType taskType, bool isFirstPlay)
        {
            var curMapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            var isPgc = curMapInfo.id == "PGC" ? 1 : 0;
            var mapId = curMapInfo.id;

            Dictionary<string, object> trackData = new Dictionary<string, object>();
            trackData.Add("IsPGC", isPgc);
            trackData.Add("MapId", mapId);
            trackData.Add("Duration", duration);
            trackData.Add("EndType", (int)endType);
            trackData.Add("TaskType", (int)taskType);
            trackData.Add("Conversation_Count", Conversation_Count);
            trackData.Add("SingleEmote_Count", SingleEmote_Count);
            trackData.Add("DoubleEmote_Count", DoubleEmote_Count);
            trackData.Add("LinkEmote_Count", LinkEmote_Count);
            trackData.Add("PhotoWall",GameWithPhotoWall(curMapInfo)?1:0);
            trackData.Add("IsFirstPlay", isFirstPlay);
            AnalyticsManager.Inst.Track(AnalyticsEventName.AIHOSPITAL_DATA, trackData);
        }

        public void OnSendConversationReq()
        {
            Conversation_Count++;
        }
        
        public void OnSendDoubleEmoteReq()
        {
            DoubleEmote_Count++;
        }
        
        public void OnSendSingleEmoteReq()
        {
            SingleEmote_Count++;
        }

        public void OnSendLinkEmoteReq()
        {
            LinkEmote_Count++;
        }

        private bool GameWithPhotoWall(MapInfo curMapInfo)
        {
            bool withPhotoWall = false;
            if (curMapInfo == null || curMapInfo.gameSetting == null || curMapInfo.gameSetting.aIGameConfig==null
                ||curMapInfo.gameSetting.aIGameConfig.hospitalPhotos==null)
            {
                return withPhotoWall;
            }
            foreach (var kvp in curMapInfo.gameSetting.aIGameConfig.hospitalPhotos)
            {
                foreach (var url in kvp.urls)
                {
                    if (!string.IsNullOrEmpty(url))
                    {
                        withPhotoWall = true;
                        break;
                    }
                }
            }
            return withPhotoWall;
        }
    }

    public enum HospitalEndType
    {
        ForceExit = 0,
        TimeOut = 1,
        Death = 2,
        Success = 3,
    }

    public enum HospitalTaskType
    {
        PersuadeDoctor = 1, 
        PersuadeNurse = 2,
        PersuadeDean = 3,
        PersuadePharmacist = 4,
        AttackDustman = 5,
        ChangeDustmanClothes = 6,
        LeaveHospital = 7,
        CompleteAllMissions = 8,
        PersuadeRegulators = 9,
    }
}
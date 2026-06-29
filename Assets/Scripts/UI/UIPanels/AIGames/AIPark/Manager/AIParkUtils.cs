using System;
using System.Collections;
using System.Collections.Generic;
using Basic.Utils;
using Game.Base;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using GameData;
using GameData.BaseInfo;
using GameData.Manager;
using GameData.UGCData;
using GameUI;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pb.Game;
using UnityEngine;

namespace AIGame.Base
{
    public class AIParkUtils : GlobalInstance<AIParkUtils>
    {
        public const string AIHospital_Offical_Bgm = ""; //AIHospital_Offical_Bgm
        public ParkGameDataRsp ParkGameData = null;
        public LocationType DiscussLocationType = LocationType.Park;
        public ParkCustomData ParkCustomData = new ParkCustomData();

        private MapInfo _officalMapInfo;
        public MapInfo OfficalMapInfo
        {
            get
            {
                if (_officalMapInfo == null)
                {
                    _officalMapInfo = new MapInfo() { id = "PGC" };
                    OfficalDefault();
                }
                return _officalMapInfo;
            }
        }
        private int Conversation_Count = 0;
        private int SingleEmote_Count = 0;
        private int DoubleEmote_Count = 0;
        private int LinkEmote_Count = 0;

        long beginTimeStamp = 0;
        long endTimeStamp = 0;

        public bool canCalcBackAppTime = false;

        public void OfficalDefault()
        {
            OfficalMapInfo.gameSetting.AICommonGameConfig.DefaultConfig();
            OfficalMapInfo.gameSetting.AICommonGameConfig.npcData = OfficalNpcData();
            OfficalMapInfo.cover = OfficalCover();
            OfficalMapInfo.gameSetting.AICommonGameConfig.plot = AIParkGuidePanel.ugcContent + AIParkGuidePanel.ugcContent1;
            OfficalMapInfo.name = "游乐园捣蛋鬼之谜";
            OfficalMapInfo.gameSetting.aiGameId = (int)PGCGameType.AIPark;
            OfficalMapInfo.sectionInfo.Add(new GameData.Base.SectionItemData() { name = "解谜" });
        }
        public bool isOffical(MapInfo map)
        {
            return map.id == "PGC";
        }
        public bool isOffical(string map)
        {
            return map == "PGC";
        }
        public List<AICommonGameConfig_NPC> OfficalNpcData()
        {
            var ls = new List<AICommonGameConfig_NPC>();
            for (int i = (int)ParkNpcRoleType.Tilia; i <= (int)ParkNpcRoleType.Rowland; i++)
            {
                var item = new AICommonGameConfig_NPC();
                var config = Es.DataTables.GetPgcNpcConfig(i);
                item.id = i.ToString();
                item.plot = config.Plot;
                item.name = config.Name;
                item.desc = config.Desc;
                item.cover = config.HeadPath;
                item.npcAvatarJson = GetPgcNpcAvatarJsonByNpcId(i.ToString());
                ls.Add(item);
            }
            return ls;
        }
        public string OfficalCover()
        {
            return "https://u3d-business-data-1318932159.cos.ap-beijing.myqcloud.com/AIGame/AI_AmusementPark/UGC_MapCover/AIAmusementPark_Def_Cover.jpeg";
        }

        public AIParkUtils()
        {
            MessageHelper.AddListener<bool>(MessageName.SetOperationPanelEnable, SetOperationPanelEnable);
            MessageHelper.AddListener<History>(MessageName.ParkTriggerNextHistory, ParkTriggerNextHistory);
            MessageHelper.AddListener<string>(MessageName.ParkNpcTalkOver, OnParkNpcTalkOver);
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener<bool>(MessageName.SetOperationPanelEnable, SetOperationPanelEnable);
            MessageHelper.RemoveListener<History>(MessageName.ParkTriggerNextHistory, ParkTriggerNextHistory);
            MessageHelper.RemoveListener<string>(MessageName.ParkNpcTalkOver, OnParkNpcTalkOver);
        }

        public MapInfo GetDefMapInfo()
        {
            var mapInfo = new MapInfo();

            mapInfo.name = "";
            mapInfo.cover = "https://u3d-business-data-1318932159.cos.ap-beijing.myqcloud.com/AIGame/AI_AmusementPark/UGC_MapCover/AIAmusementPark_Def_Cover.jpeg";

            var gameSetting = new GameSetting();
            gameSetting.limitDuration = 900;
            gameSetting.timeLimited = 1;
            gameSetting.limitHp = 3;
            gameSetting.bgMusicUrl = "";
            gameSetting.aiGameId = (int)PGCGameType.AIPark;

            mapInfo.gameSetting = gameSetting;

            mapInfo.gameType = (int)GameType.AIGame;

            gameSetting.bgMusicUrl = "";
            gameSetting.bgName = "Bgm_S11Para_MainScene";

            return mapInfo;
        }

        public void EnterOfficalParkGame()
        {
            AIParkUtils.Inst.PreloadParkRes(null);
            PreStartReq req = new()
            {
                gameId = (int)PGCGameType.AIPark,
                onSuccess = (content) =>
                {
                    if (string.IsNullOrEmpty(content))
                        return;

                    // AIParkTestTool.Inst.ReadAIParkGameData();
                    // ParkGameData = AIParkTestTool.Inst.parkGameDataRsp;
                    // ParkCustomData = new();
                    // ParkCustomData.SceneIndex = 0;
                    // AIParkTestTool.Inst.EnterParkGame2();
                    // var ParkGameData2 = JsonConvert.DeserializeObject<ParkGameDataRsp>(content);
                    // ParkGameData.sessionId = ParkGameData2.sessionId;


                    ParkCustomData = new();
                    ParkCustomData.SceneIndex = 1;
                    ParkGameData = JsonConvert.DeserializeObject<ParkGameDataRsp>(content);
                    ParkGameData.ParsePbData(content);
                    LoggerUtils.Log("乐园perstart数据:" + JsonConvert.SerializeObject(ParkGameData));
                    EnterParkGameByMapInfo(OfficalMapInfo);
                },
                onFail = (error) =>
                {
                    // AIParkTestTool.Inst.ReadAIParkGameData();
                    // ParkGameData = AIParkTestTool.Inst.parkGameDataRsp;
                    // ParkCustomData = new();
                    // ParkCustomData.SceneIndex = 0;
                    // AIParkTestTool.Inst.EnterParkGame2();
                    LoggerUtils.LogError("OnGetAIPreReqFail error = " + error);
                },
            };
            UIManager.Inst.OpenPanel<AIGameLoadingPanel>(PanelId.AIGameLoadingPanel, req, new UgcInfoRsp() { mapInfo = OfficalMapInfo });
        }

        public MapInfo GetOfficalMapInfo()
        {
            return OfficalMapInfo;
        }

        public void EnterUgcParkGame(string mapId)
        {
            JObject req = new JObject()
            {
                ["id"] = mapId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
                (content) =>
                {
                    if (string.IsNullOrEmpty(content))
                        return;

                    UgcInfoRsp rspData = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
                    var mapInfo = rspData.mapInfo;
                    AIParkUtils.Inst.PreloadParkRes(mapInfo);

                    PreStartReq req = new()
                    {
                        gameId = (int)PGCGameType.AIPark,
                        mapId = mapInfo.id,
                        onSuccess = (content) =>
                        {
                            if (string.IsNullOrEmpty(content))
                                return;

                            //ParkGameData = JsonConvert.DeserializeObject<ParkGameDataRsp>(content);
                            //EnterParkGameByMapInfo(mapInfo);
                            ParkCustomData = new();
                            ParkCustomData.SceneIndex = 1;
                            ParkGameData = JsonConvert.DeserializeObject<ParkGameDataRsp>(content);
                            ParkGameData.ParsePbData(content);
                            LoggerUtils.Log("乐园perstart数据:" + JsonConvert.SerializeObject(ParkGameData));
                            EnterParkGameByMapInfo(mapInfo);
                            GameEntrySystem.Inst.SetParkRecord(mapInfo);
                        },
                        onFail = (error) =>
                        {
                            LoggerUtils.LogError("OnGetAIPreReqFail error = " + error);
                        },
                    };
                    UIManager.Inst.OpenPanel<AIGameLoadingPanel>(PanelId.AIGameLoadingPanel, req, rspData);
                }, (error) =>
                {
                    LoggerUtils.LogError("EnterHospitalGameByMapId Fail");
                });
        }

        public void EnterParkGameByMapInfo(MapInfo mapInfo)
        {
            Conversation_Count = 0;
            SingleEmote_Count = 0;
            DoubleEmote_Count = 0;
            LinkEmote_Count = 0;
            ParkGameData.aICommonGameConfig = mapInfo.gameSetting.AICommonGameConfig;
            GameController.StartAIGame(mapInfo, EnterGameModel.AIPark, true, "AI_Park", (bo) =>
            {
                UIManager.Inst.ClosePanel(PanelId.AIGameLoadingPanel);
            });
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

        void ParkTriggerNextHistory(History history)
        {

            ParkGameData.curHistory = history;
        }

        void OnParkNpcTalkOver(string speaker)
        {
            var guestPanel = UIManager.Inst.FindPanel<AIParkGuestPanel>(WindowId.GuestWindow, PanelId.AIParkGuestPanel);
            if (guestPanel != null)
            {
                // guestPanel.UpdateChatViewLayout();
            }
            AIParkTcpNetMgr.Instance.SendAIParkSyncReq_InferAction(speaker);
        }
        public void TrackAnalyticsData()
        {
            long duration = 0;
            ParkEndType parkEndType = AIParkTcpNetMgr.Instance.parkEndType;
            var aiGame = AIGameController.Inst.GetCurAIGame<AIParkGame>();
            if (parkEndType == ParkEndType.Uncomplete)
            {
                duration = GameUtils.GetTimeStamp() - AIParkUtils.Inst.beginTimeStamp - aiGame.enterBgAllTime;
            }
            else
            {
                duration = AIParkUtils.Inst.endTimeStamp - AIParkUtils.Inst.beginTimeStamp - aiGame.enterBgAllTime;
            }
            var curMapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            var gcStr = ParkGameData.isPgcEnter ? "PGC" : "UGC";
            var mapId = curMapInfo.id;

            Dictionary<string, object> trackData = new Dictionary<string, object>();
            trackData.Add("GameMode", gcStr);
            trackData.Add("MapId", mapId);
            trackData.Add("Duration", duration);
            trackData.Add("EndType", (int)parkEndType);
            trackData.Add("Conversation_Count", Conversation_Count);
            trackData.Add("SingleEmote_Count", SingleEmote_Count);
            trackData.Add("DoubleEmote_Count", DoubleEmote_Count);
            trackData.Add("IsFirstPlay", AIGameController.Inst.GetCurAIGame<AIParkGame>().Is_FirstPlay);
            trackData.Add("CurrentState", AIGameController.Inst.GetCurAIGame<AIParkGame>().CurEventStep);
            trackData.Add("IsGuide", AIGameController.Inst.GetCurAIGame<AIParkGame>().Is_CurPlayWithEventGuide);
            // trackData.Add("LinkEmote_Count", LinkEmote_Count);
            // trackData.Add("PhotoWall", GameWithPhotoWall(curMapInfo) ? 1 : 0);
            // trackData.Add("IsFirstPlay", isFirstPlay);
            AnalyticsManager.Inst.Track(AnalyticsEventName.AILARP, trackData);
        }

        public void ReportOncePerDay_GamePage(){
            // 检查是否今天已经上报过
            string key = AccountDataManager.Inst.Uid + "_" + "ParkReportGamePage";
            int lastReportDay = PlayerPrefs.GetInt(key, 0);
            int currentDay = System.DateTime.Now.DayOfYear;
            int currentYear = System.DateTime.Now.Year;
            
            // 使用年份+天数作为唯一标识，确保跨年时也能正确判断
            int todayIdentifier = currentYear * 1000 + currentDay;
            
            // 如果今天已经上报过，直接返回
            if (lastReportDay == todayIdentifier)
            {
                LoggerUtils.Log("ReportOncePerDay_GamePage: 今天已经上报过，跳过");
                return;
            }
            
            // 记录今天已上报
            PlayerPrefs.SetInt(key, todayIdentifier);
            PlayerPrefs.Save();
            
            // 执行上报逻辑
            Dictionary<string, object> trackData = new Dictionary<string, object>();
            AnalyticsManager.Inst.Track(AnalyticsEventName.GamePage, trackData);
            
            LoggerUtils.Log("ReportOncePerDay_GamePage: 上报成功，日期标识: " + todayIdentifier);
        }

        public void ReportOncePerDay_AILARPGameHall(){
            // 检查是否今天已经上报过
            string key = AccountDataManager.Inst.Uid + "_" + "ParkReportAILARPGameHall";
            int lastReportDay = PlayerPrefs.GetInt(key, 0);
            int currentDay = System.DateTime.Now.DayOfYear;
            int currentYear = System.DateTime.Now.Year;
            
            // 使用年份+天数作为唯一标识，确保跨年时也能正确判断
            int todayIdentifier = currentYear * 1000 + currentDay;
            
            // 如果今天已经上报过，直接返回
            if (lastReportDay == todayIdentifier)
            {
                LoggerUtils.Log("ReportOncePerDay_AILARPGameHall: 今天已经上报过，跳过");
                return;
            }
            
            // 记录今天已上报
            PlayerPrefs.SetInt(key, todayIdentifier);
            PlayerPrefs.Save();
            
            // 执行上报逻辑
            Dictionary<string, object> trackData = new Dictionary<string, object>();
            AnalyticsManager.Inst.Track(AnalyticsEventName.AILARPGameHall, trackData);
            
            LoggerUtils.Log("ReportOncePerDay_AILARPGameHall: 上报成功，日期标识: " + todayIdentifier);
        }

        public ParkEndType GetCompleteEndType()
        {
            ParkEndType parkEndType;
            var ending = AIParkTcpNetMgr.Instance.curSelectEvent;
            if (AIGameController.Inst.GetCurAIGame<AIParkGame>().isPgcEnter)
            {
                if(ending == "罗兰"){
                    parkEndType = ParkEndType.SpecailSummary;
                }else{
                    parkEndType = ParkEndType.NormalSummary;
                }
            }
            else
            {
                parkEndType = ParkEndType.NormalSummary;
                var config = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.AICommonGameConfig.endings;
                if (config != null)
                {
                    foreach (var item in config)
                    {
                        if (item.type == 1 && item.triggerEvent == ending)
                        {
                            parkEndType = ParkEndType.SpecailSummary;
                            break;
                        }
                    }
                }
            }
            return parkEndType;
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
            if (curMapInfo == null || curMapInfo.gameSetting == null || curMapInfo.gameSetting.aIGameConfig == null
                || curMapInfo.gameSetting.aIGameConfig.hospitalPhotos == null)
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

        public bool IsEnterGameSummary()
        {
            return !string.IsNullOrEmpty(ParkGameData.Summary);
        }
        public string GetPgcNpcAvatarJsonByNpcId(string id)
        {
            ParkNpcRoleType parkNpcRoleType = (ParkNpcRoleType)int.Parse(id);
            switch (parkNpcRoleType)
            {
                case ParkNpcRoleType.Tilia:
                    return "{\"partDatas\":[{\"Id\":\"12200000\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#FCE2E2\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11100017\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.0908,\"y\":0.2172,\"z\":0},\"Rot\":{\"x\":-166,\"y\":-90,\"z\":0},\"Sca\":{\"x\":0.8642,\"y\":0.8642,\"z\":0.8642},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12300004\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10900487\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.24,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10400487\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10100112\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10300042\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#000000\",\"Pos\":{\"x\":-0.24,\"y\":0.21,\"z\":0.1063},\"Rot\":{\"x\":0,\"y\":-8.8809,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10600125\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.1341,\"y\":0.223,\"z\":0.103},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1.3472,\"y\":1.3472,\"z\":1.3472},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10800118\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#DCDCDC\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11200043\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11300352\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11700034\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0}]}";
                case ParkNpcRoleType.Elise:
                    return "{\"partDatas\":[{\"Id\":\"12200000\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#FFDDDC\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12300001\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10900488\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.24,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1.1565,\"y\":1.1565,\"z\":1.1565},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11300335\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10400488\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10800077\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#EC9A3D\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10600065\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.16,\"y\":0.226,\"z\":0.1},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11100013\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.0452,\"y\":0.2375,\"z\":0},\"Rot\":{\"x\":-166,\"y\":90,\"z\":0},\"Sca\":{\"x\":0.8755,\"y\":0.8755,\"z\":0.8755},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10300028\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#000000\",\"Pos\":{\"x\":-0.24,\"y\":0.21,\"z\":0.09},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11000259\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":1,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11900002\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#FFA19E\",\"Pos\":{\"x\":-0.1,\"y\":0.2021,\"z\":0.13},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1.882,\"y\":1.882,\"z\":1.882},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0}]}";
                case ParkNpcRoleType.Casper:
                    return "{\"partDatas\":[{\"Id\":\"12200000\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#FFCBA9\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12300005\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11300353\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10300016\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#653939\",\"Pos\":{\"x\":-0.2121,\"y\":0.21,\"z\":0.1015},\"Rot\":{\"x\":0,\"y\":-2.4986,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10800002\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#653B3B\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10600110\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.1501,\"y\":0.223,\"z\":0.1},\"Rot\":{\"x\":0,\"y\":-9.9938,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11100008\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.0611,\"y\":0.2375,\"z\":0},\"Rot\":{\"x\":-166,\"y\":90,\"z\":0},\"Sca\":{\"x\":0.5736,\"y\":0.5736,\"z\":0.5736},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11800001\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.1154,\"y\":0.22,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0.9745,\"y\":1,\"z\":1.678},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10900489\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.24,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10400489\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11900002\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#FFA19E\",\"Pos\":{\"x\":-0.1,\"y\":0.2021,\"z\":0.13},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1.5061,\"y\":1.5061,\"z\":1.5061},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0}]}";
                case ParkNpcRoleType.Pio:
                    return "{\"partDatas\":[{\"Id\":\"12200000\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#BC774F\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10800060\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#503D3D\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10400491\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10900491\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.2299,\"y\":0.0168,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0.912,\"y\":0.912,\"z\":0.912},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12100141\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.16,\"y\":0.22,\"z\":0},\"Rot\":{\"x\":0,\"y\":-90,\"z\":180},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0}]}";
                case ParkNpcRoleType.Teddy:
                    return "{\"partDatas\":[{\"Id\":\"12200000\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#FFFFFF\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11100014\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.05,\"y\":0.2375,\"z\":0},\"Rot\":{\"x\":-166,\"y\":90,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12300001\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10900490\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.24,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10400490\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0}]}";
                case ParkNpcRoleType.Vivien:
                    return "{\"partDatas\":[{\"Id\":\"12200000\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#E6E7EF\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12300004\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10900104\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.24,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1.2726,\"y\":1.2726,\"z\":1.2726},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11300309\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10400421\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10100012\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10800110\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#606680\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10600126\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.152,\"y\":0.223,\"z\":0.1039},\"Rot\":{\"x\":0,\"y\":-1.6147,\"z\":0},\"Sca\":{\"x\":1.0835,\"y\":1.0835,\"z\":1.0835},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11100017\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.0903,\"y\":0.2375,\"z\":0},\"Rot\":{\"x\":-166,\"y\":270,\"z\":0},\"Sca\":{\"x\":0.7747,\"y\":0.7747,\"z\":0.7747},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10300042\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#6A6D82\",\"Pos\":{\"x\":-0.2154,\"y\":0.21,\"z\":0.1038},\"Rot\":{\"x\":0,\"y\":-12.9176,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11200005\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":-0.0519,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11400061\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.06,\"y\":-0.09,\"z\":0},\"Rot\":{\"x\":0,\"y\":-90,\"z\":-180},\"Sca\":{\"x\":1.9115,\"y\":1.9115,\"z\":1.9115},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11900002\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#820074\",\"Pos\":{\"x\":-0.1196,\"y\":0.2247,\"z\":0.1006},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":2.1567,\"y\":2.1567,\"z\":2.1567},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12000028\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11000260\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":1,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0}]}";
                case ParkNpcRoleType.Rowland:
                    return "{\"partDatas\":[{\"Id\":\"12200000\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#FFFFFF\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"12300001\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11300026\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10400107\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10800054\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#464F5D\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":0,\"y\":0,\"z\":0},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10600101\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.1589,\"y\":0.223,\"z\":0.1},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1.0482,\"y\":1.0482,\"z\":1.0482},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11100019\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":-0.05,\"y\":0.2375,\"z\":0},\"Rot\":{\"x\":-166,\"y\":90,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"10300013\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#414B5D\",\"Pos\":{\"x\":-0.2262,\"y\":0.21,\"z\":0.1029},\"Rot\":{\"x\":0,\"y\":-22.0697,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11900002\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"#8F698B\",\"Pos\":{\"x\":-0.1001,\"y\":0.1951,\"z\":0.1173},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":2.0184,\"y\":2.0184,\"z\":2.0184},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0},{\"Id\":\"11200042\",\"Uid\":\"\",\"Url\":\"\",\"LRType\":0,\"Cr\":\"\",\"Pos\":{\"x\":0,\"y\":0,\"z\":0},\"Rot\":{\"x\":0,\"y\":0,\"z\":0},\"Sca\":{\"x\":1,\"y\":1,\"z\":1},\"CSca\":{\"x\":0,\"y\":0,\"z\":0},\"CAnchor\":{\"x\":0,\"y\":0,\"z\":0},\"UgcStyle\":0,\"BodyId\":0}]}";
            }
            return "";
        }

        public string CreateAvatarJsonByParkNpcType(string id)
        {
            if (ParkGameData.isPgcEnter)
            {
                return GetPgcNpcAvatarJsonByNpcId(id);
            }
            else
            {
                var npcData = ParkGameData.aICommonGameConfig.npcData;
                for (int i = 0; i < npcData.Count; i++)
                {
                    if (npcData[i].id == id)
                    {
                        return npcData[i].npcAvatarJson;
                    }
                }
            }
            Debug.LogError("缺失avatarJson数据:" + id);
            return "";
        }
        /// <summary>
        /// 暂时先这样 预下载乐园 需要改
        /// </summary> <summary>
        /// 
        /// </summary>
        public void PreloadParkRes(MapInfo mapInfo)
        {

            if (mapInfo == null)
            {
                //pgc
            }
            else
            {
                //ugc
            }

        }

        public void RecordBeginTime()
        {
            beginTimeStamp = GameUtils.GetTimeStamp();
        }

        public void RecordEndTime()
        {
            endTimeStamp = GameUtils.GetTimeStamp();
        }

        public enum ParkEndType
        {
            Uncomplete = 1,
            NormalSummary = 2,
            SpecailSummary = 3,
        }
    }
}
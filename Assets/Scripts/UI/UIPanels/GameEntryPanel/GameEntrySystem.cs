using AIGame.Base;
using Basic.Utils;
using Game.CommunityGame;
using GameData;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public enum MapListResponseType 
    {
        None = 0,
        MapPublished = 1, //已发布
        MapDrafts,     //草稿箱
    }
    public class GameEntrySystem : GlobalInstance<GameEntrySystem>
    {
        public GameEntrySystemData data = new GameEntrySystemData();

        #region 本地存储

        //玩家选择游戏记录
        public int GetSaveEntry() 
        {
            return SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.GameEntry);
        }
        public void SetSaveEntry(int val)
        {
            SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.GameEntry, val);
        }

        //玩家乐园游玩记录10个剧本
        public List<MapInfo> GetParkRecord() {
            var info = SaveGameUtil.Inst.GetStrByPlayerPrefs(SaveGameUtil.GameParkRecord);
            var ls = JsonConvert.DeserializeObject<List<GameMapSave>>(info);
            var tem = new List<MapInfo>();
            if (ls != null)
            {
                for (int i = 0; i < ls.Count; i++)
                {
                    var map = new MapInfo();
                    map.id = ls[i].id;
                    map.name = ls[i].name;
                    map.cover = ls[i].cover;
                    map.gameSetting.AICommonGameConfig.plot = ls[i].plot;
                    map.gameSetting.AICommonGameConfig.npcData = ls[i].npc;

                    tem.Add(map);
                }
            }
            tem.Reverse();
            data.MapInfos = tem;
            return tem;
        }

        public void SetParkRecord(MapInfo mapInfo)
        {
            var max = 10;

            var saveinfo = new GameMapSave();
            saveinfo.id = mapInfo.id;
            saveinfo.name = mapInfo.name;
            saveinfo.cover = mapInfo.cover;
            saveinfo.plot = mapInfo.gameSetting.AICommonGameConfig.plot;
            saveinfo.npc = mapInfo.gameSetting.AICommonGameConfig.npcData;

            var info = SaveGameUtil.Inst.GetStrByPlayerPrefs(SaveGameUtil.GameParkRecord);

            var ls = JsonConvert.DeserializeObject<List<GameMapSave>>(info);
            if (ls == null)
            {
                ls = new List<GameMapSave>();
            }
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].id == saveinfo.id)
                {
                    ls.RemoveAt(i);
                    break;
                }
            }
            if (ls.Count >= max)
            {
                ls.RemoveAt(0);
            }
            ls.Add(saveinfo);

            SaveGameUtil.Inst.SetStrByPlayerPrefs(SaveGameUtil.GameParkRecord, JsonConvert.SerializeObject(ls));
        }

        //红点
        public void SetParkRed(int bo)
        {
            SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveGameUtil.GameParkRed, bo);
            ReddotManagerUtils.Inst.RefreshRed();
        }
        public bool GetParkRed()
        {
            return SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveGameUtil.GameParkRed) == 0;
        }
        #endregion

        #region UI 

        public void OpenMainPanel() {
            UIManager.Inst.OpenPanel(PanelId.CommunityGamesPanel, WindowId.RecommendWindow);
         //   UIManager.Inst.OpenPanel(PanelId.GameEntryMainPanel, WindowId.RecommendWindow);
        }


        public void OpenParkUgcPanel()
        {
            UIManager.Inst.OpenPanel(PanelId.AIParkUgcEditPanel, WindowId.RecommendWindow, EditType.Create);
        }

        public void OpenParkSelectPanel(UgcBaseInfo mapInfo = null)
        {
            data.ResetCollect();
            SelectedReq();
            GetParkRecord();
            UIManager.Inst.OpenPanel(PanelId.GEParkSelectPanel, WindowId.RecommendWindow, mapInfo);
        }
        public void CloseParkSelectPanel()
        {
            UIManager.Inst.ClosePanel(WindowId.RecommendWindow,PanelId.GEParkSelectPanel);
        }
        public void OpenParkDetailPanel(string id,bool del = false,bool edit = false)
        {
            MapInfoReq(id, (UgcInfoRsp) => {
                UIManager.Inst.OpenPanel(PanelId.GEParkDetailPanel, WindowId.RecommendWindow, UgcInfoRsp, del,edit);
            });
        }

        public void PreloadLastPlayMapInfo()
        {
            string key = AccountDataManager.Inst.Uid + "_" + "LastPlayMapId";
            var lastPlayMapId = PlayerPrefs.GetString(key);
            if (string.IsNullOrEmpty(lastPlayMapId) || AIParkUtils.Inst.isOffical(lastPlayMapId))
            {
                return;
            }
            MapInfoReq(lastPlayMapId, (UgcInfoRsp) => {
                SetLastPlayMapInfo(UgcInfoRsp.mapInfo);
            });
        }

        public void SetLastPlayMapInfo(UgcBaseInfo mapInfo)
        {
            string key = AccountDataManager.Inst.Uid + "_" + "LastPlayMapId";
            PlayerPrefs.SetString(key, mapInfo.id);
            PlayerPrefs.Save();
            data.LastPlayMapInfo = mapInfo;
        }


        public void OpenParkGroupPanel()
        {
            UIManager.Inst.OpenPanel(PanelId.GEParkGroupPanel, WindowId.RecommendWindow);
        }
        public void CloseParkGroupPanel()
        {
            UIManager.Inst.ClosePanel(WindowId.RecommendWindow, PanelId.GEParkGroupPanel);
        }

        public void OpenParkWorkPanel()
        {
            UIManager.Inst.OpenPanel(PanelId.GEParkWorkPanel, WindowId.RecommendWindow);
        }
        public void CloseParkWorkPanel()
        {
            UIManager.Inst.ClosePanel(WindowId.RecommendWindow,PanelId.GEParkWorkPanel);
        }
        #endregion

        #region Web

        //收藏请求
        public void CollectReq(Action<List<ResInfo>> ac)
        {
            if (data.CollectResInfoList != null && data.CollectResInfoList.isEnd == 1)
            {
                return;
            }
            // 调用后端接口
            var jb = new JObject
            {
                ["gameId"] = (int)PGCGameType.AIPark,
                ["interactType"] = (int)UGCInteractType.CollectMap,
                ["cookie"] = data.CollectResInfoList?.cookie
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                onReceive: arg0 =>
                {
                    data.CollectResInfoList = JsonConvert.DeserializeObject<ResInfoList>(arg0);

                    if (data.CollectResInfoList !=null && data.CollectResInfoList.list != null)
                    {
                        data.CollectMapInfos.AddRange(data.CollectResInfoList.list);
                        ac?.Invoke(data.CollectResInfoList.list);
                    }
                    else
                    {
                        ac?.Invoke(new List<ResInfo>());
                    }
                }, 
                onFail: arg0 =>
                {
                    LoggerUtils.LogError("获取收藏列表失败 : " + arg0);
                });
        }

        //精选请求
        public void SelectedReq(Action ac = null)
        {
            JObject req = new JObject
            {
                ["gameType"] = (int)GameType.AIGame,
                ["gameId"] = (int)PGCGameType.AIPark
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.gameSpotlight, HttpMethod.GET, JsonConvert.SerializeObject(req),
                onReceive: content => {
                    data.SelectedRspInfo = JsonConvert.DeserializeObject<OfficalRecommendRsp>(content);
                    if (data.SelectedRspInfo != null && data.SelectedRspInfo.sections != null)
                    {
                        data.SelectedMapInfos.Clear();
                        foreach (var item in data.SelectedRspInfo.sections)
                        {
                            data.SelectedMapInfos.AddRange(item.ugcList);
                        }
                    }
                    ac?.Invoke();
                },
                onFail: error => {
                    LoggerUtils.LogError("CommunityGameSpotlightPanel OnGetDataFail " + error);
                });
        }

        //地图详情
        public void MapInfoReq(string mapId,Action<UgcInfoRsp> ac) {

            JObject req = new JObject()
            {
                ["id"] = mapId,
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.mapInfo, HttpMethod.GET, JsonConvert.SerializeObject(req),
                onReceive:content => {
                    var  UgcInfoRsp = JsonConvert.DeserializeObject<UgcInfoRsp>(content);
                    ac?.Invoke(UgcInfoRsp);
                },
                onFail: error => {
                    LoggerUtils.LogError("MapInfoReq 地图数据请求失败 ：" + error);
                });
        }

        //地图搜索
        public void MapSearchReq(string searchStr,Action<List<RecommendItemData>> ac)
        {
            if (string.IsNullOrEmpty(searchStr))
            {
                data.SearchStr = searchStr;
                data.SearchInfoRsp = null;
                data.SearchMapInfos.Clear();
                return;
            }
            if (searchStr == data.SearchStr && data.SearchInfoRsp != null && data.SearchInfoRsp.IsEnd == 1)
            {
                return;
            }

            if (searchStr != data.SearchStr)
            {
                data.SearchStr = searchStr;
                data.SearchInfoRsp = null;
                data.SearchMapInfos.Clear();
            }

            JObject jb = new JObject
            {
                ["cookie"] = data.SearchInfoRsp?.cookie,
                ["searchWord"] = data.SearchStr,
                ["gameType"] = (int)GameType.AIGame,
                ["gameId"] = (int)PGCGameType.AIPark
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SearchMap, HttpMethod.GET, reqParam, 
                onReceive:content => {

                    data.SearchInfoRsp = JsonConvert.DeserializeObject<SectionInfoRsp>(content);

                    if (data.SearchInfoRsp != null && data.SearchInfoRsp.list != null)
                    {
                        data.SearchMapInfos.AddRange(data.SearchInfoRsp.list);
                        ac?.Invoke(data.SearchInfoRsp.list);
                    }
                    else
                    {
                        ac?.Invoke(new List<RecommendItemData>());
                    }
                },
                onFail:error =>{ LoggerUtils.LogError("MapSearchReq" + error); });
        }

        //地图标签列表
        public void MapSectionListReq(Action<SectionListRsp> action)
        {
            JObject req = new JObject()
            {
                ["ugcType"] = (int)UgcType.Map,
                ["gameId"] = (int)PGCGameType.AIPark
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionList, HttpMethod.GET, JsonConvert.SerializeObject(req),
                onReceive: content => {

                    LoggerUtils.Log("MapSectionReq content = ", content);
                    data.SectionListRsp = JsonConvert.DeserializeObject<SectionListRsp>(content);

                    if (data.SectionListRsp != null && data.SectionListRsp.list !=null && data.SectionListRsp.list.Count > 0)
                    {
                        action?.Invoke(data.SectionListRsp);
                    }
                }, 
                onFail: error => { LoggerUtils.LogError("MapSectionReq error = " + error); });
        }

        //地图标签信息
        public void MapSectionInfoReq(string sectionId,Action<List<RecommendItemData>> resultAction)
        {
            if (string.IsNullOrEmpty(sectionId))
            {
                data.SectionId = sectionId;
                data.SectionInfoRsp = null;
                data.SectionMapInfos.Clear();
                return;
            }
            if (sectionId == data.SectionId && data.SectionInfoRsp != null && data.SectionInfoRsp.IsEnd == 1)
            {
                return;
            }

            if (data.SectionId != sectionId)
            {
                data.SectionId = sectionId;
                data.SectionInfoRsp = null;
                data.SectionMapInfos.Clear();
            }

            JObject jb = new JObject
            {
                ["gameId"] = (int)PGCGameType.AIPark,
                ["cookie"] = data.SectionInfoRsp?.cookie,
                ["sectionId"] = data.SectionId,
                ["currencyType"] = (int)CurrencyType.None
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            //var headUrl = SectionType == SectionType.V1 ? HttpUrlDefine.sectionInfo : HttpUrlDefine.sectionInfoV2;
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.sectionInfo, HttpMethod.GET, reqParam, 
            onReceive:content => {
                data.SectionInfoRsp = JsonConvert.DeserializeObject<SectionInfoRsp>(content);
            
                if (data.SectionInfoRsp == null)
                {
                    return;
                }
                if (data.SectionInfoRsp.list != null)
                {
                    data.SectionMapInfos.AddRange(data.SectionInfoRsp.list);
                }
                resultAction?.Invoke(data.SectionInfoRsp.list);
            },
            onFail: error => { LoggerUtils.LogError("MapSectionInfoReq error = " + error); });
        }

        //草稿箱或已发布
        public void GetPublishedReq(MapListResponseType mapListType,Action<List<DraftListItem>> resultAction)
        {
            if (mapListType == MapListResponseType.None)
            {
                data.MapListType = mapListType;
                data.MapListResponse = null;
                data.MapListInfos.Clear();
                return;
            }

            if (mapListType == data.MapListType && data.MapListResponse != null && data.MapListResponse.isEnd == 1)
            {
                return;
            }

            if (data.MapListType != mapListType)
            {
                data.MapListType = mapListType;
                data.MapListResponse = null;
                data.MapListInfos.Clear();
            }

            var req = new MapListReq
            {
                gameId = (int)PGCGameType.AIPark,
                gameType = (int)GameType.AIGame,
                cookie = data.MapListResponse?.cookie,
                uid = AccountDataManager.Inst.Uid,
            };
            string reqHead = "";
            switch (data.MapListType)
            {
                case MapListResponseType.MapPublished:
                    reqHead = HttpUrlDefine.publishList;
                    break;
                case MapListResponseType.MapDrafts:
                    reqHead = HttpUrlDefine.createList;
                    break;
            }
            NetworkManager.Inst.SendHttpRequest(reqHead, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                data.MapListResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
                if (data.MapListResponse.list == null)
                {
                    data.MapListResponse.list = new List<DraftListItem>();
                }

                data.MapListInfos.AddRange(data.MapListResponse.list);
                resultAction?.Invoke(data.MapListResponse.list);
            },
                (error) =>
                {
                    resultAction?.Invoke(new List<DraftListItem>());
                });
        }

        #endregion
    }


}
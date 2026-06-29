using System.Collections.Generic;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Com.TheFallenGames.OSA.Core;
using UnityEngine.UI;

namespace UI.TopList
{   
    public enum MapTopRankType
    {
        cont = 0,
        time = 1,
        fire = 2
    }

    public enum EMapRankScope
    {
        total = 0,
        Daily = 1,
        Weekly = 2,
        Monthly = 3,
    }

    public enum EMapRankCountLimit
    {
        /// <summary>
        /// 默认100个
        /// </summary>
        Default = 0,
        /// <summary>
        /// 全量数据选项
        /// </summary>
        UnLimited = 1,
    }

    [Serializable]
    public class TopListData
    {
        public string cookie;
        public int isEnd;
        public List<RankItem> rankList;
        public string displayText;
        public UserRankInfo userRank;
    }

    [Serializable]
    public class RankItem
    {
        public int rank;
        public int score;
        public GameData.BaseInfo.MapInfo mapInfo;
        public AccountUserInfo userInfo;
        public List<HeatContribution> heatContribution;
    }

    [Serializable]
    public class HeatContribution
    {
        public int rank;
        public int score;
        public AccountUserInfo userInfo;
    }

    [Serializable]
    public class UserRankInfo
    {
        public AccountUserInfo userInfo;
        public int rank;
        public int score;
    }




    public class TopListDataLoder:MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";
        protected int _rankType = 2;
        public static int _scope = 1;
        protected bool _isInit = false;

        


        public void SetLeaderboardType(int rankType)
        {
            _isEnd = false;
            _cookie = "";

            this._rankType = rankType;
        }

        public virtual void GetLeaderboardData(UnityAction<List<RankItem>> resultAction = null)
        {
            if (_isEnd)
            {
                resultAction?.Invoke(new List<RankItem>());
                return;
            }
            


            JObject jb = new JObject
            {
                ["gameId"] = (int)PGCGameType.AIHospital,
                ["cookie"] = _cookie,
                ["rankType"] = _rankType,
                ["scope"] = _scope
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest("/aigame/rankList",
                HttpMethod.GET,
                reqParam, 
                (content) =>
                {
                    if (!string.IsNullOrEmpty(content))
                    {
                        GetLeaderboardDataSuccess(content, resultAction);
                    }
                },
                GetLeaderboardDataFail);
        }

        protected void GetLeaderboardDataSuccess(string content, UnityAction<List<RankItem>> resultAction = null)
        {
            var leaderboardDataRsp = JsonConvert.DeserializeObject<TopListData>(content);

            MapTopListPanel.topListData = leaderboardDataRsp;

            this._isEnd = leaderboardDataRsp.isEnd == 1;
            this._cookie = leaderboardDataRsp.cookie;

            if (leaderboardDataRsp == null || leaderboardDataRsp.rankList == null || leaderboardDataRsp.rankList.Count == 0)
            {
                resultAction?.Invoke(new List<RankItem>());
            }
            else
            {
                resultAction?.Invoke(leaderboardDataRsp.rankList);
            }
        }

        protected void GetLeaderboardDataFail(string error)
        {
            LoggerUtils.LogError("GetLeaderboardDataFail" + error);
        }

    }


}


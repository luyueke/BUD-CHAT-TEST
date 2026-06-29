using System.Collections.Generic;
using Game.Store;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

public class LeaderboardDataLoader : MonoBehaviour
{
    protected bool _isEnd = false;
    protected string _cookie = "";
    protected int _rankType;

    public void SetLeaderboardType(int rankType)
    {
        _isEnd = false;
        _cookie = "";
        
        this._rankType = rankType;
    }
    
    public virtual void GetLeaderboardData(UnityAction<List<LeaderboardItemData>> resultAction = null, UnityAction<LeaderboardItemData> selfDataAct = null)
    {
        if (_isEnd)
        {
            resultAction?.Invoke(new List<LeaderboardItemData>());
            return;
        }

        JObject jb = new JObject
        {
            ["gameId"] = (int)PGCGameType.AIYandere,
            ["cookie"] = _cookie,
            ["rankType"] = _rankType,
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIGameRankList, HttpMethod.GET, reqParam,
            (content) =>
            {
                if (!string.IsNullOrEmpty(content))
                {
                    GetLeaderboardDataSuccess(content, resultAction, selfDataAct);
                }
            }, 
            GetLeaderboardDataFail);
    }

    protected void GetLeaderboardDataSuccess(string content, UnityAction<List<LeaderboardItemData>> resultAction = null, UnityAction<LeaderboardItemData> selfDataAct = null)
    {
        var leaderboardDataRsp = JsonConvert.DeserializeObject<LeaderboardDataRsp>(content);
        this._isEnd = leaderboardDataRsp.IsEnd == 1;
        this._cookie = leaderboardDataRsp.cookie;
        
        if (leaderboardDataRsp == null || leaderboardDataRsp.rankList == null || leaderboardDataRsp.rankList.Count == 0)
        {
            resultAction?.Invoke(new List<LeaderboardItemData>());
        }
        else
        {
            resultAction?.Invoke(leaderboardDataRsp.rankList);
        }

        selfDataAct?.Invoke(leaderboardDataRsp.userRank);
    }

    protected void GetLeaderboardDataFail(string error)
    {
        LoggerUtils.LogError("GetLeaderboardDataFail" + error);
    }
}

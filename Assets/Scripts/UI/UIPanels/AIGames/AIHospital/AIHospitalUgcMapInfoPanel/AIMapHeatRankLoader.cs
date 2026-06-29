using Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UI.TopList;
using UnityEngine;
using UnityEngine.Events;
using Network.Http;

public class AIMapHeatRankLoader : MonoBehaviour
{
    protected bool _isEnd = false;
    protected string _cookie = "";
    protected int _rankType = 3;
    public static int _scope = 1;
    protected bool _isInit = false;
    private string _mapId = string.Empty;



    public void SetMapID(string mapID)
    {
        _isEnd = false;
        _cookie = "";

        _mapId = mapID;
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
            ["mapId"] = _mapId
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIGameRankList,
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

using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using UI.TopList;
using UnityEngine;
using UnityEngine.Events;

public class AIHospitalPopularDataLoader : MonoBehaviour
{
    private bool _isEnd = false;
    private string _cookie = "";
    private string _sectionId = "";

    public void GetSectionInfoData(UnityAction<List<RankItem>> resultAction = null)  // 改为RankItem
    {
        if (_isEnd)
        {
            resultAction?.Invoke(new List<RankItem>());
            return;
        }

        JObject jb = new JObject
        {
            ["gameId"] = (int)PGCGameType.AIHospital,
            ["rankType"] = (int)MapTopRankType.fire,
            ["scope"] = (int)EMapRankScope.Daily,
            ["unlimited"] = (int)EMapRankCountLimit.UnLimited,
            ["cookie"] = _cookie,
        };

        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.AIGameRankList,  // 假设改用排行榜接口
            HttpMethod.GET,
            reqParam,
            (content) => OnGetRankInfoSuccess(content, resultAction),
            OnGetRankInfoFail
        );
    }

    private void OnGetRankInfoSuccess(string content, UnityAction<List<RankItem>> resultAction = null)
    {
        try
        {
            var rankResponse = JsonConvert.DeserializeObject<TopListData>(content);
            if (rankResponse == null)
            {
                LoggerUtils.LogError("GetRankInfoSuccess: Response is null");
                resultAction?.Invoke(new List<RankItem>());
                return;
            }

            this._isEnd = rankResponse.isEnd == 1;
            this._cookie = rankResponse.cookie;

            resultAction?.Invoke(rankResponse.rankList ?? new List<RankItem>());
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"GetRankInfoSuccess Error: {e.Message}");
            resultAction?.Invoke(new List<RankItem>());
        }
    }

    private void OnGetRankInfoFail(string error)
    {
        LoggerUtils.LogError($"获取排行榜失败: {error}");
    }
}

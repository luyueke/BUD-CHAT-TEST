using Game.CommunityGame;
using Network.Http;
using Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using GameData.BaseInfo;

public class SearchAIMapInfoDataLoader : SectionInfoDataLoader
{
    public override void GetSectionInfoData(UnityAction<List<RecommendItemData>> resultAction = null)
    {
        if (_isEnd)
        {
            resultAction?.Invoke(new List<RecommendItemData>());
            return;
        }

        JObject jb = new JObject
        {
            ["cookie"] = _cookie,
            ["searchWord"] = _sectionId,
            ["gameType"] = (int)GameType.AIGame
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.SearchMap, HttpMethod.GET, reqParam, (content) =>
        {
            GetSectionInfoSuccess(content, resultAction);
        }, GetSectionInfoFail);
    }
}

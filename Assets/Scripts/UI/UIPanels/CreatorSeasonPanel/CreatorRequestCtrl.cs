using System;
using System.Collections;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class CreatorRequestCtrl : GlobalInstance<CreatorRequestCtrl>
{


    /// <summary>
    /// 请求创作者分数排行榜信息
    /// </summary>
    /// <param name="listType">榜单类别 0,全服实时 1.全服上周 2.好友实时 3.历史</param>
    /// <param name="category">榜单类型 All = 0, Skin2D = 1, Skin3D = 2, Animation = 3, Map = 4, Tools = 5</param>
    /// <param name="onComplete"></param>
    /// <param name="onError"></param>
    public void RequestScoreRankInfo(int listType, int category, string feature,Action<CreatorScoreRankInfoData> onComplete, Action<string> onError)
    {
        var jb = new JObject
        {
            ["listType"] = listType,
            ["category"] = category,
            ["feature"] = feature,
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CreatorScoreRankInfo, HttpMethod.GET, reqParam, (content) =>
        {
            var res = JsonConvert.DeserializeObject<CreatorScoreRankInfoData>(content);
            onComplete?.Invoke(res);
        }, (error) =>
        {
            onError?.Invoke(error);
        });
    }

    /// <summary>
    /// 创作者分数
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="onComplete"></param>
    /// <param name="onError"></param>
    public void RequestCreatorUserScoreInfo(string uid, Action<CreatorUserScoreInfoList> onComplete, Action<string> onError,string feature = "s15-0")
    {
        var jb = new JObject
        {
            ["uid"] = uid,
            ["feature"] = feature,
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CreatorUserScoreInfo, HttpMethod.GET, reqParam, (content) =>
        {
            var res = JsonConvert.DeserializeObject<CreatorUserScoreInfoList>(content);
            onComplete?.Invoke(res);
        }, (error) =>
        {
            onError?.Invoke(error);
        });
    }

    /// <summary>
    /// 创作者勋章列表信息
    /// </summary>
    /// <param name="owendType">0,当前周，1，历史</param>
    /// <param name="isSyncWeek"></param>
    /// <param name="onComplete"></param>
    /// <param name="onError"></param>
    public void RequestCreatorBadgeListInfo(int owendType, Action<CreatorBadgeListInfoData> onComplete, Action<string> onError)
    {
        var jb = new JObject
        {
            ["ownedType"] = owendType,
            ["isSyncWeek"] = 0,
        };
        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.CreatorBadgeListInfo, HttpMethod.GET, reqParam, (content) =>
        {
            var res = JsonConvert.DeserializeObject<CreatorBadgeListInfoData>(content);
            onComplete?.Invoke(res);
        }, (error) =>
        {
            onError?.Invoke(error);
        });
    }
}

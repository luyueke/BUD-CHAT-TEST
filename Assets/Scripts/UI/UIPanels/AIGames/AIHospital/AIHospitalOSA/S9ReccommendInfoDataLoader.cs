using Network.Http;
using Network;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using GameData.BaseInfo;
using Game.CommunityGame;

public class S9ReccommendInfoDataLoader : MonoBehaviour
{
    private bool _isEnd = false;
    private string _cookie = "";
    private string _sectionId = "";

    public void RefreshSectionId(string sectionId)
    {
        this._sectionId = sectionId;
        _cookie = "";
        _isEnd = false;
    }

    public void GetSectionInfoData(UnityAction<List<RecommendItemData>> resultAction = null)
    {
        if (_isEnd)
        {
            resultAction?.Invoke(new List<RecommendItemData>());
            return;
        }

        JObject jb = new JObject
        {
            ["cookie"] = _cookie,
            ["sectionId"] = _sectionId,
            ["gameType"] = (int)GameType.AIGame,
        };

        var reqParam = JsonConvert.SerializeObject(jb);
        NetworkManager.Inst.SendHttpRequest(
            HttpUrlDefine.gameSpotlight, 
            HttpMethod.GET, 
            reqParam, 
            (content) => OnGetSectionInfoSuccess(content, resultAction), 
            OnGetSectionInfoFail
        );
    }

    private void OnGetSectionInfoSuccess(string content, UnityAction<List<RecommendItemData>> resultAction = null)
    {
        try
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<OfficalRecommendRsp>(content);
            if (sectionInfoRsp == null)
            {
                LoggerUtils.LogError("GetSectionInfoSuccess: Response is null");
                resultAction?.Invoke(new List<RecommendItemData>());
                return;
            }

            //this._isEnd = sectionInfoRsp.isEnd == 1;
            //this._cookie = sectionInfoRsp.cookie;

            if (sectionInfoRsp.sections == null || sectionInfoRsp.sections.Count == 0)
            {
                resultAction?.Invoke(new List<RecommendItemData>());
                return;
            }

            var recommendList = new List<RecommendItemData>();
            //foreach (var section in sectionInfoRsp.sections)
            //{
            //    var recommendItem = new RecommendItemData
            //    {
            //        ugcInfo = section.mapInfo,
            //        creatorInfo = section.creator,
            //        interactInfo = section.interactInfo,
            //        relationShipInfo = section.relationShipInfo
            //    };
            //    recommendList.Add(recommendItem);
            //}

            resultAction?.Invoke(recommendList);
        }
        catch (System.Exception e)
        {
            LoggerUtils.LogError($"GetSectionInfoSuccess Error: {e.Message}");
            resultAction?.Invoke(new List<RecommendItemData>());
        }
    }

    private void OnGetSectionInfoFail(string error)
    {
        LoggerUtils.LogError($"获取推荐列表失败: {error}");
    }
}

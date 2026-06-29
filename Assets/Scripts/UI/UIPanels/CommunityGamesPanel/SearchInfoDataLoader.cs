using System;
using System.Collections.Generic;
using System.Linq;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Game.CommunityGame
{
    public class SearchInfoDataLoader : SectionInfoDataLoader
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
                ["searchWord"] = _sectionId
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest( HttpUrlDefine.SearchMap, HttpMethod.GET, reqParam, (content) =>
            {
                GetSectionInfoSuccess(content, resultAction);
            }, GetSectionInfoFail);
        }
    }
}

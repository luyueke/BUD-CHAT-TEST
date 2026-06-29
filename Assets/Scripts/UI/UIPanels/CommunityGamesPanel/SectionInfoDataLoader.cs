using System.Collections.Generic;
using System.Linq;
using Game.Store;
using GameData.BaseInfo;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Game.CommunityGame
{
    public enum SectionType
    {
        V1 = 0,
        V2 = 1,
    }
    public class SectionInfoDataLoader : MonoBehaviour
    {
        public SectionType SectionType = SectionType.V1;
        protected bool _isEnd = false;
        protected string _cookie = "";
        protected string _sectionId = "";
        protected int _currencyType = 0;

        public void RefreshSectionId(string sectionId, int currencyType)
        {
            this._sectionId = sectionId;
            this._currencyType = currencyType;
            _cookie = "";
            _isEnd = false;
        }

        public virtual void GetSectionInfoData(UnityAction<List<RecommendItemData>> resultAction = null)
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
                ["currencyType"] = _currencyType
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            var headUrl = SectionType == SectionType.V1 ? HttpUrlDefine.sectionInfo : HttpUrlDefine.sectionInfoV2;
            NetworkManager.Inst.SendHttpRequest( headUrl, HttpMethod.GET, reqParam, (content) =>
            {
                GetSectionInfoSuccess(content, resultAction);
            }, GetSectionInfoFail);
        }

        protected void GetSectionInfoSuccess(string content, UnityAction<List<RecommendItemData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<SectionInfoRsp>(content);
            this._isEnd = sectionInfoRsp.IsEnd == 1;
            this._cookie = sectionInfoRsp.cookie;
            
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == null)
            {
                resultAction?.Invoke(new List<RecommendItemData>());
            }
            else
            {
                resultAction?.Invoke(sectionInfoRsp.list);
            }
        }

        protected void GetSectionInfoFail(string error)
        {
            LoggerUtils.LogError("GetCommunityGameDataFail" + error);
        }
    }
}

using System;
using System.Collections.Generic;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.AINPCStudio
{
    public class AINpcPurchasedDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";

        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public void GetDatas(Action<List<AINpcPurchasedItemData>> resultAction)
        {
            if (_isEnd)
                return;

            if(_isEnd)
                return;
            
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["targetUid"] = AccountDataManager.Inst.Uid,
                ["interactType"] = (int)UGCInteractType.BuyNpc,
                ["noPublish"] = 1
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList, HttpMethod.GET, reqParam, (content) =>
            {
                GetOwnNpcDataSuccess(content, resultAction);
            }, GetOwnNpcDataFail);
        }

        private void GetOwnNpcDataSuccess(string content, Action<List<AINpcPurchasedItemData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<AINpcPurchasedDataRsp>(content);
            this._isEnd = sectionInfoRsp.IsEnd == 1;
            this._cookie = sectionInfoRsp.cookie;
            
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<AINpcPurchasedItemData>());
            }
            else
            {
                resultAction?.Invoke(sectionInfoRsp.list);
            }
        }

        private void GetOwnNpcDataFail(string error)
        {
            LoggerUtils.LogError("GetOwnNpcDataFail" + error);
        }
    }


    public class AINpcPurchasedDataRsp : HttpPageBaseData
    {
        public List<AINpcPurchasedItemData> list;
    }

    public class AINpcPurchasedItemData
    {
        public AINpcInfo ugcInfo;
        public BaseInteractInfo interactInfo;
        public BaseCreator creatorInfo;
    }
}
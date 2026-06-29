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
    public class AIBuddySelectDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";


        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public void GetDatas(Action<List<AIBuddySelectItemData>> resultAction)
        {
            if (_isEnd)
                return;

            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["targetUid"] = AccountDataManager.Inst.Uid,
                ["interactType"] = (int)UGCInteractType.BuyNpc,
                ["noPublish"] = 0
            };
            var reqParam = JsonConvert.SerializeObject(jb);

            List<AIBuddySelectItemData> dataList = new List<AIBuddySelectItemData>();

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList, HttpMethod.GET, reqParam, (content) =>
            {
                GetOwnedAIBuddyDataSuccess(content, resultAction);
                
            }, GetOwnNpcDataFail);
        }

        private void GetOwnedAIBuddyDataSuccess(string content, Action<List<AIBuddySelectItemData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<AIBuddySelectDataRsp>(content);
            this._isEnd = sectionInfoRsp.IsEnd == 1;
            this._cookie = sectionInfoRsp.cookie;

            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<AIBuddySelectItemData>());
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


    public class AIBuddySelectDataRsp : HttpPageBaseData
    {
        public List<AIBuddySelectItemData> list;
    }

    public class AIBuddySelectItemData
    {
        public AINpcInfo ugcInfo;
        public BaseInteractInfo interactInfo;
        public BaseCreator creatorInfo;
    }
}
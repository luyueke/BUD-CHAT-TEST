using System;
using System.Collections.Generic;
using GameData;
using GameData.Account;
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
    public class AIBuddyOwnedDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";

        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public void GetDatas(Action<List<AIBuddyInfo>> resultAction)
        {
            if (_isEnd)
                return;

            if(_isEnd)
                return;
            
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.AIBuddyList, HttpMethod.GET, reqParam, (content) =>
            {
                GetOwnNpcDataSuccess(content, resultAction);
            }, GetOwnNpcDataFail);
        }

        private void GetOwnNpcDataSuccess(string content, Action<List<AIBuddyInfo>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<AIBuddyListRsp>(content);
            this._isEnd = sectionInfoRsp.isEnd == 1;
            this._cookie = sectionInfoRsp.cookie;
            
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<AIBuddyInfo>());
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
}

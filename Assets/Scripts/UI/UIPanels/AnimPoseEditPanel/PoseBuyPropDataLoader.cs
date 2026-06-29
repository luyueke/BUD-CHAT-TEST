using System;
using System.Collections.Generic;
using Game.AssetToolBox;
using GameData.Base;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace BUD.AnimPose
{
    public class PoseBuyPropDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";

        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public void GetDatas(Action<List<PoseBuyItemData>> resultAction)
        {
            if(_isEnd)
                return;
            
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["targetUid"] = AccountDataManager.Inst.Uid,
                ["interactType"] = (int)UGCInteractType.BuyUGCItem,
                ["noPublish"] = 1//1:不包含我创作的
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList, HttpMethod.GET, reqParam, (content) =>
            {
                GetOwnToolBoxDataSuccess(content, resultAction);
            }, GetOwnToolBoxDataFail);
        }
        
        private void GetOwnToolBoxDataSuccess(string content, Action<List<PoseBuyItemData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<PoseBuyAssetDataRsp>(content);
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<PoseBuyItemData>());
            }
            else
            {
                this._isEnd = sectionInfoRsp.IsEnd == 1;
                this._cookie = sectionInfoRsp.cookie;
                resultAction?.Invoke(sectionInfoRsp.list);
            }
        }

        private void GetOwnToolBoxDataFail(string error)
        {
            LoggerUtils.LogError("GetOwnToolBoxDataFail" + error);
        }

    }

    public class PoseBuyAssetDataRsp : HttpPageBaseData
    {
        public List<PoseBuyItemData> list;
    }

    public class PoseBuyItemData
    {
        public bool isStore;
        public bool Selected;
        public PropInfo ugcInfo;
    }
}


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
    public class PoseMyCreatePropDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";

        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public void GetDatas(Action<List<PoseCreateItemData>> resultAction)
        {
            if(_isEnd)
                return;
            
            JObject jb = new JObject
            {
                ["uid"] = AccountDataManager.Inst.Uid,
                ["cookie"] = _cookie,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.propPublishList, HttpMethod.GET, reqParam, (content) =>
            {
                GetOwnToolBoxDataSuccess(content, resultAction);
            }, GetOwnToolBoxDataFail);
        }
        
        private void GetOwnToolBoxDataSuccess(string content, Action<List<PoseCreateItemData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<PoseAssetDataRsp>(content);
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<PoseCreateItemData>());
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

    public class PoseAssetDataRsp : HttpPageBaseData
    {
        public List<PoseCreateItemData> list;
    }

    public class PoseCreateItemData
    {
        public bool Selected;
        public PropInfo propInfo;
    }
}

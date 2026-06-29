using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace Game.AssetToolBox
{
    public class ToolBoxOwnDataLoader : ToolBoxBaseDataLoader
    {
        public UGCInteractType ugcInteractType = UGCInteractType.BuyUGCItem;
        public override void GetDatas(Action<List<ToolBoxItemData>> resultAction)
        {
            base.GetDatas(resultAction);

            if (_isEnd)
                return;

            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
                ["targetUid"] = AccountDataManager.Inst.Uid,
                ["interactType"] = (int)ugcInteractType,
                ["noPublish"] = 0,
            };
            var reqParam = JsonConvert.SerializeObject(jb);

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.UGCInteractList, HttpMethod.GET, reqParam, (content) =>
            {
                GetOwnToolBoxDataSuccess(content, resultAction);
            }, GetOwnToolBoxDataFail);
        }
        
        private void GetOwnToolBoxDataSuccess(string content, Action<List<ToolBoxItemData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<ToolBoxDataRsp>(content);
            this._isEnd = sectionInfoRsp.IsEnd == 1;
            this._cookie = sectionInfoRsp.cookie;
            
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<ToolBoxItemData>());
            }
            else
            {
                resultAction?.Invoke(sectionInfoRsp.list);
            }
        }

        private void GetOwnToolBoxDataFail(string error)
        {
            LoggerUtils.LogError("GetOwnToolBoxDataFail" + error);
        }
    }
    
}


using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using GameData;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace Game.AssetToolBox
{
    public class PublishAssetOwnDataLoader : PublishAssetBaseDataLoader
    {
        //public UGCInteractType ugcInteractType = UGCInteractType.BuyUGCItem;
        public override void GetDatas(Action<List<PropResInfo>> resultAction)
        {
            base.GetDatas(resultAction);

            if (_isEnd)
                return;


            var jb = new JObject
            {
                ["targetUid"] = AccountDataManager.Inst.Uid
            };
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.publicList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(jb),
                (content) => {
                    GetOwnToolBoxDataSuccess(content, resultAction);
                }  ,
                GetOwnToolBoxDataFail,
                retryCount: 3);
        }

        public override void GetPublishList(Action<bool, List<PropResInfo>> resultAction)
        {
            if (_isEnd) return;
            if (string.IsNullOrEmpty(AccountDataManager.Inst.Uid)) return;
            var req = new MapListReq
            {
                cookie = _cookie,
                uid = AccountDataManager.Inst.Uid,
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.propPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
            {
                PropListRsp listResponse = JsonConvert.DeserializeObject<PropListRsp>(content);
                this._isEnd = listResponse.isEnd == 1;
                this._cookie = listResponse.cookie;

                if (listResponse.list == null)
                {
                    listResponse.list = new List<PropResInfo>();
                }

                resultAction?.Invoke(true, listResponse.list);
            },
                (error) =>
                {
                    resultAction?.Invoke(false, new List<PropResInfo>());
                });
        }

        private void GetOwnToolBoxDataSuccess(string content, Action<List<PropResInfo>> resultAction = null)
        {
            GetPublishListRsp resData = JsonConvert.DeserializeObject<GetPublishListRsp>(content);
            if (resData == null || resData.prop == null || resData.prop.list.Count == 0)
            {
                resultAction?.Invoke(new List<PropResInfo>());
                return;
            }
            
            var sectionInfoRsp = resData.prop;
            this._isEnd = sectionInfoRsp.isEnd == 1;
            this._cookie = sectionInfoRsp.cookie;
            
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<PropResInfo>());
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


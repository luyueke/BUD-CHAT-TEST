using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileAnimDataLoader : ProfileCommonDataLoader
    {
        
        public override void GetPublishList(Action<bool, List<DraftListItem>> resultAction)
        {
            if (isEnd) return;
            if (string.IsNullOrEmpty(ToUid)) return;
            
            JObject req = new JObject
            {
                ["cookie"] = cookie,
                ["uid"] = ToUid,
                ["skinType"] = 0
            };
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.animPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    MapListResponse listResponse = JsonConvert.DeserializeObject<MapListResponse>(content);
                    this.isEnd = listResponse.isEnd == 1;
                    this.cookie = listResponse.cookie;
            
                    if (listResponse.list == null)
                    {
                        listResponse.list = new List<DraftListItem>();
                    }
            
                    resultAction?.Invoke(true,listResponse.list);
                },
                (error) =>
                {
                    resultAction?.Invoke(false,new List<DraftListItem>());
                });
        }
    }
}

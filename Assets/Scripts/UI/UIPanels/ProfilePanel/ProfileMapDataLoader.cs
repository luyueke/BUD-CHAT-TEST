using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileMapDataLoader : MonoBehaviour
    {
        public string ToUid { get; set; }
        private string cookie;
        private bool isEnd;
        
        
        public void GetPublishList(Action<bool, List<MapResInfo>> resultAction)
        {
            if (isEnd) return;
            if (string.IsNullOrEmpty(ToUid)) return;
            var req = new MapListReq
            {
                cookie = cookie,
                uid = ToUid,
            };
        
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.publishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    MapListRsp mapListResponse = JsonConvert.DeserializeObject<MapListRsp>(content);
                    this.isEnd = mapListResponse.isEnd == 1;
                    this.cookie = mapListResponse.cookie;
        
                    if (mapListResponse.list == null)
                    {
                        mapListResponse.list = new List<MapResInfo>();
                    }
        
                    resultAction?.Invoke(true,mapListResponse.list);
                },
                (error) =>
                {
                    resultAction?.Invoke(false,new List<MapResInfo>());
                });
        }

        public void SetCookie(string cookie)
        {
            this.cookie = cookie;
        }
        
        public void SetIsEnd(int intEnd)
        {
            isEnd = intEnd == 1;
        }
    }
}

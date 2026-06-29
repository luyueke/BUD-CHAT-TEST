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
    public class ProfilePropDataLoader : MonoBehaviour
    {
        public string ToUid { get; set; }
        private string cookie;
        private bool isEnd;
        
        
        public void GetPublishList(Action<bool, List<PropResInfo>> resultAction)
        {
            if (isEnd) return;
            if (string.IsNullOrEmpty(ToUid)) return;
            var req = new MapListReq
            {
                cookie = cookie,
                uid = ToUid,
            };
        
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.propPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    PropListRsp listResponse = JsonConvert.DeserializeObject<PropListRsp>(content);
                    this.isEnd = listResponse.isEnd == 1;
                    this.cookie = listResponse.cookie;
        
                    if (listResponse.list == null)
                    {
                        listResponse.list = new List<PropResInfo>();
                    }
        
                    resultAction?.Invoke(true,listResponse.list);
                },
                (error) =>
                {
                    resultAction?.Invoke(false,new List<PropResInfo>());
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

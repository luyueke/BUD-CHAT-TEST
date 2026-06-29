using System;
using System.Collections;
using System.Collections.Generic;
using GameData;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileOutfitDataLoader : MonoBehaviour
    {
        public string ToUid { get; set; }
        private string cookie;
        private bool isEnd;
        
        
        public void GetPublishList(Action<bool, List<SkinResInfo>> resultAction)
        {
            if (isEnd) return;
            if (string.IsNullOrEmpty(ToUid)) return;
            var req = new SkinListReq
            {
                cookie = cookie,
                uid = ToUid,
                subType = (int)AvatarSubType.All
            };
        
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.clothPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    SkinListRsp listResponse = JsonConvert.DeserializeObject<SkinListRsp>(content);
                    this.isEnd = listResponse.isEnd == 1;
                    this.cookie = listResponse.cookie;
        
                    if (listResponse.list == null)
                    {
                        listResponse.list = new List<SkinResInfo>();
                    }
        
                    resultAction?.Invoke(true,listResponse.list);
                },
                (error) =>
                {
                    resultAction?.Invoke(false,new List<SkinResInfo>());
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

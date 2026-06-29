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
    public class ProfileMaterialDataLoader : MonoBehaviour
    {
        public string ToUid { get; set; }
        private string cookie;
        private bool isEnd;
        
        
        public void GetPublishList(Action<bool, List<MaterialResInfo>> resultAction)
        {
            if (isEnd) return;
            if (string.IsNullOrEmpty(ToUid)) return;
            
            var req = new MaterialListReq
            {
                cookie = cookie,
                uid = ToUid,
            };
        
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.MaterialPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
                {
                    MaterialListRsp listResponse = JsonConvert.DeserializeObject<MaterialListRsp>(content);
                    this.isEnd = listResponse.isEnd == 1;
                    this.cookie = listResponse.cookie;
        
                    if (listResponse.list == null)
                    {
                        listResponse.list = new List<MaterialResInfo>();
                    }
        
                    resultAction?.Invoke(true,listResponse.list);
                },
                (error) =>
                {
                    resultAction?.Invoke(false,new List<MaterialResInfo>());
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

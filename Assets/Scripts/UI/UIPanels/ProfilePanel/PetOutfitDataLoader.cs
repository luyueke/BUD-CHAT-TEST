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
    public class PetOutfitDataLoader : ProfileCommonDataLoader
    {
        public override void GetPublishList(Action<bool, List<DraftListItem>> resultAction)
        {
            if (isEnd) return;
            if (string.IsNullOrEmpty(ToUid)) return;
            
            var req = new AvatarStudioListReq
            {
                cookie = cookie,
                uid = ToUid,
                subType = (int)AvatarSubType.All,
                skinType = (int)CharacterStyle.Pet,
            };
        
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.clothPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
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

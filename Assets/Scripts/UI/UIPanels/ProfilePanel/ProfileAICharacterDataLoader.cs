using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UI.UIPanels.ProfilePanel
{
    public class ProfileAICharacterDataLoader : MonoBehaviour
    {
        public string ToUid { get; set; }

        public void FetchPublished(string uid, Action<bool, List<CabinPublishData>> callback)
        {
            JObject req = new JObject
            {
                ["uid"] = uid,
                ["purchasedType"] = $"{(int)CabinPurchasedType.Published}",
            };

            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.CabinCharacterPublishList,
                HttpMethod.GET,
                JsonConvert.SerializeObject(req),
                content =>
                {
                    var rsp = JsonConvert.DeserializeObject<CabinPublishListData>(content);
                    callback?.Invoke(true, rsp?.list ?? new List<CabinPublishData>());
                },
                _ => callback?.Invoke(false, new List<CabinPublishData>())
            );
        }
    }
}

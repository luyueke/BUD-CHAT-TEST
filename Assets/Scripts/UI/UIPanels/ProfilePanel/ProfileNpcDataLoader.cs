using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UI.UIPanels.ProfilePanel {
    public class ProfileNpcDataLoader : ProfileCommonDataLoader {
        public override void GetPublishList(Action<bool, List<DraftListItem>> resultAction)
        {
            if (isEnd) return;
            if (string.IsNullOrEmpty(ToUid)) return;

            JObject req = new JObject
            {
                ["cookie"] = cookie,
                ["uid"] = ToUid,
            };

            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.NpcPublishList, HttpMethod.GET, JsonConvert.SerializeObject(req), (content) =>
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

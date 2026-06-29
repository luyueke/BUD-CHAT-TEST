using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;

namespace Game.AIResData
{
    public class AIResResponse
    {
        public List<AIContentData> list;
    }

    public class AIContentData
    {
        public int free;
        public int total;
        public int type;
        public int remaining;
    }

    public class AIResDataManager: GlobalInstance<AIResDataManager>
    {

        private AIResResponse netData;

        public void GetNetworkAIResData(Action success = null)
        {
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.usedLimit, HttpMethod.GET, null,
                onReceive: arg0 =>
                {
                    netData = JsonConvert.DeserializeObject<AIResResponse>(arg0); 
                    success?.Invoke();
                },
                onFail: arg0 => { }, null, 0, 3
            );
        }

        public AIContentData GetAIGameData(AIResType aiYandere)
        {
            if (netData != null && netData.list != null)
            {
                var data = netData.list.Find(x => x.type == (int) aiYandere);
                return data;
            }
            return null;
        }
    }
}
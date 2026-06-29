using System;
using System.Collections.Generic;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.PublicSever
{
    public class JoinFriendDataLodaer : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";


        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public virtual void GetDatas(Action<List<JoinFriendData>> resultAction)
        {
            if (_isEnd)
            {
                resultAction?.Invoke(new List<JoinFriendData>());
                return;
            }
            
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetRelationServerList, HttpMethod.POST, reqParam,
                (content) => { GetRelationServerListSuccess(content, resultAction); }, GetRelationServerListFail);
        }

        private void GetRelationServerListSuccess(string content, Action<List<JoinFriendData>> resultAction = null)
        {
            var friendsPlayingRsp = JsonConvert.DeserializeObject<FriendsPlayingRsp>(content);
            this._isEnd = friendsPlayingRsp.IsEnd == 1;
            this._cookie = friendsPlayingRsp.cookie;

            if (friendsPlayingRsp == null || friendsPlayingRsp.serverList == null || friendsPlayingRsp.serverList.Count == 0)
            {
                resultAction?.Invoke(new List<JoinFriendData>());
            }
            else
            {
                resultAction?.Invoke(friendsPlayingRsp.serverList);
            }
        }

        private void GetRelationServerListFail(string error)
        {
            LoggerUtils.LogError("GetRelationServerList" + error);
        }
    }
}

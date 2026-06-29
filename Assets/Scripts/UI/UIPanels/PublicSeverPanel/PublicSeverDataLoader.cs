using System;
using System.Collections;
using System.Collections.Generic;
using GameData.Base;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Game.PublicSever
{
    public class PublicSeverDataLoader : MonoBehaviour
    {
        protected bool _isEnd = false;
        protected string _cookie = "";
        

        public void ResetCookie()
        {
            _cookie = "";
            _isEnd = false;
        }

        public virtual void GetDatas(Action<List<PublicMapData>> resultAction)
        {
            if (_isEnd)
            {
                resultAction?.Invoke(new List<PublicMapData>());
                return;
            }
            
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.GetPublicMapList, HttpMethod.POST, reqParam, (content) =>
            {
                GetPublicRoomInfoSuccess(content, resultAction);
            }, GetPublicRoomInfoFail);
        }
        
        private void GetPublicRoomInfoSuccess(string content, Action<List<PublicMapData>> resultAction = null)
        {
            var publicMapRsp = JsonConvert.DeserializeObject<PublicMapRsp>(content);
            this._isEnd = publicMapRsp.IsEnd == 1;
            this._cookie = publicMapRsp.cookie;
            
            if (publicMapRsp == null || publicMapRsp.mapList == null || publicMapRsp.mapList.Count == 0)
            {
                resultAction?.Invoke(new List<PublicMapData>());
            }
            else
            {
                resultAction?.Invoke(publicMapRsp.mapList);
            }
        }

        private void GetPublicRoomInfoFail(string error)
        {
            LoggerUtils.LogError("GetPublicRoomInfoFail" + error);
        }
    }

    public class PublicMapRsp : HttpPageBaseData
    {
        public List<PublicMapData> mapList;
    }

    public class PublicMapData
    {
        public string mapId;
        public string mapCover;
        public string mapName;
        public string allPlayerCnt;
    }
    
    public class FriendsPlayingRsp : HttpPageBaseData
    {
        public List<JoinFriendData> serverList;
    }

    public class JoinFriendData
    {
        public int maxPlayer;
        public string mapName;
        public string mapCover;
        public string roomCode;
        public string mapId;
        public int roomPlayerNum;
        public List<playerInfo> players;
        
        public class playerInfo
        {
            public string uid;
            public string name;
            public string headUrl;
        }
    }
}
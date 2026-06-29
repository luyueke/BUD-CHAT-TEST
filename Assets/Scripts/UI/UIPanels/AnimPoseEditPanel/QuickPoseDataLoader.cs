using System;
using System.Collections;
using System.Collections.Generic;
using Game.CommunityGame;
using GameData.BaseInfo;
using GameData.UGCData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;


namespace BUD.AnimPose
{
    public class QuickPoseDataLoader : BasePoseDataLoader
    {
        public override void GetDatas(Action<List<QuickPoseData>> resultAction)
        {
            if(_isEnd)
                return;
            
            JObject jb = new JObject
            {
                ["cookie"] = _cookie,
            };
            var reqParam = JsonConvert.SerializeObject(jb);
            
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.quickPoseList, HttpMethod.GET, reqParam, (content) =>
            {
                GetDataSuccess(content, resultAction);
            }, GetDataFail);
        }
        
        private void GetDataSuccess(string content, Action<List<QuickPoseData>> resultAction = null)
        {
            var sectionInfoRsp = JsonConvert.DeserializeObject<QuickPoseDataRsp>(content);
            if (sectionInfoRsp == null || sectionInfoRsp.list == null || sectionInfoRsp.list.Count == 0)
            {
                resultAction?.Invoke(new List<QuickPoseData>());
            }
            else
            {
                this._isEnd = sectionInfoRsp.IsEnd == 1;
                this._cookie = sectionInfoRsp.cookie;
                resultAction?.Invoke(sectionInfoRsp.list);
            }
        }

        private void GetDataFail(string error)
        {
            LoggerUtils.LogError("GetOwnToolBoxDataFail" + error);
        }
    }
    
    public class QuickPoseDataRsp : HttpPageBaseData
    {
        public List<QuickPoseData> list;
    }

    public class QuickPoseData
    {
        public bool Selected;
        public PoseInfo poseInfo;
    }
}


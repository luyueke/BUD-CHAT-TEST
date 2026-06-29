using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using View.UI.PopupPanelSystem.Data;

namespace GameUI
{
    public class ActivitySkipSystem : GlobalInstance<ActivitySkipSystem>
    {
        public ActivitySkipMsg  ActivitySkipMsg;

        public List<int> ShowPopupIds = new List<int>();
        public void RequestInfo(Action<ActivitySkipMsg> callback)
        {
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityNavInfo,
                HttpMethod.GET,
                "",
                onReceive: arg0 =>
                {
                    ActivitySkipMsg = JsonConvert.DeserializeObject<ActivitySkipMsg>(arg0);
                    callback?.Invoke(ActivitySkipMsg);

                }, onFail: arg0 =>
                {
    
                });

        }

        public void PushreportData() {
            if (ShowPopupIds.Count > 0)
            {
                PushreportData(ShowPopupIds,0,1);
                ShowPopupIds.Clear();
            }
        }


        public void PushreportData(List<int> showPopupIds,int clickedPopupIds,int type)
        {
            UploadPopupReq req = new UploadPopupReq();
            if (showPopupIds!= null &&  showPopupIds.Count > 0)
            {
                req.showPopupIds.AddRange(showPopupIds);
            }
            if (clickedPopupIds > 0)
            {
                req.clickedPopupIds.Add(clickedPopupIds);
            }
            req.type = type;
            var paramStr = JsonConvert.SerializeObject(req);
            NetworkManager.Inst.SendHttpRequest(
                HttpUrlDefine.PopupStatusUpload,
                HttpMethod.POST,
                paramStr,
                (response) =>
                {
                    LoggerUtils.Log($"弹窗埋点上报成功: {response}");

                },
                (error) =>
                {
                    LoggerUtils.LogError($"弹窗埋点上报失败: {error}");

                }
            );
        }
    }

    public class ActivitySkipMsg 
    {
        public List<ActivitySkipMsgItem> list;
    }
    public class ActivitySkipMsgItem 
    { 
        public int id;
        public string name;
        public string skipData;
        public string coverUrl;
        public string buttonColor;
        public string buttonName;
        public string bgColor;
        public int skipType;
        public int userType;
    }
    public class UploadPopupReq
    {
        public List<int> showPopupIds = new();
        public List<int> clickedPopupIds = new();
        public int type;
    }
}
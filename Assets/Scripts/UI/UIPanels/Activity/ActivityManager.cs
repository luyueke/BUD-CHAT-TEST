using Basic.Extensions;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameUI
{
    public enum ActivityPanelType
    {
        AnniversaryPanel = 1,
    }
    public class ActivityManager : GlobalInstance<ActivityManager>
    {
        public Dictionary<ActivityId,IActivity> Activitys = new Dictionary<ActivityId,IActivity>();

        public override void Initialize()
        {
            base.Initialize();

            _ = GroupConsumeSystem.Inst;
            _ = AnniversaryCelePackMgr.Inst;
            _ = AnniversaryMonthCardMgr.Inst;
            _ = AnniversaryMgr.Inst;
            MessageHelper.AddListener(MessageName.TcpTimeUpdate, ActivityReq);
        }

        public void AddActivity(ActivityId activityId , IActivity activity)
        {
            if (!Activitys.ContainsKey(activityId))
            {
                Activitys.Add(activityId, activity);
            }
        }

        public bool CheckActivity(ActivityId activityId) {
            if (!InTime(activityId))
            {
                return false;
            }

            if (Activitys.ContainsKey(activityId)) {
                return Activitys[activityId].IsOpen();
            }

            return true;
        }

        public bool InTime(ActivityId activityId) {
            var config = Es.DataTables.GetActivityConfig((int)activityId);
            if (config != null)
            {
                if (!string.IsNullOrEmpty(config.StartTime) && !string.IsNullOrEmpty(config.EntTime))
                {
                    var cur = TcpTimeSystem.Inst.ServerTime;
                    var start = TimeTools.DateTimeToSeconds(DateTime.Parse(config.StartTime));
                    var end = TimeTools.DateTimeToSeconds(DateTime.Parse(config.EntTime));
                    if (cur < start || cur > end)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public List<IActivity> GetActivitys(ActivityPanelType panel)
        {
            var ls = new List<IActivity>();
            var config = Es.DataTables.GetActivityConfigList();
            foreach (var item in config)
            {
                if (item.Panel == (int)panel && Activitys.ContainsKey((ActivityId)item.id) && CheckActivity((ActivityId)item.id))
                {
                    ls.Add(Activitys[(ActivityId)item.id]);
                }
            }

            return ls;
        }

        public List<ActivityId> GetActivityTypes(ActivityPanelType panel)
        {
            var ls = new List<ActivityId>();
            var config = Es.DataTables.GetActivityConfigList();
            foreach (var item in config)
            {
                if (item.Panel == (int)panel && Activitys.ContainsKey((ActivityId)item.id) && CheckActivity((ActivityId)item.id))
                {
                    ls.Add((ActivityId)item.id);
                }
            }

            return ls;
        }

        public List<ReddotType> GetReddotTypes(ActivityId activityId) 
        {
            if (Activitys.ContainsKey(activityId))
            {
                return Activitys[activityId].GetReddotTypes();
            }

            return null;
        }

        #region UI

        public void OpenActivity(ActivityId activityId) { 
            
        }

        #endregion

        #region Web

        public void ActivityReq() {
            ActivityCenterInfoReq req = new ActivityCenterInfoReq();
            req.idList = new List<string>();

            var config = Es.DataTables.GetActivityConfigList();
            foreach (var item in config)
            {
                var t = (ActivityId)item.id;
                if (InTime(t))
                {
                    req.idList.Add(t.ToString());
                }
            }

            LoggerUtils.Log($"发送活动请求，idList: {string.Join(", ", req.idList)}");
            NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST, JsonConvert.SerializeObject(req), (content) =>
            {
                LoggerUtils.Log($"收到活动请求，idList: {content}");
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    foreach (var item in activityResponse.list) 
                    {
                        if (Enum.TryParse(item.activityId, out ActivityId type))
                        {
                            if (Activitys.ContainsKey(type))
                            {
                                Activitys[type].LoginActivityInfo(item);
                            }
                        }
                    }
                    MessageHelper.Broadcast(MessageName.ReddotNotice);
                }

                //客服数据
                if (!string.IsNullOrEmpty(activityResponse.customerServiceUrl))
                {

                }
            },
            (error) =>
            {
                LoggerUtils.Log($"ActivityReq，idList: {error}");
            });

        }


        #endregion
    }
}
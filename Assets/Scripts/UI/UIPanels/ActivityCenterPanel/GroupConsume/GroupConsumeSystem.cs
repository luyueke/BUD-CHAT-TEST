using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameUI
{
    public class GroupConsumeSystem : GlobalInstance<GroupConsumeSystem>,IActivity
    {
        public ActivityInfo ActivityInfo;

        public override void Initialize()
        {
            base.Initialize();

            RedDotSystemNew.Inst.AddReddotType(ReddotType.S11GroupConsume,RedDot);
            ActivityManager.Inst.AddActivity(ActivityId.LaborDayGroupConsume, this);
        }

        public void ShowPanel()
        {
            throw new System.NotImplementedException();
        }

        public List<ReddotType> GetReddotTypes()
        {
            return new List<ReddotType> { ReddotType.S11GroupConsume };
        }

        public bool IsOpen()
        {
            return true;
        }

        public bool RedDot(string param)
        {
            if (ActivityInfo == null)
            {
                ActivityCenterInfoReq req = new ActivityCenterInfoReq();
                req.idList = new List<string>
                    {
                        ActivityId.LaborDayGroupConsume.ToString()
                    };
                NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
                    JsonConvert.SerializeObject(req), (content) =>
                    {
                        ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                        if (activityResponse.list == null)
                        {
                            activityResponse.list = new List<ActivityInfo>();
                        }

                        ActivityInfo = activityResponse.list.Find(x => x.activityId == ActivityId.LaborDayGroupConsume.ToString());

                        MessageHelper.Broadcast(MessageName.AnniversaryPanel_PackRedDot, ActivityId.LaborDayGroupConsume);
                    },
                    (error) =>
                    {
                        Debug.LogError("GroupConsumeSystem   " + error);
                    });
                return false;
            }

            if (ActivityInfo != null)
            {
                foreach (var item in ActivityInfo.eventList)
                {
                    if (item.eventStatus == (int)ClaimStatus.Unlocked)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void LoginActivityInfo(ActivityInfo activityInfo)
        {
            ActivityInfo = activityInfo;
        }
    }
}
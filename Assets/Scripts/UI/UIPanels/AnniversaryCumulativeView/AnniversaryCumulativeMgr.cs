using GameUI;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class AnniversaryCumulativeMgr : GlobalInstance<AnniversaryCumulativeMgr>,IActivity
{
    List<ActivityInfo> _AnniversaryCelebrationGift;
    public void InitData()
    {
        GetDataByHttp(null);
        RedDotSystemNew.Inst.AddReddotType(ReddotType.AnniversaryCelebrationEvent, RedDot);
        ActivityManager.Inst.AddActivity(ActivityId.AnniversaryEvent, this);
    }
    public void GetDataByHttp(Action<List<ActivityInfo>> OnGetActivityListSuccess)
    {   

        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string>
        {
            ActivityId.AnniversaryEvent.ToString()
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    _AnniversaryCelebrationGift = activityResponse.list;
                    OnGetActivityListSuccess?.Invoke(activityResponse.list);
                    MessageHelper.Broadcast(MessageName.ReddotNotice);
                }
            },
            (error) =>
            {
            });
    }

    public bool RedDot(string param)
    {
        if (_AnniversaryCelebrationGift == null)
        {
            return false;
        }
        var activityInfo = _AnniversaryCelebrationGift.Find(x => x.activityId == ActivityId.AnniversaryEvent.ToString());
        if (activityInfo == null)
        {
            return false;
        }
        foreach (var task in activityInfo.eventList)
        {
            
            if (task.eventStatus == 2)
            {
                return true;
            }
        }
        return false;
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }

    public bool IsOpen()
    {
        return true;
    }

    public void ShowPanel()
    {

    }

    public List<ReddotType> GetReddotTypes()
    {
        return new List<ReddotType> { ReddotType.AnniversaryCelebrationEvent };
    }
}

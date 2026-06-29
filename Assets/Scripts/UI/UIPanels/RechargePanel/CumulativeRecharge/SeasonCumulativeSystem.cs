using Basic.Utils;
using GameUI;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeasonCumulativeSystem : GlobalInstance<SeasonCumulativeSystem>, IActivity
{
    public const string SaveKey = "SeasonCumulative_Red4";
    public override void Initialize()
    {
        base.Initialize();


        RedDotSystemNew.Inst.AddReddotType(ReddotType.SeasonCumulative, RedDot);
        //ActivityManager.Inst.AddActivity(ActivityId.MidAutumnGroupConsume, this);

    }

    public List<ReddotType> GetReddotTypes()
    {
        return new List<ReddotType>() { ReddotType.SeasonCumulative };
    }

    public bool IsOpen()
    {
        return false;
    }

    public void LoginActivityInfo(ActivityInfo activityInfo)
    {

    }

    public void ShowPanel()
    {
    
    }

    public void SetRed() {
        SaveGameUtil.Inst.SetIntByPlayerPrefs(SaveKey, 1);
        MessageHelper.Broadcast(MessageName.ReddotNotice);
    }

    public bool RedDot(string param)
    {
        return SaveGameUtil.Inst.GetIntByPlayerPrefs(SaveKey) != 1;
    }

}
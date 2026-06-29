using System;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AbandonedHospitalEscapeDailyViewItem : CommonDailyViewItem
{
    public override void Init(ActivityEventInfo data, Action<ActivityEventInfo> claimAction)
    {
        base.Init(data, claimAction);
        
    }
    
}

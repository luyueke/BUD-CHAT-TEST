using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;

public class CuteRabbitWarsDailyItem : CommonDailyViewItem
{
    protected override void GoButtonClick()
    {
        //金币扭蛋
        if (_info.eventId == 6 || _info.eventId == 7)
        {
            UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.coin");
        }
        else if (_info.eventId == 8 || _info.eventId == 9)
        {
            UIManager.Inst.OpenPanel(PanelId.StoreMallPanel, "lottery.newCottageCore");
        }
        else
        {
            EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_info.eventSkipType);
        }
    }
}

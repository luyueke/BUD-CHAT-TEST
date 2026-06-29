using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UnityEngine;
using UnityEngine.UI;

public class HospitalSeason_DailyTaskView : DailyTaskView
{
    public Text Txt_TotalExp;
    public override void RefreshData(TaskListRsp rsp)
    {
        base.RefreshData(rsp);
        if(rsp == null)
            return;
        
        var taskInfoData = rsp.list.Find(x => x.taskId == TASK_ID.S15SeasonChallenge.ToString());
        
        var totalFinishAmount = taskInfoData.eventList[0].finishAmount;
        taskInfoData.eventList.ForEach(x =>
        {
            if (x.finishAmount != 0)
            {
                Txt_TotalExp.text = $"每日任务本周经验上限{totalFinishAmount}/700";
                return;
            }
        });
    }
}

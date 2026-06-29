using Game.Event;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;

public class AnniversaryBtn : MonoBehaviour
{
    public CButton clickBtn;
    public GameObject redDot;
    public Dictionary<ActivityId, int> ammoversaryRedDot;

    public void CheckRedDot()
    {
        int redNum = 0;
        redNum += CheckSummerRedDot();





        redDot.gameObject.SetActive(redNum > 0);
    }
    int CheckSummerRedDot()
    {
        int num = 0;
        var taskList = AnniversarySummerMgr.Inst.taskInfo?.eventList;
        for (int i = 0; i < 7; i++)
        {
            if (taskList != null && taskList[i].eventStatus== (int)SevenDaySignClaimStatus.Unlocked)
            {
                num++;
            }
            
        }
        //if (ammoversaryRedDot.ContainsKey(ActivityId.AnniversaryCelebrationSummer))
        {
            //ammoversaryRedDot[ActivityId.AnniversaryCelebrationSummer] = num;
        }
        //else
        {
            //ammoversaryRedDot.Add(ActivityId.AnniversaryCelebrationSummer, num);
        }
        return num;
    }
    /*
    int CheckStoreRedDot()
    {
        int num=0;
        string dailyTaskId = "S11CelebrationStoreDailyTask";
        string weekTaskId = "S11CelebrationStoreWeeklyTask";
        // --- 步骤1: 异步获取网络数据 ---
        //bool isDailyDone = false;
        //bool isWeeklyDone = false;
        List<TaskItemData> daylyServerEventList = new List<TaskItemData>();
        List<TaskItemData> weekServerEventList = new List<TaskItemData>();
        Debug.Log($"开始每日请求数据");
        IAPDataManager.Inst.GetTaskList(dailyTaskId, (b, taskInfoResponse) =>
        {
            if (b && taskInfoResponse?.list?.Count > 0 && taskInfoResponse.list[0]?.eventList != null)
            {
                daylyServerEventList = taskInfoResponse.list[0].eventList;
            }
            else
            {
                num = 0;
                return;
            }
            //isDailyDone = true;


            Debug.Log($"开始每周请求数据");

            IAPDataManager.Inst.GetTaskList(weekTaskId, (b, taskInfoResponse) =>
            {
                if (b && taskInfoResponse?.list?.Count > 0 && taskInfoResponse.list[0]?.eventList != null)
                {
                    weekServerEventList = taskInfoResponse.list[0].eventList;
                }
                else
                {
                    num = 0;
                    return;
                }

            });
        });

    }
    */
}

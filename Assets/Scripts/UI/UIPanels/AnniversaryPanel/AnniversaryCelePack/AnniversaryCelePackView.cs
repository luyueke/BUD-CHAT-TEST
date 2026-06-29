using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Network;
using Network.Http;
using Basic.Utils;
using EventTracking;
using System;
using Game.Event;
using UI.Base;
using Network.Message;


public class AnniversaryCelePackView : ActivityBaseView
{

    [Header("UI相关")]

    public TabGroup tabGroup;

    public Button btn_fuli;
    public AnniversaryCelePackSubView1 subView1;
    public AnniversaryCelePackSubView2 subView2;
    public AnniversaryCelePackSubView3 subView3;
    public AnniversaryCelePackSubView4 subView4;
    public Text txt_timeDown;

    private BudTimer _timer;
    private int _hour = -1;
    public void Awake()
    {
        btn_fuli.onClick.AddListener(OnFuliClicked);
        tabGroup.OnChanged(OnTabChanged);
        tabGroup.Select(1);
        tabGroup.Select(0);

        subView1.parentView = this;
        subView2.parentView = this;
        subView3.parentView = this;
        subView4.parentView = this;
        AnniversaryCelePackMgr.Inst.view = this;

        CheckTime();
        _timer = TimerManager.Inst.Run("AnniversaryCelePackView", 0, 1, () =>
        {
            CheckTime();
        });
        RefreshUI();
    }

    void CheckTime()
    {
        DateTime now = TcpTimeSystem.Inst.ServerDataTime;
        if (now < AnniversaryCelePackMgr.ACTIVITY_END_TIME)
        {
            if (now.Hour != _hour)
            {
                var day = (AnniversaryCelePackMgr.ACTIVITY_END_TIME - now).Duration().TotalDays;
                day = (int)day;
                var hour = (AnniversaryCelePackMgr.ACTIVITY_END_TIME - now).Duration().Hours % 24;
                _hour = now.Hour;
                txt_timeDown.text = $"{day}天{hour}小时";
            }
        }
        else
        {
            TimerManager.Inst.Stop(_timer);
            _timer = null;
            txt_timeDown.text = "活动已结束";
        }
    }

    void OnDestroy()
    {
        TimerManager.Inst.Stop(_timer);
        _timer = null;
    }

    void OnTabChanged(int index)
    {
        subView1.gameObject.SetActive(false);
        subView2.gameObject.SetActive(false);
        subView3.gameObject.SetActive(false);
        subView4.gameObject.SetActive(false);
        switch (index)
        {
            case 0: subView1.gameObject.SetActive(true); break;
            case 1: subView2.gameObject.SetActive(true); break;
            case 2: subView3.gameObject.SetActive(true); break;
            case 3: subView4.gameObject.SetActive(true); break;
        }
    }

    void RefreshUI()
    {
        //周年福利入口
        var taskInfo = AnniversaryCelePackMgr.Inst.TaskId2InfoDict[AnniversaryCelePackMgr.TaskId_5];
        if (taskInfo == null)
        {
            btn_fuli.gameObject.SetActive(false);
            return;
        }
        btn_fuli.gameObject.SetActive(taskInfo.eventList.Count > 0 && taskInfo.eventList[0].eventStatus == 2);
    }

    void OnFuliClicked()
    {
        // TODO: 打开周年福利界面
        AnniversaryCelePackMgr.Inst.ClaimTaskReward(AnniversaryCelePackMgr.TaskId_5, 1, null, null);

    }

    // void CheckTime()
    // {
    //     DateTime now = DateTime.Now;
    //     if (now < _activityCanReceiveTime)
    //     {
    //         if (now.Hour != _hour)
    //         {
    //             var day = (now - _activityCanReceiveTime).Duration().TotalDays;
    //             day = (int)day;
    //             var hour = (now - _activityCanReceiveTime).Duration().Hours % 24;
    //             _hour = now.Hour;
    //             Txt_countDown.text = $"{day}天{hour}小时后将收到周年赠礼";
    //             RefreshUI();
    //         }
    //     }
    //     else
    //     {
    //         TimerManager.Inst.Stop(_timer);
    //         _timer = null;
    //         RefreshUI();
    //     }
    // }

    public void RefreshUI(TASK_ID taskId)
    {
        if (taskId == AnniversaryCelePackMgr.TaskId_1)
        {
            subView1.RefreshUI();
        }
        else if (taskId == AnniversaryCelePackMgr.TaskId_2)
        {
            subView2.RefreshUI();
        }

        else if (taskId == AnniversaryCelePackMgr.TaskId_3)
        {
            subView3.RefreshUI();
        }
        else if (taskId == AnniversaryCelePackMgr.TaskId_4)
        {
            subView4.RefreshUI();
        }
        else if (taskId == AnniversaryCelePackMgr.TaskId_5)
        {
            RefreshUI();
        }
    }



    private void OnReceiveClicked()
    {
        if (!GlobalFuncExtensions.CheckCanClick())
        {
            return;
        }
    }



}

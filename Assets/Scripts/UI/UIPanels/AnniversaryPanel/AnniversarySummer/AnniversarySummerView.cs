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


public class AnniversarySummerView : ActivityBaseView
{

    [Header("UI相关")]
    public Text Txt_LeftTime;
    public CButton Btn_Rule;

    List<AnniversarySummerItem> CheckInItems;
    private ActivityInfo _info;

    int curActivityDay = 0;
    BudTimer timer;

    public Transform itemContent;
    public CButton yearGiftBtn;

    void Awake()
    {
        AnniversarySummerMgr.Inst.Init(this.gameObject, this);
        _info = AnniversarySummerMgr.Inst.activityInfo;

        DateTime now = DateTime.Now;
        curActivityDay = now.DayOfYear;

        CheckInItems ??= new();
        CheckInItems.Clear();
        for (int i = 0; i < itemContent.childCount; i++)
        {
            var child = itemContent.GetChild(i);
            CheckInItems.Add(child.GetComponent<AnniversarySummerItem>());
        }

        InitUI();
        timer = TimerManager.Inst.Run("AnniversarySummerView", 0, 1, () =>
        {
            CheckActivityValid();
        });
    }

    void OnDestroy()
    {
        TimerManager.Inst.Stop(timer);
        timer = null;
    }

    private void CheckActivityValid()
    {
        if (!AnniversarySummerMgr.Inst.IsDuringActivity())
        {
            TimerManager.Inst.Stop(timer);
            timer = null;
            return;
        }
        DateTime now = DateTime.Now;
        if (now.DayOfYear != curActivityDay)
        {
            AnniversarySummerMgr.Inst.GetTaskData();
            curActivityDay = now.DayOfYear;
        }
    }

    void InitUI()
    {
        Btn_Rule.onClick.AddListener(OnRuleClicked);
        yearGiftBtn.gameObject.SetActive(!AnniversarySummerMgr.Inst.AnniversaryGiftClaimed);
        yearGiftBtn.onClick.AddListener(OnYearGiftBtnClicked);
        InitContent();
    }

    public override void Init(ActivityInfo info)
    {
        base.Init(info);
    }

    public void RefreshUI()
    {
        InitContent();
    }

    private void OnRuleClicked()
    {
        //todo
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel, "Assets/Loadable/UI/UIPanel/AnniversarySummer/Rule.json");
    }

    void OnYearGiftBtnClicked()
    {
        UIManager.Inst.OpenPanel<AnniversaryGiftView>(PanelId.AnniversaryGiftView);
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);

        InitContent();
    }

    private void InitContent()
    {
        var taskList = AnniversarySummerMgr.Inst.taskInfo?.eventList;
        for (int i = 0; i < 7; i++)
        {
            if (taskList != null && i < taskList.Count)
            {
                var data = taskList[i];
                CheckInItems[i].InitData(data, i + 1);
            }
            else
            {
                CheckInItems[i].InitData(null, i + 1);
            }
        }
    }

}

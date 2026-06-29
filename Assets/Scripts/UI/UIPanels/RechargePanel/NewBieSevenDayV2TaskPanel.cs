using System.Collections.Generic;
using EventTracking;
using Game.Event;
using Message;
using Newbie;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class NewBieSevenDayV2TaskPanel : BasePanel<PinkCoinPanel>
{
    [SerializeField] public CButton closeBtn;

    public List<NewBieSevenDayV2TaskItem> newbieTaskItems;
    private string _taskId = "NewbieCheckInV2";
    public SevenDaySigneView view;
    private TaskInfoData _taskInfoData;

    protected override void FindMask()
    {
        mask = transform.Find("BaseLayout2D/BgMask");
    }

    public override void OnCreate()
    {
        base.OnCreate();
        InitClickListener();
        RefreshTaskStatus();
        string key = "FirstOpenNewBieSevenDayTask_" + AccountDataManager.Inst.Uid;
        //上报展示埋点
        if (!PlayerPrefs.HasKey(key))
        {
            LoadEvent.ReportTaskUV(new TaskUVInfo
            {
                taskId = _taskId,
                type = 0
            });
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
    }


    private void RefreshTaskStatus()
    {
        MessageHelper.Broadcast(MessageName.UpdateHallTask);
        IAPDataManager.Inst.GetTaskList(_taskId, (b, taskInfoResponse) =>
        {
            if (view == null)
            {
                return;
            }
            if (taskInfoResponse == null)
            {
                return;
            }

            List<TaskInfoData> taskInfoDatas = taskInfoResponse.list;
            if (taskInfoDatas == null || taskInfoDatas.Count <= 0)
            {
                return;
            }


            TaskInfoData tsTaskInfoData = taskInfoDatas[0];
            if (tsTaskInfoData == null)
            {
                return;
            }
            view.gameObject.SetActive(false);
            this._taskInfoData = tsTaskInfoData;
            List<TaskItemData> eventList = tsTaskInfoData.eventList;
            int nexCnt = 1;
            for (int i = 0; i < newbieTaskItems.Count; i++)
            {
                if (eventList[i].eventStatus == (int)EventStatus.Claim)
                {
                    nexCnt = i + 1;
                }
                newbieTaskItems[i].SetData(view , tsTaskInfoData.taskId, eventList[i], taskItemData => { RefreshTaskStatus(); } , tsTaskInfoData.value , this , i == nexCnt );
            }
        });
    }
    public override void OnWindowBeFocused()
    {
        base.OnWindowBeFocused();
        RefreshTaskStatus();
    }
    private void InitClickListener()
    {
        closeBtn.onClick.AddListener(() => {
            string key = "FirstClosePinkCoinPanel" + AccountDataManager.Inst.Uid;
            var panel = UIManager.Inst.FindPanel(PanelId.FittingRoomPanel);
            if (!PlayerPrefs.HasKey(key) && panel!=null && AccountDataManager.Inst.UserInfo.isNewUser == 1)
            {
                UIManager.Inst.OpenPanel(PanelId.BootPanel,WindowId.FittingRoomWindow, 4);
                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
            CloseSelf(); 
        });
    }
    
}
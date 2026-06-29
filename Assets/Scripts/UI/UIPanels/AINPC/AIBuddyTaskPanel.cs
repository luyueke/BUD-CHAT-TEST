using System;
using System.Collections.Generic;
using Game.Event;
using GameData.Account;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyTaskPanel : BasePanel<AIBuddyTaskPanel>
{
    [SerializeField] private Transform transBg;
    [SerializeField] private LogicToggleGroupItem toggleGroupItem;
    [SerializeField] private AIBuddyTaskView taskView;
    [SerializeField] private AIBuddyRewardView rewardView;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject taskPageRedDot;
    [SerializeField] private GameObject wayPageRedDot;
    private TaskInfoData _taskData;
    private ActivityInfo _rewardData;
    public ActivityInfo RewardData => _rewardData;
    private string _taskId = TASK_ID.AIBuddyIntimacyTask.ToString();
    private string _rewardId = ActivityId.AIBuddyIntimacy.ToString();
    private AIBuddyInfo _aiBuddyInfo;
   
    public override void OnCreate()
    {
        base.OnCreate();
        // creatorWayView.Init(this);
        InitBGUI();
        SetViewShow("TaskTab",true);
        MessageHelper.AddListener(MessageName.OnAINpcChatPanelClose,OnNpcChatPanelClose);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        MessageHelper.RemoveListener(MessageName.OnAINpcChatPanelClose,OnNpcChatPanelClose);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args != null && args.Length > 0)
        {
            _aiBuddyInfo = (AIBuddyInfo)args[0];
        }
        
        taskView.Init(this,_aiBuddyInfo);
        taskView.AddCliamClickListener(OnTaskClaim);
        rewardView.Init(this,_aiBuddyInfo);
        InitListener();
        RequestTaskData();
        RequestRewardData();
    }
    
    public override void OnWindowShow()
    {
        base.OnWindowShow();
        RequestTaskData();
    }

    private void InitListener()
    {
        backButton.onClick.AddListener(CloseSelf);
        
        toggleGroupItem.AddListenerUIAll(((tabName, isOn) =>
        {
            SetViewShow(tabName,isOn);
        }));
    }
    
    public string GetTaskId()
    {
        string result = _taskId;
        if (_aiBuddyInfo != null)
        {
            result = _taskId + "-" + _aiBuddyInfo.id;
        }

        return result;
    }

    public string GetRewardId()
    {
        string result = _rewardId;
        if (_aiBuddyInfo != null)
        {
            result = _rewardId + "-" + _aiBuddyInfo.id;
        }

        return result;
    }


    public void OnTaskClaim(List<TaskItemData> taskList)
    {
        if (_taskData == null)
        {
            _taskData = new TaskInfoData();
        }

        _taskData.eventList = taskList;
    }
    
    public void RequestRewardData()
    {
        string rewardId = GetRewardId();
        ActivityCenterInfoReq req = new ActivityCenterInfoReq();
        req.idList = new List<string> { rewardId };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityList, HttpMethod.POST,
            JsonConvert.SerializeObject(req), (content) =>
            {
                ActivityResponse activityResponse = JsonConvert.DeserializeObject<ActivityResponse>(content);
                if (activityResponse.list != null)
                {
                    OnGetRewardDataSuccess(activityResponse.list);
                }
            },
            (error) =>
            {
            });
    }

    public void RequestTaskData(Action<bool, TaskListRsp> resultAction = null)
    {
        string taskId = GetTaskId();
        GetTaskListReq getTaskListReq = new GetTaskListReq()
        {
            idList = new List<string>{taskId}
        };
    
        var reqParam = JsonConvert.SerializeObject(getTaskListReq);
        
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.TaskList, HttpMethod.POST, reqParam, content =>
        {
            var taskListRsp = JsonConvert.DeserializeObject<TaskListRsp>(content);
            resultAction?.Invoke(true, taskListRsp);
            OnGetTaskDataSuccess(taskListRsp);

        } , failMessage =>
        {
            resultAction?.Invoke(false, null);
        });
    }
    

    private void SetViewShow(string tabName, bool isOn)
    {
        taskView.gameObject.SetActive(isOn&&tabName == "TaskTab");
        rewardView.gameObject.SetActive(isOn&&tabName == "RewardTab");
    }
    private void InitBGUI()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(transBg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#ED8CFE", atlasPath, new List<string>()
        {
            "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
        });
        item.SetImagesColor(new Color(1,1,1,0.4f));
        itemObj.gameObject.SetActive(true);
    }
    
    private void OnGetRewardDataSuccess(List<ActivityInfo> activityList)
    {
        if(!this) return;
        var activityInfo = activityList.Find(x => x.activityId == _rewardId);
        if (activityInfo == null) { return; }
        
        _rewardData = activityInfo;
        rewardView.InitData(_rewardData);
        
        var hasRedDot = false;
        for (int i = 0; i < _rewardData.eventList.Count; i++)
        {
            if (_rewardData.eventList[i].eventStatus == (int)BudRewardStatus.Unlocked)
            {
                hasRedDot = true;
                break;
            }
        }
        wayPageRedDot.SetActive(hasRedDot);
    }

    private void OnGetTaskDataSuccess(TaskListRsp taskListRsp)
    {
        if (!this) return;
        if (taskListRsp == null || taskListRsp.list == null || taskListRsp.list.Count <= 0) return;
        TaskInfoData taskInfoData = taskListRsp.list.Find(x => x.taskId == _taskId);
        if(taskInfoData == null) return;
        _taskData = taskInfoData;
        taskView.InitData(_taskData);
        
        var hasRedDot = false;
        for (int i = 0; i < _taskData.eventList.Count; i++)
        {
            if (_taskData.eventList[i].eventStatus == (int)BudRewardStatus.Unlocked)
            {
                hasRedDot = true;
                break;
            }
        }
        taskPageRedDot.SetActive(hasRedDot);
    }

    //聊天关闭，刷新一下任务
    private void OnNpcChatPanelClose()
    {
        RequestTaskData();
    }
}
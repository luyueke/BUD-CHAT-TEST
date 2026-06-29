using System;
using System.Collections.Generic;
using System.Linq;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using RTG;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreatorCenterPanel : BasePanel<CreatorCenterPanel>
{
    [SerializeField] private Transform transBg;
    [SerializeField] private LogicToggleGroupItem toggleGroupItem;
    [SerializeField] private CreatorTaskView creatorTaskView;
    [SerializeField] private CreatorWayView creatorWayView;
    [SerializeField] private CreatorGashaponView creatorGashaponView;
    [SerializeField] private CreatorUpLevelView creatorUpLevelView;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject taskPageRedDot;
    [SerializeField] private GameObject wayPageRedDot;
    private CreatorCenterData _data;
    public CreatorCenterData Data => _data;
    private int _currentType = 0;
    public override void OnCreate()
    {
        base.OnCreate();
        creatorTaskView.Init(this,OnCliam);
        creatorWayView.Init(this,OnCliam);
        creatorGashaponView.Init(this);
        InitBGUI();
        InitTag();
        backButton.onClick.AddListener(CloseSelf);
        SendPanelHttp(InitData);
        SetViewShow("",false);
        
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        if (args.Length > 0)
        {
            _currentType = (int)args[0];
            if(_currentType <= 0)
            {
                _currentType = 0;
            }
        }
        SetToggleGroupInteractable(args.Length == 0);
    }

    private void SetToggleGroupInteractable(bool interactable)
    {
        if (toggleGroupItem?.Toggles == null)
        {
            return;
        }

        for (int i = 0; i < toggleGroupItem.Toggles.Count; i++)
        {
            var t = toggleGroupItem.Toggles[i];
            if (t != null)
            {
                t.interactable = interactable;
            }
        }
    }

    public void OnCliam(CreatorCenterData data )
    {
        _data = data;
        creatorTaskView.Refrash();
        creatorWayView.Refrash();
        SetRedDot();
        MessageHelper.Broadcast(MessageName.OnCreatorCenterDataRefreshed, _data);
    }
    private void SendPanelHttp(Action callback)
    {
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.Center,
            HttpMethod.GET,
            null,
            (content) => {
                CreatorCenterData data =
                    JsonConvert.DeserializeObject<CreatorCenterData>(content);
                _data = data;
                callback?.Invoke();
                
            },
            (error) => {
            });
    }
    /// <summary>
    /// 任务页是否存在可领取（与接口 reddot 字段一致）
    /// </summary>
    public static bool HasClaimableCreativeTask(CreatorCenterData data)
    {
        if (data?.creativeTasks?.list == null)
        {
            return false;
        }

        for (int i = 0; i < data.creativeTasks.list.Count; i++)
        {
            if (data.creativeTasks.list[i].reddot == 1)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 创作者之路页是否存在可领取（与接口 creatorsPath.reddot 一致）
    /// </summary>
    public static bool HasCreatorRoadRedDot(CreatorCenterData data)
    {
        return data?.creatorsPath != null && data.creatorsPath.reddot == 1;
    }

    private void SetRedDot()
    {
        if (Data==null)
        {
          return;  
        }
        var isTaskPageRedDot = HasClaimableCreativeTask(Data);
        taskPageRedDot.SetActive(isTaskPageRedDot);
        wayPageRedDot.SetActive(Data.creatorsPath.reddot == 1);
    }
    private void InitData()
    {
        //SetViewShow("CreateTask",true);
        creatorTaskView.InitData();
        creatorWayView.InitData();
        SetRedDot();
        MessageHelper.Broadcast(MessageName.OnCreatorCenterDataRefreshed, _data);
        var tabName = _currentType switch
        {
            1 => "CreateWay",
            2 => "CreateGashapon",
            _ => "CreateTask",
        };
        if (toggleGroupItem != null)
        {
            toggleGroupItem.SetIsOnNoLogic(tabName, true);
        }
    }
    private void InitTag()
    {
        toggleGroupItem.AddListenerUIAll(((tabName, isOn) =>
        {
            SetViewShow(tabName,isOn);
        }));
        
    }

    private void SetViewShow(string tabName, bool isOn)
    {
        creatorTaskView.gameObject.SetActive(isOn&&tabName == "CreateTask");
        creatorWayView.gameObject.SetActive(isOn&&tabName == "CreateWay");
        creatorGashaponView.gameObject.SetActive(isOn&&tabName == "CreateGashapon");
        backButton.gameObject.SetActive(!creatorGashaponView.gameObject.activeSelf);
    }
    private void InitBGUI()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/CommonBgPanel/CommonBgIcon.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(transBg);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#AA8DFE", atlasPath, new List<string>()
        {
            "avatar_icon_1", "avatar_icon_2", "avatar_icon_3", "avatar_icon_4"
        });
        itemObj.gameObject.SetActive(true);
    }

    public void ShowLevelUp(int level)
    {
        creatorUpLevelView.Show(level);
    }
}

public class CreatorCenterData
{
    public CreatorCenterTasksData creativeTasks;
    public CreatorCenterPathData creatorsPath;
}
public class CreatorRewardGetData
{
    public CreatorCenterTasksData creativeTasks;
    public CreatorCenterPathData creatorsPath;
    public List<RewardData> rewardList;
    public int isLevelUp;
}
public class RewardData
{
    public int rewardType;
    public int amount;
    public string pgcId;
}
public class CreatorCenterTasksData
{
    public CreatorCenterLevelInfo creatorLevelInfo;
    public List<CreatorTasksInfo> list;
}
public class CreatorCenterPathData
{
    public int level;
    public int reddot;
    public List<CreatorTaskData> taskList;
    public CreatorProgressInfo progressInfo;
}
public class CreatorCenterLevelInfo
{
    public int point;
    public int titleType;
    public int level;
    public int unlockTime;
    public int currentTitlePoint;//当前等级开始积分
    public int nextTitlePoint;//当前等级目标积分
    public int nextTitleType;
    public int isHighestTitleType;
}
public class CreatorTasksInfo
{
    public int taskType;
    public int reddot;
    public List<CreatorTaskData> taskList;
}
public class CreatorTaskData
{
    public string taskId;
    public int rewardStatus;
    public int currentAmount;
    public int targetAmount;
}
public class CreatorProgressInfo
{
    public int start;
    public int end;
   
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Event;
using GameUI;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UI;

public class CreatorTaskView : MonoBehaviour
{
    [SerializeField] private CreatorCenterTaskItem viewItemPrefab;
    [SerializeField] private LogicToggleGroupItem toggleGroupItem;
    [SerializeField] private List<GameObject> creatorLevelInfoBgList;
    [SerializeField] private Text tatalCount;
    [SerializeField] private Text pointCount;
    [SerializeField] private Button claimAllBtn;
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject infoRoot;
    [SerializeField] private GameObject dailyRedDot;
    [SerializeField] private GameObject weekRedDot;
    [SerializeField] private GameObject achievementRedDot;
    [SerializeField] private Button skipAvatarBtn;
    private List<CreatorCenterTaskItem> viewItems = new List<CreatorCenterTaskItem>();
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform characterRoot;
    private bool isSending = false;
    private List<CreatorCenterTaskLocalInfo> creatorCenterTaskLocalInfo;
    private CreatorCenterPanel Panel;
    [SerializeField] private List<GameObject> tagList;
    private string atlasPath = "Assets/Loadable/UI/UIPanel/CreatorCenterPanel/CreatorCenterPanel.spriteatlas";
    private List<CommonTaskType> _unClaimList = new List<CommonTaskType>();
    private List<int> _unClaimTaskList = new List<int>();
    private TaskType _currentTaskType = TaskType.Daily;
    private Action<CreatorCenterData> cliamAction;

    public void Init(CreatorCenterPanel panel,Action<CreatorCenterData> cliamAction)
    {
        this.cliamAction = cliamAction;
        Panel = panel;
        var configAsset =
            Loader.Load<TextAsset>("Assets/Loadable/UI/UIPanel/CreatorCenterPanel/CreatorCenterTaskConfig.json",
                gameObject);
        var tmpEventInfos = JsonConvert.DeserializeObject<List<CreatorCenterTaskLocalInfo>>(configAsset.text);
        if (tmpEventInfos!=null)
        {
            creatorCenterTaskLocalInfo = tmpEventInfos; 
        }
        skipAvatarBtn.onClick.AddListener(() =>
        {
            ContestEventManager.Inst.OpenContestPage();
            // EventCenterDataManager.Inst.SkipToTask(EventCenterSkipType.AvatarStudio);
        });
        toggleGroupItem.AddListenerUIAll((tabName, isOn) =>
        {
            LoggerUtils.Log("toggle invoked");
            if (isOn)
            {
                var type = TaskType.Daily;
                switch (tabName)
                {
                    case "DayTask":
                        type =  TaskType.Daily;
                        break;
                    case "WeekTask":
                        type = TaskType.Weakly;
                        break;
                    case "AchievementTask":
                        type = TaskType.Achievements;
                        break;
                }
                ShowList(type);
                _currentTaskType =type;
            }
        });
        claimAllBtn.onClick.AddListener(ClaimAllReward);
        InitAvatar();
    }
    private void SetRedDot()
    {
        if (Panel.Data==null)
        {
            return;  
        }
        var isTaskPageRedDot = false;
        
        for (int i = 0; i < Panel.Data.creativeTasks.list.Count; i++)
        {
            var task = Panel.Data.creativeTasks.list[i];
            
            switch (Panel.Data.creativeTasks.list[i].taskType)
            {
                case (int)TaskType.Daily:
                    dailyRedDot.SetActive(task.reddot==1);
                    break;
                case (int)TaskType.Weakly:
                    weekRedDot.SetActive(task.reddot==1);
                    break;
                case (int)TaskType.Achievements:
                    achievementRedDot.SetActive(task.reddot==1);
                    break;
            }
        }
       
    }
    private void InitAvatar() {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        characterWrapper.SetParent(characterRoot, true);
        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        avatarCameraController.isZoomEnabled = false;
    }
    public void InitData()
    {
        Refrash();
    }
    public void Refrash()
    {
        SetTagShow();
        var info = Panel.Data.creativeTasks.list.Find(x=>x.taskType == (int)_currentTaskType);
        if (info==null)
        {
            _currentTaskType = (TaskType)Panel.Data.creativeTasks.list[0].taskType;
        }
        ShowList(_currentTaskType);
        RefrashInfoRoot();
        SetRedDot();
        RefrashGetAllBtn();
    }

    private void RefrashGetAllBtn()
    {
        bool isCanTakeAll = false;
        for (int i = 0; i < Panel.Data.creativeTasks.list.Count; i++)
        {
            var taskData = Panel.Data.creativeTasks.list[i];
            for (int j = 0; j < taskData.taskList.Count; j++)
            {
                if (taskData.taskList[j].rewardStatus == (int)BudRewardStatus.Unlocked)
                {
                    isCanTakeAll = true;
                    break;
                }
                
            }
        }
        claimAllBtn.interactable = isCanTakeAll;
    }
    
    private void RefrashInfoRoot()
    {
        if (Panel.Data==null)
            return;
        infoRoot.SetActive(true);
        var levelInfo = Panel.Data.creativeTasks.creatorLevelInfo;
        for (int i = 0; i < creatorLevelInfoBgList.Count; i++)
        {
            creatorLevelInfoBgList[i].SetActive(levelInfo.titleType == i+1);
        }
        tatalCount.text = levelInfo.point + "";
        slider.minValue = levelInfo.currentTitlePoint;
        slider.maxValue = levelInfo.nextTitlePoint;
        slider.value = levelInfo.point;
        if (levelInfo.titleType == (int)CreatorTitleType.PopularCreator)
        {
            pointCount.gameObject.SetActive(false);
        }
        else
        {
            pointCount.gameObject.SetActive(true);
            pointCount.text = "头衔还有"+(levelInfo.nextTitlePoint - levelInfo.point)+"分";
        }
        
    }
    public void ClearList()
    {
        for (int i = 0; i < viewItems.Count; i++)
        {
            Destroy(viewItems[i].gameObject);
        }
        viewItems.Clear();
    }
    public void ShowList(TaskType type)
    {
        if (Panel.Data==null)
            return;
        ClearList();
        var data = Panel.Data.creativeTasks.list.Find(x=>x.taskType == (int)type);
        for (int i = 0; i < data.taskList.Count; i++)
        {
            var obj = Instantiate(viewItemPrefab, viewItemPrefab.transform.parent);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            CreatorCenterTaskItem itemComp = obj.GetComponent<CreatorCenterTaskItem>();
            itemComp.Init(data.taskList[i],creatorCenterTaskLocalInfo.Find(x=>x.taskId == data.taskList[i].taskId), ClaimReward);
            viewItems.Add(itemComp);
        }
    }
   
    private void SetTagShow()
    {
        for (int i = 0; i < tagList.Count; i++)
        {
            tagList[i].SetActive(Panel.Data.creativeTasks.list.Find(x=>x.taskType == i+1)!=null);
        }
    }
  
  


    public void ClaimReward(CreatorTaskData data) {
        if (isSending) {
            return;
        }
        isSending = true;
        JObject jObject = new JObject();
        jObject["pageType"] = 1;
        jObject["taskId"] = data.taskId;
        ShowLoading(data.taskId, true);
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) => {
                ShowLoading(data.taskId, false);
                isSending = false;
                OnClaimSuccess(content);
            },
            (error) => {
                ShowLoading(data.taskId, false);
                isSending = false;
            });
    }
    public void ClaimAllReward() {
        if (isSending) {
            return;
        }
        isSending = true;
        JObject jObject = new JObject();
        jObject["isAll"] = 1;
        jObject["pageType"] = 1;
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) => {
                isSending = false;
                OnClaimSuccess(content);
            },
            (error) => {
                isSending = false;
            });
    }
    private void OnClaimSuccess(string content){
       
        CreatorRewardGetData creatorRewardGetData = JsonConvert.DeserializeObject<CreatorRewardGetData>(content);
        CreatorCenterData creatorCenterData = new CreatorCenterData()
        {
            creatorsPath = creatorRewardGetData.creatorsPath,
            creativeTasks = creatorRewardGetData.creativeTasks
        };
        if (creatorRewardGetData.isLevelUp==1)
        {
            Panel.ShowLevelUp(creatorRewardGetData.creativeTasks.creatorLevelInfo.titleType);
        }
        else
        {
            int amount = 0;
            foreach (var rewardData in creatorRewardGetData.rewardList)
            {
                amount += rewardData.amount;
            }
            var commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.RewardAmount = amount;
            commonRewardItemData.rewardName = "创作者积分";
            commonRewardItemData.IconSp =   XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "currency", this.gameObject);
            var rewardPanel =  UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            rewardPanel.ShowRewards(new List<CommonRewardItemData>(){commonRewardItemData});
        }
       
        cliamAction?.Invoke(creatorCenterData);
    }
    private void ShowLoading(string taskId, bool isShow) {
        var tmpItemView = viewItems.Find(x => x.TaskId == taskId);
        if (tmpItemView == null) {
            return;
        }

        if (isShow) {
            tmpItemView.ClaimStart();
        } else {
            tmpItemView.ClaimCallBack();
        }
    }

}

public enum TaskType {
    ErrTaskType = 0,
    Daily = 1, //每日任务
    Weakly = 2, //每周任务
    Achievements = 3, //成就任务
}
public enum CreatorTitleType {
    ErrCreatorTitleType = 0,
    Newbie = 1, //萌新创作者
    NewCreator = 2,  //新晋创作者
    LightChaserCreator = 3, //逐光创作者
    PopularCreator = 4, //人气创作者
}
public class CreatorCenterTaskLocalInfo {
    public string taskId;
    public string pgcId;
    public int skipType;
    public int taskType;
    public string name;
    public int rewardCount;
    public int rewardType;
    public int rewardStatus;
}
public class CreatorCenterRewardClaimData {
   
}

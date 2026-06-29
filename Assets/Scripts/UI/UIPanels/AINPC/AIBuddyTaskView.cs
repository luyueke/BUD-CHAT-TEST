using System;
using System.Collections.Generic;
using Game.Avatar;
using Game.Event;
using GameData;
using GameData.Account;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyTaskView : MonoBehaviour
{
    [SerializeField] private Transform listContent;
    [SerializeField] private AIBuddyTaskItem itemPrefab;
    [SerializeField] private AIBuddyIntimacyView intimacyView;
    [SerializeField] private SuperTextMesh levelTips;
    [SerializeField] private LoadingButton claimAllBtn;
    [SerializeField] private GameObject dailyRedDot;
    [SerializeField] private AvatarCameraController avatarCameraController;
    [SerializeField] private Transform characterRoot;
    [SerializeField] private List<GameObject> tagList;
    
    private List<AIBuddyTaskItem> viewItems = new List<AIBuddyTaskItem>();
    private bool isSending = false;
    private AIBuddyTaskPanel _mainPanel;
    private TaskInfoData _taskData;
    private AIBuddyInfo _aiBuddyInfo;
    
    private TaskInfoData _taskLocalConfigData;
    
    private string taskConfigPath = "Assets/Loadable/UI/UIPanel/AINPC/Configs/AIBuddyTaskConfig.json";
    
    private Action<List<TaskItemData>> _cliamAction;
    
    public void Init(AIBuddyTaskPanel panel,AIBuddyInfo aiBuddyInfo)
    {
        _mainPanel = panel;
        _aiBuddyInfo = aiBuddyInfo;
        InitConfig();
        claimAllBtn.onClick.AddListener(ClaimAllReward);
        InitAvatar();
        SetIntimacyValue(aiBuddyInfo.intimacyRate);
    }
    
    private void InitConfig()
    {
        var textAsset = Loader.Load<TextAsset>(taskConfigPath, gameObject);
        _taskLocalConfigData = JsonConvert.DeserializeObject<TaskInfoData>(textAsset.text);
    }

    private void Awake()
    {
        MessageHelper.AddListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated,OnRefreshInfoSuccess);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated,OnRefreshInfoSuccess);
    }

    public void AddCliamClickListener(Action<List<TaskItemData>> callback)
    {
        _cliamAction += callback;
    }

    public void RemoveClaimClickListener(Action<List<TaskItemData>> callback)
    {
        _cliamAction -= callback;
    }

    private void SetRedDot()
    {
        if (_taskData == null || _taskData.eventList == null)
        {
            dailyRedDot.SetActive(false);
            return;
        }

        var isTaskPageRedDot = false;
        var taskList = _taskData.eventList;
        bool hasRed = false;
        for (int i = 0; i < taskList.Count; i++)
        {
            var task = taskList[i];
            if (task.eventStatus == (int)ClaimStatus.Unlocked)
            {
                hasRed = true;
                break;
            }
        }
        dailyRedDot.SetActive(hasRed);
        claimAllBtn.SetClickAble(hasRed);
    }
    private void InitAvatar()
    {
        var saveCharacterData =  CharacterData.DeserializeObject(_aiBuddyInfo.npc.npcAvatarJson);
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, characterRoot);
        characterWrapper.SetParent(characterRoot, true);
        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.isMoveEnabled = false;
        avatarCameraController.isZoomEnabled = false;
    }
    public void InitData(TaskInfoData taskInfoData)
    {
        if (taskInfoData == null) return;
        _taskData = taskInfoData;
        ShowList(taskInfoData.eventList);
        Refresh();
    }
    public void Refresh()
    {
        SetRedDot();
    }
    
    public TaskItemData GetTaskItemConfig(int eventId)
    {
        if (_taskLocalConfigData == null || _taskLocalConfigData.eventList == null) return null;
        var result =_taskLocalConfigData.eventList.Find(x => x.eventId == eventId);
        return result;
    }
   
    
    public void ClearList()
    {
        for (int i = 0; i < viewItems.Count; i++)
        {
            Destroy(viewItems[i].gameObject);
        }
        viewItems.Clear();
    }
    public void ShowList(List<TaskItemData> eventList)
    {
        ClearList();
        for (int i = 0; i < eventList.Count; i++)
        {
            var obj = Instantiate(itemPrefab, listContent);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            AIBuddyTaskItem itemComp = obj.GetComponent<AIBuddyTaskItem>();
            var taskInfo = eventList[i];
            
            //组装本地配置数据
            var config = GetTaskItemConfig(taskInfo.eventId);
            if (config != null)
            {
                taskInfo.eventName = config.eventName;
                taskInfo.targetAmount = config.targetAmount;
                taskInfo.rewardList = config.rewardList;
                taskInfo.skipType = config.skipType;
            }
            itemComp.Init(eventList[i], ClaimReward);
            itemComp.AddGoBtnClickListener(OnGoBtnClick);
            viewItems.Add(itemComp);
        }
    }

    private void SetIntimacyValue(int intimacyRate)
    {
        intimacyView.SetValue(intimacyRate);

        int titleLevel = AIBuddyDataManager.Inst.GetIntimacyTitleLevel(intimacyRate);
        if (AIBuddyDataManager.Inst.IsMaxTitleLevel(titleLevel))
        {
            levelTips.SetLocalText("当前头衔已达最高等级");
        }
        else
        {
            int maxValue = AIBuddyDataManager.Inst.GetTitleMaxByLevel(titleLevel);
            int nextLevel = titleLevel + 1;
            int leftValue = maxValue - intimacyRate;
            leftValue = Mathf.Max(leftValue, 0);
            levelTips.SetLocalText("距离<q=intimacy_lv_{0}>头衔还有{1}分",nextLevel,leftValue);
        }

        
    }

    private void OnGoBtnClick(TaskItemData data)
    {
        if (data.skipType == (int)EventCenterSkipType.AIBuddyChat)
        {
            EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)data.skipType,_aiBuddyInfo);
        }
        else
        {
            EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)data.skipType);
        }
    }


    public void ClaimReward(TaskItemData data) {
        if (isSending) {
            return;
        }
        isSending = true;
        ShowLoading(data.eventId, true);
        EventCenterDataManager.Inst.CliamReward(_mainPanel.GetTaskId(), data.eventId, 1, 0, (claimRspData) =>
        {
            ShowLoading(data.eventId, false);
            isSending = false;
            OnClaimSuccess(claimRspData);

        }, () =>
        {
            ShowLoading(data.eventId, false);
            isSending = false;
        });
    }
    
    public void ClaimAllReward() {
        if (isSending) {
            return;
        }
        isSending = true;
        claimAllBtn.ShowLoading();
        EventCenterDataManager.Inst.CliamReward(_mainPanel.GetTaskId(), 0 ,2, 0, (claimRspData) =>
        {
            claimAllBtn.HideLoading();
            claimAllBtn.SetClickAble(false);
            isSending = false;
            OnClaimSuccess(claimRspData);
        
        }, () =>
        {
            claimAllBtn.HideLoading();
            isSending = false;
        });
    }

    private void OnClaimSuccess(TaskClaimRsp claimRspData)
    {
        if (claimRspData != null)
        {
            _cliamAction.Invoke(claimRspData.eventList);
            if (claimRspData.eventList != null && claimRspData.eventList.Count > 0)
            {
                ShowList(claimRspData.eventList);
            }
        }

        if (claimRspData.rewardList != null && claimRspData.rewardList.Count > 0)
        {
            var commonRewardData = new List<CommonRewardItemData>();
            for (int i = 0; i < claimRspData.rewardList.Count; i++)
            {
                var commonRewardItemData = new CommonRewardItemData();
                commonRewardItemData.RewardAmount = claimRspData.rewardList[i].amount;
                commonRewardItemData.rewardType = claimRspData.rewardList[i].rewardType;
                commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)claimRspData.rewardList[i].rewardType);
                commonRewardData.Add(commonRewardItemData);
            }
            var rewardPanel =  UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            rewardPanel.ShowRewards(commonRewardData);
        }
        
        // //刷外部红点
        // ReddotManagerUtils.Inst.RefreshRedDot();
        //刷新亲密度
        AIBuddyDataManager.Inst.RequestAIBuddyInfo(_aiBuddyInfo.id);
        _mainPanel.RequestTaskData();
    }

    private void ShowLoading(int taskId, bool isShow) {
        var tmpItemView = viewItems.Find(x => x.EventId == taskId);
        if (tmpItemView == null) {
            return;
        }

        if (isShow) {
            tmpItemView.ClaimStart();
        } else {
            tmpItemView.ClaimCallBack();
        }
    }
    
    private void OnRefreshInfoSuccess(AIBuddyInfoRsp infoRsp)
    {
        if (infoRsp == null || infoRsp.info == null)
        {
            LoggerUtils.Log("OnRefreshInfoSuccess aiBuddyInfo is null");
            return;
        }

        if (infoRsp.info.id == _aiBuddyInfo.id)
        {
            _aiBuddyInfo = infoRsp.info;
            SetIntimacyValue(_aiBuddyInfo.intimacyRate);
        }
    }

}


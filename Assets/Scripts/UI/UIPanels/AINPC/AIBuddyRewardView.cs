using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameData;
using GameData.Account;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyRewardView : MonoBehaviour
{
    [SerializeField] private Transform listContent;
    [SerializeField] private AIBuddyRewardItem itemPrefab;
    [SerializeField] private Text progressText;
    [SerializeField] private Text levelText;
    [SerializeField] private Slider slider;

    private AIBuddyTaskPanel _mainPanel;
    private List<AIBuddyRewardItem> viewItems = new List<AIBuddyRewardItem>();
    private Action<ActivityEventInfo> cliamAction;
    private ActivityInfo _rewardData;
    private ActivityInfo _rewardLocalConfigData;
    private AIBuddyInfo _aiBuddyInfo;
    
    private string rewardConfigPath = "Assets/Loadable/UI/UIPanel/AINPC/Configs/AIBuddyRewardConfig.json";
    private string atlasPath = "Assets/Loadable/UI/UIPanel/AINPC/AIBuddyTask.spriteatlas";
    
    public void Init(AIBuddyTaskPanel panel ,AIBuddyInfo aiBuddyInfo)
    {
        _mainPanel = panel;
        _aiBuddyInfo = aiBuddyInfo;
        InitConfig();
        SetIntimacyValue(_aiBuddyInfo.intimacyRate);
    }

    private void InitConfig()
    {
        var configAsset = Loader.Load<TextAsset>(rewardConfigPath, gameObject);
        _rewardLocalConfigData = JsonConvert.DeserializeObject<ActivityInfo>(configAsset.text);
    }

    private void Awake()
    {
        MessageHelper.AddListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated,OnRefreshInfoSuccess);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<AIBuddyInfoRsp>(MessageName.OnAIBuddyInfoUpdated,OnRefreshInfoSuccess);
    }

    public void InitData(ActivityInfo activityInfo)
    {
        _rewardData = activityInfo;

        ShowList(_rewardData.eventList);
    }

    public void SetIntimacyValue(int intimacyRate)
    {
        int level = AIBuddyDataManager.Inst.GetIntimacyLevel(intimacyRate);
        levelText.SetLocalText("{0}级",level);

        var config = AIBuddyDataManager.Inst.GetIntimacyLevelConfig(level);
        
        if (AIBuddyDataManager.Inst.IsMaxIntimacyLevel(level))
        {
            progressText.SetText(config.Min + "/" + config.Min);
            slider.value = 1f;
            return;
        }
        
        int showValue = intimacyRate - config.Min;
        int maxValue = config.Max - config.Min;
        
        progressText.SetText(showValue + "/" + maxValue);
        
        float progress = (float)showValue / maxValue;
        progress = Mathf.Min(progress, 1);
        progress = Mathf.Max(progress, 0);
        slider.value = progress;
    }

    bool isInit = false;
    
    public ActivityEventInfo GetItemLocalConfig(int eventId)
    {
        if (_rewardLocalConfigData == null || _rewardLocalConfigData.eventList == null) return null;
        var result =_rewardLocalConfigData.eventList.Find(x => x.eventId == eventId);
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
    
    public void ShowList(List<ActivityEventInfo> eventList)
    {
        ClearList();
        for (int i = 0; i < eventList.Count; i++)
        {
            var obj = Instantiate(itemPrefab, listContent);
            obj.transform.localScale = Vector3.one;
            obj.gameObject.SetActive(true);
            AIBuddyRewardItem itemComp = obj.GetComponent<AIBuddyRewardItem>();
            var taskInfo = eventList[i];
            
            //组装本地配置数据
            var config = GetItemLocalConfig(taskInfo.eventId);
            if (config != null)
            {
                taskInfo.rewardName = config.rewardName;
                taskInfo.rewardNum = config.rewardNum;
                taskInfo.targetAmount = config.targetAmount;
                taskInfo.rewardType = config.rewardType;
                taskInfo.targetAmount = config.targetAmount;
                taskInfo.pgcId = config.pgcId;
            }
            itemComp.Init(taskInfo);
            itemComp.AddItemClickListener(OnItemClick);
            if (i == eventList.Count - 1)
            {
                itemComp.SetProgressVisible(false);
            }
            
            viewItems.Add(itemComp);
        }
    }

    private bool isSending;

    private int test = 0;
    private void OnItemClick(AIBuddyRewardItem itemNode,ActivityEventInfo eventInfo)
    {
        if (isSending) {
            return;
        }
        isSending = true;
        
        JObject jObject = new JObject()
        {
            ["activityId"] = _mainPanel.GetRewardId(),
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse,eventInfo);
            },
            (error) =>
            {
                isSending = false;
            });
    }
    
    private void OnClaimSuccess(ActivityEventClaimResponse response,ActivityEventInfo eventInfo)
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        var rewardIcon = GetRewardSprite(eventInfo);
        var commonRewardData = new List<CommonRewardItemData>();
        var commonRewardItemData = new CommonRewardItemData();
        commonRewardItemData.RewardAmount = eventInfo.rewardNum;
        if (rewardIcon != null)
        {
            commonRewardItemData.IconSp = rewardIcon;
        }
        else
        {
            commonRewardItemData.rewardName = eventInfo.rewardName;
        }
        commonRewardItemData.rewardName = eventInfo.rewardName;
        commonRewardItemData.isConverted = CommonRewardPanel.HasRepleaceReward(response);
        commonRewardData.Add(commonRewardItemData);

        string replaceTips = CommonRewardPanel.GetRepleaceTips(response);
        var rewardPanel =  UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        rewardPanel.ShowRewards(commonRewardData,false,false,replaceTips);
        
        AccountDataManager.Inst.BalanceInfo.Refresh();//刷新货币数量
        AIBuddyDataManager.Inst.RequestAIBuddyInfo(_aiBuddyInfo.id);
        _mainPanel.RequestRewardData();//刷新数据
    }
    
    public Sprite GetRewardSprite(ActivityEventInfo data)
    {
        var iconName = "reward_"+data.rewardType;
        if (!string.IsNullOrEmpty(data.pgcId))
        {
            iconName = "reward_"+data.pgcId;
        }
        var rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, iconName, gameObject);
        return rewardSp;
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

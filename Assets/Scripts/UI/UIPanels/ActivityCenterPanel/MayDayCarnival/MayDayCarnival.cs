using System.Collections.Generic;
using AIGame.Base;
using Basic.Utils;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;

public class MayDayCarnival : ActivityBaseView
{
    public CButton Btn_GoFinish;
    public CButton Btn_Tips;
    public List<MayDayCarnival_TaskItem> _taskItems;

    private readonly List<int> rewardEventIds = new List<int>() { 1, 2, 3, 4, 5, 6, 7 };
    private ActivityInfo activityInfo;
    private string spriteatlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
    private bool isSending = false;

    public override void Init(ActivityInfo info)
    {
        activityInfo = info;
        base.Init(info);
        InitUIComponent();
        RefreshTaskItems(activityInfo);
    }
    
    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        activityInfo = info;
        foreach (var eventInfo in info.eventList)
        {
            var taskItem = _taskItems.Find(x => x.itemId == eventInfo.eventId);
            if (taskItem != null)
            {
                taskItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
            }
        }
    }

    private void InitUIComponent()
    {
        Btn_GoFinish.onClick.AddListener(OnBtnGoFinishClick);   
        Btn_Tips.onClick.AddListener(OnBtnTipsClick);
    }

    private void OnBtnTipsClick()
    {
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel, "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/MayDayCarnival/Rule.json");
    }

    private void RefreshTaskItems(ActivityInfo info)
    {
        for (var i = 0; i < info.eventList.Count; i++)
        {
            if(i >= _taskItems.Count)
                continue;

            var eventInfo = info.eventList[i];

            var rewardData = new CommonRewardItemData()
            {
                pgcId = eventInfo.pgcId,
                RewardAmount = eventInfo.rewardNum,
                rewardName = eventInfo.eventName,
                rewardType = eventInfo.rewardType,
            };
            var rewardEventId = rewardEventIds[i];
            _taskItems[i].Init(rewardEventId, rewardData, OnClaimCallBack);
            _taskItems[i].SetStatus((ClaimStatus)eventInfo.eventStatus);
        }
    }

    private void OnBtnGoFinishClick()
    {
        UIManager.Inst.OpenPanel<AIHospitalMainEntryPanel>(PanelId.AIHospitalMainEntryPanel);
    }
    
    private void OnClaimCallBack(MayDayCarnival_TaskItem rewardItem)
    {
        var eventInfo = activityInfo.eventList.Find(tmp => tmp.eventId == rewardItem.itemId);
        if (eventInfo == null)
        {
            return;
        }

        if (eventInfo.eventStatus != (int)ClaimStatus.Unlocked)
        {
            OnPreviewClick(rewardItem.rewardData);
            return;
        }

        if (isSending)
        {
            return;
        }

        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = activityInfo.activityId,
            ["eventId"] = eventInfo.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse activityEventClaimResponse =
                    JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(activityEventClaimResponse, rewardItem);
            },
            (error) => { isSending = false; });
    }
    
    private void OnPreviewClick(CommonRewardItemData commonRewardItemData = null)
    {
        if (activityInfo == null)
        {
            return;
        }

        if (commonRewardItemData != null)
        {
            int rewardType = commonRewardItemData.rewardType;

            switch (rewardType)
            {
                case (int)BUDRewardType.RewardAvatarFrame:
                    var previewAvatarFrame = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    previewAvatarFrame.PreviewAvatarFrame(AvatarFrameType.AvatarFrameMayDayCarnival);
                    break;
                
                case (int)BUDRewardType.RewardVipFreeTrail:
                    var previewVip = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
                    previewVip.SetVipContent();
                    break;
                
                default:
                    CurrencyType currencyType = GameUtils.ConvertRewardType(rewardType);
                    UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, currencyType);
                    break;
            }
        }
    }
    
    private void OnClaimSuccess(ActivityEventClaimResponse response, MayDayCarnival_TaskItem rewardItem)
    {
        if (this == null || gameObject == null)
        {
            return;
        }

        var eventInfo = activityInfo.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo == null)
        {
            return;
        }
        
        AccountDataManager.Inst.BalanceInfo.Refresh();
        activityInfo.currencyAmount = response.currencyAmount;
        eventInfo.eventStatus = response.eventInfo.eventStatus;
        rewardItem.SetStatus((ClaimStatus)eventInfo.eventStatus);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        panel.ShowRewards(new List<CommonRewardItemData>() { rewardItem.rewardData });
        UpdateRedDot();
        RefrashData(activityInfo);
        MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
    }
}

using System.Collections.Generic;
using System.Linq;
using Game.Event;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;

public class PetPlayer : ActivityBaseView
{
    [SerializeField] private List<PetPlayerItem> _itemViews;
    [SerializeField] private CButton GoBtn;
    
    private bool isSending;
    private ActivityInfo _info;
    public override void Init(ActivityInfo info)
    {
        _info = info;
        var bgParent = GameObjectEx.FindChildByName(transform, "BG");
        
        InitBg(bgParent, "#dae488",new List<string>()
        {
            "pet_player_bg1",
            "pet_player_bg2",
            "pet_player_bg3",
            "pet_player_bg4"
        });
        InitEventBaseInfo(info);
        
        GoBtn?.onClick.RemoveAllListeners();
        GoBtn?.onClick.AddListener(() =>
        {
            var firstItem = ContestDataManager.Inst.CurrentLobbyInfo?.contestList?.First();
            if (firstItem != null)
            {
                ContestEventManager.Inst.OpenContestSelectPage(firstItem.contestId);
            }
        });
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);
        _info = info;
        RefreshItems();
    }
    
    //先用本地数据显示任务信息
    private void InitEventBaseInfo(ActivityInfo info)
    {
        if (_itemViews.Count != info.eventList.Count)
        {
            LoggerUtils.LogError("[PetPlayer] init Event ItemView fail");
            return;
        }

        for (int i = 0; i < info.eventList.Count; i++)
        {
            _itemViews[i].Init(info.eventList[i], ClaimReward);
        }
    }

    public void RefreshItems()
    {
        if (_itemViews.Count != _info.eventList.Count)
        {
            LoggerUtils.LogError("[PetPlayer] Refresh Event ItemView fail");
            return;
        }
        
        for (int i = 0; i < _info.eventList.Count; i++)
        {
            _itemViews[i].Refresh(_info.eventList[i]);
        }
    }
    public void ClaimReward(ActivityEventInfo info, TaskClaimState state)
    {
        if (info.rewardType == (int)BUDRewardType.RewardPgcResource && state != TaskClaimState.Enable && !string.IsNullOrEmpty(info.pgcId))
        {
            string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";

            EventRewardPanelData data = new EventRewardPanelData()
            {
                bgColor = "#dae488",
                rewardItemBgColor = "#78b37d",
                atlasPath = atlasPath,
                iconList = new List<string>()
                {
                    "pet_player_bg1",
                    "pet_player_bg2",
                    "pet_player_bg3",
                    "pet_player_bg4"
                },
                rewardList = new List<string>() { info.pgcId }
            };
            
            UIManager.Inst.OpenPanel<EventCenterRewardPanel>(PanelId.EventCenterRewardPanel, data);
            return;
        }
        
        if (state == TaskClaimState.Enable)
        {
            onClaimDirect(info); 
        }
    }
    
    private void onClaimDirect(ActivityEventInfo info)
    {
        if (isSending)
        {
            return;
        }
        isSending = true;

        JObject jObject = new JObject()
        {
            ["activityId"] = _info.activityId,
            ["eventId"] = info.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST, 
            JsonConvert.SerializeObject(jObject), 
            (content) =>
            {
                ActivityEventClaimResponse avtivityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                isSending = false;
                OnClaimSuccess(avtivityEventClaimResponse);
            },
            (error) =>
            {
                isSending = false;
            });
    }
    
    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        var eventInfo = _info.eventList.Find(x => x.eventId == response.eventInfo.eventId);
        if (eventInfo==null)
        {
            return;
        }

        eventInfo.eventStatus = response.eventInfo.eventStatus;
        RefreshItems();
        
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        if (eventInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            panel.ShowPgcRewards(new List<string>() {eventInfo.pgcId}, eventInfo.eventName);
        }
        else
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
            var rewardList = new List<CommonRewardItemData>();
            CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject);
            commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)eventInfo.rewardType);
            commonRewardItemData.RewardAmount = response.claimAmount;
            rewardList.Add(commonRewardItemData);
        
            panel.ShowRewards(rewardList);
        }
        
        UpdateRedDot();
    }
}

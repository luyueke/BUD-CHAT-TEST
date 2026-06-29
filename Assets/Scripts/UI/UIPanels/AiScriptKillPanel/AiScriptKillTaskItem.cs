using Game.Event;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class AiScriptKillTaskItem : MonoBehaviour
{
    //UI组件
    public Text title;
    public Text num;
    public Button claimBtn;
    public Button goBtn;
    public Button overBtn;

    public Sprite icon;

    string _activityId;
    ActivityEventInfo _localData;
    ActivityEventInfo _serverData;

    Action<ActivityEventClaimResponse> _claimAction;

    private void Start()
    {
        claimBtn.onClick.AddListener(Claim);
    }

    public void SetData(ActivityEventInfo localData , ActivityEventInfo serverData ,  string activityId ,Action<ActivityEventClaimResponse> claimAction)
    {
        _localData = localData;
        _serverData = serverData;
        _activityId = activityId;
        _claimAction = claimAction;
        title.text = $"{localData.eventName}({serverData.finishAmount}/{localData.targetAmount})" ;
        num.text = localData.rewardNum.ToString();
        goBtn.onClick.RemoveAllListeners();
        goBtn.onClick.AddListener(()=> {
            NewbieTaskSkipManager.Inst.HandleSkip(localData.eventSkipType.ToString());
        });
        SetStatus(serverData.eventStatus);
    }


    void SetStatus(int statu)
    {
        switch (statu) {
            case (int)EventStatus.UnClaim:
                goBtn.gameObject.SetActive(true);
                claimBtn.gameObject.SetActive(false);
                overBtn.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Claim:
                goBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(true);
                overBtn.gameObject.SetActive(false);
                break;
            case (int)EventStatus.Finish:
                goBtn.gameObject.SetActive(false);
                claimBtn.gameObject.SetActive(false);
                overBtn.gameObject.SetActive(true);
                break;
        }
    }
    void Claim()
    {
        JObject jObject = new JObject()
        {
            ["activityId"] = _activityId,
            ["eventId"] = _serverData.eventId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                ActivityEventClaimResponse avtivityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                OnClaimSuccess(avtivityEventClaimResponse);
            },
            (error) =>
            {
            });
    }

    private void OnClaimSuccess(ActivityEventClaimResponse avtivityEventClaimResponse)
    {
        this._claimAction?.Invoke(avtivityEventClaimResponse);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        MessageHelper.Broadcast(MessageName.UpdateHallTask);

        CommonRewardItemData item;
        MessageHelper.Broadcast(MessageName.OnPlayerInfoAccountChange, (CurrencyType)_localData.rewardType);
        item = new CommonRewardItemData()
        {
            RewardAmount = _localData.rewardNum,
            IconSp = icon,
            rewardName = "气球"
        };
        items.Add(item);
        panel.ShowRewards(items);
        AccountDataManager.Inst.BalanceInfo.Refresh();
        VipDataManager.Inst.UpdateVipStatus();
        ReddotManagerUtils.Inst.RefreshRedDot();
        SetStatus(avtivityEventClaimResponse.eventInfo.eventStatus);
    }
}

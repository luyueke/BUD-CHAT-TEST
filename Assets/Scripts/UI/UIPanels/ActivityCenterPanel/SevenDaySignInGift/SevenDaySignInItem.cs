using System.Collections.Generic;
using Message;
using Network;
using Network.Http;
using Network.Message;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SevenDaySignInItem : MonoBehaviour
{
    public CButton Btn_ReCheck;
    public CButton Btn_Claim;
    public GameObject Go_BgClaim;
    public GameObject Go_Unable;
    public GameObject Go_Claimed;

    private ActivityEventInfo _eventInfo;
    
    public void Awake()
    {
        Btn_ReCheck.onClick.AddListener(OnBtnRecheckClick);
        Btn_Claim.onClick.AddListener(OnBtnClaimClick);
    }

    public void InitData(ActivityEventInfo eventInfo)
    {
        this._eventInfo = eventInfo;
        var claimStatus = (SevenDaySignClaimStatus)eventInfo.eventStatus;
        switch (claimStatus)
        {
            case SevenDaySignClaimStatus.Lock:
                Btn_ReCheck.gameObject.SetActive(false);
                Btn_Claim.gameObject.SetActive(false);
                Go_Unable.SetActive(true);
                Go_Claimed?.SetActive(false);
                Go_BgClaim?.SetActive(false);
                break;
            case SevenDaySignClaimStatus.Unlocked:
                Btn_ReCheck.gameObject.SetActive(false);
                Btn_Claim.gameObject.SetActive(true);
                Go_Unable.SetActive(false);
                Go_Claimed?.SetActive(false);
                Go_BgClaim?.SetActive(true);
                break;
            case SevenDaySignClaimStatus.Claimed:
                Btn_ReCheck.gameObject.SetActive(false);
                Btn_Claim.gameObject.SetActive(false);
                Go_Unable.SetActive(false);
                Go_Claimed?.SetActive(true);
                Go_BgClaim?.SetActive(false);
                break;
            case SevenDaySignClaimStatus.ReCheck:
                Btn_ReCheck.gameObject.SetActive(true);
                Btn_Claim.gameObject.SetActive(false);
                Go_Unable.SetActive(false);
                Go_Claimed?.SetActive(false);
                Go_BgClaim?.SetActive(false);
                break;
        }
    }

    private void OnBtnRecheckClick()
    {
        var panel = UIManager.Inst.OpenPanel<SevenSiginRecheckPanel>(PanelId.SevenSiginRecheckPanel);

        if (panel != null)
        {
            panel.SetAction(() =>
            {
                SendClaimReq(true);
            });
        }

    }

    private void OnBtnClaimClick()
    {
        SendClaimReq(false);
    }

    private void SendClaimReq(bool isRecheck)
    {
        JObject jObject = new JObject()
        {
            ["activityId"] = ActivityId.SevenDaySignInGift.ToString(),
            ["eventId"] = this._eventInfo.eventId,
            ["expireClaim"] = isRecheck ? 1 : 0,
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ClaimActivityReward,
            HttpMethod.POST,
            JsonConvert.SerializeObject(jObject),
            (content) =>
            {
                MessageHelper.Broadcast(MessageName.OnRefreshTaskDataAfterBack);
                AccountDataManager.Inst.BalanceInfo.Refresh();
                
                ActivityEventClaimResponse avtivityEventClaimResponse = JsonConvert.DeserializeObject<ActivityEventClaimResponse>(content);
                OnClaimSuccess(avtivityEventClaimResponse);
            },
            (error) =>
            {
            });
    }
    
    private void OnClaimSuccess(ActivityEventClaimResponse response)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        
        AccountDataManager.Inst.BalanceInfo.Refresh();
        var rewardList = new List<CommonRewardItemData>();
        CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)_eventInfo.rewardType, gameObject);
        commonRewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)_eventInfo.rewardType);
        commonRewardItemData.RewardAmount = response.claimAmount;
        rewardList.Add(commonRewardItemData);

        panel.ShowRewards(rewardList);
    }
}

public enum SevenDaySignClaimStatus
{
    ErrStatus = 0,
    Lock = 1,
    Unlocked = 2,
    Claimed = 3,
    ReCheck = 4
}

using System;
using System.Collections;
using System.Collections.Generic;
using NetBusiness.Store;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class CoinAssistItem : MonoBehaviour {

    [SerializeField]
    private Image rewardIcon;

    [SerializeField] private Text rewardText;

    [SerializeField] private Text eventText;

    [SerializeField] private GameObject maskObj;
    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject claimedObj;
    [SerializeField] private GameObject selectObj;
    [SerializeField] private GameObject unSelectObj;

    [SerializeField] private CButton claimBtn;

    public int eventId { get; private set; }

    public void Init(ActivityEventInfo eventInfo, ActivityRewardInfo rewardInfo, Action<ActivityEventInfo> claimAction) {

        eventId = eventInfo.eventId;
        BUDRewardType rewardType = (BUDRewardType)eventInfo.rewardType;

        if (rewardType != BUDRewardType.RewardPgcResource) {
            rewardIcon.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject);
        } else {
            var data = Es.DataTables.GetAvatarCommonData(eventInfo.pgcId);
            rewardText.SetLocalText($"x{data.texName}");
            rewardIcon.sprite = PgcUtils.GetIconSpriteByPgcId(eventInfo.pgcId, gameObject);
        }
        rewardText.SetLocalText(rewardInfo.rewardName);
        eventText.SetLocalText(eventInfo.eventName);

        var claimStatus = (ClaimStatus)eventInfo.eventStatus;
        RefreshClaimStatus(claimStatus);
        claimBtn.onClick.RemoveAllListeners();
        claimBtn.onClick.AddListener(() => {
            claimAction.Invoke(eventInfo);
        });
    }


    public void Refresh(ActivityEventInfo eventInfo) {
        var claimStatus = (ClaimStatus)eventInfo.eventStatus;
        RefreshClaimStatus(claimStatus);
    }

    private void RefreshClaimStatus(ClaimStatus status) {
        switch (status) {
            case ClaimStatus.Claimed:
                maskObj.SetActive(true);
                claimedObj.SetActive(true);
                lockObj.SetActive(false);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                break;
            case ClaimStatus.Lock:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(true);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                break;
            case ClaimStatus.Unlocked:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(false);
                unSelectObj.SetActive(false);
                selectObj.SetActive(true);
                break;
            default:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(true);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                break;
        }
    }

}

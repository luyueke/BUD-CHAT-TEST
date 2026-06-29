using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class WinterCarnivalRewardItem : MonoBehaviour
{
    [SerializeField]
    private Image rewardIcon;

    [SerializeField] private Text rewardText;
    [SerializeField] private GameObject maskObj;
    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject claimedObj;
    [SerializeField] private GameObject selectObj;
    [SerializeField] private GameObject unSelectObj;

    [SerializeField] private CButton claimBtn;
    [SerializeField] private CButton previewBtn;

    public int eventId { get; private set; }

    public void Init(ActivityEventInfo eventInfo, ActivityRewardInfo rewardInfo, Action<ActivityEventInfo> claimAction, Action previewAct)
    {
        claimBtn.gameObject.SetActive(false);
        previewBtn.gameObject.SetActive(false);
        
        eventId = eventInfo.eventId;
        BUDRewardType rewardType = (BUDRewardType)eventInfo.rewardType;

        if (rewardType != BUDRewardType.RewardPgcResource) {
            rewardIcon.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject);
        } else {
            rewardIcon.sprite = PgcUtils.GetIconSpriteByPgcId(eventInfo.pgcId, gameObject);
        }

        if (rewardInfo.budRewardType == (int)BUDRewardType.RewardBadge || rewardInfo.budRewardType == (int)BUDRewardType.RewardPinkCoin)
        {
            rewardText.SetLocalText("x" + rewardInfo.rewardNum);
        }
        else
        {
            rewardText.gameObject.SetActive(false);
        }

        var claimStatus = (ClaimStatus)eventInfo.eventStatus;
        RefreshClaimStatus(claimStatus);
        claimBtn.onClick.RemoveAllListeners();
        claimBtn.onClick.AddListener(() => {
            claimAction.Invoke(eventInfo);
        });
        
        previewBtn.onClick.RemoveAllListeners();
        previewBtn.onClick.AddListener(() => { previewAct?.Invoke();});
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
                previewBtn.gameObject.SetActive(true);
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(true);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                break;
            case ClaimStatus.Unlocked:
                claimBtn.gameObject.SetActive(true);
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

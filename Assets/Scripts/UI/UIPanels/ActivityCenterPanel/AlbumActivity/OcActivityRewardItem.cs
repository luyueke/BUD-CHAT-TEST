using Game.Event;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class OcActivityRewardItem : MonoBehaviour
{
    [SerializeField]
    private Image rewardIcon;

    [SerializeField] private Text rewardText;
    [SerializeField] private GameObject maskObj;
    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject claimedObj;
    [SerializeField] private GameObject selectObj;
    [SerializeField] private GameObject unSelectObj;
    [SerializeField] private GameObject rewardContent;
    [SerializeField] private GameObject progressObj;
    [SerializeField] private GameObject unProgressObj;
    [SerializeField] private Text progressTxt;
    [SerializeField] private Text unProgressTxt;
    [SerializeField] private CButton claimBtn;

    public int eventId { get; private set; }


    public void Init(ActivityEventInfo eventInfo, ActivityRewardInfo rewardInfo, Action<ActivityEventInfo> claimAction)
    {
        eventId = eventInfo.eventId;
        BUDRewardType rewardType = (BUDRewardType)rewardInfo.budRewardType;

        if (string.IsNullOrEmpty(rewardInfo.rewardIcon)) {
            rewardIcon.sprite = PgcUtils.LoadRewardIcon((BUDRewardType)rewardInfo.budRewardType, gameObject);
        } else {
            rewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(OcActivityView.atlasPath, rewardInfo.rewardIcon, gameObject);
        }
        if (rewardInfo.budRewardType == (int)BUDRewardType.RewardSkinSlot)
        {
            rewardIcon.rectTransform.sizeDelta = new Vector2(118f, 118f); // 88 * 1.34 ≈ 118
        }
        if (rewardInfo.budRewardType == (int)BUDRewardType.RewardBadge 
            || rewardInfo.budRewardType == (int)BUDRewardType.RewardCoin 
            || rewardInfo.budRewardType == (int)BUDRewardType.RewardPinkCoin
            || rewardInfo.budRewardType == (int)BUDRewardType.RewardLuckyCoin
            || rewardInfo.budRewardType == (int)BUDRewardType.RewardYouYouCoin
            || rewardInfo.budRewardType == (int)BUDRewardType.RewardMagicCoin)
        {
            rewardText.gameObject.SetActive(true);
            rewardText.SetLocalText("x" + rewardInfo.rewardNum);
        }
       else if (rewardInfo.budRewardType == (int)BUDRewardType.RewardVipFreeTrail)
        {
            rewardText.gameObject.SetActive(true);
            rewardText.SetLocalText("1天") ;
        }
        else
        {
            rewardText.gameObject.SetActive(false);
        }

        unProgressTxt.text = progressTxt.text = eventInfo.targetAmount.ToString();
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
                unProgressObj.SetActive(false);
                progressObj.SetActive(true);
                break;
            case ClaimStatus.Lock:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(true);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                unProgressObj.SetActive(true);
                progressObj.SetActive(false);
                break;
            case ClaimStatus.Unlocked:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(false);
                unSelectObj.SetActive(false);
                selectObj.SetActive(true);
                unProgressObj.SetActive(false);
                progressObj.SetActive(true);
                break;
            default:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(true);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                unProgressObj.SetActive(true);
                progressObj.SetActive(false);
                break;
        }
    }
    
}


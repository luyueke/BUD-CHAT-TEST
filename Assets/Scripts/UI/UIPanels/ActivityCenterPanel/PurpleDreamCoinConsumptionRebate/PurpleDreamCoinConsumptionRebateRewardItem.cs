using System;
using Basic.Extensions;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class PurpleDreamCoinConsumptionRebateRewardItem : MonoBehaviour
{
    [SerializeField]
    private Image rewardIcon;

    [SerializeField] private Text rewardText;
    [SerializeField] private Text rewardName;
    [SerializeField] private Text targetNum;
    [SerializeField] private GameObject maskObj;
    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject claimedObj;
    [SerializeField] private GameObject selectObj;
    [SerializeField] private GameObject unSelectObj;
    [SerializeField] private CButton claimBtn;
    [SerializeField] private GameObject Go_PriceEnable;
    [SerializeField] private GameObject Go_PriceUnable;
    [SerializeField] private CButton btnPreview;

    private string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";

    public int eventId { get; private set; }

    public void Init(ActivityEventInfo eventInfo, ActivityRewardInfo rewardInfo, Action<ActivityEventInfo> claimAction, Action<ActivityRewardInfo> goPreviewAct)
    {
            
        claimBtn.enabled = false;
        eventId = eventInfo.eventId;
        btnPreview.onClick.AddListener((() =>
        {
            goPreviewAct.Invoke(rewardInfo);
        }));
        BUDRewardType rewardType = (BUDRewardType)eventInfo.rewardType;

        btnPreview.gameObject.SetActive(rewardType is BUDRewardType.RewardPurpleDreamCoin or BUDRewardType.RewardPgcResource);

        if (!rewardInfo.rewardIcon.IsNullOrEmpty())
        {
            rewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, rewardInfo.rewardIcon, gameObject);
        }
        else
        {
            rewardIcon.sprite = rewardType != BUDRewardType.RewardPgcResource ? PgcUtils.LoadRewardIcon((BUDRewardType)eventInfo.rewardType, gameObject) : PgcUtils.GetIconSpriteByPgcId(eventInfo.pgcId, gameObject);
        }
      

        if (rewardInfo.budRewardType == (int)BUDRewardType.RewardBadge || rewardInfo.budRewardType == (int)BUDRewardType.RewardPinkCoin || rewardInfo.budRewardType == (int)BUDRewardType.RewardMagicCoin
            || rewardInfo.budRewardType == (int)BUDRewardType.RewardPurpleDreamCoin || rewardInfo.budRewardType == (int)BUDRewardType.RewardSkinSlot)
        {
            rewardText.SetLocalText("x" + rewardInfo.rewardNum);
        }
        else
        {
            rewardText.gameObject.SetActive(false);
        }
        
        rewardName.text = rewardInfo.rewardName;
        targetNum.text = eventInfo.targetAmount.ToString();
        
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
                Go_PriceEnable.SetActive(true);
                Go_PriceUnable.SetActive(false);
                break;
            case ClaimStatus.Lock:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(true);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                Go_PriceEnable.SetActive(false);
                Go_PriceUnable.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                claimBtn.enabled = true;
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(false);
                unSelectObj.SetActive(false);
                selectObj.SetActive(true);
                Go_PriceEnable.SetActive(true);
                Go_PriceUnable.SetActive(false);
                break;
            default:
                maskObj.SetActive(false);
                claimedObj.SetActive(false);
                lockObj.SetActive(true);
                unSelectObj.SetActive(true);
                selectObj.SetActive(false);
                Go_PriceEnable.SetActive(false);
                Go_PriceUnable.SetActive(true);
                break;
        }
    }
    
}



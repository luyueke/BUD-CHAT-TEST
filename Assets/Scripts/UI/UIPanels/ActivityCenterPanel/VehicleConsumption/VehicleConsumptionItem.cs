using System;
using System.Collections;
using System.Collections.Generic;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class VehicleConsumptionItem : MonoBehaviour {
    [SerializeField] private Image rewardBg;

    [SerializeField] private Image rewardIcon;

    [SerializeField] private Image maskObj;

    [SerializeField] private Text rewardText;

    [SerializeField] private GameObject lockObj;

    [SerializeField] private GameObject tickObj;

    [SerializeField] private Text custNum;

    [SerializeField] private CButton claimBtn;


    private int eventId;


    public void Init(ActivityEventInfo eventInfo, ActivityRewardInfo rewardInfo,
        Action<ActivityEventInfo> claimAction) {
        eventId = eventInfo.eventId;
        BUDRewardType rewardType = (BUDRewardType)rewardInfo.budRewardType;
        if (rewardType == BUDRewardType.RewardPgcBundle)
        {
            rewardIcon.sprite = PgcUtils.LoadBundleIcon(rewardInfo.bundleId, gameObject);
            rewardText.gameObject.SetActive(false);
        }
       else if (rewardType != BUDRewardType.RewardPgcResource) {
            rewardIcon.sprite = PgcUtils.LoadRewardIcon(rewardType, gameObject);
            rewardText.gameObject.SetActive(true);
            rewardText.SetText($"x {rewardInfo.rewardNum}" );
        }
        else {
            var cfg = PgcUtils.GetPgcConfigData(rewardInfo.pgcId);
            if (cfg != null)
            {
                rewardIcon.sprite = PgcUtils.GetIconSpriteByPgcId(rewardInfo.pgcId, gameObject);
            } else {
                LoggerUtils.LogError("获取不到对应的 PGC 物品:" + rewardInfo.pgcId);
            }

            rewardText.gameObject.SetActive(false);
        }


        custNum.SetText(rewardInfo.spendNum.ToString());

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
                rewardBg.color = new Color32(253, 100, 223, 255);
                maskObj.gameObject.SetActive(true);
                lockObj.gameObject.SetActive(false);
                tickObj.gameObject.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                rewardBg.color = new Color32(255, 201, 119, 255);
                maskObj.gameObject.SetActive(false);
                lockObj.gameObject.SetActive(false);
                tickObj.gameObject.SetActive(false);
                break;
            default:
                rewardBg.color = new Color32(253, 100, 223, 255);
                maskObj.gameObject.SetActive(false);
                lockObj.gameObject.SetActive(true);
                tickObj.gameObject.SetActive(false);
                break;
        }
    }
}

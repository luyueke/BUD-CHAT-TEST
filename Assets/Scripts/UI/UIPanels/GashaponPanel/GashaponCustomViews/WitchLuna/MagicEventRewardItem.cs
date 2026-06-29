using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MagicEventRewardItem : MonoBehaviour {
    [SerializeField] public int eventId;

    [SerializeField] private Image bgImage;
    [SerializeField] private GameObject lockObj;

    [SerializeField] private GameObject doneObj;

    [SerializeField] private GameObject previewObj;
    [SerializeField] private bool isCanPreview;

    [SerializeField] private CommonRewardItemData rewardData;

    [SerializeField] private Sprite unlockSprite;

    private BudRewardStatus claimStatus = BudRewardStatus.Unlocked;
    private Action<int> onClaimedCallback;
    private Action<int> onPreviewCallback;

    private void Awake() {
        GetComponent<CButton>().onClick.AddListener(OnClaimClick);
    }

    public BudRewardStatus GetStatus() {
        return claimStatus;
    }

    public void SetStatus(BudRewardStatus status) {
        claimStatus = status;
        lockObj.SetActive(false);
        doneObj.SetActive(false);
        previewObj.SetActive(isCanPreview);
        switch (claimStatus) {
            case BudRewardStatus.Lock:
                lockObj.SetActive(true);
                break;
            case BudRewardStatus.Unlocked:
                bgImage.sprite = unlockSprite;
                break;
            case BudRewardStatus.Claimed:
                bgImage.sprite = unlockSprite;
                lockObj.SetActive(false);
                doneObj.SetActive(true);
                previewObj.SetActive(false);
                break;
        }
    }


    public void OnClaimClick() {
        if (isCanPreview && claimStatus == BudRewardStatus.Lock) {
            onPreviewCallback?.Invoke(eventId);
            return;
        } else if (claimStatus == BudRewardStatus.Unlocked) {
            onClaimedCallback?.Invoke(eventId);
        }

    }

    public void SetClaimCallBack(Action<int> callback) {
        onClaimedCallback = callback;
    }

    public void SetPreviewCallBack(Action<int> callback) {
        onPreviewCallback = callback;
    }

    public CommonRewardItemData GetRewardData() {
        return rewardData;
    }


}

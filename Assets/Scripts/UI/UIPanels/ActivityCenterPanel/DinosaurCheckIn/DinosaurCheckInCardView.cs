using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class DinosaurCheckInCardView : MonoBehaviour {
    public int eventId;

    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject doneObj;
    [SerializeField] private Image bgImage;
    [SerializeField] CButton claimBtn;
    [SerializeField] Sprite normalSprite;
    [SerializeField] Sprite unlockSprite;
    [SerializeField] private Animator unlockAnimator;

    private Action<int> onClaimCallBack;

    private void Awake() {
        claimBtn.onClick.AddListener(OnClaimClicked);
    }


    public void SetCallBack(Action<int> callBack) {
        onClaimCallBack = callBack;
    }

    private void OnClaimClicked() {
        onClaimCallBack?.Invoke(eventId);
    }

    public void SetStatus(ClaimStatus status) {
        lockObj.SetActive(false);
        doneObj.SetActive(false);
        claimBtn.enabled = false;
        bgImage.sprite = normalSprite;
        unlockAnimator.Play("LoginGiftPanel_done");
        switch (status) {
            case ClaimStatus.Lock:
                lockObj.SetActive(true);
                break;
            case ClaimStatus.Claimed:
                doneObj.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                bgImage.sprite = unlockSprite;
                claimBtn.enabled = true;
                unlockAnimator.Play("LoginGiftPanel_prompt");
                break;
        }
    }

}

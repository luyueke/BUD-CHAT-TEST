
using System;
using UI.BaseWidgets;
using UnityEngine;

public class DinosaurCheckInDailyView : MonoBehaviour {
    public int eventId;

    [SerializeField] private GameObject lockObj;
    [SerializeField] private GameObject doneObj;
    [SerializeField] private GameObject iconDoneObj;
    [SerializeField] CButton claimBtn;


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
        doneObj.SetActive(false);
        lockObj.SetActive(false);
        iconDoneObj.SetActive(false);
        claimBtn.gameObject.SetActive(false);
        switch (status) {
            case ClaimStatus.Lock:
                lockObj.SetActive(true);
                break;
            case ClaimStatus.Claimed:
                iconDoneObj.SetActive(true);
                doneObj.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                claimBtn.gameObject.SetActive(true);
                break;
        }
    }

}

using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class DinosaurCheckInClaimView : MonoBehaviour {
    public int eventId;

    [SerializeField] private GameObject doneObj;
    [SerializeField] private CButton claimBtn;

    [SerializeField] private RectTransform barTransform;
    [SerializeField] private Text barText;

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
        claimBtn.gameObject.SetActive(false);
        switch (status) {
            case ClaimStatus.Lock:
                claimBtn.gameObject.SetActive(true);
                break;
            case ClaimStatus.Claimed:
                doneObj.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                claimBtn.gameObject.SetActive(true);
                break;
            case ClaimStatus.ErrStatus:
                doneObj.SetActive(true);
                break;
        }
    }

    public void SetProgress(int value) {
        int maxValue = 50;
        barTransform.sizeDelta = new Vector2((value *1.0f / maxValue) * 154f, 10 );
        barText.text = $"{value}/{maxValue}";
    }

}

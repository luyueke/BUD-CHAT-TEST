using System;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class WeeklyTaskRewardItem : MonoBehaviour {

    [SerializeField]
    private Text eventNameText;

    [SerializeField]
    private Text eventProgressText;

    [SerializeField] private RectTransform eventProgressBar;

    [SerializeField] protected CButton goButton;
    [SerializeField] protected CButton claimButton;
    [SerializeField] protected GameObject doneObj;

    private SeasonTaskEventInfo eventInfo;
    private Action<int> onClaimCallBack;


    private void Awake() {
        claimButton.onClick.AddListener(OnClaimButtonClick);
    }

    public void Init(SeasonTaskEventInfo info, Action<int> callBack) {
        onClaimCallBack = callBack;
        eventInfo = info;
        eventNameText.SetLocalText(info.eventName);
        eventProgressText.SetText($"0/5");
        eventProgressBar.sizeDelta = new Vector2(0, 33);

    }

    public virtual void SetData(TaskItemData itemData) {

        eventProgressText.gameObject.SetActive(true);
        eventProgressText.SetLocalText($"{itemData.finishAmount}/{eventInfo.targetAmount}");
        eventProgressBar.sizeDelta = new Vector2(304 * itemData.finishAmount * 1.0f/eventInfo.targetAmount, 33);
        doneObj.SetActive(false);
        claimButton.gameObject.SetActive(false);
        goButton.gameObject.SetActive(false);
        switch ((ClaimStatus)itemData.eventStatus) {
            case ClaimStatus.Claimed:
                doneObj.SetActive(true);
                break;
            case ClaimStatus.Unlocked:
                claimButton.gameObject.SetActive(true);
                break;
            case ClaimStatus.Lock:
                goButton.gameObject.SetActive(true);
                break;
        }
    }


    private void OnClaimButtonClick() {
        onClaimCallBack?.Invoke(eventInfo.eventId);
    }

}

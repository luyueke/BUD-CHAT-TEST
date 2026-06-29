using System;
using Game.Event;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CommonDailyViewItem : MonoBehaviour {
    private Text rewardNum;
    private Text eventName;
    private LoadingButton claimButton;
    protected CButton goButton;
    private GameObject claimedObj;
    private int claimStatus;
    private Action<ActivityEventInfo> _claimAction;
    private Text progressText;
    public ActivityEventInfo _info;
    int targetAmonunt;
    public int EventId {
        get {
            return _info?.eventId ?? 0;
        }
    }


    public virtual void Init(ActivityEventInfo data, Action<ActivityEventInfo> claimAction) {
        _info = data;
        rewardNum = GameObjectEx.FindComponentByName<Text>(transform, "RewardNum");
        eventName = GameObjectEx.FindComponentByName<Text>(transform, "EventName");
        claimButton = GameObjectEx.FindComponentByName<LoadingButton>(transform, "ClaimRewardButton");
        goButton = GameObjectEx.FindComponentByName<CButton>(transform, "GoButton");
        claimedObj = GameObjectEx.FindChildByName(transform, "Claimed").gameObject;
        progressText = GameObjectEx.FindComponentByName<Text>(transform, "EventProgress");
        _claimAction = claimAction;
        claimButton.onClick.AddListener(ClaimButtonClick);
        goButton.onClick.AddListener(GoButtonClick);

        RefreshData(data);


        if (_info.eventSkipType == (int)EventCenterSkipType.Login || _info.eventSkipType == (int)EventCenterSkipType.OnlineTime)
        {
            goButton.GetComponentInChildren<Text>().text = "进行中";
        }
    }


    public void RefreshData(ActivityEventInfo info) {
        _info = info;
        rewardNum.SetText("x" + info.rewardNum);
        eventName.SetText(info.eventName);
        targetAmonunt = info.targetAmount;
        if (info.targetAmount > 1) {
            progressText.gameObject.SetActive(true);
            progressText.text = info.finishAmount + "/" + info.targetAmount;
        } else {
            progressText.gameObject.SetActive(false);
        }

        ClaimBtnShow(info.eventStatus);
    }
    public void UpdateUI(ActivityEventInfo info)
    {
        if (targetAmonunt > 1)
        {
            progressText.gameObject.SetActive(true);
            progressText.text = info.finishAmount + "/" + targetAmonunt;
        }
        else
        {
            progressText.gameObject.SetActive(false);
        }
        ClaimBtnShow(info.eventStatus);
    }
    public void ClaimBtnShow(int status) {
        goButton.gameObject.SetActive(status == (int)ClaimStatus.Lock);
        claimButton.gameObject.SetActive(status == (int)ClaimStatus.Unlocked);
        claimedObj.gameObject.SetActive(status == (int)ClaimStatus.Claimed);
    }

    protected virtual void GoButtonClick() {
        EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_info.eventSkipType);
    }

    private void ClaimButtonClick() {
        if (_info == null) {
            return;
        }

        _claimAction?.Invoke(_info);
    }

    public void ClaimStart() {
        claimButton.ShowLoading();
    }

    public void ClaimCallBack() {
        claimButton.HideLoading();
    }
}

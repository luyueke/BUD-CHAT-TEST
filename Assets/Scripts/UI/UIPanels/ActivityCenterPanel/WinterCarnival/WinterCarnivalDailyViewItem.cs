using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class WinterCarnivalDailyViewItem : MonoBehaviour
{
    private Text rewardNum;
    private Text eventName;
    private LoadingButton claimButton;
    private CButton goButton;
    private GameObject claimedObj;
    private int claimStatus;
    private Action<ActivityEventInfo> _claimAction;
    private Text progressText;
    public ActivityEventInfo _info;

    public int EventId {
        get {
            return _info?.eventId ?? 0;
        }
    }


    public void Init(int index, ActivityEventInfo data, Action<ActivityEventInfo> claimAction) {
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

        RefrashData(data);

        if (index == 0)
        {
            goButton.GetComponentInChildren<Text>().text = "进行中";
        }
    }


    public void RefrashData(ActivityEventInfo info) {
        _info = info;
        rewardNum.SetText("x" + info.rewardNum);
        eventName.SetText(info.eventName);
        if (info.targetAmount > 1) {
            progressText.gameObject.SetActive(true);
            progressText.text = info.finishAmount + "/" + info.targetAmount;
        } else {
            progressText.gameObject.SetActive(false);
        }
        ClaimBtnShow(info.eventStatus);
    }

    private void ClaimBtnShow(int status) {
        goButton.gameObject.SetActive(status == (int)ClaimStatus.Lock);
        claimButton.gameObject.SetActive(status == (int)ClaimStatus.Unlocked);
        claimedObj.gameObject.SetActive(status == (int)ClaimStatus.Claimed);
    }

    private void GoButtonClick() {
        EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_info.eventSkipType);
    }

    private void ClaimButtonClick() {
        if (_info == null) {
            return;
        }

        _claimAction?.Invoke(_info);
    }

    public void CliamStart() {
        claimButton.ShowLoading();
    }

    public void CliamCallBack() {
        claimButton.HideLoading();
    }
}

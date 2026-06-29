using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class OcActivityDailyViewItem : MonoBehaviour
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
    public int EventStatus
    {
        get
        {
            return _info?.eventStatus ?? 0;
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

        InitData(data);
        
        
        //if (index == 0)
        //{
        //    goButton.GetComponentInChildren<Text>().text = "进行中";
        //}
    }


    public void InitData(ActivityEventInfo info) {
        _info = info;
        rewardNum.SetText("x" + info.rewardNum);
        eventName.SetText(info.eventName);
        //GrandShow();
        if (info.targetAmount > 1) {
            progressText.gameObject.SetActive(true);
            progressText.text = info.finishAmount + "/" + info.targetAmount;
        } else {
            progressText.gameObject.SetActive(false);
        }
        ClaimBtnShow(info.eventStatus);
    }

    public void GrandShow()
    {
        if (!string.IsNullOrEmpty(_info.targetAmountList) || !string.IsNullOrEmpty(_info.rewardNumList))
        {
            string[] targetList = _info.targetAmountList.Split(",");
            string[] rewardList = _info.rewardNumList.Split(",");
            if (targetList.Length > 0 && rewardList.Length > 0)
            {
                for (int i = 0; i < targetList.Length; i++)
                {
                    if (int.Parse(targetList[i]) == _info.targetAmount && rewardList.Length > i)
                    {
                        _info.rewardNum =int.Parse( rewardList[i]);
                        rewardNum.SetText("x" + rewardList[i]);
                        eventName.SetText(string.Format(_info.eventName, _info.targetAmount));
                        break;
                    }
                }
            }
        }
    }
    public void RefrashData(ActivityEventInfo info)
    {
        //_info = info;
        //rewardNum.SetText("x" + info.rewardNum);
        //eventName.SetText(info.eventName);
        //_info.targetAmount = info.targetAmount;
        _info.finishAmount = info.finishAmount;
        _info.eventStatus = info.eventStatus;
        //GrandShow();
        if (_info.targetAmount > 1)
        {
            progressText.gameObject.SetActive(true);
        progressText.text = info.finishAmount + "/" + _info.targetAmount;
    }
        else
        {
            progressText.gameObject.SetActive(false);
        }
      //  Debug.LogError("ClaimBtnShow status=" + info.eventStatus+",id="+_info.eventId+ ",info.targetAmount ="+ _info.targetAmount+",finish="+ info.finishAmount);
        ClaimBtnShow(info.eventStatus);
    }

    private void ClaimBtnShow(int status) {
        //Debug.LogError("ClaimBtnShow status=" + status);
        goButton.gameObject.SetActive(status == (int)TaskClaimState.Unable);
        claimButton.gameObject.SetActive(status == (int)TaskClaimState.Enable);
        claimedObj.gameObject.SetActive(status == (int)TaskClaimState.Finished);
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

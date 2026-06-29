using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CreatorCenterTaskItem : MonoBehaviour
{
    private Text rewardNum;
    private Text eventName;
    private LoadingButton claimButton;
    private CButton goButton;
    private GameObject claimedObj;
    private int claimStatus;
    private Action<CreatorTaskData> _claimAction;
    private Text progressText;
    public CreatorTaskData _data;
    public CreatorCenterTaskLocalInfo _info;
    public string TaskId {
        get
        {
            return _data.taskId;
        }
    }
    public void Init(CreatorTaskData data,CreatorCenterTaskLocalInfo info, Action<CreatorTaskData> claimAction) {
        _data = data;
        _info = info;
        rewardNum = GameObjectEx.FindComponentByName<Text>(transform, "RewardNum");
        eventName = GameObjectEx.FindComponentByName<Text>(transform, "EventName");
        claimButton = GameObjectEx.FindComponentByName<LoadingButton>(transform, "ClaimRewardButton");
        goButton = GameObjectEx.FindComponentByName<CButton>(transform, "GoButton");
        claimedObj = GameObjectEx.FindChildByName(transform, "Claimed").gameObject;
        progressText = GameObjectEx.FindComponentByName<Text>(transform, "EventProgress");
        _claimAction = claimAction;
        claimButton.onClick.AddListener(ClaimButtonClick);
        goButton.onClick.AddListener(GoButtonClick);
        RefreshData(data,info);
        if (info.skipType != (int)EventCenterSkipType.ErrType) {
            goButton.SetLocalText("去完成");
        } else {
            goButton.SetLocalText("进行中");
        }
    }


    public void RefreshData(CreatorTaskData data,CreatorCenterTaskLocalInfo info) {
        _data = data;
        _info = info;
        rewardNum.SetText("x" + info.rewardCount);
        eventName.SetText(info.name);
        if (data.targetAmount > 1) {
            progressText.gameObject.SetActive(true);
            progressText.text = data.currentAmount + "/" + data.targetAmount;
        } else {
            progressText.gameObject.SetActive(false);
        }

        ClaimBtnShow(data.rewardStatus);
    }

    private void ClaimBtnShow(int status) {
        goButton.gameObject.SetActive(status == (int)ClaimStatus.Lock);
        claimButton.gameObject.SetActive(status == (int)ClaimStatus.Unlocked);
        claimedObj.gameObject.SetActive(status == (int)ClaimStatus.Claimed);
    }

    private void GoButtonClick(){
        EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_info.skipType);
    }

    private void ClaimButtonClick() {
        if (_data == null) {
            return;
        }

        _claimAction?.Invoke(_data);
    }

    public void ClaimStart() {
        claimButton.ShowLoading();
    }

    public void ClaimCallBack() {
        claimButton.HideLoading();
    }
}

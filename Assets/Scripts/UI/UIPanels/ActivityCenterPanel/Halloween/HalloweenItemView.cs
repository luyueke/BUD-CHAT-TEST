
using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using Newtonsoft.Json.Linq;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class HalloweenItemView : MonoBehaviour
{
    private Text rewardNum;
    private Text eventName;
    private LoadingButton claimButton;
    private CButton goButton;
    private GameObject claimedObj;
    private int claimStatus;
    private Action<ActivityEventInfo> _claimAction;
    private Slider progressSlider;
    private Text progressText;
    public ActivityEventInfo _info;

    public int EventId
    {
        get
        {
            return _info?.eventId ?? 0;
        }
    }
    
    public void Init(ActivityEventInfo data, Action<ActivityEventInfo> claimAction)
    {
        _info = data;
        rewardNum = GameObjectEx.FindComponentByName<Text>(transform,"RewardNum");
        eventName = GameObjectEx.FindComponentByName<Text>(transform,"EventName");
        claimButton = GameObjectEx.FindComponentByName<LoadingButton>(transform,"ClaimRewardButton");
        goButton = GameObjectEx.FindComponentByName<CButton>(transform,"GoButton");
        claimedObj = GameObjectEx.FindChildByName(transform,"Claimed").gameObject;
        progressSlider = GameObjectEx.FindComponentByName<Slider>(transform,"ProgressSlider");
        progressText = GameObjectEx.FindComponentByName<Text>(transform,"ProgressText");
        _claimAction = claimAction;
        claimButton.onClick.AddListener(ClaimButtonClick);
        goButton.onClick.AddListener(GoButtonClick);
        
        RefrashData(data);
    }

    public void RefrashData(ActivityEventInfo info)
    {
        _info = info;
        rewardNum.SetText("x"+info.rewardNum);
        eventName.SetText(info.eventName);
        if (info.targetAmount>1)
        {
            progressSlider.gameObject.SetActive(true);
            progressSlider.maxValue = info.targetAmount;
            progressSlider.value = info.finishAmount;
            progressText.text = progressSlider.value + "/" + progressSlider.maxValue;
        }
        else
        {
            progressSlider.gameObject.SetActive(false);
        }
        ClaimBtnShow(info.eventStatus);
    }
    private void ClaimBtnShow(int claimStatus)
    {
        goButton.gameObject.SetActive(claimStatus == (int)ClaimStatus.Lock);
        claimButton.gameObject.SetActive(claimStatus == (int)ClaimStatus.Unlocked);
        claimedObj.gameObject.SetActive(claimStatus == (int)ClaimStatus.Claimed);
    }

    private void GoButtonClick()
    {
        EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_info.eventSkipType);
    }
    
    private void ClaimButtonClick()
    {
        if (_info == null)
        {
            return;
        }
        _claimAction?.Invoke(_info);
    }
    public void CliamStart()
    {
        claimButton.ShowLoading();
    }
    public void CliamCallBack()
    {
        claimButton.HideLoading();
    }
}

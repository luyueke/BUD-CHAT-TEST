using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class AIBuddyTaskItem : MonoBehaviour
{
    [SerializeField] private Text rewardNum;
    [SerializeField] private Text taskContent;
    [SerializeField] private LoadingButton claimButton;
    [SerializeField] private CButton goButton;
    [SerializeField] private GameObject claimedObj;
    [SerializeField] private Text progressText;
    private Action<TaskItemData> _claimAction;
    private Action<TaskItemData> _goAction;
    
    public TaskItemData _itemData;
    public int EventId {
        get
        {
            return _itemData.eventId;
        }
    }

    private void Start()
    {
        claimButton.onClick.AddListener(ClaimButtonClick);
        goButton.onClick.AddListener(GoButtonClick);
    }
    
    

    public void Init(TaskItemData data,Action<TaskItemData> claimAction) {
        _itemData = data;
        _claimAction = claimAction;
        RefreshData(data);
    }

    public void AddGoBtnClickListener(Action<TaskItemData> callback)
    {
        _goAction += callback;
    }


    public void RefreshData(TaskItemData data) {
        _itemData = data;
        taskContent.SetText(data.eventName);
        if (data.targetAmount > 1) {
            progressText.gameObject.SetActive(true);
            progressText.text = data.finishAmount + "/" + data.targetAmount;
        } else {
            progressText.gameObject.SetActive(false);
        }

        if (data.rewardList != null && data.rewardList.Count > 0)
        {
            var rewardInfo = data.rewardList[0];
            rewardNum.SetText("x" + rewardInfo.amount);
        }
        
        ClaimBtnShow(data.eventStatus);
    }

    private void ClaimBtnShow(int status) {
        goButton.gameObject.SetActive(status == (int)ClaimStatus.Lock);
        claimButton.gameObject.SetActive(status == (int)ClaimStatus.Unlocked);
        claimedObj.gameObject.SetActive(status == (int)ClaimStatus.Claimed);
    }

    private void GoButtonClick() {
        _goAction?.Invoke(_itemData);
    }

    private void ClaimButtonClick() {
        if (_itemData == null) {
            return;
        }

        _claimAction?.Invoke(_itemData);
    }

    public void ClaimStart() {
        claimButton.ShowLoading();
    }

    public void ClaimCallBack() {
        claimButton.HideLoading();
    }
}

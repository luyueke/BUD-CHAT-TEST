using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class GiftForFollowItemView : MonoBehaviour, EventItemViewProtocol
{

    [SerializeField] private LoadingButton claimButton;
    [SerializeField] private CButton goButton;
    [SerializeField] private GameObject claimedObj;
    [SerializeField] private CButton iconButton;
    
    private Action<ActivityEventInfo, TaskClaimState> _claimAction;
    public Action<ActivityEventInfo> FollowAction;
    
    private ActivityEventInfo _info;
    public TaskClaimState eventStatus { get; set; }

    public int eventId
    {
        get
        {
            return _info?.eventId ?? -1;
        }
    }
    
    public void Init(ActivityEventInfo data, Action<ActivityEventInfo, TaskClaimState> clickAction)
    {
        eventStatus = TaskClaimState.Unable;

        _info = data;
        _claimAction = clickAction;
        
        goButton?.onClick.RemoveAllListeners();
        goButton.onClick.AddListener(HandleButtonAction);
        
        claimButton?.onClick.RemoveAllListeners();
        claimButton?.onClick.AddListener(HandleButtonAction);
        
        iconButton?.onClick.RemoveAllListeners();
        iconButton?.onClick.AddListener(HandleSkipOnlyAction);
    }

    private void HandleButtonAction()
    {
        if (this == null || _info == null)
        {
            return;
        }
            
        _claimAction?.Invoke(_info, eventStatus);
    }

    private void HandleSkipOnlyAction()
    {
        if (this == null || _info == null)
        {
            return;
        }
            
        FollowAction?.Invoke(_info);
    }
    
    public void Refresh(ActivityEventInfo data)
    {
        eventStatus = (TaskClaimState)data.eventStatus;
        UpdateState();
    }

    private void UpdateState()
    {
        goButton.gameObject.SetActive(eventStatus == TaskClaimState.Unable);
        claimedObj.SetActive(eventStatus == TaskClaimState.Finished);
        claimButton.gameObject.SetActive(eventStatus == TaskClaimState.Enable);
    }

    public void SyncClaimed()
    {
        eventStatus = TaskClaimState.Finished;
        UpdateState();
    }

}

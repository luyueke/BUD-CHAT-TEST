using System;
using System.Collections;
using System.Collections.Generic;
using Game.Event;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class VotingFunItemView : MonoBehaviour, EventItemViewProtocol
{
    private CButton actionButton;
    private GameObject heightlightObj;
    private GameObject claimdObj;
    private GameObject lockObj;
    private Text progressText;
    private Animator Effect_animator;
    private Image Img_Effect;
    

    private void InitUI()
    {
        if (Img_Effect != null)
        {
            return;
        }
        
        actionButton = GameObjectEx.FindChildByName(transform, "ActionBtn").GetComponent<CButton>();
        heightlightObj = GameObjectEx.FindChildByName(transform,"Heightlight").gameObject;

        claimdObj = GameObjectEx.FindChildByName(transform, "ClaimedObj").gameObject;
        lockObj = GameObjectEx.FindChildByName(transform, "Lock").gameObject; 
        Effect_animator = GameObjectEx.FindChildByName(transform, "Heightlight").GetComponent<Animator>();
        Img_Effect = GameObjectEx.FindChildByName(heightlightObj, "Image").GetComponent<Image>();
        progressText = GameObjectEx.FindChildByName(transform, "Progress").GetComponent<Text>();
    }
    
    private Action<ActivityEventInfo, TaskClaimState> _claimAction;
    
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
        InitUI();
        UpdateProgress(data);
        _info = data;
        _claimAction = clickAction;
        actionButton?.onClick.RemoveAllListeners();
        actionButton?.onClick.AddListener(() =>
        {
            if (this == null || _info == null)
            {
                return;
            }
            
            _claimAction?.Invoke(_info, eventStatus);
        });
    }

    private void UpdateProgress(ActivityEventInfo data)
    {
        if (data == null)
        {
            return;
        }

        var targetAmount =  data.targetAmount;
        var finishAmount = data.finishAmount;
        progressText.text = $"{finishAmount}/{targetAmount}";
    }

    public void Refresh(ActivityEventInfo data)
    {
        eventStatus = (TaskClaimState)data.eventStatus;
        UpdateProgress(data);
        UpdateState();
    }

    private void UpdateState()
    {
        Effect_animator.enabled = eventStatus == TaskClaimState.Enable;
        lockObj.gameObject.SetActive(eventStatus == TaskClaimState.Unable);
        heightlightObj.SetActive(eventStatus == TaskClaimState.Enable);
        claimdObj.SetActive(eventStatus == TaskClaimState.Finished);
        if (eventStatus == TaskClaimState.Unable)
        {
            Img_Effect.color = new Color(255, 255, 255, 0);
        } 
        else if (eventStatus == TaskClaimState.Enable)
        {
            Effect_animator.CrossFade("LoginGiftPanel_prompt",0.1f);
        }
        else if (eventStatus == TaskClaimState.Finished)
        {
            Img_Effect.color = new Color(255, 255, 255, 0);
        }
    }

    public void SyncClaimed()
    {
        eventStatus = TaskClaimState.Finished;
        UpdateState();
    }

}

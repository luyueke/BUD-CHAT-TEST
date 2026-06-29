using AIGame.Base;
using Game.Props.PropsBehaviours;
using Message;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIHospital_MainDoorBehaviour : AIPropBaseBehaviour
{
    protected const string AnimName_Idle = "idle";
    protected Animator anim;
    protected int aniState;
    // Start is called before the first frame update
    public override void OnInitByCreate()
    {
        base.OnInitByCreate();
        IsCanClick = true;
        aniState = Animator.StringToHash("state");
        anim = this.GetComponentInChildren<Animator>(true);
        
        MessageHelper.AddListener(MessageName.OnS9UgcMainDoorLock, LockDoor);
    }

    public override void OnTouchClick()
    {
        base.OnTouchClick();
        LoggerUtils.Log("On touchClick s9 main door");
        MessageHelper.Broadcast<Action>(MessageName.OnS9MainDoorClick,HandOpenDoor);
    }

    public void HandOpenDoor()
    {
        
        IsCanClick = false;
        if (anim == null)
        {
            return;
        }

        LoggerUtils.Log("On s9 main door Open");
        var curState = anim.GetInteger(aniState);
        if (curState == (int)DoorState.Close)
        {
            anim.SetInteger(aniState, (int)DoorState.Open);
            PlayOpenSound();
        }
        MessageHelper.Broadcast(MessageName.OnS9MainDoorOpen);
    }

    protected virtual void PlayOpenSound()
    {
        AIGameSoundUtils.Inst.PlaySound(AIHospitalConfig.SOUND_OPEN_DOOR, this.gameObject);
    }

    private void LockDoor()
    {
        IsCanClick = false;
    }

    public void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnS9UgcMainDoorLock, LockDoor);
    }
}

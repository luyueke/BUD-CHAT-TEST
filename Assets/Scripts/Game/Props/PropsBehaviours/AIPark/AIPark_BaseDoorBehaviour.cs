using System;
using System.Collections.Generic;
using System.Linq;
using AIGame.Base;
using Game.Base;
using Game.Scene.EnterModelController;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_BaseDoorBehaviour: AIPropBaseBehaviour
    {
        protected const string AnimName_Idle = "idle";
        protected Animator anim;
        protected int aniState;
        protected List<Collider> doorColliders;
        public override bool AllowClickInEmoteLink => true;

        /// <summary>
        /// 是否允许npc碰撞开启
        /// </summary>
        public  bool bEnableColliderOepn = false;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            IsCanClick = true;
            aniState = Animator.StringToHash("state");
            anim = this.GetComponentInChildren<Animator>(true);
            doorColliders = this.GetComponentsInChildren<Collider>().ToList();
        }

        public override void OnTouchClick()
        {
            base.OnTouchClick();
            HandOpenDoor();
            // TimerManager.Inst.RunOnce("closeDoor", 5, HandCloseDoor);
        }

        public void HandOpenDoor()
        {
            IsCanClick = false;
            if (anim == null)
            {
                return;
            }
            
            doorColliders.ForEach(x => x.isTrigger = true);
            
            var curState = anim.GetInteger(aniState);
            if (curState == (int)DoorState.Close)
            {
                anim.SetInteger(aniState, (int)DoorState.Open);
                PlayOpenSound();
            }
        }

        protected void HandCloseDoor()
        {
            IsCanClick = true;
            var curState = anim.GetInteger(aniState);
            if (curState == (int) DoorState.Open)
            {
                anim.SetInteger(aniState, (int)DoorState.Close);
                PlayCloseSound();
            }
            
            doorColliders.ForEach(x => x.isTrigger = false);
        }

        public override void OnReset()
        {
            base.OnReset();
            HandCloseDoor();
            anim.Play(AnimName_Idle);
        }

        public override void OnNpcColliderHit()
        {
            if (!bEnableColliderOepn) return;
            base.OnNpcColliderHit();
            HandOpenDoor();
        }

        protected virtual void PlayOpenSound()
        {
            AIGameSoundUtils.Inst.PlaySound(AIParkConfig.SOUND_OPEN_DOOR, this.gameObject);
        }
        
        protected virtual void PlayCloseSound()
        {
            AIGameSoundUtils.Inst.PlaySound(AIParkConfig.SOUND_CLOSE_DOOR, this.gameObject);
        }

        public void SetEnableColliderHitOpenDoor(bool value)
        {
            bEnableColliderOepn = value;
        }
    }
}
        

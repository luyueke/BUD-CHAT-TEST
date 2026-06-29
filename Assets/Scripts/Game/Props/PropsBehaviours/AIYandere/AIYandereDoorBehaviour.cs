using System;
using AIGame.Base;
using Game.Base;
using Game.Scene.EnterModelController;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public enum DoorState
    {
        Open = 1,
        Close = 0
    }
    public class AIYandereDoorBehaviour: AIPropBaseBehaviour
    {
        protected Animator anim;
        protected int aniState;
        private Vector3 offset = new(-0.5f, 0, 0);
        
        protected Func<bool, bool> _checkStartGame;

        public void SetFunctions(Func<bool, bool> func)
        {
            this._checkStartGame = func;
        }
        
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            IsCanClick = true;
            aniState = Animator.StringToHash("state");
            anim = this.GetComponentInChildren<Animator>(true);
        }

        public override Vector3 GetTouchData()
        {
            return offset;
        }

        public override void OnTouchClick()
        {
            base.OnTouchClick();
            if (!_checkStartGame(false))
            {
                return;
            }
            HandOpenDoor();
            TimerManager.Inst.RunOnce("closeDoor", 5, HandCloseDoor);
        }

        public void HandOpenDoor()
        {
            IsCanClick = false;
            if (anim == null)
            {
                return;
            }
            var curState = anim.GetInteger(aniState);
            if (curState == (int)DoorState.Close)
            {
                anim.SetInteger(aniState, (int)DoorState.Open);
                AIGameSoundUtils.Inst.PlaySound(YandereConfig.SOUND_OPEN_DOOR,this.gameObject);
            }
        }

        protected void HandCloseDoor()
        {
            IsCanClick = true;
            var curState = anim.GetInteger(aniState);
            if (curState == (int) DoorState.Open)
            {
                anim.SetInteger(aniState, (int)DoorState.Close);
                AIGameSoundUtils.Inst.PlaySound(YandereConfig.SOUND_CLOSE_DOOR,this.gameObject);
            }
        }

        public override void OnReset()
        {
            base.OnReset();
            HandCloseDoor();
            anim.Play("idle");
        }

        public override void OnNpcColliderHit()
        {
            base.OnNpcColliderHit();
            HandOpenDoor();
        }
    }
}
        

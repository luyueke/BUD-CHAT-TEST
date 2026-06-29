using System;
using System.Collections;
using AIGame.Base;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereMainDoorBehaviour : AIYandereDoorBehaviour
    {
        public Transform camTransform;
        private Action<YandereAreaType> _sendPosAct;

        public void SetSendPosAct(Action<YandereAreaType> act)
        {
            this._sendPosAct = act;
        }
        
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            camTransform = GameObjectEx.FindChildByName(transform,"camTransform" );
            IsCanClick = true;
        }

        public override void OnTouchClick()
        {
            if (IsCanClick && _checkStartGame(false))
            {
                AIGameSoundUtils.Inst.PlaySound(YandereConfig.YE_DoorLocked);
                _sendPosAct?.Invoke(YandereAreaType.OpenDoor);
                IsCanClick = false;
                StartCoroutine(WaitCanClick());
            }

        }

        IEnumerator WaitCanClick()
        {
            yield return new WaitForSeconds(5);
            IsCanClick = true;
        }

        public override void OnReset()
        {
            base.OnReset();
        }

        public override void OnNpcColliderHit()
        {
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Scene.EnterModelController;
using Message;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIHospital_EquipmentBehaviour : AIHospital_BaseInteractiveBehaviour
    {
        public Vector3 pos;
        public Vector3 rot;
        private string _shockEmoId = "40100461";
        
        
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIGameCommonComponent>();
            pos = dataComp.Pos;
            rot = dataComp.Rot;
            _curEmoteId = "40200460";
            MessageHelper.AddListener<string>(MessageName.AICancelEmote, OnCancelEmote);
        }
        
        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.AICancelEmote, OnCancelEmote);
        }
        
        private void OnCancelEmote(string emoId)
        {
            LoggerUtils.Log("AIHospital_EquipmentBehaviour EmoId = " + emoId);   
            if (_curEmoteId == emoId || _shockEmoId == emoId)
            {
                IsCanClick = true;
            }
        }
        
        public override void OnInteractive()
        {
            base.OnInteractive();
            if (!IsCanClick)
                return;

            IsCanClick = false;
            
            SelfPlayer_PlayInteractiveAnim(_curEmoteId, pos, rot);
            
            TimerManager.Inst.RunOnce("closeDoor", 5, () =>
            {
                if (UnityEngine.Random.value < 0.5f)
                {
                    RandomDoShotAnim();
                }
            });
        }
        
        private void RandomDoShotAnim()
        {
            SelfPlayer_PlayInteractiveAnim(_shockEmoId, pos, rot);
        }
    }
}

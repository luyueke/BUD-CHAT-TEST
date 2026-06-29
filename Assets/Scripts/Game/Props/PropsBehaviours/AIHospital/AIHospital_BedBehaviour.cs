using Game.Props.PropsComponents;
using Message;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIHospital_BedBehaviour : AIHospital_BaseInteractiveBehaviour
    {
        public Vector3 pos;
        public Vector3 rot;
        
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIGameCommonComponent>();
            pos = dataComp.Pos;
            rot = dataComp.Rot;
            _curEmoteId = "40200406";
            MessageHelper.AddListener<string>(MessageName.AICancelEmote, OnCancelEmote);
        }
        
        private void OnDestroy()
        {
            MessageHelper.RemoveListener<string>(MessageName.AICancelEmote, OnCancelEmote);
        }
        
        private void OnCancelEmote(string emoId)
        {
            if (_curEmoteId == emoId)
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
        }
    }
}

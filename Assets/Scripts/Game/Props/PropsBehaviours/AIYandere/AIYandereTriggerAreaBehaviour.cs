using System;
using Game.Props.PropsComponents;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereTriggerAreaBehaviour : AIPropBaseBehaviour
    {
        private Action<YandereAreaType> _sendPosAct;
        private int _curTag;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            var dataComp = this.entity.GetComp<AIYandereTriggerAreaComponent>();
            this._curTag = dataComp.Tag;
        }

        public void SetSendPosAct(Action<YandereAreaType> act)
        {
            this._sendPosAct = act;
        }
        
        public override void OnTrigEnter()
        {
            base.OnTrigEnter();
            //_sendPosAct?.Invoke((YandereAreaType)this._curTag);
        }
    }
}


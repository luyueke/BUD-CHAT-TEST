using System;

namespace Game.Props.PropsBehaviours
{
    public class AIYandereRunTargetPointBehaviour : AIPropBaseBehaviour
    {
        private Action _onTriEnterAct;

        public void SetAction(Action act)
        {
            this._onTriEnterAct = act;
        }
        
        public override void OnTrigEnter()
        {
            base.OnTrigEnter();
            this._onTriEnterAct?.Invoke();
            this._onTriEnterAct = null;
        }
    }
}


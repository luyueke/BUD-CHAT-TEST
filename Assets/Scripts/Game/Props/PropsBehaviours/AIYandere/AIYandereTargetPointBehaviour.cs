using System;
using Game.Base;
using Game.Props.PropsManagers;
using Game.Scene.EnterModelController;


namespace Game.Props.PropsBehaviours
{
    public class AIYandereTargetPointBehaviour : AIPropBaseBehaviour
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
        }
    }
}


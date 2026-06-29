using System;
using AIGame.Base;
using Es;
using Game.Avatar;
using Game.Base;
using Game.Scene.EnterModelController;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    //可交互AI道具的基类
    public class AIPark_BaseInteractiveBehaviour: AIPropBaseBehaviour
    {
        protected string _curEmoteId;
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            IsCanClick = true;
        }
        
        public override void OnTouchClick()
        {
            base.OnTouchClick();
            OnInteractive();
        }

        //  进行交互
        public virtual void OnInteractive()
        {
            
        }
    }
}
        

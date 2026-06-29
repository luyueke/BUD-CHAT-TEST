using Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIHospital_TargetPointBehaviour : AIPropBaseBehaviour
    {
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
        }

        public override void OnTrigEnter()
        {
            base.OnTrigEnter();
            MessageHelper.Broadcast(MessageName.OnS9TargetAreaTrigEnter);
        }

    }
}
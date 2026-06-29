
using Game.Base;
using UnityEngine;
using Game.Props.PropsManagers;
using Message;

namespace Game.Props.PropsBehaviours
{
    public class CameraLandMarkBehaviour : NodeBaseBehaviour
    {
        private MeshRenderer[] meshRenders;

        public override void OnInitByCreate()
        {
            meshRenders = GetComponentsInChildren<MeshRenderer>();
        }

        public void SetBoxVisiable(bool state)
        {
            if (meshRenders == null) return;
            foreach (var item in meshRenders)
            {
                item.enabled = state;
            }
        }

        public override void OnTrigEnter()
        {
            base.OnTrigEnter();
            GlobalNodeManager.Inst.Get<CameraLandMarkManager>()?.OnLandMarkTrigEnter(this);
            MessageHelper.Broadcast(MessageName.CameraLandMarkTrigEnter,true);
        }

        public override void OnTrigExit()
        {
            base.OnTrigExit();
            GlobalNodeManager.Inst.Get<CameraLandMarkManager>()?.OnLandMarkTrigExit(this);
            MessageHelper.Broadcast(MessageName.CameraLandMarkTrigEnter,false);
        }
    }
}
        

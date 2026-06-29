
using Game.Base;
using Game.Props.PropsManagers;
using Google.Protobuf.Collections;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class CombineBehaviour : MultiChildBehaviour
    {
        public void CombineSuccess()
        {
           
        }

        public override void CreateChildNodes(RepeatedField<PNodeData> nodes) {
            GlobalNodeManager.Inst.Get<CombineManager>().BuildChildNodes(this, nodes);
        }

 
    }
}
        

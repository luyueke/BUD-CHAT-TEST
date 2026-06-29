using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(WinReachEngFlagBehaviour))]
    public class WinReachEngFlagManager : BaseNodeManager
    {
        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
        {
            if (nodeBehaviour)
            {
                nodeBehaviour.transform.localScale = new Vector3(2, 2, 2);
            }
        }

        public WinReachEngFlagBehaviour GetReachEngFlagBehaviour()
        {
            if (entities is { Count: > 0 })
            {
                return entities[0] as WinReachEngFlagBehaviour;
            }

            return null;
        }
    }
}
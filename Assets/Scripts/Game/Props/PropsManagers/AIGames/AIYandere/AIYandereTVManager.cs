using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereTVBehaviour))]
    public class AIYandereTVManager: BaseNodeManager
    {
        public AIYandereTVBehaviour GetBev()
        {
            var npcBehaviour = entities[0] as AIYandereTVBehaviour;
            return npcBehaviour;
        }
    }
}
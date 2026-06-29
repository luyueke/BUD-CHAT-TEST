using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereComputerBehaivour))]
    public class AIYandereComputerManager : BaseNodeManager
    {
        public AIYandereComputerBehaivour GetBev()
        {
            var npcBehaviour = entities[0] as AIYandereComputerBehaivour;
            return npcBehaviour;
        }
    }
}
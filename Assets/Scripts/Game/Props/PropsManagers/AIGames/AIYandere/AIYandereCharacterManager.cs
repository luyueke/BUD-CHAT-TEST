using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereCharacterBehaviour))]
    public class AIYandereCharacterManager : BaseNodeManager
    {
        public AIYandereCharacterBehaviour GetNpcBev()
        {
            var npcBehaviour = entities[0] as AIYandereCharacterBehaviour;
            return npcBehaviour;
        }
    }
}
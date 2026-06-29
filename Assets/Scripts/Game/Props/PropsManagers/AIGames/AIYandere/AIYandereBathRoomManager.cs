using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereBathRoomBehaivour))]
    public class AIYandereBathRoomManager : BaseNodeManager
    {
        public AIYandereBathRoomBehaivour GetBev()
        {
            var npcBehaviour = entities[0] as AIYandereBathRoomBehaivour;
            return npcBehaviour;
        }
    }
}
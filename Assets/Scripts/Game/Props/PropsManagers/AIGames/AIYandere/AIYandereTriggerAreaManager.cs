using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereTriggerAreaBehaviour))]
    public class AIYandereTriggerAreaManager : BaseNodeManager
    {
        public List<AIYandereTriggerAreaBehaviour> GetTriggerAreaBevs()
        {
            List<AIYandereTriggerAreaBehaviour> list = new List<AIYandereTriggerAreaBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIYandereTriggerAreaBehaviour);
            });
            return list;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIYandereTargetPointBehaviour))]
    public class AIYandereTargetPointManager : BaseNodeManager
    {
        public AIYandereTargetPointBehaviour GetargetPointBev()
        {
            return entities[0] as AIYandereTargetPointBehaviour;
        }
    }
    
    [NodeBehaviourAttribute(typeof(AIYandereRunTargetPointBehaviour))]
    public class AIYandereRunTargetManager : BaseNodeManager
    {
        public AIYandereRunTargetPointBehaviour GetRunTargetPointBev()
        {
            return entities[0] as AIYandereRunTargetPointBehaviour;
        }
    }
}


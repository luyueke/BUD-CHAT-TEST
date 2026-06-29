using System.Collections.Generic;
using System.Linq;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereSofaBehaviour))]
    public class AIYandereSofaManager : BaseNodeManager
    {
        public List<AIYandereSofaBehaviour> GetSofaBevs()
        {
            List<AIYandereSofaBehaviour> list = new List<AIYandereSofaBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIYandereSofaBehaviour);
            });
            return list;
        }
    }
}
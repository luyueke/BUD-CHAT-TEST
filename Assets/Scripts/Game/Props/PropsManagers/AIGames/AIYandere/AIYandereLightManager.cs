using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UIAgent;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereLightBehaviour))]
    public class AIYandereLightManager: BaseNodeManager
    {
        public List<AIYandereLightBehaviour> GetSofaBevs()
        {
            List<AIYandereLightBehaviour> list = new List<AIYandereLightBehaviour>();
            entities.ToList().ForEach((x) =>
            {
                list.Add(x as AIYandereLightBehaviour);
            });
            return list;
        }
    }
}
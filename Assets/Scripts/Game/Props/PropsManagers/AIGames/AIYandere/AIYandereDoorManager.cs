using System.Collections.Generic;
using System.Linq;
using Game.Base;
using Game.Props.PropsBehaviours;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(AIYandereDoorBehaviour))]
	public class AIYandereDoorManager : BaseNodeManager
	{
		public List<AIYandereDoorBehaviour> GetDoorBevs()
		{
			List<AIYandereDoorBehaviour> list = new List<AIYandereDoorBehaviour>();
			entities.ToList().ForEach((x) =>
			{
				list.Add(x as AIYandereDoorBehaviour);
			});
			return list;
		}
	}
}

namespace Game.Props.PropsManagers
{
	[NodeBehaviourAttribute(typeof(AIYandereMainDoorBehaviour))]
	public class AIYandereMainDoorManager : BaseNodeManager
	{
		public AIYandereMainDoorBehaviour GetYandereMainDoorBev()
		{
			var mainDoorBev = entities[0] as AIYandereMainDoorBehaviour;
			return mainDoorBev;
		}
	}
}


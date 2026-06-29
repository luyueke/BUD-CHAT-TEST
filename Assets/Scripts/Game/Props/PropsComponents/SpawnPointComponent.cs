
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class SpawnPointComponent : BaseComponent, IComponentSerializer
	{
		public int SpawnIndex;
		public int SpawnDefault;
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.Is(PSpawnPointComponentData.Descriptor))
            {
	            var spawnComponentData = componentData.CmpData.Unpack<PSpawnPointComponentData>();
	            SpawnDefault = spawnComponentData.SpawnDefault;
	            SpawnIndex = spawnComponentData.SpawnIndex;
            }
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pSpawnPointComponentData = new PSpawnPointComponentData();
			pSpawnPointComponentData.SpawnDefault = SpawnDefault;
			pSpawnPointComponentData.SpawnIndex = SpawnIndex;
			componentData.CmpData = Any.Pack(pSpawnPointComponentData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new SpawnPointComponent()
			{
				SpawnDefault = SpawnDefault,
				SpawnIndex = SpawnIndex
			};
			return component;
		}
	}
}
        


using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class TrapSpawnComponent : BaseComponent, IComponentSerializer
	{
		public uint BoxId = 0;//Box uid
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PTrapSpawnComponent>(out var pbBodyData))
			{
				BoxId = pbBodyData.BoxId;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PTrapSpawnComponent();
			var componentData = new PComponentData();
			pbBodyData.BoxId = BoxId;
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new TrapSpawnComponent()
			{
			};
			return component;
		}
	}
}
        

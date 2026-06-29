
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class PGCPlantComponent : BaseComponent, IComponentSerializer
	{
		public Color Color = Color.white;
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PGCPlantCompData>(out var pbBodyData))
			{
				Color = pbBodyData.Color.ToColor();
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PGCPlantCompData();
			pbBodyData.Color = Color.ToPB();
			var componentData = new PComponentData();
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new PGCPlantComponent()
			{
				Color = Color
			};
			return component;
		}
	}
}
        

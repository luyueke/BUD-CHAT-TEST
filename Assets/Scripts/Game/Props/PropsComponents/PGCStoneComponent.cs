
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class PGCStoneComponent : BaseComponent, IComponentSerializer
	{
		public Color Color = Color.white;
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PPGCStoneCompData>(out var pbBodyData))
			{
				Color = pbBodyData.Color.ToColor();
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PPGCStoneCompData();
			pbBodyData.Color = Color.ToPB();
			var componentData = new PComponentData();
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new PGCStoneComponent()
			{
				Color = Color
			};
			return component;
		}
	}
}
        

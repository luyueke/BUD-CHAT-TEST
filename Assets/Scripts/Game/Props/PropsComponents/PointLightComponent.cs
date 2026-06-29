
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class PointLightComponent : BaseComponent, IComponentSerializer
	{
		
		public float intensity = 2.25f;
		public float range = 9;
		public Color color = Color.white;
		
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData != null &&  componentData.CmpData.Is(PPointLightComponentData.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PPointLightComponentData>(out var pbBodyData))
				{
					intensity = pbBodyData.Intensity;
					range = pbBodyData.Range;
					color = pbBodyData.Color.ToColor();
				}
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pPointLightComponentData = new PPointLightComponentData()
			{
				Intensity = intensity,
				Range = range,
				Color = color.ToPB()
			};
			componentData.CmpData = Any.Pack(pPointLightComponentData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new PointLightComponent()
			{
				intensity = intensity,
				range = range,
				color = color,
			};
			return component;
		}
	}
}
        

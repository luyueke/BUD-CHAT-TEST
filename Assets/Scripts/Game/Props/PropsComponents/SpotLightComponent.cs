
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class SpotLightComponent : BaseComponent, IComponentSerializer
	{


		public float intensity = 4;
		public float range = 8.25f;
		public Color color = Color.white;
		public float angle = 76.5f;
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData != null &&  componentData.CmpData.Is(PSpotLightComponentData.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PSpotLightComponentData>(out var pbBodyData))
				{
					intensity = pbBodyData.Intensity;
					range = pbBodyData.Range;
					color = pbBodyData.Color.ToColor();
					angle = pbBodyData.Angle;
				}
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pSpotLightComponentData = new PSpotLightComponentData()
			{
				Intensity = intensity,
				Range = range,
				Color = color.ToPB(),
				Angle = angle
			};
			componentData.CmpData = Any.Pack(pSpotLightComponentData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new SpotLightComponent()
			{
				intensity = intensity,
				range = range,
				color = color,
				angle = angle,
			};
			return component;
		}
	}
}
        

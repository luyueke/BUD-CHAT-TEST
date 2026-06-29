
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class DirLightComponent : BaseComponent, IComponentSerializer
	{

		public float intensity;

		public float elevation;
		public float direction;

		public Color lightColor;
		
		// public string 
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData != null &&  componentData.CmpData.Is(PDirLightData.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PDirLightData>(out var pbBodyData))
				{
					intensity = pbBodyData.Intensity;
					elevation = pbBodyData.Elevation;
					direction = pbBodyData.Direction;
					lightColor = pbBodyData.LightColor.ToColor();
				}
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pDirLightData = new PDirLightData()
			{
				Intensity = intensity,
				Elevation = elevation,
				Direction = direction,
				LightColor = lightColor.ToPB()
			};
			componentData.CmpData = Any.Pack(pDirLightData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new DirLightComponent()
			{
				intensity = intensity,
				elevation = elevation,
				direction = direction,
				lightColor = lightColor
			};
			return component;
		}
	}
}
        

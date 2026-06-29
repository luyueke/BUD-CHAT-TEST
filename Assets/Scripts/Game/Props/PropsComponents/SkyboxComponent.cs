
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{

	public enum SkyboxType
	{
		Normal = 0, //普通天空盒
		DayNight = 1, //昼夜天空盒
		Custom = 2, // 自定义天空盒
	}
	
	[System.Serializable]
	public class SkyboxColor
	{
		public Color sky;
		public Color equator;
		public Color ground;
	}

	public class SkyboxComponent : BaseComponent, IComponentSerializer
	{
		public string skyboxId = "10000";
		public SkyboxColor skyboxColor;
		
		
		public int type; // 1 - gradient, 3 - color 
		public string scol;
		public string ecol;
		public string gcol;

		public SkyboxType skyboxType;
		public int dayLength;
		public int daytimeHour;
		public int daytimeMin;

		public string ucol;
		public string mcol;
		public string hcol;
		public float horizonPosition;
		public float gradientLength;

		public bool starLayer;
		// 0:None  1:Moon  2:Sun
		public int skyObjects;
		public int moonTexture;
		public string moonColor;
		public float moonSize;
		public float moonTransparency;
		public float moonEdgeFeathering;
		public float moonBloomIntensity;
		public string moonLightColor;
		public float moonLightIntensity;
		public float moonPositionH;
		public float moonPositionV;

		public string sunColor;
		public float sunSize;
		public float sunTransparency;
		public float sunEdgeFeathering;
		public float sunBloomIntensity;
		public string sunLightColor;
		public float sunLightIntensity;
		public float sunPositionH;
		public float sunPositionV;
		
		
		
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.Is(PSkyboxComponentData.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PSkyboxComponentData>(out var pbBodyData))
				{
					skyboxId = pbBodyData.SkyboxId;
					skyboxColor = new SkyboxColor
					{
						sky = default,
						equator = default,
						ground = default
					};
					if (pbBodyData.SkyColor != null)
					{
						skyboxColor.sky = pbBodyData.SkyColor.ToColor();
					}
					if (pbBodyData.EquatorColor != null)
					{
						skyboxColor.equator = pbBodyData.EquatorColor.ToColor();
					}
					if (pbBodyData.GroundColor != null)
					{
						skyboxColor.ground = pbBodyData.GroundColor.ToColor();
					}
				}
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pSkyboxComponentData = new PSkyboxComponentData
			{
				SkyboxId = skyboxId,
				SkyColor = skyboxColor.sky.ToPB(),
				EquatorColor = skyboxColor.equator.ToPB(),
				GroundColor = skyboxColor.ground.ToPB(),
			};
			componentData.CmpData = Any.Pack(pSkyboxComponentData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new SkyboxComponent()
			{
				skyboxId = skyboxId
			};
			return component;
		}
	}
}
        

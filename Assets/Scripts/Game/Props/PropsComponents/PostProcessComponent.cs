
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class PostProcessComponent : BaseComponent, IComponentSerializer
	{
		public int BloomState = 0;
		public float BloomIntensity = 0;
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PPostProcessData>(out var pbBodyData))
			{
				BloomState = pbBodyData.BloomState;
				BloomIntensity = pbBodyData.BloomIntensity;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PPostProcessData();
			var componentData = new PComponentData();
			pbBodyData.BloomState = BloomState;
			pbBodyData.BloomIntensity = BloomIntensity;
			componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new PostProcessComponent()
			{
				BloomState = BloomState,
				BloomIntensity = BloomIntensity
			};
			return component;
		}
	}
}
        

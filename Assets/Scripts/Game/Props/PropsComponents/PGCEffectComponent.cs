
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class PGCEffectComponent : BaseComponent, IComponentSerializer
	{
		public Color Color = Color.white;
		public int PlaySound = 0;
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PGCEffectCompData>(out var pbBodyData))
			{
				Color = pbBodyData.Color.ToColor();
				PlaySound = pbBodyData.PlaySound;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PGCEffectCompData();
			pbBodyData.Color = Color.ToPB();
			pbBodyData.PlaySound = PlaySound;
			var componentData = new PComponentData();
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new PGCEffectComponent()
			{
				Color = Color,
				PlaySound = PlaySound
			};
			return component;
		}
	}
}
        

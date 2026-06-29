
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class ActiveCtrComponent : BaseComponent, IComponentSerializer
	{
		public int DefaultHide = 0;
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PActiveCtrComponentData>(out var pbBodyData))
			{
				DefaultHide = pbBodyData.DefaultHide;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PActiveCtrComponentData();
			var componentData = new PComponentData();
			pbBodyData.DefaultHide = DefaultHide;
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new ActiveCtrComponent();
			component.DefaultHide = DefaultHide;
			return component;
		}
	}
}
        

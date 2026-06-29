
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class LikeButtonComponent : BaseComponent, IComponentSerializer
	{
		public string ShowText = "";
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PLikeButtonComponent>(out var pbBodyData))
			{
				ShowText = pbBodyData.ShowText;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PLikeButtonComponent();
			var componentData = new PComponentData();
			pbBodyData.ShowText = ShowText;
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new LikeButtonComponent()
			{
				ShowText = ShowText
			};
			return component;
		}
	}
}
        

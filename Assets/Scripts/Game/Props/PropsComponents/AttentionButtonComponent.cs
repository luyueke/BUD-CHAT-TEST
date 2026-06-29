
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents
{
	public class AttentionButtonComponent : BaseComponent, IComponentSerializer
	{
		public string ShowText = "";
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PAttentionButtonComponent>(out var pbBodyData))
			{
				ShowText = pbBodyData.ShowText;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PAttentionButtonComponent();
			var componentData = new PComponentData();
			pbBodyData.ShowText = ShowText;
			componentData.CmpData = Any.Pack(pbBodyData);
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new AttentionButtonComponent()
			{
				ShowText = ShowText
			};
			return component;
		}
	}
}
        


using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class DTextComponent : BaseComponent, IComponentSerializer
	{
		public string Content = "请输入文字";
		public Color TextColor = Color.white;

		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PDTextComponentData>(out var pbBodyData))
            {
				Content = pbBodyData.Content;
				TextColor = pbBodyData.TextColor.ToColor();
            }
		}

		public PComponentData Write()
		{
			var pbBodyData = new PDTextComponentData();
			pbBodyData.Content = Content;
			pbBodyData.TextColor = TextColor.ToPB();

			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new DTextComponent()
			{
				Content = Content,
				TextColor = TextColor
			};
			return component;
		}
	}
}


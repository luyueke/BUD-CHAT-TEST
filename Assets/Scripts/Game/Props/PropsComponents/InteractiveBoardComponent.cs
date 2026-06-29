
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class InteractiveBoardComponent : BaseComponent, IComponentSerializer {
        public string EmoteId = "40200053";
		public string ShowText = "";
        public string EmoteName = "坐下";

		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PInteractiveBoardComponent>(out var pbBodyData))
			{
				EmoteId = pbBodyData.EmoteId;
				ShowText = pbBodyData.ShowText;
                EmoteName = pbBodyData.EmoteName;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PInteractiveBoardComponent();
			var componentData = new PComponentData();
			pbBodyData.EmoteId = EmoteId;
			pbBodyData.ShowText = ShowText;
            pbBodyData.EmoteName = EmoteName;
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new InteractiveBoardComponent()
			{
				EmoteId = EmoteId,
				ShowText = ShowText,
                EmoteName = EmoteName,
			};
			return component;
		}
	}
}


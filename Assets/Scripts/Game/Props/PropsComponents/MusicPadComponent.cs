
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using System.Collections.Generic;

namespace Game.Props.PropsComponents
{
	public class MusicPadComponent : BaseComponent, IComponentSerializer
	{
		public List<string> KeyIds = new List<string>(){"0","0","0"}; // 左 中 右

		public void Read(PComponentData componentData)
		{
			KeyIds.Clear();
			if (componentData.CmpData.TryUnpack<PMusicPadComponentData>(out var pbBodyData))
            {
				KeyIds.AddRange(pbBodyData.KeyIds);
            }
		}

		public PComponentData Write()
		{
			var pbBodyData = new PMusicPadComponentData();
			pbBodyData.KeyIds.AddRange(KeyIds);
            
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new MusicPadComponent()
			{
				KeyIds = new List<string>(KeyIds),
			};
			return component;
		}
	}
}
        

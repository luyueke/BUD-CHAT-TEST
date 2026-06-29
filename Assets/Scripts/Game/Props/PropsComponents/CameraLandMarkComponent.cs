
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class CameraLandMarkComponent : BaseComponent, IComponentSerializer
	{

		public string ShowText = "";
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<CameraLandMarkComponentData>(out var pbBodyData))
			{
				ShowText = pbBodyData.ShowText;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new CameraLandMarkComponentData();
			pbBodyData.ShowText = ShowText;
            
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new CameraLandMarkComponent()
			{
				ShowText = ShowText,
			};
			return component;
		}
	}
}
        

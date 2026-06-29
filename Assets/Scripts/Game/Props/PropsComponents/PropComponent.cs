
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class PropComponent : BaseComponent, IComponentSerializer
	{

		public string uItemId = "";
        public Vector3 anchor = Vector3.zero;
        public Vector3 scale = Vector3.one;

		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData != null &&  componentData.CmpData.Is(PUGCItemComponentData.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PUGCItemComponentData>(out var pbBodyData))
				{
					uItemId = pbBodyData.UItemId;
                    anchor = pbBodyData.Anchor.ToVector3();
                    scale = pbBodyData.Scale?.ToVector3() ?? Vector3.one;
                }
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pUGCItemComponentData = new PUGCItemComponentData()
			{
				UItemId = uItemId,
                Anchor = anchor.ToPB(),
                Scale = scale.ToPB(),
			};
			componentData.CmpData = Any.Pack(pUGCItemComponentData);
			componentData.CmpId = (uint)GameTypeRegister.Inst.GetComponentId(GetType());
			return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new PropComponent()
			{
				uItemId = uItemId,
                anchor = anchor,
                scale = scale
			};
			return component;
		}
	}
}



using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class PortalButtonComponent : BaseComponent, IComponentSerializer
	{
		public uint PointUid; //光柱Uid

		// 临时变量
		public Vector3? TmpOriginPostion = null;
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PPortalButtonComponentData>(out var pbBodyData))
            {
				PointUid = pbBodyData.PointUid;
            }
		}

		public PComponentData Write()
		{
			var pbBodyData = new PPortalButtonComponentData();
            pbBodyData.PointUid = PointUid;
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new PortalButtonComponent()
			{
			};
			return component;
		}
	}
}
        

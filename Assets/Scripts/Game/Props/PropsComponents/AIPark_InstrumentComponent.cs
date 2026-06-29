
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class AIPark_InstrumentComponent : BaseComponent, IComponentSerializer
	{
		public void Read(PComponentData componentData)
		{
			// if (componentData.CmpData.TryUnpack<${PBData}>(out var pbBodyData))
            // {

            // }
		}

		public PComponentData Write()
		{
			// var pbBodyData = new ${PBData}();
            
			var componentData = new PComponentData();
            // componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new AIPark_InstrumentComponent()
			{
			};
			return component;
		}
	}
}
        

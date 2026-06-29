
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class TransactionComponent : BaseComponent, IComponentSerializer
	{
		
		public bool isTransaction = true;
		
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData != null &&  componentData.CmpData.Is(PTransactionComponentData.Descriptor))
			{
				if (componentData.CmpData.TryUnpack<PTransactionComponentData>(out var pbBodyData))
				{
					isTransaction = pbBodyData.IsTransaction;
				}
			}
		}

		public PComponentData Write()
		{
			var componentData = new PComponentData();
			var pbBodyData = new PTransactionComponentData
			{
				IsTransaction = isTransaction
			};
			componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new TransactionComponent()
			{
				isTransaction = isTransaction
			};
			return component;
		}
	}
}
        

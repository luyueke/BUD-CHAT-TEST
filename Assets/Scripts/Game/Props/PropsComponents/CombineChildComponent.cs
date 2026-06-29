
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class CombineChildComponent : BaseComponent
	{
		
		public override BaseComponent Clone()
		{
			var component = new CombineChildComponent()
			{
			};
			return component;
		}
	}
}
        

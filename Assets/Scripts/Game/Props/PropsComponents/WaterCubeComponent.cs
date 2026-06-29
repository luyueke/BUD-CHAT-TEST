
using Game.Base;
using Game.ECS;
using Game.Props.PropsManagers;
using GameData;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class WaterCubeComponent : BaseComponent, IComponentSerializer
	{
		public int WaterId = 1;
		public PropModelShape ModelShape = PropModelShape.Cube;
		public Vector2 Tile = Vector2.one;
		public float Speed = WaterCubeManager.SlowSpeed;
		public int OxygenType = 0;
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PWaterCubeComponentData>(out var pbBodyData))
			{
				WaterId = pbBodyData.WaterId;
				ModelShape = (PropModelShape)pbBodyData.ModelShape;
				Speed = pbBodyData.Speed;
				Tile = pbBodyData.Tile.ToVector2();
				OxygenType = pbBodyData.OxygenType;
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PWaterCubeComponentData();
			pbBodyData.WaterId = WaterId;
			pbBodyData.ModelShape = (int)ModelShape;
			pbBodyData.Speed = Speed;
			pbBodyData.Tile = Tile.ToPB();
			pbBodyData.OxygenType = OxygenType;
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new WaterCubeComponent()
			{
				WaterId = WaterId,
				ModelShape = ModelShape,
				Speed = Speed,
				Tile = Tile,
				OxygenType = OxygenType,
			};
			return component;
		}
	}
}
        

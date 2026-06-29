
using Game.Base;
using Game.ECS;
using GameData;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
	public class IceCubeComponent : BaseComponent, IComponentSerializer
	{
		public PropModelShape ModelShape;
		public Vector2 Tile = Vector2.one;
		
		public void Read(PComponentData componentData)
		{
			if (componentData.CmpData.TryUnpack<PIceCubeComponentData>(out var pbBodyData))
			{
				ModelShape = (PropModelShape)pbBodyData.Shape;
				Tile = pbBodyData.Tile.ToVector2();
			}
		}

		public PComponentData Write()
		{
			var pbBodyData = new PIceCubeComponentData();
			pbBodyData.Shape = (int)ModelShape;
			pbBodyData.Tile = Tile.ToPB();
			var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
		}

		public override BaseComponent Clone()
		{
			var component = new IceCubeComponent()
			{
				ModelShape = ModelShape,
				Tile = Tile
			};
			return component;
		}
	}
}
        

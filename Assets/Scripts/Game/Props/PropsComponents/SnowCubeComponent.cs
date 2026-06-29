using Game.Base;
using Game.ECS;
using GameData;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
    public class SnowCubeComponent : BaseComponent, IComponentSerializer
    {
        public Color Color = Color.white;
        public PropModelShape ModelShape;
        public Vector2 Tile = Vector2.one;

        public void Read(PComponentData componentData)
        {
            if (componentData.CmpData.TryUnpack<PSnowCubeComponentData>(out var pbBodyData))
            {
                ModelShape = (PropModelShape)pbBodyData.Shape;
                Tile = pbBodyData.Tile.ToVector2();
                Color = pbBodyData.Color.ToColor();
            }
        }

        public PComponentData Write()
        {
            var pbBodyData = new PSnowCubeComponentData();
            pbBodyData.Shape = (int)ModelShape;
            pbBodyData.Tile = Tile.ToPB();
            pbBodyData.Color = Color.ToPB();
            var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
        }

        public override BaseComponent Clone()
        {
            var component = new SnowCubeComponent()
            {
                ModelShape = ModelShape,
                Tile = Tile,
                Color = Color,
            };
            return component;
        }
    }
}
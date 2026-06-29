using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents {
    public class TerrainComponent : BaseComponent, IComponentSerializer {

        public bool IsVisible = true; // 是否可见
        public float size = 5;

        public bool isMigrated;


        public void Read(PComponentData componentData) {
            if (componentData.CmpData.TryUnpack<PTerrainComponentData>(out var pbBodyData)) {
                IsVisible = pbBodyData.IsVisible;
                size = pbBodyData.Size;
                isMigrated = pbBodyData.IsMigrated;
                if (size < float.Epsilon) {
                    size = 5;
                }
            }
        }

        public PComponentData Write() {
            var pbBodyData = new PTerrainComponentData();
            pbBodyData.IsVisible = IsVisible;
            pbBodyData.Size = size;
            pbBodyData.IsMigrated = isMigrated;

            var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
        }

        public override BaseComponent Clone() {
            var component = new TerrainComponent() {
                IsVisible = IsVisible
            };
            return component;
        }
    }
}

using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents {
    public class PreviewModelComponent : BaseComponent, IComponentSerializer {
        public Color color = Color.white;


        public void Read(PComponentData componentData) {
            if (componentData.CmpData != null && componentData.CmpData.TryUnpack<PPreviewModelComponent>(out var pbBodyData)) {
                color = pbBodyData.Color.ToColor();
            }
        }

        public PComponentData Write() {
            var pbBodyData = new PPreviewModelComponent();
            pbBodyData.Color = color.ToPB();
            var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
        }

        public override BaseComponent Clone() {
            var component = new PreviewModelComponent() {
                color = color
            };
            return component;
        }
    }
}

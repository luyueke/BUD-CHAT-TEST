
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents
{
    public class TheatreTriggerComponent : BaseComponent, IComponentSerializer
    {
        public int TriggerType;    // 0=Theatre, 1=Actor, 2=Trigger
        public string TheatreId = "";
        public string TheatreName = "";
        public string TheatreCover = "";
        public string ActorId = "";
        public string ActorName = "";
        public int ClothesIndex;
        public string ClothesName = "";

        public void Read(PComponentData componentData)
        {
            if (componentData.CmpData == null) return;
            if (!componentData.CmpData.Is(TheatreTriggerComponentData.Descriptor)) return;
            var d = componentData.CmpData.Unpack<TheatreTriggerComponentData>();
            TriggerType = d.TriggerType;
            TheatreId = d.TheatreId ?? "";
            TheatreName = d.TheatreName ?? "";
            TheatreCover = d.TheatreCover ?? "";
            ActorId = d.ActorId ?? "";
            ActorName = d.ActorName ?? "";
            ClothesIndex = d.ClothesIndex;
            ClothesName = d.ClothesName ?? "";
        }

        public PComponentData Write()
        {
            var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(new TheatreTriggerComponentData
            {
                TriggerType = TriggerType,
                TheatreId = TheatreId,
                TheatreName = TheatreName,
                TheatreCover = TheatreCover,
                ActorId = ActorId,
                ActorName = ActorName,
                ClothesIndex = ClothesIndex,
                ClothesName = ClothesName,
            });
            return componentData;
        }

        public override BaseComponent Clone()
        {
            return new TheatreTriggerComponent
            {
                TriggerType = TriggerType,
                TheatreId = TheatreId,
                TheatreName = TheatreName,
                TheatreCover = TheatreCover,
                ActorId = ActorId,
                ActorName = ActorName,
                ClothesIndex = ClothesIndex,
                ClothesName = ClothesName,
            };
        }
    }
}

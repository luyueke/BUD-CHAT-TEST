using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
    public class AIYandereTriggerAreaComponent : BaseAIComponent
    {
        public int Tag;
        public override void Read(PComponentData componentData)
        {
            if (componentData.CmpData.Is(AIYandereTriggerAreaComponentData.Descriptor))
            {
                var aiYandereTriggerAreaComponent = componentData.CmpData.Unpack<AIYandereTriggerAreaComponentData>();
                Tag = aiYandereTriggerAreaComponent.Tag;
            }
        }
    }
}


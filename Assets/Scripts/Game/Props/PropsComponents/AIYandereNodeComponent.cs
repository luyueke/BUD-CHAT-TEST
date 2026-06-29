using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
    public class AIYandereNodeComponent: BaseAIComponent
    {
        public string nodeName;
     
        public override void Read(PComponentData componentData)
        {
            if (componentData.CmpData.Is(AIYandereNodeComponentData.Descriptor))
            {
                var aiYandereTriggerAreaComponent = componentData.CmpData.Unpack<AIYandereNodeComponentData>();
                nodeName = aiYandereTriggerAreaComponent.NodeName;
            }
        }
    }
}
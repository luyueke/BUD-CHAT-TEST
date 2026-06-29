using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
    public class AIYandereCommonComponent: BaseAIComponent
    {
        public int Tag;
        public int Index;
        public Vector3 Pos;
        public Vector3 Rot;
        public override void Read(PComponentData componentData)
        {
            if (componentData.CmpData.Is(AIYandereCommonComponentData.Descriptor))
            {
                var aiYandereTriggerAreaComponent = componentData.CmpData.Unpack<AIYandereCommonComponentData>();
                Tag = aiYandereTriggerAreaComponent.Tag;
                Pos = DataUtil.DeSerializeVector3(aiYandereTriggerAreaComponent.Pos);
                Rot = DataUtil.DeSerializeVector3(aiYandereTriggerAreaComponent.Rot);
                Index =  aiYandereTriggerAreaComponent.Index;
            }
        }
    }
}
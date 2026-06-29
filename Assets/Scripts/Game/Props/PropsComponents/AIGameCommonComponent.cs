using Pb.Map;
using UnityEngine;
using System.Collections.Generic;

namespace Game.Props.PropsComponents
{
    public class AIGameCommonComponent: BaseAIComponent
    {
        public int Tag;
        public int Index;
        public Vector3 Pos;
        public Vector3 Rot;
        public int location;
        public string hexColor;
        public IList<int> neighbor;
        public override void Read(PComponentData componentData)
        {
            if (componentData.CmpData.Is(AIGameCommonComponentData.Descriptor))
            {
                var aiYandereTriggerAreaComponent = componentData.CmpData.Unpack<AIGameCommonComponentData>();
                Tag = aiYandereTriggerAreaComponent.Tag;
                Pos = DataUtil.DeSerializeVector3(aiYandereTriggerAreaComponent.Pos);
                Rot = DataUtil.DeSerializeVector3(aiYandereTriggerAreaComponent.Rot);
                Index =  aiYandereTriggerAreaComponent.Index;
                location = aiYandereTriggerAreaComponent.Location;
                hexColor = aiYandereTriggerAreaComponent.Color;
                neighbor = aiYandereTriggerAreaComponent?.Neighbor;
            }
        }
    }
}
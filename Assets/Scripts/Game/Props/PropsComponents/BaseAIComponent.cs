using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
    public class BaseAIComponent : BaseComponent, IComponentSerializer
    {
        public virtual void Read(PComponentData componentData)
        {
            // 获取 CmpData 中的 Value 字段，它是一个字节数组
            byte[] valueBytes = componentData.CmpData.Value.ToByteArray();

            // 将字节数组转为原始的 JSON 字符串
            string jsonString = System.Text.Encoding.UTF8.GetString(valueBytes);

            // 输出原始的 JSON 字符串（可选）
            Debug.LogError("Decoded JSON: " + jsonString);
            
        }

        public PComponentData Write()
        {
            var componentData = new PComponentData();
            return componentData;
        }

        public override BaseComponent Clone()
        {
            return null;
        }
    }
}


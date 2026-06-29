using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;
using UnityEngine;

namespace Game.Props.PropsComponents
{
    public class CollectStarComponent : BaseComponent, IComponentSerializer
    {
        public int Id; //序号
        public string StarName; //用户配置的星星名字

        public bool IsCollect = false; // runtime字段

        public void Read(PComponentData componentData)
        {
            if (componentData.CmpData.TryUnpack<PCollectAllStarComponentData>(out var pbBodyData))
            {
                Id = pbBodyData.Id;
                StarName = pbBodyData.StarName;
            }
        }

        public PComponentData Write()
        {
            var pbBodyData = new PCollectAllStarComponentData();
            pbBodyData.Id = Id;
            pbBodyData.StarName = StarName;
            var componentData = new PComponentData();
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
        }

        public override BaseComponent Clone()
        {
            var component = new CollectStarComponent()
            {
                Id = Id,
                StarName = StarName,
                IsCollect = false,
            };
            return component;
        }
    }
}
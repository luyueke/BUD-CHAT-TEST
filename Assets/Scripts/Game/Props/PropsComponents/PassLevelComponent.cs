using System;
using Game.Base;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents
{
    public class PassLevelComponent : BaseComponent, IComponentSerializer
    {
        public int WinConditionId = 0;
        public int GameDuration = 0;
        public int MaxHp = 0;
        public string GameHint = String.Empty;

        public void Read(PComponentData componentData)
        {
            if (componentData.CmpData.TryUnpack<PPassLevelComponentData>(out var pbBodyData))
            {
                WinConditionId = pbBodyData.WinCondition;
                GameDuration = pbBodyData.GameDuration;
                MaxHp = pbBodyData.MaxHp;
                GameHint = pbBodyData.GameHint;
            }
        }

        public PComponentData Write()
        {
            var pbBodyData = new PPassLevelComponentData();
            var componentData = new PComponentData();
            pbBodyData.WinCondition = WinConditionId;
            pbBodyData.GameDuration = GameDuration;
            pbBodyData.MaxHp = MaxHp;
            pbBodyData.GameHint = GameHint;
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
        }

        public override BaseComponent Clone()
        {
            var component = new PassLevelComponent()
            {
                WinConditionId = WinConditionId,
                GameDuration = GameDuration,
                MaxHp = MaxHp,
                GameHint = GameHint,
            };
            return component;
        }
    }
}
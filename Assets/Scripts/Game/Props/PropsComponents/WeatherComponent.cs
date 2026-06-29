using Game.Base;
using Game.Config;
using Game.ECS;
using Google.Protobuf.WellKnownTypes;
using Pb.Map;

namespace Game.Props.PropsComponents
{
    public class WeatherComponent :  BaseComponent, IComponentSerializer
    {
        public GameGlobalEnum.WeatherType WeatherType;
        
        public void Read(PComponentData componentData)
        {
            if (componentData.CmpData.TryUnpack<PWeatherData>(out var pbBodyData))
            {
                WeatherType = (GameGlobalEnum.WeatherType)pbBodyData.WeatherType;
            }
        }

        public PComponentData Write()
        {
            var pbBodyData = new PWeatherData();
            var componentData = new PComponentData();
            pbBodyData.WeatherType = (int)WeatherType;
            componentData.CmpData = Any.Pack(pbBodyData);
            return componentData;
        }
        
        public override BaseComponent Clone()
        {
            return null;
        }
    }
}

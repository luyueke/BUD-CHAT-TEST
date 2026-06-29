using Game.Base;
using Game.Props.PropsComponents;
using GameData.BaseInfo;
using GameData.Manager;

namespace Game.MapSetting {
    public class GameDurationManager : BaseMapSettingManager<GameDurationManager> {

        public int DurationLimit {
            get => GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().gameSetting.limitDuration;
            set => GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().gameSetting.limitDuration = value;
        }
    }
}

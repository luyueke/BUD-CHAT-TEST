using Game.Base;
using Game.Props.PropsComponents;
using GameData.BaseInfo;
using GameData.Manager;

namespace Game.MapSetting {
    public class HpManager : BaseMapSettingManager<HpManager> {


        public int HpLimit {
            get => GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().gameSetting.limitHp;
            set => GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>().gameSetting.limitHp = value;
        }


    }
}

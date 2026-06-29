using System;
using Game.Base;
using Game.Props.PropsComponents;
using GameData.Manager;
using GameData.BaseInfo;
using UnityEngine;

namespace Game.MapSetting
{
    public enum PassLevelStatus
    {
        NotStart,
        Running,
        Wait,
    }

    public class PassLevelDataManager : BaseMapSettingManager<PassLevelDataManager>
    {
        private PassLevelComponent component;

        public Action OnMapDataInit;

        #region Runtime通关数据

        //当前通关状态
        public PassLevelStatus CurStatus = PassLevelStatus.NotStart;

        #endregion


        //地图还原数据
        public override void OnCreateByData()
        {
            base.OnCreateByData();
            component = GameMapSettingManager.Inst.settingEntity.GetOrAddComp<PassLevelComponent>();
            //每次进编辑重置自测时间
            SetPlayTestDuration(0);
            //还原表现层
            OnMapDataInit?.Invoke();
        }

        public PassLevelComponent GetComponentData()
        {
            return component;
        }

        /////////////////////////////////// 各业务设置地图数据，后端也需要此数据，所以同时也要设置到MapInfo中 ///////////////////////////////////


        public void SetWinConditionData(int wId)
        {
            component.WinConditionId = wId;
            var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            if (mapInfo != null)
            {
                mapInfo.gameSetting.winCondition = wId;
            }

        }

        // 通关自测时间
        public void SetPlayTestDuration(int time)
        {

            var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            if (mapInfo != null)
            {
                mapInfo.gameSetting.playTestDuration = time;
            }
        }

        public void SetGameDurationData(int duration)
        {
            component.GameDuration = duration;
            var mapInfo = GameDataManager.Inst.mapGlobalData.GetCurInfo<MapInfo>();
            if (mapInfo != null)
            {
                mapInfo.gameSetting.timeLimited = duration > 0 ? 1 : 0;
                mapInfo.gameSetting.limitDuration = duration;
            }

        }



        public void SetGameHintData(string hint)
        {
            component.GameHint = hint;
        }
    }
}

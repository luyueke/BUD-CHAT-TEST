using AIGame.Base;
using GameData.BaseInfo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class SceneSettingContent : SettingContentBase
    {
        private enum ESceneAtmosphere
        {
            Default = 0,
            Dark,
            Medium,
            Bright,
        }

        private ESceneAtmosphere _curAtmosphere;

        [SerializeField] private Toggle _tog_Atmosphere1;
        [SerializeField] private Toggle _tog_Atmosphere2;
        [SerializeField] private Toggle _tog_Atmosphere3;


        public override void InitData(MapInfo mapInfo, EditType editType)
        {
            base.InitData(mapInfo, editType);
            AddListener();
            SyncEditData();
        }


        private void AddListener()
        {
            // 添加监听
            _tog_Atmosphere1.onValueChanged.AddListener(OnAtmosphere1Changed);
            _tog_Atmosphere2.onValueChanged.AddListener(OnAtmosphere2Changed);
            _tog_Atmosphere3.onValueChanged.AddListener(OnAtmosphere3Changed);
        }

        private void RemoveListener()
        {
            // 移除监听，防止重复添加
            _tog_Atmosphere1.onValueChanged.RemoveListener(OnAtmosphere1Changed);
            _tog_Atmosphere2.onValueChanged.RemoveListener(OnAtmosphere2Changed);
            _tog_Atmosphere3.onValueChanged.RemoveListener(OnAtmosphere3Changed);
        }

        private void OnAtmosphere1Changed(bool isOn)
        {
            if (isOn)
            {
                _curAtmosphere = ESceneAtmosphere.Dark;
            }
        }

        private void OnAtmosphere2Changed(bool isOn)
        {
            if (isOn)
            {
                _curAtmosphere = ESceneAtmosphere.Medium;
            }
        }

        private void OnAtmosphere3Changed(bool isOn)
        {
            if (isOn)
            {
                _curAtmosphere = ESceneAtmosphere.Bright;
            }
        }

        public override void SyncEditData()
        {
            base.SyncEditData();
            
            // 先移除监听，防止触发回调
            RemoveListener();
            
            // 根据MapInfo设置默认选中状态
            _curAtmosphere = (ESceneAtmosphere)curMapInfo.gameSetting.aIGameConfig.gameAtmosphere;
            
            // 直接设置toggle状态，不使用Invoke
            _tog_Atmosphere1.isOn = (_curAtmosphere == ESceneAtmosphere.Dark);
            _tog_Atmosphere2.isOn = (_curAtmosphere == ESceneAtmosphere.Medium);
            _tog_Atmosphere3.isOn = (_curAtmosphere == ESceneAtmosphere.Bright);
            
            // 重新添加监听
            AddListener();
            
            LoggerUtils.Log($"当前氛围 {_curAtmosphere}");
        }

        public override void SaveData()
        {
            base.SaveData();
            this.curMapInfo.gameSetting.aIGameConfig.gameAtmosphere = (int)_curAtmosphere;
        }
    }
}
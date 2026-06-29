using System;
using System.Collections.Generic;
using Basic.UndoRedo;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.ECS;
using Game.Props.PropsManagers;
using GameData;
using UndoSystem;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class WaterCubeViewAdapter : BasePropertyAdapter
    {
        private WaterCubeSubView _waterCubeSubView;
        private SimpleMatSubView _colorEditSubView;
        private WaterCubeBehaviour _bev;
        private WaterCubeComponent _cmp;

        private const float TileChangeStepSize = 0.5f;
        
        protected override void OnCreate()
        {
            _waterCubeSubView = AddTabView<WaterCubeSubView>("设置");
            _waterCubeSubView.SetTileSettingVisible(true);
            _waterCubeSubView.AddTileChangeListener(OnTileAdd, OnTileReduce);
            _waterCubeSubView.AddTileUndoListener(OnTileUndo);
            _waterCubeSubView.AddShapeChangeListener(OnShapeChanged);
            _waterCubeSubView.AddSpeedChangeListener(OnSpeedChanged);
            _waterCubeSubView.AddOxygenChangeListener(OnOxygenChanged);
            
            _colorEditSubView = AddTabView<SimpleMatSubView>("颜色");
            _colorEditSubView.AddItemSelectListener(OnColorItemSelect);
            _colorEditSubView.AddUndoSelectListener(OnColorUndoSelect);
            _colorEditSubView.AddTileChangeListener(OnTileAdd, OnTileReduce);
            _waterCubeSubView.AddTileUndoListener(OnTileUndo);
            
            var configList = ConverData(GlobalNodeManager.Inst.Get<WaterCubeManager>().GetWaterCubeConfigList());
            _colorEditSubView.InitConfig(configList);
        }
        
        
        /// <summary>
        /// 转换水材质数据
        /// </summary>
        /// <param name="propDataList"></param>
        /// <returns></returns>
        public List<BasePropConfigData> ConverData(List<Es.WaterCubeConfig> propDataList)
        {
            List<BasePropConfigData> iconDatas = new List<BasePropConfigData>();
            for (int i = 0; i < propDataList.Count; i++)
            {
                var propConfig = propDataList[i];
                var iconData = new BasePropConfigData
                {
                    Id = propConfig.Id + "",
                    IconName = propConfig.IconName
                };
                iconDatas.Add(iconData);
            }

            return iconDatas;
        }

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            _cmp = selectEntity.GetComp<WaterCubeComponent>();
            _bev = go.GetComponent<WaterCubeBehaviour>();

            if(_cmp == null) return;

            _waterCubeSubView.SetDefaultShape(_cmp.ModelShape);
            _waterCubeSubView.SetDefaultOxygen(_cmp.OxygenType);
            _colorEditSubView.SelectItemWithNoNotify(_cmp.WaterId + "");
            
            _waterCubeSubView.SetTiling(_cmp.Tile);
            _colorEditSubView.SetTiling(_cmp.Tile);

            SetDefaultSpeed();
        }

        private void SetDefaultSpeed()
        {
            if (_cmp.Speed <= WaterCubeManager.SlowSpeed)
            {
                _waterCubeSubView.SetDefaultSpeed(WaterSpeed.Slow);
            }
            else if (_cmp.Speed <= WaterCubeManager.MediumSpeed)
            {
                _waterCubeSubView.SetDefaultSpeed(WaterSpeed.Mid);
            }
            else
            {
                _waterCubeSubView.SetDefaultSpeed(WaterSpeed.Fast);
            }
        }

        private void SetSpeed(float speedVaule)
        {
            _cmp.Speed = speedVaule;
            _bev.SetSpeed(speedVaule);
        }

        #region SubView listeners

        private void OnShapeChanged(PropModelShape modelShape)
        {
            var beginData = CreateUndoData(WaterUndoType.ModelShape);
            GlobalNodeManager.Inst.Get<WaterCubeManager>().UpdateAssetObj(_bev, modelShape);
            var endData = CreateUndoData(WaterUndoType.ModelShape);
            AddRecord(beginData,endData);
        }

        private void OnSpeedChanged(WaterSpeed speed)
        {
            var beginData = CreateUndoData(WaterUndoType.Speed);
            switch (speed)
            {
                case WaterSpeed.Slow:
                    SetSpeed(WaterCubeManager.SlowSpeed);
                    break;
                case WaterSpeed.Mid:
                    SetSpeed(WaterCubeManager.MediumSpeed);
                    break;
                case WaterSpeed.Fast:
                    SetSpeed(WaterCubeManager.FastSpeed);
                    break;
            }
            var endData = CreateUndoData(WaterUndoType.Speed);
            AddRecord(beginData,endData);
        }

        private void OnOxygenChanged(int value)
        {
            var beginData = CreateUndoData(WaterUndoType.OxygenType);
            _cmp.OxygenType = value;
            var endData = CreateUndoData(WaterUndoType.OxygenType);
            AddRecord(beginData,endData);
        }


        private void ApplyTiling(Vector2 tiling)
        {
            _cmp.Tile = tiling;
            _bev.SetTiling(tiling);
            _waterCubeSubView.SetTiling(tiling);
            _colorEditSubView.SetTiling(tiling);
        }

        private void OnTileAdd()
        {
            var tiling = _cmp.Tile;
            tiling.x += TileChangeStepSize;
            tiling.y += TileChangeStepSize;
            ApplyTiling(tiling);
        }

        private void OnTileReduce()
        {
            var tiling = _cmp.Tile;
            tiling.x -= TileChangeStepSize;
            tiling.y -= TileChangeStepSize;
            if (tiling.x < 0)
            {
                tiling.x = 0;
            }
            
            if (tiling.y < 0)
            {
                tiling.y = 0;
            }

            ApplyTiling(tiling);
        }



        void OnColorItemSelect(string waterId)
        {
            LoggerUtils.Log("OnItemSelect: "+waterId);
            _cmp.WaterId = int.Parse(waterId);
            _bev.SetConfig(_cmp.WaterId);
        }
        #endregion


        #region  undo / redo
        private void OnTileUndo(Vector2 tiling)
        {
            _cmp.Tile = tiling;
            _bev.SetTiling(tiling);
        }
        
        
        void OnColorUndoSelect(string waterId)
        {
            SelectTabItem(1);
            _cmp.WaterId = int.Parse(waterId);
            _bev.SetConfig(_cmp.WaterId);
        }

        public void OnWaterUndo(WaterCubeUndoData undoData)
        {
            switch (undoData.UndoType)
            {
                case WaterUndoType.OxygenType:
                    _cmp.OxygenType = undoData.OxygenType;
                    _waterCubeSubView.SetDefaultOxygen(undoData.OxygenType);
                    break;
                case WaterUndoType.Speed:
                    SetSpeed(undoData.Speed);
                    SetDefaultSpeed();
                    break;
                case WaterUndoType.ModelShape:
                    GlobalNodeManager.Inst.Get<WaterCubeManager>().UpdateAssetObj(_bev, undoData.ModelShape);
                    _waterCubeSubView.SetDefaultShape(_cmp.ModelShape);
                    break;
            }
        }

        private WaterCubeUndoData CreateUndoData(WaterUndoType undoType)
        {
            WaterCubeUndoData undoData = new WaterCubeUndoData();
            undoData.Speed = _cmp.Speed;
            undoData.ModelShape = _cmp.ModelShape;
            undoData.OxygenType = _cmp.OxygenType;
            undoData.targetEntity = selectEntity;
            undoData.UndoType = undoType;
            return undoData;
        }

        private void AddRecord(WaterCubeUndoData beginData, WaterCubeUndoData endData)
        {
            UndoRecord record = new UndoRecord(UndoHelperName.WaterCubeUndoHelper);
            record.BeginData = beginData;
            record.EndData = endData;
            UndoRecordPool.Inst.PushRecord(record);
        }

        #endregion
       
    }
}
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.ECS;
using Game.Props.PropsManagers;
using GameData;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class SnowCubeViewAdapter : BasePropertyAdapter
    {
        private ChangeShapeSubView _changeShapeSubView;
        private GameColorEditSubView _colorEditSubView;
        private SnowCubeBehaviour _bev;
        private SnowCubeComponent _cmp;

        private const float TileChangeStepSize = 0.5f;
        
        protected override void OnCreate()
        {
            _changeShapeSubView = AddTabView<ChangeShapeSubView>("设置");
            _colorEditSubView = AddTabView<GameColorEditSubView>("颜色");
            _changeShapeSubView.SetTileSettingVisible(true);
            _changeShapeSubView.AddTileChangeListener(OnTileAdd, OnTileReduce);
            _changeShapeSubView.AddShapeChangeListener(OnShapeChanged);
            _colorEditSubView.AddColorChangeListener(OnColorChanged);
        }

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            _cmp = selectEntity.GetComp<SnowCubeComponent>();
            _bev = go.GetComponent<SnowCubeBehaviour>();

            if (_cmp != null)
            {
                _changeShapeSubView.SetDefaultShape(_cmp.ModelShape);
            }
        }

        #region SubView listeners

        private void OnShapeChanged(PropModelShape modelShape)
        {
            GlobalNodeManager.Inst.Get<SnowCubeManager>().UpdateAssetObj(_bev, modelShape);
        }

        private void OnTileAdd()
        {
            var tiling = _cmp.Tile;
            tiling.x += TileChangeStepSize;
            tiling.y += TileChangeStepSize;
            _bev.SetTiling(tiling);
        }

        private void OnTileReduce()
        {
            var tiling = _cmp.Tile;
            tiling.x -= TileChangeStepSize;
            tiling.y -= TileChangeStepSize;
            _bev.SetTiling(tiling);
        }

        private void OnColorChanged(Color color)
        {
            _bev.SetColor(color);
        }

        #endregion
    }
}
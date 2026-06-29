using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.ECS;
using Game.Props.PropsManagers;
using GameData;

namespace UI.UIPanels.GameEdit
{
    public class IceCubeViewAdapter : BasePropertyAdapter
    {
        private ChangeShapeSubView _changeShapeSubView;
        private IceCubeBehaviour _bev;
        private IceCubeComponent _cmp;

        private const float TileChangeStepSize = 0.5f;

        protected override void OnCreate()
        {
            _changeShapeSubView = this.AddTabView<ChangeShapeSubView>("设置");
            _changeShapeSubView.SetTileSettingVisible(true);
            _changeShapeSubView.AddTileChangeListener(OnTileAdd, OnTileReduce);
            _changeShapeSubView.AddShapeChangeListener(OnShapeChanged);
        }

        protected override void OnSelectEntity()
        {
            var go = selectEntity.GetViewGo();
            _cmp = selectEntity.GetComp<IceCubeComponent>();
            _bev = go.GetComponent<IceCubeBehaviour>();

            if (_cmp != null)
            {
                _changeShapeSubView.SetDefaultShape(_cmp.ModelShape);
            }
        }

        #region SubView listeners

        private void OnShapeChanged(PropModelShape modelShape)
        {
            GlobalNodeManager.Inst.Get<IceCubeManager>().UpdateAssetObj(_bev, modelShape);
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

        #endregion
    }
}
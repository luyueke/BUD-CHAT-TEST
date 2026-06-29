using System;
using Game.Base;
using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.SurfaceDetection;
using Game.SurfaceDetection.Base;
using GameData;

namespace Game.Props.PropsManagers
{
    [NodeBehaviourAttribute(typeof(SnowCubeBehaviour))]
    public class SnowCubeManager : BaseNodeManager
    {
        public const PropModelShape DefaultShape = PropModelShape.Cube;

        public SnowCubeManager()
        {
            SurfaceDetectManager.Inst.AddSurfaceHandler(new SnowCubeSurfaceHandler());
        }

        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
        {
            nodeBehaviour.entity.AddComp<SnowCubeComponent>();
            CreateAssetObj(nodeBehaviour as SnowCubeBehaviour, DefaultShape);
        }

        protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
        {
            base.OnNotifyCreateInBuild(nodeBehaviour);
            var bev = nodeBehaviour as SnowCubeBehaviour;
            var cmp = bev.entity.GetComp<SnowCubeComponent>();
            CreateAssetObj(bev, cmp.ModelShape);
        }

        protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
        {
            base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
            SnowCubeBehaviour bev = newBehaviour as SnowCubeBehaviour;
            var oldCmp = oldBehaviour.entity.GetComp<SnowCubeComponent>();
            if (bev)
            {
                bev.RefreshMats();
                bev.SetTiling(oldCmp.Tile);
                bev.SetColor(oldCmp.Color); 
            }
        }

        private void CreateAssetObj(SnowCubeBehaviour bev, PropModelShape shape)
        {
            var snowCmp = bev.entity.GetComp<SnowCubeComponent>();
            var goComp = bev.entity.GetComp<GameObjectComponent>();
            var propId = GetPropIdByShape(shape);
            goComp.PropId = propId;
            var newAssetObj = ModelCachePool.Inst.Get(propId);
            bev.SetAssetObj(newAssetObj);
            bev.SetTiling(snowCmp.Tile);
            bev.SetColor(snowCmp.Color);
        }

        public void UpdateAssetObj(SnowCubeBehaviour bev, PropModelShape shape)
        {
            var snowCmp = bev.entity.GetComp<SnowCubeComponent>();
            var goComp = bev.entity.GetComp<GameObjectComponent>();
            if (bev.assetObj != null)
            {
                ModelCachePool.Inst.Release(goComp.PropId, bev.assetObj);
            }
            snowCmp.ModelShape = shape;

            var propId = GetPropIdByShape(shape);
            goComp.PropId = propId;
            var newAssetObj = ModelCachePool.Inst.Get(propId);
            bev.SetAssetObj(newAssetObj);
            bev.SetTiling(snowCmp.Tile);
            bev.SetColor(snowCmp.Color);
        }

        private string GetPropIdByShape(PropModelShape shape)
        {
            switch (shape)
            {
                case PropModelShape.Cube:
                    return "20100042";
                case PropModelShape.Cylinder:
                    return "20100043";
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }
    }
}
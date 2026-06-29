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
    [NodeBehaviourAttribute(typeof(IceCubeBehaviour))]
    public class IceCubeManager : BaseNodeManager
    {
        public const PropModelShape DefaultShape = PropModelShape.Cube;

        public IceCubeManager()
        {
            SurfaceDetectManager.Inst.AddSurfaceHandler(new IceCubeSurfaceHandler());
        }

        protected override void OnNotifyCreateInEdit(NodeBaseBehaviour nodeBehaviour)
        {
            nodeBehaviour.entity.AddComp<IceCubeComponent>();
            CreateAssetObj(nodeBehaviour as IceCubeBehaviour, DefaultShape);
        }

        protected override void OnNotifyCreateInBuild(NodeBaseBehaviour nodeBehaviour)
        {
            base.OnNotifyCreateInBuild(nodeBehaviour);
            var bev = nodeBehaviour as IceCubeBehaviour;
            var cmp = bev.entity.GetComp<IceCubeComponent>();
            CreateAssetObj(bev, cmp.ModelShape);
        }

        protected override void OnNotifyCreateInClone(NodeBaseBehaviour oldBehaviour, NodeBaseBehaviour newBehaviour)
        {
            base.OnNotifyCreateInClone(oldBehaviour, newBehaviour);
            IceCubeBehaviour bev = newBehaviour as IceCubeBehaviour;
            var oldCmp = oldBehaviour.entity.GetComp<IceCubeComponent>();
            if (!bev) return;
            bev.RefreshMat();
            bev.SetTiling(oldCmp.Tile);
        }

        private void CreateAssetObj(IceCubeBehaviour bev, PropModelShape shape)
        {
            var iceCmp = bev.entity.GetComp<IceCubeComponent>();
            var goComp = bev.entity.GetComp<GameObjectComponent>();
            var propId = GetPropIdByShape(shape);
            goComp.PropId = propId;
            var newAssetObj = ModelCachePool.Inst.Get(propId);
            bev.SetAssetObj(newAssetObj);
            bev.SetTiling(iceCmp.Tile);
        }

        public void UpdateAssetObj(IceCubeBehaviour bev, PropModelShape shape)
        {
            var iceCmp = bev.entity.GetComp<IceCubeComponent>();
            var goComp = bev.entity.GetComp<GameObjectComponent>();
            if (bev.assetObj != null)
            {
                ModelCachePool.Inst.Release(goComp.PropId, bev.assetObj);
            }

            iceCmp.ModelShape = shape;
            var propId = GetPropIdByShape(shape);
            goComp.PropId = propId;
            var newAssetObj = ModelCachePool.Inst.Get(propId);
            bev.SetAssetObj(newAssetObj);
            
            bev.SetTiling(iceCmp.Tile);
        }

        private string GetPropIdByShape(PropModelShape shape)
        {
            switch (shape)
            {
                case PropModelShape.Cube:
                    return "20100040";
                case PropModelShape.Cylinder:
                    return "20100041";
                default:
                    throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
            }
        }
    }
}
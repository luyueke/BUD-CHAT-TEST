using Game.Props.PropsComponents;
using GameData;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class IceCubeBehaviour : ActorNodeBehaviour
    {
        private Material _iceMat;
        
        public override void SetAssetObj(GameObject gameObject)
        {
            base.SetAssetObj(gameObject);
            var scale = gameObject.transform.localScale;
            var curShape = entity.GetComp<IceCubeComponent>().ModelShape;
            scale.y = curShape == PropModelShape.Cube ? 0.1f : 0.05f;
            gameObject.transform.localScale = scale;
            RefreshMat();
        }

        public void RefreshMat()
        {
            _iceMat = assetObj.GetComponent<MeshRenderer>().material;
        }
        
        public void SetTiling(Vec2 tiling)
        {
            if (_iceMat == null || tiling == null) return;
            _iceMat.SetTextureScale("_BaseMap", tiling);
            entity.GetComp<IceCubeComponent>().Tile = tiling;
        }
    }
}
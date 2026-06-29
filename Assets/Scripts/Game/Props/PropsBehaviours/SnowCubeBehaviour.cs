using System.Collections.Generic;
using Game.Props.PropsComponents;
using GameData;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class SnowCubeBehaviour : ActorNodeBehaviour
    {
        private List<Material> _snowMats;
        private static MaterialPropertyBlock mpb;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
        }

        public override void SetAssetObj(GameObject gameObject)
        {
            base.SetAssetObj(gameObject);
            var scale = gameObject.transform.localScale;
            var curShape = entity.GetComp<SnowCubeComponent>().ModelShape;
            scale.y = curShape == PropModelShape.Cube ? 0.1f : 1f;
            gameObject.transform.localScale = scale;

            RefreshMats();
        }

        public void RefreshMats()
        {
            _snowMats ??= new List<Material>();
            _snowMats.Clear();
            var renders = assetObj.GetComponentsInChildren<MeshRenderer>();
            foreach (var r in renders)
            {
                if (r.material) _snowMats.Add(r.material);
            }
        }

        public void SetTiling(Vec2 tiling)
        {
            if (_snowMats is not { Count: > 0 } || tiling == null) return;
            foreach (var mat in _snowMats)
            {
                if (mat)
                {
                    mat.SetTextureScale("_BaseMap", tiling);
                    mat.SetTextureScale("_BumpMap", tiling);
                    mat.SetTextureScale("_SSSMask", tiling);
                }
            }

            entity.GetComp<SnowCubeComponent>().Tile = tiling;
        }

        public void SetColor(Color color)
        {
            var renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                var render = renderers[i];
                render.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", color);
                render.SetPropertyBlock(mpb);
            }

            entity.GetComp<SnowCubeComponent>().Color = color;
        }
    }
}
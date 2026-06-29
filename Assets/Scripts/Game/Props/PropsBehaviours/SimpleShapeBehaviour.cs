/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-19 15:34:32
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-12 19:05:44
 * @ Description: 基础模具
 */

using Es;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class SimpleShapeBehaviour : NodeBaseBehaviour
    {
        private static MaterialPropertyBlock mpb;
        GameMaterialLoader matLoader;

        public override void OnInitByCreate()
        {
            if (mpb == null)
            {
                mpb = new MaterialPropertyBlock();
            }
            matLoader = new GameMaterialLoader(gameObject, mpb);
        }

        private void OnDestroy()
        {
            matLoader.Destroy();
        }

		public override void HighLight(bool isHigh)
		{
			base.HighLight(isHigh);
            var matComponent = entity.GetComp<MaterialComponent>();
            if (isHigh)
            {
                SetColor(GamePropUtils.GetHighlightColor(matComponent.color));
            } else {
                SetColor(matComponent.color);
            }
		}

        public void SetColor(Color color)
        {
            matLoader.SetColor(color);
        }

        public void SetMaterial(MaterialUnionID matId)
        {
            matLoader.Load(matId);
        }

        public void SetMaterialTiling(Vector2 tiling)
        {
            matLoader.SetMaterialTiling(tiling);
        }
    }
}


using Game.Base;
using Game.ECS;
using Game.Props.PropsComponents;
using Game.Utils;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class PGCStoneBehaviour:ActorNodeBehaviour
    {
        private Color[] originColor;
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
        }

        public override void HighLight(bool isHigh)
        {
            base.HighLight(isHigh);
            Color srcColor = entity.GetComp<PGCStoneComponent>().Color;
            if (isHigh)
            {
                var hightColor = GamePropUtils.GetHighlightColor(srcColor);
                SetColor(hightColor);
            }
            else
            {
                SetColor(srcColor);
            }
        }
        
        /// <summary>
        /// 设置颜色
        /// </summary>
        /// <param name="color"></param>
        public void SetColor(Color color)
        {
            var Renderers = gameObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < Renderers.Length; i++)
            {
                var render = Renderers[i];
                render.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", color);
                render.SetPropertyBlock(mpb);
            }
        }
    }
}
        


using System;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using Game.Utils;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class PGCPlantBehaviour : ActorNodeBehaviour
    {
        private Color[] originColor;
        private static MaterialPropertyBlock mpb;

        const float WindOriginValue = 0.025f;//抖动值初始值

        //只有叶子能被染色
        private Renderer leafRender;
        public Renderer LeafRender
        {
            get
            {
                if (assetObj.transform.childCount > 1 && assetObj.transform.GetChild(0).name == "leaf")
                {
                    leafRender = assetObj.transform.GetChild(0).GetComponent<Renderer>();
                }
                else
                {
                    leafRender = assetObj.GetComponentInChildren<Renderer>();
                }
                return leafRender;
            }
        }
        
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
            Color srcColor = entity.GetComp<PGCPlantComponent>().Color;
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
        /// 设置抖动值
        /// </summary>
        /// <param name="id"></param>
        public void SetIntensity(string id)
        {
            var scaleY = transform.lossyScale.y;//lossyScale获取世界scale
            float intensity = scaleY * WindOriginValue;
            if (intensity < 0) intensity = 0;
            LeafRender.GetPropertyBlock(mpb);
            mpb.SetFloat("_Wind_Intensity", intensity);
            LeafRender.SetPropertyBlock(mpb);
        }
        
        public void SetColor(Color color)
        {
            LeafRender.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", color);
            LeafRender.SetPropertyBlock(mpb);
        }
    }
}
        

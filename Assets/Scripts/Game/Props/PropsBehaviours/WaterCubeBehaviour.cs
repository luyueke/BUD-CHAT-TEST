
using Es;
using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class WaterCubeBehaviour : ActorNodeBehaviour
    {
        private Material surfaceMat;
        private Material innerMat;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
        }

        private void InitNode(GameObject assetNode)
        {
            surfaceMat = GameObjectEx.FindChildByName(assetNode,"surface").GetComponent<Renderer>().material;
            innerMat =GameObjectEx.FindChildByName(assetNode,"inner").GetComponent<Renderer>().material;
        }

        public override void SetAssetObj(GameObject gameObject)
        {
            base.SetAssetObj(gameObject);
            InitNode(gameObject);
        }

        public void Refresh()
        {
            InitNode(assetObj);
        }

        public override string GetAssetId()
        {
            var propId = base.GetAssetId();
            var waterComp = entity.GetComp<WaterCubeComponent>();
            string assetId = propId + "_" + waterComp.ModelShape;
            return assetId;
        }

        public void SetConfig(int waterId)
        {
            var config = GlobalNodeManager.Inst.Get<WaterCubeManager>().GetConfigDataByWaterId(waterId);
            surfaceMat.SetColor("_Color",config.surfaceColor);
            surfaceMat.SetFloat("_opacity_int",config.opacity);
            innerMat.SetColor("_MainTex_Color",config.innerColor);
            
        }

        public void SetTiling(Vector2 tiling)
        {
            surfaceMat.SetTextureScale("_Noise_01",tiling);
            surfaceMat.SetTextureScale("_Noise_02",tiling * 0.5f);
            surfaceMat.SetTextureScale("_Noise_03",tiling * 0.5f);
            surfaceMat.SetTextureScale("_Emission_02",tiling);
            surfaceMat.SetTextureScale("_DiffuseNormal",tiling);
        }


        public void SetSpeed(float value)
        {
            surfaceMat.SetFloat("_01_time_v",value);
            surfaceMat.SetFloat("02_time_v",-value);
            surfaceMat.SetFloat("03_time_u",value);
            surfaceMat.SetFloat("03_time_v",value);
        }
    }
}
        


using Game.Base;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class PointLightBehaviour : NodeBaseBehaviour
    {
        private Light pointLight;
        
        private GameObject lightGo;
        
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            pointLight = GetComponentInChildren<Light>();
            var comp = entity.GetOrAddComp<PointLightComponent>();
            SetIntensity(comp.intensity);
            SetRange(comp.range);
            SetColor(comp.color);
        }
        
        public void SetIntensity(float intensity)
        {
            pointLight.intensity = intensity;
        }
        
        
        public void SetRange(float range)
        {
            pointLight.range = range;
        }

        public void SetColor(Color color)
        {
            pointLight.color = color;
        }
        
        public void SetLightVisible(bool visible)
        {
            if (lightGo == null)
            {
                lightGo = transform.Find("gdgt_spotlight_Light_FBX").gameObject;
            }
            lightGo.SetActive(visible);
        }



    }
}
        


using Game.Base;
using Game.Props.PropsComponents;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class SpotLightBehaviour : NodeBaseBehaviour
    {
        
        
        private Light spotLight;
        private GameObject lightGo;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            spotLight = GetComponentInChildren<Light>();
            var comp = entity.GetOrAddComp<SpotLightComponent>();
            SetIntensity(comp.intensity);
            SetAngle(comp.angle);
            SetRange(comp.range);
            SetColor(comp.color);
        }


        public void SetIntensity(float intensity)
        {
            spotLight.intensity = intensity;
        }
        
        public void SetAngle(float angle)
        {
            spotLight.spotAngle = angle;
            spotLight.innerSpotAngle = angle / 1.5f;
        }
        
        public void SetRange(float range)
        {
            spotLight.range = range;
        }
        
        public void SetColor(Color color)
        {
            spotLight.color = color;
        }
        
        public void SetLightVisible(bool visible)
        {
            if (lightGo == null)
            {
                lightGo = transform.Find("BulbModel").gameObject;
            }
            lightGo.SetActive(visible);
        }
        
    }
}
        

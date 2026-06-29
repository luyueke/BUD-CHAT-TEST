// @Author: YangJie
// @Description:
// @Date:  2023/08/09
// @Modify:

using Game.Base;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UnityEngine;

namespace Game.MapSetting
{
    
    

    
    public class DirLightManager : BaseMapSettingManager<DirLightManager>
    {

        private Light globalLight;
        private DirLightComponent dirLightComponent = null;

        public override void OnCreateByData()
        {
            base.OnCreateByData();
            globalLight = GameObject.Find("MapSceneDirectional Light")?.GetComponent<Light>();
            if (globalLight == null)
            { 
                globalLight = GameObject.Find("Directional Light")?.GetComponent<Light>();
            }
            dirLightComponent = GameMapSettingManager.Inst.settingEntity.GetOrAddComp<DirLightComponent>();
            SetIntensity(dirLightComponent.intensity);
            SetElevationAngle(dirLightComponent.elevation);
            SetDirectionAngle(dirLightComponent.direction);
            SetLightColor(dirLightComponent.lightColor);
        }
        
        
        
        
        
        public void SetIntensity(float intensity)
        {
            dirLightComponent.intensity = intensity;
            // 设置光照
            globalLight.intensity = intensity;
            
        }
        
        
        public void SetElevationAngle(float angle)
        {
            dirLightComponent.elevation = angle;
            var rot = globalLight.transform.rotation.eulerAngles;
            rot.x = angle;
            globalLight.transform.rotation = Quaternion.Euler(rot);
            
        }
        
        public void SetDirectionAngle(float angle)
        {
            dirLightComponent.direction = angle;
            
            var rot = globalLight.transform.rotation.eulerAngles;
            rot.y = angle;
            globalLight.transform.rotation = Quaternion.Euler(rot);
        }
        
        public void SetLightColor(Color color)
        {
            dirLightComponent.lightColor = color;
            globalLight.color = color;
        }
        
        public DirLightComponent GetDirLightComponent()
        {
            return dirLightComponent;
        }

        public Light GetGlobalDirLight()
        {
            return globalLight;
        }
        
    }
}
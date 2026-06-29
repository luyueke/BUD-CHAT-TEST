// @Author: YangJie
// @Description:
// @Date:  2023/08/10
// @Modify:

using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using GameData;
using UI.UIPanels.GameEdit.Light;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class SpotLightAdapter : BasePropertyAdapter
    {

        private LightBasicSubView basicSubView;
        private GameColorEditSubView colorEditSubView;
        
        
        private DirLightSlider intensitySlider;
        private DirLightSlider angleSlider;
        private DirLightSlider rangeSlider;
        
        
        
        private Vector2 intensityRange = new Vector2(0, 8);
        private Vector2 lightRange = new Vector2(1.5f, 15);
        private Vector2 angleRange = new Vector2(1, 150);
        
        
        protected override void OnCreate()
        {
            basicSubView = AddTabView<LightBasicSubView>("设置");
            colorEditSubView = AddTabView<GameColorEditSubView>("颜色");
            intensitySlider = basicSubView.AddSlider("强度", 100, f =>
            {
                var realValue = f.Normalization(0, 100, intensityRange.x, intensityRange.y);
                selectEntity.GetComp<SpotLightComponent>().intensity = realValue;
                var spotLightBehaviour = selectEntity.GetBehaviour<SpotLightBehaviour>();
                spotLightBehaviour.SetIntensity(realValue);
                
            }, value => $"{(int)value}");
            angleSlider = basicSubView.AddSlider("光束角", 100, f =>
            {
                var realValue = f.Normalization(0, 100, angleRange.x, angleRange.y);
                selectEntity.GetComp<SpotLightComponent>().angle = realValue;
                var spotLightBehaviour = selectEntity.GetBehaviour<SpotLightBehaviour>();
                spotLightBehaviour.SetAngle(realValue);
                
                
            }, value => $"{(int)value}");
            rangeSlider = basicSubView.AddSlider("射程", 100, f =>
            {
                var realValue = f.Normalization(0, 100, lightRange.x, lightRange.y);
                selectEntity.GetComp<SpotLightComponent>().range = realValue;
                var spotLightBehaviour = selectEntity.GetBehaviour<SpotLightBehaviour>();
                spotLightBehaviour.SetRange(realValue);
            }, value => $"{(int)value}");
      
            colorEditSubView.AddColorChangeListener((color) =>
            {
                selectEntity.GetComp<SpotLightComponent>().color = color;
                var spotLightBehaviour = selectEntity.GetBehaviour<SpotLightBehaviour>();
                spotLightBehaviour.SetColor(color);
            });
            
        }
        
        protected override void OnSelectEntity()
        {
            RefreshValue();
        }


        void RefreshValue()
        {
            var spotLightComponent = selectEntity.GetComp<SpotLightComponent>();
            var intensityValue = spotLightComponent.intensity.Normalization(intensityRange.x, intensityRange.y,0, 100);
            intensitySlider.SetValue(intensityValue);
            
            var angleValue = spotLightComponent.angle.Normalization(angleRange.x, angleRange.y,0, 100);
            angleSlider.SetValue(angleValue);
            
            var rangeValue = spotLightComponent.range.Normalization(lightRange.x, lightRange.y,0, 100);
            rangeSlider.SetValue(rangeValue);
            
            colorEditSubView.SetColorWithNoNotify(spotLightComponent.color);
        }

    }
}
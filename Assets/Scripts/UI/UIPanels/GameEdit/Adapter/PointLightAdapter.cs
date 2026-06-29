// @Author: YangJie
// @Description:
// @Date:  2023/08/10
// @Modify:

using Game.ECS;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using UI.UIPanels.GameEdit.Light;
using UnityEngine;

namespace UI.UIPanels.GameEdit
{
    public class PointLightAdapter : BasePropertyAdapter
    {
        
        private LightBasicSubView basicSubView;
        private GameColorEditSubView colorEditSubView;
        
        
        private DirLightSlider intensitySlider;
        private DirLightSlider angleSlider;
        
        
        private Vector2 intensityRange = new Vector2(0, 4.5f);
        private Vector2 angleRange = new Vector2(3, 15);

        protected override void OnCreate()
        {
            basicSubView = AddTabView<LightBasicSubView>("设置");
            colorEditSubView = AddTabView<GameColorEditSubView>("颜色");
            intensitySlider = basicSubView.AddSlider("强度", 100, f =>
            {
                var realValue = f.Normalization(0, 100, intensityRange.x, intensityRange.y);
                selectEntity.GetComp<PointLightComponent>().intensity = realValue;
                var pointLightBehaviour = selectEntity.GetBehaviour<PointLightBehaviour>();
                pointLightBehaviour.SetIntensity(realValue);
            }, value => $"{(int)value}");
            angleSlider = basicSubView.AddSlider("范围", 100, f =>
            {
                var realValue = f.Normalization(0, 100, angleRange.x, angleRange.y);
                selectEntity.GetComp<PointLightComponent>().range = realValue;
                var pointLightBehaviour = selectEntity.GetBehaviour<PointLightBehaviour>();
                pointLightBehaviour.SetRange(realValue);
            }, value => $"{(int)value}");
            colorEditSubView.AddColorChangeListener((color) =>
            {
                selectEntity.GetComp<PointLightComponent>().color = color;
                var pointLightBehaviour = selectEntity.GetBehaviour<PointLightBehaviour>();
                pointLightBehaviour.SetColor(color);
            });

        }
        protected override void OnSelectEntity()
        {
            RefreshValue();
        }
        
        void RefreshValue()
        {
            var pointLightComponent = selectEntity.GetComp<PointLightComponent>();

            var intensityValue = pointLightComponent.intensity.Normalization(intensityRange.x, intensityRange.y,0, 100);
            intensitySlider.SetValue(intensityValue);
            Debug.Log("pointLightComponent intensity:" + pointLightComponent.intensity + "," + intensityValue);
            
            var angleValue = pointLightComponent.range.Normalization(angleRange.x, angleRange.y,0, 100);
            angleSlider.SetValue(angleValue);
            
            Debug.Log("pointLightComponent angleValue:" + pointLightComponent.intensity + "," + intensityValue);
            colorEditSubView.SetColorWithNoNotify(pointLightComponent.color);
        }
    }
}
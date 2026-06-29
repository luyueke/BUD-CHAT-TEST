// @Author: YangJie
// @Description:
// @Date:  2023/08/08
// @Modify:

using System;
using System.Globalization;
using System.Linq;
using Game.Base;
using Game.MapSetting;
using Game.Props.PropsComponents;
using Game.Props.PropsManagers;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.GameEdit.Light
{

    public class DirLightSlider
    {

        private Slider slider;
        private Text valueText;
        private Text titleText;
        private int maxValue;
        private Func<float, string> valueSetFunc;
        
        
        public DirLightSlider(Transform sliderTrans, int maxValue, Func<float, string> valueSet = null)
        {
            
            valueSetFunc = valueSet;
            slider = GameObjectEx.FindComponentByName<Slider>(sliderTrans, "SliderContainer/Slider");
            valueText = GameObjectEx.FindComponentByName<Text>(sliderTrans, "SliderContainer/ValueText");
            titleText = GameObjectEx.FindComponentByName<Text>(sliderTrans, "Title");
            sliderTrans.gameObject.SetActive(true);
            slider.maxValue = maxValue;
            this.maxValue = maxValue;


        }

        public void SetValue(float value)
        {
            value = value > maxValue ? maxValue : (int)value;
            slider.value = value;
            valueText.text = valueSetFunc != null ? valueSetFunc?.Invoke(value) : value.ToString(CultureInfo.InvariantCulture);
        }
        
        public void SetValueChange(Action<float> onValueChange)
        {
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener((value) =>
            {
                valueText.text = valueSetFunc != null ? valueSetFunc?.Invoke(value) : value.ToString(CultureInfo.InvariantCulture);
                onValueChange?.Invoke(value);
            });
        }

        public void SetTitle(string title)
        {
            titleText.SetLocalText(title);
        }

    }



    public class DirLightBasicSubView: BasePropertyEditSubView
    {

        private DirLightSlider intensitySlider;
        private DirLightSlider elevationSlider;
        private DirLightSlider directionSlider;


        [SerializeField]
        private Color[] lightColors;
        
        [SerializeField]
        private SkyboxColor[] skyboxColors;
        
        [SerializeField]
        private GameColorSelectSubView lightColorSelectSubView;
        
        [SerializeField]
        private GameColorSelectSubView ambientColorSelectSubView;
        
        
        private float intensityMax = 3;
        
        protected override void OnInit()
        {
            intensitySlider = new DirLightSlider(GameObjectEx.FindChildByName(transform, "Scroll View/Viewport/Content/IntensitySlider"), 100, i => $"{(int)i}");
            elevationSlider = new DirLightSlider(GameObjectEx.FindChildByName(transform, "Scroll View/Viewport/Content/ElevationAngleSlider"), 90, i => $"{(int)i}°");
            directionSlider = new DirLightSlider(GameObjectEx.FindChildByName(transform, "Scroll View/Viewport/Content/DirectionSlider"), 360,i => $"{(int)i}°");
            var dirLightComponent = GameMapSettingManager.Inst.settingEntity.GetOrAddComp<DirLightComponent>();
            
            intensitySlider.SetValue((dirLightComponent.intensity / intensityMax) * 100);
            elevationSlider.SetValue(dirLightComponent.elevation);
            directionSlider.SetValue(dirLightComponent.direction);

            var skyLightComponent = GameMapSettingManager.Inst.settingEntity.GetOrAddComp<SkyboxComponent>();
            
            intensitySlider.SetValueChange(OnIntensityChange);
            elevationSlider.SetValueChange(OnElevationAngleChange);
            directionSlider.SetValueChange(OnDirectionAngleChange);
            lightColorSelectSubView.SetColors(lightColors, dirLightComponent.lightColor, OnLightColorSelect);
            ambientColorSelectSubView.SetColors(skyboxColors.Select(tmp => tmp.sky).ToArray(), skyLightComponent.skyboxColor.sky, OnAmbientColorSelect);
        }
        
        
        
        private void OnAmbientColorSelect(Color selectColor, int index)
        {
            SkyboxManager.Inst.SetSkyboxColor(skyboxColors[index]);
        }
        private void OnLightColorSelect(Color selectColor, int index)
        {
            DirLightManager.Inst.SetLightColor(selectColor);
        }


        private void OnIntensityChange(float value)
        {
            LoggerUtils.Log("OnIntensityChange:" , value);
            DirLightManager.Inst.SetIntensity(value / 100.0f * intensityMax);
        }

        private void OnElevationAngleChange(float value)
        {
            DirLightManager.Inst.SetElevationAngle(value);
        }
        
        private void OnDirectionAngleChange(float value)
        {
            DirLightManager.Inst.SetDirectionAngle(value);
        }


    }
}
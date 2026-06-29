using Game.MapSetting;
using Game.Props.PropsComponents;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:Jaywill
/// Desc:后处理设置面板
/// Date:23-08-03 17:56:39
/// </summary>

namespace UI.UIPanels.GameEdit
{
    public class PostProcessSubView : BasePropertyEditSubView
    {
        private CButton resetBtn;
        private EditSlider bloomSlider;
        private Toggle bloomToggle;
        private PostProcessComponent postComp;
        private Image[] sliderImages;

        private Color inactiveColor = new Color(0.729f, 0.717f, 0.68f);

        protected override void OnInit()
        {
            resetBtn = GameObjectEx.FindChildByName(transform, "ResetBtn").GetComponent<CButton>();
            bloomSlider = GameObjectEx.FindChildByName(transform, "IntensitySlider").GetComponent<EditSlider>();
            bloomToggle = GameObjectEx.FindChildByName(transform, "BloomToggle").GetComponent<Toggle>();
            sliderImages = bloomSlider.GetComponentsInChildren<Image>();

            resetBtn.onClick.AddListener(OnResetClick);
            bloomSlider.AddValueChangeListener(OnBloomIntensityChange);
            bloomToggle.onValueChanged.AddListener(OnBloomToggleValueChange);
        }

        protected override void OnStart()
        {
            base.OnStart();
            postComp = PostProcessManager.Inst.GetSettingComp();
            InitByData();
        }

        private void InitByData()
        {
            bloomSlider.SetValueWithoutNotify(postComp.BloomIntensity);
            bloomToggle.isOn = (postComp.BloomState == 1);
        }

        private void OnBloomIntensityChange(float value)
        {
            float roundValue = (float)System.Math.Round(value, 1);
            PostProcessManager.Inst.SetBloomIntensity(roundValue);
        }

        private void OnResetClick()
        {
            PostProcessManager.Inst.SetDefault();
            InitByData();
        }

        private void OnBloomToggleValueChange(bool value)
        {
            int val = value ? 1 : 0;
            PostProcessManager.Inst.SetBloomActive(val);
            bloomSlider.SetInteractable(value);
            for (var i = 0; i < sliderImages.Length; i++)
            {
                sliderImages[i].color = value ? Color.white : inactiveColor;
            }
        }
    }
}
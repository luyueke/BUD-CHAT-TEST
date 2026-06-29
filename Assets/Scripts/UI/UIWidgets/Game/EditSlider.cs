using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIWidgets
{
    public class EditSlider : MonoBehaviour
    {
        public Text titleText;
        public Slider valueSlider;
        public Text valueText;
        private Action<float> valueChangeCallback;
        
        
        private void Awake()
        {
            valueSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
        
        public void InitValue(float min, float max, float value, bool wholeNumbers = false)
        {
            valueSlider.minValue = min;
            valueSlider.maxValue = max;
            valueSlider.value = value;
            valueSlider.wholeNumbers = wholeNumbers;
            SetValueText(value);
        }

        public void SetTitle(string title)
        {
            if(titleText != null)
            {
                titleText.text = title;
            }
        }

        public void SetValue(float value)
        {
            valueSlider.value = value;
        }

        public void SetValueWithoutNotify(float value)
        {
            valueSlider.SetValueWithoutNotify(value);
            SetValueText(value);
        }

        public void SetInteractable(bool value)
        {
            valueSlider.interactable = value;
        }

        private void SetValueText(float value)
        {
            if (valueText != null)
            {
                valueText.text = Math.Round(value, 2).ToString();
            }
        }
        

        private void OnSliderValueChanged(float value)
        {
            SetValueText(value);
            valueChangeCallback?.Invoke(value);
        }

        public void AddValueChangeListener(Action<float> callback)
        {
            valueChangeCallback += callback;
        }

        public void RemoveValueChangeListener(Action<float> callback)
        {
            valueChangeCallback -= callback;
        }

        public void ClearValueChangeListener()
        {
            valueChangeCallback = null;
        }
    }
}

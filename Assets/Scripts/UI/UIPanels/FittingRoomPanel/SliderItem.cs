using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class SliderItem : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [SerializeField] Slider slider;

        private Action<float> onValueChanged;

        private void Awake()
        {
            slider.onValueChanged.AddListener(OnValueChanged);
        }

        private void OnValueChanged(float value)
        {
            onValueChanged?.Invoke(value);
        }

        public void SetName(string name)
        {
            nameText.SetText(name);
        }

        public void SetValueWithoutNotify(float value)
        {
            slider.SetValueWithoutNotify(value);
        }

        public void SetCallback(Action<float> action)
        {
            onValueChanged = action;
        }
    }
}

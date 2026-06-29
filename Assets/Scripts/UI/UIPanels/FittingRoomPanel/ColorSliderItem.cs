using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class ColorSliderItem : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [SerializeField] Slider slider;
        [SerializeField] Image image;

        private Texture2D texture2D;
        private int width;
        private Action<float> onValueChanged;

        public float Value => slider.value;

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
            nameText.text = name;
        }

        public void SetValueWithoutNotify(float value)
        {
            slider.SetValueWithoutNotify(value);
        }

        public void SetCallback(Action<float> action)
        {
            onValueChanged = action;
        }

        public void SetColors(Color[] colors)
        {
            if (texture2D == null || width != colors.Length)
            {
                width = colors.Length;
                texture2D = new Texture2D(width, 1);
                image.sprite = Sprite.Create(texture2D, new Rect(new Vector2(0.5f, 0), new Vector2(width - 1, 1)), Vector2.one * 0.5f);
            }

            texture2D.SetPixels(colors);
            texture2D.Apply();
        }
    }
}
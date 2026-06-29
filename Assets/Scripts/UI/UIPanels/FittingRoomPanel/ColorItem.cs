using System;
using System.Collections;
using System.Collections.Generic;
using Game.Audio;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class ColorItem : MonoBehaviour
    {
        [SerializeField] Image colorBg;
        [SerializeField] Image colorSelectImage;
        [SerializeField] Toggle toggle;

        private Action<Color> onValueChanged;
        private Color mColor = Color.white;

        public Color Color => mColor;

        public void SetSelectedCallBack(Action<Color> action)
        {
            onValueChanged = action;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        public void SetColor(Color color)
        {
            mColor = color;
            colorBg.color = color;
            colorSelectImage.color = color;
        }

        private void OnValueChanged(bool isOn)
        {
            if (isOn)
            {
                onValueChanged?.Invoke(mColor);
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftItems_B2);
            }
            colorBg.enabled = !isOn;
        }

        public void SetIsOn(bool isOn)
        {
            colorBg.enabled = !isOn;
            toggle.SetIsOnWithoutNotify(isOn);
        }

        public void SetToggleGroup(ToggleGroup toggleGroup)
        {
            toggle.group = toggleGroup;
        }
    }
}
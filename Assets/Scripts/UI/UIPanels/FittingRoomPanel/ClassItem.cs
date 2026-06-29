using Game.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class ClassItem : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Toggle toggle;
        [SerializeField] SpriteAtlas spriteAtlas;
        [SerializeField] GameObject redDot;

        private Action<ClassData> onValueChanged;
        private ClassData mClass;

        public void SetSelectedCallBack(Action<ClassData> action)
        {
            onValueChanged = action;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(OnValueChanged);
        }

        public void SetClass(ClassData data)
        {
            mClass = data;
            icon.sprite = spriteAtlas.GetSprite(data.SpriteName);
        }

        private void OnValueChanged(bool isOn)
        {
            if (isOn)
            {
                onValueChanged?.Invoke(mClass);
                AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
            } 
        }

        public void SetIsOn(bool isOn)
        {
            toggle.SetIsOnWithoutNotify(isOn);
        }

        public void SetToggleGroup(ToggleGroup group)
        {
            toggle.group = group;
        }

        public void SetRedDot(bool flag)
        {
            redDot.SetActive(flag);
        }
    }
}

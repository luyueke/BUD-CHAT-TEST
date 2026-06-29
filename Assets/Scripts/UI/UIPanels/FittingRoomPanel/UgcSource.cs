using Game.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class UgcSource : MonoBehaviour
    {
        public enum Source
        {
            Create,
            Buy
        }

        [SerializeField] List<Toggle> sourceToggles;
        [SerializeField] List<GameObject> redDots;

        private Action<Source> onValueChanged;

        private void Awake()
        {
            for (int i = 0, C = sourceToggles.Count; i < C; i++)
            {
                var sourceToggle = sourceToggles[i];
                Source source = (Source)i;
                sourceToggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn) onValueChanged?.Invoke(source);
                    AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
                });
            }
        }

        public void SetCallback(Action<Source> action)
        {
            onValueChanged = action;
        }

        public void DefualtOn(Source source)
        {
            if (sourceToggles[(int)source].isOn == true)
            {
                onValueChanged?.Invoke(source);
            }
            else
            {
                sourceToggles[(int)source].isOn = true;
            }

        }

        public void SetRedDot(Source source, bool flag)
        {
            redDots[(int)source].SetActive(flag);
        }
    }
}

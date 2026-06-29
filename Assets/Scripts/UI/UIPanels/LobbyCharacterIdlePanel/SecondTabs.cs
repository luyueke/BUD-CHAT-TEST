using Game.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.LobbyCharacterIdlePanel
{
    public class SecondTabs : MonoBehaviour
    {
        public enum Tab
        {
            Main,
            Sub
        }

        [SerializeField] List<Toggle> tabToggles;
        [SerializeField] List<GameObject> redDots;

        private Action<Tab> onValueChanged;

        private void Awake()
        {
            for (int i = 0, C = tabToggles.Count; i < C; i++)
            {
                var tabToggle = tabToggles[i];
                Tab tab = (Tab)i;
                tabToggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn) onValueChanged?.Invoke(tab);
                    AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
                });
            }
        }

        public void SetCallback(Action<Tab> action)
        {
            onValueChanged = action;
        }

        public void DefualtOn(Tab tab)
        {
            if (tabToggles[(int)tab].isOn == true)
            {
                onValueChanged?.Invoke(tab);
            }
            else
            {
                tabToggles[(int)tab].isOn = true;
            }
        }
        
        public void SetIsOnWithoutNotify(Tab tab)
        {
            tabToggles[(int)tab].SetIsOnWithoutNotify(true);
        }

        public void SetRedDot(Tab tab, bool flag)
        {
            redDots[(int)tab].SetActive(flag);
        }
    }
}
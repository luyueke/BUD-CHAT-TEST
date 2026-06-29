using Game.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class IncubationCabinMainTabs : MonoBehaviour
    {
        public enum Tab
        {
            Skin, //皮肤
            BaseMsg, //基础信息
            Action, //动作
            Voice, //语音
            Interactive //互动
        }

        [SerializeField] List<Toggle> tabToggles;

        private Action<Tab> onValueChanged;
        private bool isInitializing = true;

        private void Awake()
        {
            for (int i = 0, C = tabToggles.Count; i < C; i++)
            {
                var tabToggle = tabToggles[i];
                Tab tab = (Tab)i;
                #region 测试
                if (tab == Tab.Skin)
                {
#if UNITY_EDITOR
                tabToggle.gameObject.SetActive(true);
#else
                tabToggle.gameObject.SetActive(false);
#endif
                }
                #endregion
                tabToggle.onValueChanged.AddListener((isOn) =>
                {
                    if (isOn)
                    {
                        onValueChanged?.Invoke(tab);
                        if (!isInitializing)
                        {
                            AkSoundManager.Inst.PlayUIEffectSound(UISoundType.UI_ShiftTab_B1);
                        }
                    }
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
            isInitializing = false;
        }

    }
}
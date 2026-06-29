using Game.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.FittingRoom
{
    public class MainTabs : MonoBehaviour
    {
        public enum Tab
        {
            Test,
            BUD,
            Action,
            Ugc,
            Bag
        }

        [SerializeField] List<Toggle> tabToggles;
        [SerializeField] Color SelectedColor;
        [SerializeField] Color UnSelectedColor;
        [SerializeField] GameObject RedDot;

        private Action<Tab> onValueChanged;
        private bool isInitializing = true;

        private void Awake()
        {
            for (int i = 0, C = tabToggles.Count; i < C; i++)
            {
                var tabToggle = tabToggles[i];
                Tab tab = (Tab)i;
                #region 测试
                if (tab == Tab.Test)
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
                    tabToggle.GetComponent<Text>().color = isOn ? SelectedColor : UnSelectedColor;
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
                tabToggles[(int)tab].GetComponent<Text>().color = SelectedColor;
                onValueChanged?.Invoke(tab);

            }
            else
            {
                tabToggles[(int)tab].GetComponent<Text>().color = SelectedColor;
                tabToggles[(int)tab].isOn = true;
            }
            isInitializing = false;
        }

        public void SetRedDot(bool flag)
        {
            RedDot.SetActive(flag);
        }
    }
}
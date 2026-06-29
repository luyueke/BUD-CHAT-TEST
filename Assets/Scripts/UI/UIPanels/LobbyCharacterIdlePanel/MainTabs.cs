using Game.Audio;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.LobbyCharacterIdlePanel
{
    public class MainTabs : MonoBehaviour
    {
        public enum Tab
        {
            Main,
            Sub
        }

        [SerializeField] List<Toggle> tabToggles;
        [SerializeField] Color SelectedColor;
        [SerializeField] Color UnSelectedColor;

        private Action<Tab> onValueChanged;

        private void Awake()
        {
            for (int i = 0, C = tabToggles.Count; i < C; i++)
            {
                var tabToggle = tabToggles[i];
                Tab tab = (Tab)i;
                tabToggle.onValueChanged.AddListener((isOn) =>
                {
                    tabToggle.GetComponent<Text>().color = isOn ? SelectedColor : UnSelectedColor;
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
                tabToggles[(int)tab].GetComponent<Text>().color = SelectedColor;
                onValueChanged?.Invoke(tab);
            }
            else
            {
                tabToggles[(int)tab].GetComponent<Text>().color = SelectedColor;
                tabToggles[(int)tab].isOn = true;
            }
        }
        
        public void SetIsOnWithoutNotify(Tab tab)
        {
            tabToggles[(int)tab].GetComponent<Text>().color = SelectedColor;
            tabToggles[(int)tab].SetIsOnWithoutNotify(true);
        }

        public void SetToggleLineVisible(bool visible)
        {
            for (var i = 0; i < tabToggles.Count; i++)
            {
                var lineNode = tabToggles[i].transform.GetChild(0);
                lineNode.gameObject.SetActive(visible);
            }
        }

        public void SetToggleName(Tab tab,string value)
        {
            tabToggles[(int) tab].GetComponent<Text>().SetLocalText(value);
        }


        public void SetToggleVisible(Tab tab,bool visible)
        {
            tabToggles[(int)tab].gameObject.SetActive(visible);
        }
    }
}

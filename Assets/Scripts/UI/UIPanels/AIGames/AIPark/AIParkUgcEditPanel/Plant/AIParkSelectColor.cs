using Es;
using GameData.BaseInfo;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace AIGame.Base
{
    public class AIParkSelectColor : MonoBehaviour
    {
        public Toggle Toggle;

        public Image ImageOn;

        public Image ImageOff;

        private string color;

        private Action<string> action;
        private void Awake()
        {
            Toggle.onValueChanged.AddListener(OnToggle);
        }
        public void InitData(string _config, Action<string> _action) {
            action = _action;

            color = _config;

            RefreshView();
        }

        private void RefreshView() {
            if (ColorUtility.TryParseHtmlString("#" + color, out var c))
            {
                ImageOn.color = c;
                ImageOff.color = c;
            }
    
        }

        private void OnToggle(bool bo) {
            OnToggleValueChanged(Toggle.gameObject, bo);
            if (bo)
            {
                action?.Invoke(color);
            }
        }

        public void OnToggleValueChanged(GameObject obj, bool isOn)
        {
            var togSwitch = obj.GetComponent<CommonToggleSwitch>();
            togSwitch.SetSelectState(isOn);
        }
    }
}
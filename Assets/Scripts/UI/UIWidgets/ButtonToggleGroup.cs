using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIWidgets
{
    public class ButtonToggleGroup : ToggleGroup
    {
        private List<Toggle> Toggles = new List<Toggle>();
        public Transform ToggleItemTrans;

        protected override void OnDestroy()
        {
            base.OnDestroy();
            Toggles.Clear();
        }

        /// <param name="btnText">Toggle按钮显示的文本</param>
        /// <param name="btnToggleClickAct">int:Toggle的index序号</param>
        public void AddToggleItem(string btnText, Action<int> btnToggleClickAct)
        {
            var newItem = GameObject.Instantiate(ToggleItemTrans, this.transform);
            newItem.gameObject.SetActive(true);
            var toggle = newItem.GetComponent<Toggle>();
            var btnToggleText = newItem.GetComponentInChildren<Text>();
            toggle.group = this;
            Toggles.Add(toggle);

            btnToggleText.SetLocalText(btnText);
            if (btnToggleText.preferredWidth > 270) {
                ((RectTransform)newItem).sizeDelta = new Vector2(btnToggleText.preferredWidth + 50, 90);
            } else {
                ((RectTransform)newItem).sizeDelta = new Vector2(320, 90);
            }

            toggle.onValueChanged.AddListener((isOn) =>
            {
                if (isOn)
                {
                    btnToggleClickAct?.Invoke(Toggles.IndexOf(toggle));
                }
            });
        }

        /// <summary>
        /// 设置Toggle为选中状态，会触发Toggle的OnValueChanged监听
        /// </summary>
        /// <param name="index"></param>
        public void SetToggleOn(int index)
        {
            var toggle = Toggles[index];
            if (toggle)
            {
                toggle.isOn = true;
            }
            else
            {
                LoggerUtils.LogError($"SetToggleOn error, not find toggle index:{index}");
            }
        }

        /// <summary>
        /// 设置Toggle为选中状态，但不会触发Toggle的OnValueChanged监听
        /// </summary>
        /// <param name="index"></param>
        public void SetToggleOnWithoutNotify(int index)
        {
            var toggle = Toggles[index];
            if (toggle)
            {
                toggle.SetIsOnWithoutNotify(true);
            }
            else
            {
                LoggerUtils.LogError($"SetToggleOnWithoutNotify error, not find toggle index:{index}");
            }
        }
    }
}

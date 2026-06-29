using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class ToneShopTag : MonoBehaviour
    {
        [SerializeField] private GoToggle Tg;
        [SerializeField] private Text Label;


        private string _tagName;
        private Action<string, bool> _onValueChanged;

        public void SetData(string tagName, Action<string, bool> onValueChanged)
        {
            _tagName = tagName;
            _onValueChanged = onValueChanged;

            if (Label != null)
            {
                Label.text = tagName;
            }

            if (Tg != null)
            {
                Tg.Set(false, false);
                Tg.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        private void OnToggleChanged(bool isOn)
        {
            RefreshLabelColor(isOn);
            _onValueChanged?.Invoke(_tagName, isOn);
        }

        public void SetGroup(GoToggleGroup group)
        {
            Tg?.SetGroup(group);
        }

        public void Select()
        {
            RefreshLabelColor(true);
            Tg?.Set(true, true);
        }

        public void Reset()
        {
            if (Tg != null)
            {
                Tg.Set(false, false);
            }

            RefreshLabelColor(false);
        }

        /// <summary>
        /// 根据选中状态刷新 Label 颜色。
        /// 选中：#8867C3，未选中：#5E51A5。
        /// </summary>
        /// <param name="isOn">true 为选中状态，false 为未选中状态。</param>
        private void RefreshLabelColor(bool isOn)
        {
            if (Label == null)
                return;

            // 选中：#8867C3，未选中：#5E51A5
            Label.color = isOn
                ? new Color(0.5333334f, 0.4039216f, 0.7647059f)
                : new Color(0.3686275f, 0.3176471f, 0.6470588f);
        }
    }
}

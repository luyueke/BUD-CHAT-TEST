using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    public class ToneShopTypeItem : MonoBehaviour
    {
        [SerializeField] private GoToggle Tg;
        [SerializeField] private Text OffLabel;
        [SerializeField] private Text OnLabel;

        private string _sectionId;
        private Action<string> _onSelected;

        public void SetData(string sectionName, string sectionId, Action<string> onSelected)
        {
            _sectionId = sectionId;
            _onSelected = onSelected;

            if (OffLabel != null)
            {
                OffLabel.text = sectionName;
            }

            if (OnLabel != null)
            {
                OnLabel.text = sectionName;
            }

            if (Tg != null)
            {
                Tg.onValueChanged.AddListener(OnToggleChanged);
            }
        }

        public void SetIsOn(bool isOn,bool sendCallback = true)
        {
            if (Tg != null)
            {
                Tg.Set(isOn, sendCallback);
            }
        }

        private void OnToggleChanged(bool isOn)
        {
            if (isOn)
            {
                _onSelected?.Invoke(_sectionId);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChocDino.UIFX;

namespace UI.BaseWidgets
{
    public class TabItem : MonoBehaviour
    {
        private Toggle _toggle;
        [HideInInspector] public Text ItemNameText;
        [SerializeField] public BUD_Text ItemNameTextBud;
        [HideInInspector] public Image ItemLock;
        private OutlineFilter outlineFilter;
        private DropShadowFilter dropShadowFilter;

        private Action<bool> _onValueChangeCallback;
        private bool isInited = false;
        public bool IsInited {get=>isInited;private set{}}

        private Color OriTextColor = Color.white;
        private Color OriOutLineColor = Color.black;

        public void Init()
        {
            if (isInited) return;

            _toggle = GetComponentInChildren<Toggle>(true);
            if (_toggle)
            {
                _toggle.onValueChanged.AddListener(OnValueChangeCallBack);
            }
            
            ItemNameText = transform.Find("ItemName")?.GetComponent<Text>();
            
            if (ItemNameText != null)
            {
                OriTextColor = ItemNameText.color;
                outlineFilter = ItemNameText.GetComponent<OutlineFilter>();
                dropShadowFilter = ItemNameText.GetComponent<DropShadowFilter>();
            }
            
            
            if(outlineFilter != null)
                OriOutLineColor = outlineFilter.Color;
            
            isInited = true;
        }

        public void SetIsSelect(bool value)
        {
            if (_toggle)
            {
                _toggle.isOn = value;
            }
        }

        public void SetIsLock(bool isLock)
        {
            ItemLock = transform.Find("ItemLock")?.GetComponent<Image>();
            ItemLock?.gameObject.SetActive(isLock);
        }

        public bool GetIsSelect()
        {
            if (_toggle)
            {
                return _toggle.isOn;
            }

            return false;
        }

        public void SetIsSelectWithoutCallback(bool value)
        {
            if (_toggle)
            {
                _toggle.SetIsOnWithoutNotify(value);
            }
        }

        public void SetShowName(string showName)
        {
            if (ItemNameTextBud != null)
            {
                ItemNameTextBud.SetLocalText(showName);
            }
            
            if (ItemNameText == null)
            {
                ItemNameText = transform.Find("ItemName")?.GetComponent<Text>();
            }

            if (ItemNameText != null)
            {
                ItemNameText.SetLocalText(showName);
            }
        }

        public void SetEditorName(string itemName)
        {
            gameObject.name = itemName;
        }

        public void AddValueChangeCallListener(Action<bool> callback)
        {
            _onValueChangeCallback += callback;
        }

        public void RemoveValueChangeCallListener(Action<bool> callback)
        {
            _onValueChangeCallback -= callback;
        }

        public void RemoveAllListener()
        {
            _onValueChangeCallback = null;
        }

        private void OnValueChangeCallBack(bool isOn)
        {
            _onValueChangeCallback?.Invoke(isOn);

            InvertColor(isOn);
        }

        private void OnDestroy()
        {
            RemoveAllListener();
        }

        private void InvertColor(bool isOn)
        {
            if (isOn)
            {
                if (ItemNameText && outlineFilter)
                    ItemNameText.color = OriOutLineColor;
                
                if (outlineFilter)
                    outlineFilter.Color = OriTextColor;
                
                if (dropShadowFilter)
                    dropShadowFilter.Color = OriTextColor;
            }
            else
            {
                
                if (ItemNameText && outlineFilter)
                    ItemNameText.color = OriTextColor;
                
                if (outlineFilter)
                    outlineFilter.Color = OriOutLineColor;
                
                if (dropShadowFilter)
                    dropShadowFilter.Color = OriOutLineColor;
            }
        }
    }
}

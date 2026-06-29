/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-25 13:44:05
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-12 10:55:37
 * @ Description: 带选择状态的IconUI组件
 */

using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIWidgets
{

    public class GameIconSelectItem : MonoBehaviour 
    {
        [SerializeField]protected Image Icon;
        [SerializeField]private Image SelectIcon;
        [SerializeField]private Text NameText;
        [Header("扩展逻辑")]

        [Tooltip("选择时隐藏主Icon")]
        [SerializeField]private bool HideMainIconWhenSelect; // 选择时隐藏主Icon
        
        [Tooltip("选择时显示选择UI")]
        [SerializeField]private bool ShowSelectUIWhenSelect = true; // 选择时显示选择UI
        
        CButton button;
        bool isSelect = true;
        Action onSelectAction;

        protected virtual void OnNotifyDestroy(){}
        protected virtual void OnNotifyAwake(){}

        private void Awake() 
        {
            button = GetComponentInChildren<CButton>(true);
            button?.onClick.AddListener(OnClick);
            OnNotifyAwake();
        }

        private void OnDestroy() 
        {
            onSelectAction = null;
        }

        void OnClick()
        {
            onSelectAction?.Invoke();
            if (ShowSelectUIWhenSelect)
            {
                SetSelectWithNoNotify(true);
            }
        }

        public void AddOnSelectListener(Action callback)
        {
            onSelectAction += callback;
        }

        public GameObject GetIconGo()
        {
            return this.Icon.gameObject;
        }

        public void SetSelect(bool bIsSelect)
        {
            SetSelectWithNoNotify(bIsSelect);

            onSelectAction?.Invoke();
        }

        public void SetSelectWithNoNotify(bool bIsSelect)
        {
            isSelect = bIsSelect;
            SelectIcon?.gameObject.SetActive(bIsSelect);

            if (HideMainIconWhenSelect)
            {
                Icon?.gameObject.SetActive(!bIsSelect);
            }
        }

        public virtual void SetIcon(Sprite sprite)
        {
            Icon.sprite = sprite;   
        }

        public virtual void SetIconSize(int w, int h)
        {
            Icon.rectTransform.sizeDelta = new Vector2(w, h);
        }

        public void SetIconColor(Color color)
        {
            Icon.color = color;
        }

        public void SetSelectIconColor(Color color)
        {
            if (SelectIcon != null)
            {
                SelectIcon.color = color;
            }
        }

        public void SetText(string name)
        {
            if (NameText != null)
            {
                NameText.SetLocalText(name);
            }
        }
    }
}
/**
 * @ Author: Jun Zhou
 * @ Create Time: 2023-07-24 13:27:57
 * @ Modified by: Jun Zhou
 * @ Modified time: 2023-09-08 19:08:16
 * @ Description: 扩展显示收起箭头的Tab组件
 */

using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIWidgets
{
    public class PropEditTabItem : MonoBehaviour 
    {
        [SerializeField]private Text nameTxt;
        [SerializeField]private CButton flodBtn;
        [SerializeField]private TabItem tabItem;
        [Header("切换样式")]
        [SerializeField]private GameObject checkedGO;
        [SerializeField]private GameObject unCheckedGO;
        [SerializeField]private Sprite checkedDownIcon;
        [SerializeField]private Sprite unCheckedDownIcon;
        [SerializeField]private Sprite checkedUpIcon;
        [SerializeField]private Sprite unCheckedUpIcon;

        [Header("调试")]
        [SerializeField]private bool isSelected;
        [SerializeField]private bool isExpand = true;

        Image flodBtnIcon;
        Action<bool> onFlodClickAction;

        private void Awake() 
        {
            flodBtnIcon = flodBtn.GetComponent<Image>();
            flodBtn.onClick.AddListener(OnFlodBtnClick);
            tabItem.Init();
        }

        void SetSelectUIState(bool bIsSelect)
        {
            checkedGO.gameObject.SetActive(bIsSelect);
            unCheckedGO.gameObject.SetActive(!bIsSelect);
            nameTxt.color = bIsSelect ? Color.white : new Color(1,1,1,0.5f);
            SetFlodUIState(isExpand, bIsSelect);
            flodBtn.enabled = bIsSelect;
        }

        void SetFlodUIState(bool bIsExpand, bool bIsSelect)
        {
            if (flodBtnIcon==null) return;
            if (bIsSelect)
            {
                flodBtnIcon.sprite = bIsExpand ? checkedDownIcon : checkedUpIcon;
            } else {
                flodBtnIcon.sprite = bIsExpand ? unCheckedDownIcon : unCheckedUpIcon;
            }
        }

        void OnFlodBtnClick()
        {
            isExpand = !isExpand;
            SetFlodUIState(isExpand, isSelected);

            onFlodClickAction?.Invoke(isExpand);
        }

        public void ShowFlodBtn(bool isShow)
        {
            flodBtn.gameObject.SetActive(isShow);
        }

        public void AddValueChangeCallListener(Action<bool> action)
        {
            tabItem.AddValueChangeCallListener(action);
        }

        public void AddFlodChangeCallListener(Action<bool> action)
        {
            onFlodClickAction += action;
        }

        public void SetIsExpand(bool bIsExpand)
        {
            SetIsExpandWithoutCallback(bIsExpand);
            onFlodClickAction?.Invoke(isExpand);
        }

        public void SetIsExpandWithoutCallback(bool bIsExpand)
        {
            isExpand = bIsExpand;
            SetFlodUIState(isExpand, isSelected);
        }

        public void SetIsSelectWithoutCallback(bool bIsSelect)
        {
            tabItem.SetIsSelectWithoutCallback(bIsSelect);
            SetSelectUIState(bIsSelect);
            isSelected = bIsSelect;
        }

        public void SetName(string name)
        {
            nameTxt.SetLocalText(name);
        }

    #if UNITY_EDITOR
        private void OnValidate() 
        {
            if(!Application.isPlaying)
            {
                if (flodBtnIcon == null && flodBtn != null)
                {
                    flodBtnIcon = flodBtn.GetComponent<Image>();
                    tabItem.Init();
                }
                SetIsSelectWithoutCallback(isSelected);
            }
        }
    #endif
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.AvatarImage.ColorPicker
{
    public enum RoleAdjustType
    {
        Adjust = 0,
        Color = 1,
        AdjustAndColor = 2,
    }

    public class RoleAdjustView : MonoBehaviour
    {
        public Button ResetBtn;
        public Button ReturnBtn;
        public GameObject AdjustItem;
        public Transform AdjustParent;
        public SwitchLine SwitchObject;
        private bool isSwitchHand = false;
        private bool isSpecialHandle = false;

        AdjustItemFactory itemFactory;
        public Dictionary<EAdjustItemType, AdjustItem> mAdjustItems;
        private Action mOnClickResetCallBack;
        public Action<bool> mShowOrHide;
        public Action<bool> showPaltte;
        public Action<bool> showHsv;
        public Action<bool> showAdjust;
        public Action switchHand;

        public RolePaletteColorView rolePaletteColorView;
        public GameObject adjustPanel;

        public void ShowHsvView(string color)
        {
            this.gameObject.SetActive(true);
            rolePaletteColorView?.ShowOnlyHsvView(color);
        }
        
        public void ShowOnlyCommonColor()
        {
            this.gameObject.SetActive(true);
            rolePaletteColorView?.ShowOnlyCommonColor();
        }
        

        public void ShowView(RoleAdjustType type)
        {
            this.gameObject.SetActive(true);
            if (type == RoleAdjustType.Adjust)
            {
                rolePaletteColorView.gameObject.SetActive(false);
                adjustPanel.SetActive(true);
                adjustPanel.GetComponent<RectTransform>().offsetMin = new Vector2(0.0f, 0.0f);
                adjustPanel.GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, 0.0f);
            }
            else if (type == RoleAdjustType.Color)
            {
                rolePaletteColorView.gameObject.SetActive(true);
                adjustPanel.SetActive(false);
            }
            else if (type == RoleAdjustType.AdjustAndColor)
            {
                rolePaletteColorView.gameObject.SetActive(true);
                adjustPanel.SetActive(true);
                adjustPanel.GetComponent<RectTransform>().offsetMin = new Vector2(0.0f, 0.0f);
                adjustPanel.GetComponent<RectTransform>().offsetMax = new Vector2(0.0f, -116.0f);
            }
        }

        protected virtual void Start()
        {
            ResetBtn.onClick.AddListener(OnResetClick);
            ReturnBtn.onClick.AddListener(OnReturnClick);
            rolePaletteColorView.showHsv = isShow =>
            {
                showHsv?.Invoke(isShow);
            };

            rolePaletteColorView.showPalette = isShow =>
            {
                showPaltte?.Invoke(isShow);
            };
        }

        public void CreateItems(List<AdjustItemContext> itemContexts, Action<EAdjustItemType, float> ItemChanged)
        {
            for (int i = 0; i < itemContexts.Count; i++)
            {
                if (i == 0 && isSwitchHand)
                {
                    var switchHand = Instantiate(SwitchObject, AdjustParent);
                    switchHand.gameObject.SetActive(true);
                    switchHand.InitUI(SwitchHandAction);
                }
                var go = Instantiate(AdjustItem, AdjustParent);
                AdjustItemContext itemContext = itemContexts[i];
                AdjustItem item = itemFactory.Create(itemContext, go);
                item.mItemValueChanged = ItemChanged;
                if (!mAdjustItems.ContainsKey(item.mItemType))
                    mAdjustItems.Add(item.mItemType, item);
            }
        }

        /// <summary>
        /// 换手点击事件 
        /// </summary>
        private void SwitchHandAction()
        {
            switchHand?.Invoke();
        }
        

        public void Init(List<AdjustItemContext> itemContexts, Action<EAdjustItemType, float> ItemChanged,
            Action resetCallBack, Action<bool> showOrHide = null, Action<bool> showPaltte = null,
            Action<bool> showHsv = null, Action switchHand = null)
        {
            itemFactory = new AdjustItemFactory();
            mAdjustItems = new Dictionary<EAdjustItemType, AdjustItem>();

            //每次创建前删除之前的
            for (int i = 0; i < AdjustParent.childCount; i++)
            {
                Destroy(AdjustParent.GetChild(i).gameObject);
            }

            CreateItems(itemContexts, ItemChanged);

            mOnClickResetCallBack = resetCallBack;
            mShowOrHide = showOrHide;
            this.showPaltte = showPaltte;
            this.showHsv = showHsv;
            this.switchHand = switchHand;
        }

        public void SetSliderValue(EAdjustItemType itemType, float normal)
        {
            if (mAdjustItems == null)
            {
                return;
            }

            AdjustItem item = null;
            if (mAdjustItems.TryGetValue(itemType, out item))
            {
                item.SetValue(normal);
            }
        }

        /// <summary>
        /// 是否显示换手按钮
        /// </summary>
        /// <param name="isShow"></param>
        public void ShowHand(bool isShow)
        {
            isSwitchHand = isShow;
        }

        /// <summary>
        /// 调整页面重置按钮的点击事件
        /// </summary>
        public virtual void OnResetClick()
        {
            mOnClickResetCallBack?.Invoke();
        }

        /// <summary>
        /// 调整页面返回按钮的点击事件
        /// </summary>
        public virtual void OnReturnClick()
        {
            adjustPanel.SetActive(false);
            mShowOrHide?.Invoke(false);
            showAdjust?.Invoke(false);
        }
        
    }
}
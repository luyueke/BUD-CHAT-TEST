using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.AvatarImage.ColorPicker
{
    public class RolePaletteColorView : MonoBehaviour
    {
        //全部颜色按钮
        public Button paletteBtn;

        //hsv按钮
        public Button hsvBtn;
        public Button backBtn;
        public Button hsvBackBtn;
        public PaletteColorView paletteView;
        public RoleColorView roleColorView;
        public HsvColorView hsvView;

        public Action<bool> showPalette;
        public Action<bool> showHsv;

        public void ShowOnlyHsvView(string color)
        {
            this.gameObject.SetActive(true);
            hsvBtn.gameObject.SetActive(false);
            paletteBtn.gameObject.SetActive(false);
            hsvView.gameObject.SetActive(true);
            hsvView.SelectWithoutNotify(color);
        }

        public void ShowOnlyCommonColor()
        {
            this.gameObject.SetActive(true);
            hsvBtn.gameObject.SetActive(false);
            paletteBtn.gameObject.SetActive(false);

            var targetRectTransform = roleColorView.GetComponent<RectTransform>();
            // 获取当前的锚点位置和尺寸
            Vector2 currentPosition = targetRectTransform.anchoredPosition;
            Vector2 sizeDelta = targetRectTransform.sizeDelta;

            // 设置新的左右边距
            currentPosition.x = 0;
            sizeDelta.x = 0; // 计算新的宽度

            // 应用新位置和尺寸
            targetRectTransform.anchoredPosition = currentPosition;
            targetRectTransform.sizeDelta = sizeDelta;
            
        }

        public void Start()
        {
            paletteBtn.onClick.AddListener(PaletteBtnClick);
            hsvBtn.onClick.AddListener(OnHsvBtnClick);
            backBtn.onClick.AddListener(OnBackBtnClick);
            hsvBackBtn.onClick.AddListener(OnBackHsvClick);
        }

        private void OnEnable()
        {
            if (paletteView.gameObject.activeSelf)
            {
                paletteView.gameObject.SetActive(false);
            }

            if (hsvView.gameObject.activeSelf)
            {
                hsvView.gameObject.SetActive(false);
            }
        }

        public void PaletteBtnClick()
        {
            paletteView.gameObject.SetActive(true);
            if (roleColorView.curItem != null)
            {
                paletteView.SetSelect(roleColorView.curItem.rcData);
            }

            showPalette?.Invoke(true);
        }

        public void OnHsvBtnClick()
        {
            hsvView.gameObject.SetActive(true);
            if (roleColorView.curItem != null)
            {
                hsvView.SelectWithoutNotify(roleColorView.curItem.rcData);
            }

            showHsv?.Invoke(true);
        }

        /// <summary>
        /// 全部颜色面板返回按钮点击事件
        /// </summary>
        public void OnBackBtnClick()
        {
            paletteView.gameObject.SetActive(false);
            if (paletteView.paletteCurItem != null)
            {
                roleColorView.SetSelect(paletteView.paletteCurItem.rcData);
            }

            showPalette?.Invoke(false);
        }

        /// <summary>
        /// HSV颜色面板返回按钮点击事件
        /// </summary>
        public void OnBackHsvClick()
        {
            hsvView.gameObject.SetActive(false);
            roleColorView.SetSelect(hsvView.GetCurrentColor());
            paletteView.SetSelect(hsvView.GetCurrentColor());

            showHsv?.Invoke(false);
        }
    }
}
using System;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem.Data;

namespace View.UI.PopupPanelSystem.ExtendsPopups
{
    /// <summary>
    /// 标签项组件，用于展示标签信息
    /// </summary>
    public class LabelItem : MonoBehaviour
    {
        [Tooltip("标签文本组件")]
        public Text labelText;
        
        [Tooltip("标签背景图片")]
        public Image background;

        public Button button;

        // 标签数据
        private LabelData _data;
        
        // 标签是否被选中
        private bool _isSelected = false;
        
        // 点击回调
        public Action<LabelItem> OnLabelClicked;
        
        private void Awake()
        {   

            // 添加点击事件监听
            background = GetComponent<Image>();

            // 设置点击事件
            button.onClick.AddListener(OnClick);
        }
        
        private void OnDestroy()
        {
            // 移除点击事件监听
            Button button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(OnClick);
            }
        }
        
        /// <summary>
        /// 设置标签数据
        /// </summary>
        /// <param name="data">标签数据</param>
        public void SetData(LabelData data)
        {
            _data = data;

            // 设置标签文本
            if (labelText != null)
            {
                labelText.text = data.name;
            }
        }
        
        public LabelData GetData()
        {
            return _data;
        }
        
        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            
            // 更新UI显示
            UpdateVisual();
        }
        
        /// <summary>
        /// 更新视觉效果
        /// </summary>
        private void UpdateVisual()
        {
            // 根据选中状态更新背景颜色
            if (_isSelected)
            {   

                background.color = new Color(0.678f, 0.341f, 1.000f, 1f);
                labelText.color = new Color(1f, 1f, 1.000f, 1f);
            }
            else {
                labelText.color = new Color(0f, 0f, 0f, 1f);
                background.color = new Color(1f, 1f, 1f, 1f);
            }

        }
        
        /// <summary>
        /// 点击事件处理
        /// </summary>
        private void OnClick()
        {
            // 调用点击回调
            OnLabelClicked?.Invoke(this);
        }
    }
} 
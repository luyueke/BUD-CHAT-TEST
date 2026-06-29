using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台互动面板 - 激活列表 Item
    ///       展示单条唤醒动作的标题，并提供预览按钮
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllActivateItem : MonoBehaviour
    {
        [SerializeField] private Text TitleTxt;      // 激活标题，格式："唤醒动作N"
        [SerializeField] private Button PreviewBtn;  // 预览按钮，点击后播放对应动作与语音

        /// <summary>点击预览按钮时触发，由父级面板绑定具体预览逻辑</summary>
        public Action onPreviewClick;

        private characterInteraction _data;

        // 绑定预览按钮点击事件
        void Awake()
        {
            PreviewBtn.onClick.AddListener(() => onPreviewClick?.Invoke());
        }

        /// <summary>
        /// 绑定数据并刷新显示
        /// </summary>
        /// <param name="data">唤醒动作数据</param>
        /// <param name="idx">列表序号（从 0 开始），用于生成标题编号</param>
        public void Init(characterInteraction data, int idx)
        {
            _data = data;
            TitleTxt.text = $"唤醒动作{idx + 1}";
        }
    }
}



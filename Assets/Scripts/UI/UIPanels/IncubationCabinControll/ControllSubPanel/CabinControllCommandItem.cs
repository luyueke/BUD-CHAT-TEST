using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 控制台互动面板 - 口令列表 Item
    ///       展示单条口令的触发词，并提供预览按钮
    /// Date: 26-04-09
    /// </summary>
    public class CabinControllCommandItem : MonoBehaviour
    {
        [SerializeField] private Text TitleTxt;      // 口令标题，格式："口令N:触发词"
        [SerializeField] private Button PreviewBtn;  // 预览按钮，点击后播放对应动作与语音

        /// <summary>点击预览按钮时触发，由父级面板绑定具体预览逻辑</summary>
        public Action onPreviewClick;

        private voiceCommands _data;

        // 绑定预览按钮点击事件
        void Awake()
        {
            PreviewBtn.onClick.AddListener(() => onPreviewClick?.Invoke());
        }

        /// <summary>
        /// 绑定数据并刷新显示
        /// </summary>
        /// <param name="data">口令数据</param>
        /// <param name="idx">列表序号（从 0 开始），用于生成标题编号</param>
        public void Init(voiceCommands data, int idx)
        {
            _data = data;
            // 若口令触发词为空则用占位符显示
            string label = string.IsNullOrEmpty(data.command) ? "—" : data.command;
            TitleTxt.text = $"口令{idx + 1}:{label}";
        }
    }
}


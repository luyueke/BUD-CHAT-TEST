using System;
using UI.Base;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.IncubationCabin
{
    /// <summary>
    /// Author:
    /// Desc: 修改BOX硬件名称弹窗
    /// Date: 26-04-09
    /// </summary>
    public class IncubationReNamePop : BasePanel<IncubationReNamePop>
    {
        [SerializeField] private Text titleText;            // 弹窗标题
        [SerializeField] private TextInputView nameInputView; // 名称输入框
        [SerializeField] private Button btnClose;           // 右上角关闭按钮
        [SerializeField] private Button btnCancel;          // 取消按钮
        [SerializeField] private Button btnConfirm;         // 确定按钮

        private Action<string> onConfirm;

        #region 生命周期

        public override void OnCreate()
        {
            btnClose.onClick.AddListener(CloseSelf);
            btnCancel.onClick.AddListener(CloseSelf);
            btnConfirm.onClick.AddListener(OnConfirmClick);
        }

        public override void OnShow(params object[] args)
        {
            nameInputView.SetInputWithoutNotify(string.Empty);
        }

        public override void OnHidden()
        {
            // 关闭后清空回调，防止外部持有引用导致内存泄漏
            onConfirm = null;
        }

        #endregion

        #region 公开接口

        /// <summary>
        /// 配置弹窗数据，在 OpenPanel 之后立即调用
        /// </summary>
        /// <param name="onConfirm">确认回调，参数为用户输入的新内容</param>
        /// <param name="currentName">输入框预填文本</param>
        /// <param name="title">标题文本（传 null 则保持 Prefab 原始值）</param>
        /// <param name="placeholder">Placeholder 文本（传 null 则保持 Prefab 原始值）</param>
        public void SetData(Action<string> onConfirm = null, string currentName = null, string title = null, string placeholder = null)
        {
            nameInputView.SetInputWithoutNotify(currentName ?? string.Empty);
            this.onConfirm = onConfirm;

            if (title != null && titleText != null)
            {
                titleText.text = title;
            }

            if (placeholder != null)
            {
                nameInputView.SetPlaceholder(placeholder);
            }
        }

        #endregion

        #region 按钮点击

        /// <summary>
        /// 确定按钮：校验输入不为空后触发 onConfirm 并关闭弹窗
        /// </summary>
        private void OnConfirmClick()
        {
            string newName = nameInputView.Input;
            if (string.IsNullOrEmpty(newName))
            {
                TipPanel.ShowToast("请输入名称");
                return;
            }
            onConfirm?.Invoke(newName);
            CloseSelf();
        }

        public override void OnWindowBeFocused() { }
        public override void OnWindowPop() { }

        #endregion
    }

}

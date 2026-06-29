using System;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIPanels.ProfilePanel
{
    /// <summary>
    /// 修改昵称页面
    /// </summary>
    public class ChangeNamePanel : BasePanel<ChangeNamePanel>
    {
        public Text PanelTip;
        public TextInputView nameEditBox;
        public CButton BackBtn;
        public LoadingButton SaveBtn;
        public Action<string> OnComplete { set; private get; }
        
        public override void OnCreate()
        {
            nameEditBox.filterEmoji = true;
            SaveBtn.onClick.AddListener(OnConfirmClick);
            BackBtn.onClick.AddListener(OnBackBtnClick);
        }

        private void OnConfirmClick()
        {
            if (string.IsNullOrEmpty(nameEditBox.Input))
            {
                TipPanel.ShowToast("输入不能为空");
                return;
            }

            this.OnComplete?.Invoke(nameEditBox.Input);
            CloseSelf();
        }

        private void OnBackBtnClick()
        {
            CloseSelf();
        }

        
        public override void OnShow(params object[] args)
        {
            var nickName = args[0] as string;
            
            if (!string.IsNullOrEmpty(nickName))
            {
                nameEditBox.SetInputWithoutNotify(nickName);
            }
        }
    }
}
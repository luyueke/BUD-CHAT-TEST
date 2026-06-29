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
    public class ChangeNickNamePanel : BasePanel<ChangeNickNamePanel>
    {
        public TextInputView nameEditBox;
        public CButton BackBtn;
        public LoadingButton SaveBtn;
        private AccountUserInfo _accountUserInfo;
        private Action onSetSuccess;
        private Action onSetFail;
        
        public override void OnCreate()
        {
            nameEditBox.filterEmoji = true;
            SaveBtn.onClick.AddListener(OnConfirmClick);
            BackBtn.onClick.AddListener(OnBackBtnClick);
        }

        private void OnConfirmClick()
        {
            if (_accountUserInfo == null)
            {
                return;
            }
            
            if (string.IsNullOrEmpty(nameEditBox.Input))
            {
                TipPanel.ShowToast("输入不能为空");
                return;
            }

            SaveBtn.SetLoadingVisible(true);
            AccountDataManager.Inst.SendSetNickReqeust(nameEditBox.Input,OnChageNickCallback);
        }

        private void OnBackBtnClick()
        {
            CloseSelf();
        }

        private void CloseSelf()
        {
            UIManager.Inst.ClosePanel(this);
        }

        private void OnChageNickCallback(bool isSuccess)
        {
            SaveBtn.SetLoadingVisible(false);
            if (isSuccess)
            {
                this.onSetSuccess?.Invoke();
                CloseSelf();
            }
            else
            {
                this.onSetFail?.Invoke();
            }
        }

        public void SetAction(Action onSuccess = null, Action onFail = null)
        {
            this.onSetSuccess = onSuccess;
            this.onSetFail = onFail;
        }
        
        
        public override void OnShow(params object[] args)
        {
            _accountUserInfo = args[0] as AccountUserInfo;

            if (_accountUserInfo == null)
            {
                return;
            }

            if (_accountUserInfo.nickname != null && !string.IsNullOrEmpty(_accountUserInfo.nickname))
            {
                nameEditBox.SetInputWithoutNotify(_accountUserInfo.nickname);
            }
        }
    }
}

using System;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;

namespace UI.UIPanels.ProfilePanel
{
    /// <summary>
    /// 修改简介页面
    /// </summary>
    public class EditDescPanel : BasePanel<EditDescPanel>
    {
        public TextInputView descEditBox;
        public CButton BackBtn;
        public LoadingButton saveBtn;

        private AccountUserInfo _accountUserInfo;
        private Action onSetSuccess;
        private Action onSetFail;

        public override void OnCreate()
        {
            BackBtn.onClick.AddListener(OnBackBtnClick);
            saveBtn.onClick.AddListener(OnConfirmClick);
            descEditBox.SetOnInput(OnDescEditBox);
        }

        private void OnDescEditBox(string input) {
            if (string.IsNullOrEmpty(descEditBox.Input))
            {
                return;
            }

            if (_accountUserInfo != null && descEditBox.Input == _accountUserInfo.bio)
            {
                return;
            }
            saveBtn.SetClickAble(true);
        }

        private void OnConfirmClick()
        {
            if (string.IsNullOrEmpty(descEditBox.Input))
            {
                TipPanel.ShowToast("输入不能为空");
                return;
            }

            if (_accountUserInfo != null && descEditBox.Input == _accountUserInfo.bio)
            {
                return;
            }

            saveBtn.SetLoadingVisible(true);
            AccountDataManager.Inst.SendSetBioReqeust(descEditBox.Input,OnChageBioCallback);
        }

        private void OnChageBioCallback(bool isSuccess)
        {
            saveBtn.SetLoadingVisible(false);
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



        private void OnBackBtnClick()
        {
            CloseSelf();
        }

        public override void OnShow(params object[] args)
        {
            _accountUserInfo = args[0] as AccountUserInfo;
            if (_accountUserInfo == null)
            {
                return;
            }

#if PACKAGE_TYPE_US
            descEditBox.charLimit = 400;
            descEditBox.RefreshLimitText();
#endif

            if (_accountUserInfo.bio != null && !string.IsNullOrEmpty(_accountUserInfo.bio))
            {
                descEditBox.SetInputWithoutNotify(_accountUserInfo.bio);
            }

            saveBtn.SetClickAble(false);
        }


        protected override void OnDestroy()
        {
        }

        public void SetAction(Action onSuccess = null, Action onFail = null)
        {
            this.onSetSuccess = onSuccess;
            this.onSetFail = onFail;
        }
    }
}

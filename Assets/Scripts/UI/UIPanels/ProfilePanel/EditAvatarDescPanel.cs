
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
    public class EditAvatarDescPanel : BasePanel<EditAvatarDescPanel>
    {
        public TextInputView descEditBox;
        public CButton BackBtn;
        public LoadingButton saveBtn;

        private AccountUserInfo _accountUserInfo;
        public Action<string> OnComplete { set; private get; }

        public override void OnCreate()
        {
            BackBtn.onClick.AddListener(OnBackBtnClick);
            saveBtn.onClick.AddListener(OnConfirmClick);
        }


        private void OnConfirmClick()
        {
            OnComplete?.Invoke(descEditBox.Input);
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
                descEditBox.SetInputWithoutNotify(nickName);
            }
        }
        

    }
}

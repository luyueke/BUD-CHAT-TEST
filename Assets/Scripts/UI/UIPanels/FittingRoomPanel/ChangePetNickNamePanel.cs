using System;
using GameData.Account;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;
public class ChangePetNickNamePanel : BasePanel<ChangePetNickNamePanel>
{
      public TextInputView nameEditBox;
        public CButton BackBtn;
        public LoadingButton SaveBtn;
        private AccountPetInfo _accountPetInfo;
        private Action onSetSuccess;
        private Action onSetFail;
        
        public override void OnCreate()
        {
            nameEditBox.filterEmoji = true;
            nameEditBox.checkSecurity = true;
            SaveBtn.onClick.AddListener(OnConfirmClick);
            BackBtn.onClick.AddListener(OnBackBtnClick);
        }

        private void OnConfirmClick()
        {
            if (_accountPetInfo == null)
            {
                return;
            }
            SaveBtn.SetLoadingVisible(true);
            AccountDataManager.Inst.SendSetPetNickReqeust(nameEditBox.Input,OnChageNickCallback);
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
            _accountPetInfo = args[0] as AccountPetInfo;

            if (_accountPetInfo == null)
            {
                return;
            }
            if (_accountPetInfo.nickname != null && !string.IsNullOrEmpty(_accountPetInfo.nickname))
            {
                nameEditBox.SetInputWithoutNotify(_accountPetInfo.nickname);
            }
        }
    }

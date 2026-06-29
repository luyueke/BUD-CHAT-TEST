using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UI.Base;
using UnityEngine.UI;

public class AccountDeletePanel : BasePanel<AccountDeletePanel>
{
    [SerializeField] private GameObject InstructionView;
    [SerializeField] private GameObject IDCheckView;

    [SerializeField] private CButton CloseBtn1;
    [SerializeField] private CButton CloseBtn2;
    [SerializeField] private CButton InstructionDoneBtn;
    [SerializeField] private LoadingButton IDCheckConfirmBtn;
    [SerializeField] private TextInputView _inputView;

    private string inputUserName = "";
    
    public Action DeleteSuccessAction;
    
    public override void OnCreate()
    {
        CloseBtn1.onClick.AddListener(OnClose);
        CloseBtn2.onClick.AddListener(OnClose);
        InstructionDoneBtn.onClick.AddListener(() =>
        {
            InstructionView.SetActive(false);
            IDCheckView.SetActive(true);
        });
        
        _inputView.SetOnInput(OnInputComplete);
        IDCheckConfirmBtn.onClick.AddListener(OnConfirmBtnClick);
    }

    private void OnClose()
    {
        CloseSelf();
    }

    private void OnInputComplete(string input)
    {
       bool isSame = input.Length > 0;
       inputUserName = input;
       var img = IDCheckConfirmBtn.GetComponent<Image>();
       img.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common,
           isSame ? "Yellow_Btn_3" : "Gray_Btn_3", gameObject);
    }

    private void OnConfirmBtnClick()
    {
        if (string.IsNullOrEmpty(inputUserName))
        {
            return;
        }
        
        var userName = AccountDataManager.Inst.UserInfo.username;
        
        if (inputUserName != userName)
        {
            TipPanel.ShowToast("ID输入不正确请重试");
            return;
        }
        
        IDCheckConfirmBtn.ShowLoading();
        AccountDataManager.Inst.deregister(resultAction: b =>
        {
            if (this == null)
            {
                return;
            }
            
            IDCheckConfirmBtn.HideLoading();

            if (b)
            {
                CloseSelf();
                TapCoreManager.Inst.ClearUser();
                DeleteSuccessAction?.Invoke();
            }
        });
        
    }
    
}

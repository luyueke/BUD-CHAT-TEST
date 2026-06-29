using System;
using UI.Base;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-23 18:03:44
/// </summary>
public class AppleAuditInputPopupPanel : BasePanel<AppleAuditInputPopupPanel>
{
    [SerializeField] private CButton BackBtn;
    [SerializeField] private TextInputView _inputView;
    [SerializeField] private LoadingButton ConfirmBtn;
    
    public Action<string> SuccessAction;
    private string inputText = "";
    private const string AppleAuditPassword = "BUD666888";
    
    private const string AppleAuditU8ID = "001652.0c078b5896b745aa8c101ae2d006994e.0231_apple";
    
    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });
        _inputView.SetOnInput(OnInputComplete);
        ConfirmBtn.onClick.AddListener(OnConfirmBtnClick);
    }

    private void OnInputComplete(string input)
    {
        bool isEnable = input.Length > 0;
        inputText = input;
        var img = ConfirmBtn.GetComponent<Image>();
        img.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common,
            isEnable ? "Yellow_Btn_3" : "Gray_Btn_3", gameObject);
    }

    private void OnConfirmBtnClick()
    {
        if (string.IsNullOrEmpty(inputText))
        {
            return;
        }
        
        if (inputText != AppleAuditPassword)
        {
            TipPanel.ShowToast("密码输入错误");
            return;
        }
        SuccessAction.Invoke(AppleAuditU8ID);
        CloseSelf();
    }
}
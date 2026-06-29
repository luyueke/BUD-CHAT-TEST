using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 基础通用弹窗
/// @stanley
/// </summary>
public class CommonConfirmPanel : BaseCommonComfirmPanel<CommonConfirmPanel>
{
    public string TitalTextString = "";
    public string ContentTextString = "";

    public string CancelTextString = "";

    public string ConfirmTextString = "";
        
    public Text TitalText;
    public CText ContentText;

    public CButton CancelButton;

    public LoadingButton ConfirmButton;

    [Header("主题颜色设置")]
    public Image Img_TopContent;
    public Image Img_LeftBtn;
    public Image Img_RightBtn;
    private Action cancelClickAction;
    private Action confirmClickAction;

    private bool isCloseSelf = true;


    public override void OnCreate()
    {
        InitUI();
        InitClickListener();
    }

    private void InitUI()
    {
        if (!string.IsNullOrEmpty(TitalTextString))
        {
            TitalText.SetText(TitalTextString);
        }
        if (!string.IsNullOrEmpty(ContentTextString))
        {
            ContentText.SetText(ContentTextString);
        }

        if (!string.IsNullOrEmpty(CancelTextString))
        {
            CancelButton.SetText(CancelTextString);
        }

        if (!string.IsNullOrEmpty(ConfirmTextString))
        {
            ConfirmButton.SetText(ConfirmTextString);
        }
    }

    public void SetText(string titalText,string contentText, string confirmText, string cancelText)
    {
        if (!string.IsNullOrEmpty(titalText))
        {
            TitalText.SetText(titalText);
        }
        if (!string.IsNullOrEmpty(contentText))
        {
            ContentText.SetText( contentText);
        }

        if (!string.IsNullOrEmpty(cancelText))
        {
            CancelButton.SetText(cancelText);
        }

        if (!string.IsNullOrEmpty(confirmText))
        {
            ConfirmButton.SetText(confirmText);
        }
    }
    
    public void SetLocalText(string titalText,string contentText, string confirmText, string cancelText)
    {
        if (!string.IsNullOrEmpty(titalText))
        {
            TitalText.SetLocalText(titalText);
        }
        if (!string.IsNullOrEmpty(contentText))
        {
            ContentText.SetLocalText( contentText);
        }

        if (!string.IsNullOrEmpty(cancelText))
        {
            CancelButton.SetLocalText(cancelText);
        }

        if (!string.IsNullOrEmpty(confirmText))
        {
            ConfirmButton.SetLocalText(confirmText);
        }
    }

    public void SetThemeColor(string topContentColor, string leftBtnColor, string rightBtnColor){
        if(Img_TopContent != null){
            Img_TopContent.color = DataUtil.DeSerializeColorCheckHash(topContentColor);
        }
        if(Img_LeftBtn != null){
            Img_LeftBtn.color = DataUtil.DeSerializeColorCheckHash(leftBtnColor);
        }
        if(Img_RightBtn != null){
            Img_RightBtn.color = DataUtil.DeSerializeColorCheckHash(rightBtnColor);
        }
    }  

    public void SetConfirmLoadingVisible(bool value)
    {
        ConfirmButton.SetLoadingVisible(value);
        ConfirmButton.SetClickAble(!value);
    }

    public void SetIsCloseSelf(bool value)
    {
        isCloseSelf = value;
    }

    public void Close()
    {
        SetConfirmLoadingVisible(false);
        CloseSelf();
    }

    private void InitClickListener()
    {
        if (CancelButton != null)
        {
            CancelButton.onClick.AddListener(CancelClick);
        }

        if (ConfirmButton != null)
        {
            ConfirmButton.onClick.AddListener(ConfirmClick);
        }
    }

    private void CancelClick()
    {
        cancelClickAction?.Invoke();
        if (isCloseSelf)
        {
            CloseSelf();
        }
    }

    private void ConfirmClick()
    {
        confirmClickAction?.Invoke();
        if (isCloseSelf)
        {
            CloseSelf();
        }
    }

    public void SetOnClickAction(Action confirmClick = null, Action cancelClick = null)
    {
        confirmClickAction = confirmClick;
        cancelClickAction = cancelClick;
    }
    
    public override void OnShow(params object[] args)
    {
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
    }
    
    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}
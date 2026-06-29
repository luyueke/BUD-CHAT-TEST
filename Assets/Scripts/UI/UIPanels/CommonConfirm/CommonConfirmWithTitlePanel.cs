using System;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.UI;

/// <summary>
/// 带title的弹窗
/// @stanley
/// </summary>
public class CommonConfirmWithTitlePanel : BaseCommonComfirmPanel<CommonConfirmWithTitlePanel>
{
    public string TitleTextString = "";

    public string ContentTextString = "";

    public string CancelTextString = "";

    public string ConfirmTextString = "";

    public Text TitleText;

    public Text ContentText;

    public CButton CancelButton;

    public CButton ConfirmButton;

    private Action cancelClickAction;
    private Action confirmClickAction;
    private Action closeClickAction;

    protected override void Start()
    {
        InitUI();
        InitClickListener();
    }

    private void InitUI()
    {

        if (!string.IsNullOrEmpty(TitleTextString))
        {
            TitleText.text = TitleTextString;
        }

        if (!string.IsNullOrEmpty(ContentTextString))
        {
            ContentText.text = ContentTextString;
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

    public void SetText(string titleText, string contentText, string confirmText, string cancelText)
    {
        if (!string.IsNullOrEmpty(titleText))
        {
            TitleText.SetText(titleText);
        }

        if (!string.IsNullOrEmpty(contentText))
        {
            ContentText.SetText(contentText);
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
    
    public void SetLocalText(string titleText, string contentText, string confirmText, string cancelText)
    {
        if (!string.IsNullOrEmpty(titleText))
        {
            TitleText.SetLocalText(titleText);
        }

        if (!string.IsNullOrEmpty(contentText))
        {
            ContentText.SetLocalText(contentText);
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
        CloseSelf();
        cancelClickAction?.Invoke();
    }

    private void ConfirmClick()
    {
        CloseSelf();
        confirmClickAction?.Invoke();
    }
    
    public void SetOnClickAction(Action confirmClick = null, Action cancelClick = null)
    {
        confirmClickAction = confirmClick;
        cancelClickAction = cancelClick;
    }

    public void SetTextAndAction(string titleText, string contentText, string confirmText, string cancelText, Action confirmClick, Action cancelClick)
    {
        SetText(titleText, contentText, confirmText, cancelText);
        SetOnClickAction(confirmClick, cancelClick);
    }
}
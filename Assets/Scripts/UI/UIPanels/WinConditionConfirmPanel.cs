using System;
using UI.BaseWidgets;
using UnityEngine.UI;

/// <summary>
/// 更换通关条件弹窗，样式和通用不同，独立出来
/// Shaocheng
/// </summary>
public class WinConditionConfirmPanel : BaseCommonComfirmPanel<WinConditionConfirmPanel>
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

    private void Start()
    {
        InitUI();
        InitClickListener();
    }

    public override void OnShow(params object[] args)
    {
        var fromText = args[0] as string;
        var toText = args[1] as string;
        var clickAct = args[2] as Action;
        var cancelAct = args[3] as Action;

        // var titleStr = $"确定要将地图通关条件\n从<color=#4831E3FF>{fromText}</color>更改为<color=#4831E3FF>{toText}</color>吗？";
        string fromStr = LocalizationManager.Inst.GetLocalizedText(fromText);
        string toStr = LocalizationManager.Inst.GetLocalizedText(toText);
        string titleStr = LocalizationManager.Inst.GetLocalizedText("确定要将地图通关条件\n从<color=#4831E3FF>{0}</color>更改为<color=#4831E3FF>{1}</color>吗？",fromStr,toStr);
        SetText(titleStr, string.Empty, "更改", "取消");
        SetOnClickAction(clickAct, cancelAct);
    }

    private void InitUI()
    {
        if (!string.IsNullOrEmpty(TitleTextString))
        {
            TitleText.SetLocalText(TitleTextString);
        }

        if (!string.IsNullOrEmpty(ContentTextString))
        {
            ContentText.SetLocalText(ContentTextString);
        }

        if (!string.IsNullOrEmpty(CancelTextString))
        {
            CancelButton.SetLocalText(CancelTextString);
        }

        if (!string.IsNullOrEmpty(ConfirmTextString))
        {
            ConfirmButton.SetLocalText(ConfirmTextString);
        }
    }

    public void SetText(string titleText, string contentText, string confirmText, string cancelText)
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

    // private void SetTextAndAction(string titleText, string contentText, string confirmText, string cancelText, Action confirmClick, Action cancelClick)
    // {
    //     SetText(titleText, contentText, confirmText, cancelText);
    //     SetOnClickAction(confirmClick, cancelClick);
    // }
}
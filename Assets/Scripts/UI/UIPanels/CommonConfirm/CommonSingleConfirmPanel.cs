using System;
using UI.BaseWidgets;
using UnityEngine.UI;


public class CommonSingleConfirmPanelData
{
    public string ContextString = "";

    public string ConfirmString = "";

    public Action ConfirmClickAction;
}

/// <summary>
/// 通用弹窗 只有一个确认按钮
/// @stanley
/// </summary>
public class CommonSingleConfirmPanel : BaseCommonComfirmPanel<CommonSingleConfirmPanel>
{
    public string ContentTextString = "";

    public string ConfirmTextString = "";

    public Text ContentText;

    public CButton ConfirmButton;
    private Action confirmClickAction;

    protected override void Start()
    {
        InitUI();
        InitClickListener();
    }

    public override void OnShow(params object[] args)
    {
        CommonSingleConfirmPanelData panelData = args[0] as CommonSingleConfirmPanelData;
        SetText(panelData.ContextString, panelData.ConfirmString);
        SetOnClickAction(panelData.ConfirmClickAction);
    }

    private void InitUI()
    {
        if (!string.IsNullOrEmpty(ContentTextString))
        {
            ContentText.SetLocalText(ContentTextString);
        }

        if (!string.IsNullOrEmpty(ConfirmTextString))
        {
            ConfirmButton.SetLocalText(ConfirmTextString);
        }
    }

    public void SetText(string contentText, string confirmText)
    {
        if (!string.IsNullOrEmpty(contentText))
        {
            ContentText.SetLocalText(contentText);
        }

        if (!string.IsNullOrEmpty(confirmText))
        {
            ConfirmButton.SetLocalText(confirmText);
        }
    }

    private void InitClickListener()
    {
        if (ConfirmButton != null)
        {
            ConfirmButton.onClick.AddListener(ConfirmClick);
        }
    }

    private void ConfirmClick()
    {
        CloseSelf();
        confirmClickAction?.Invoke();
    }

    public void SetOnClickAction(Action confirmClick = null)
    {
        confirmClickAction = confirmClick;
    }
}
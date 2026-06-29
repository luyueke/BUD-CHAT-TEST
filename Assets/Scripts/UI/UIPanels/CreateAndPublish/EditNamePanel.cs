using System;
using UI.BaseWidgets;

/// <summary>
/// 修改名字弹窗
/// </summary>
public class EditNamePanel : BaseCommonComfirmPanel<EditNamePanel>
{
    public CText titleText;
    public CButton confirmBtn;
    public CreateAndPublishEditBox createAndPublishEditBox;

    private Action _cancelClickAction;
    private Action<string> _confirmClickAction;


    public override void OnCreate()
    {
        InitUI();
        InitClickListener();
    }

    private void InitUI()
    {
        createAndPublishEditBox.InitUI("请输入名字");
    }

    public void SetText(string titleTextString, string contentString, string confirmText)
    {
        if (!string.IsNullOrEmpty(contentString))
        {
            createAndPublishEditBox.SetText(contentString);
        }

        if (!string.IsNullOrEmpty(titleTextString))
        {
            titleText.text = titleTextString;
        }

        if (!string.IsNullOrEmpty(confirmText))
        {
            confirmBtn.SetText(confirmText);
        }
    }

    private void InitClickListener()
    {
        if (confirmBtn != null)
        {
            confirmBtn.onClick.AddListener(ConfirmClick);
        }
    }

    private void CancelClick()
    {
        CloseSelf();
        _cancelClickAction?.Invoke();
    }

    private void ConfirmClick()
    {
        if (createAndPublishEditBox != null)
        {
            string name = createAndPublishEditBox.GetText();
            if (!string.IsNullOrEmpty(name))
            {
                _confirmClickAction?.Invoke(name);
            }
        }
    }
    public void SetOnClickAction(Action<string> confirmClick = null, Action cancelClick = null)
    {
        _confirmClickAction = confirmClick;
        _cancelClickAction = cancelClick;
    }

    public override void OnShow(params object[] args)
    {
        string title = args[0] as string;
        string name = args[1] as string;
        string confirmText = args[2] as string;

        if (!string.IsNullOrEmpty(name))
        {
            createAndPublishEditBox.SetText(name);
        }

        if (!string.IsNullOrEmpty(title))
        {
            titleText.text = title;
        }

        if (!string.IsNullOrEmpty(confirmText))
        {
            confirmBtn.SetText(confirmText);
        }
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
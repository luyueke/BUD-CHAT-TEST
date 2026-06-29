using System;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.UI;

/// <summary>
/// 带title的弹窗
/// @stanley
/// </summary>
public class CommonBoxConfirmWithTitlePanel : BaseCommonComfirmPanel<CommonBoxConfirmWithTitlePanel>
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
    private Action<Action> _asyncConfirmAction;  // 异步确认：按钮置灰，等 done() 调用后才关闭弹窗

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

        // cancelText 为 null/空时隐藏取消按钮，否则显示并设置文字
        if (string.IsNullOrEmpty(cancelText))
        {
            CancelButton.gameObject.SetActive(false);
        }
        else
        {
            CancelButton.gameObject.SetActive(true);
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

        // cancelText 为 null/空时隐藏取消按钮，否则显示并设置文字
        if (string.IsNullOrEmpty(cancelText))
        {
            CancelButton.gameObject.SetActive(false);
        }
        else
        {
            CancelButton.gameObject.SetActive(true);
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
        if (_asyncConfirmAction != null)
        {
            ConfirmButton.SetClickAble(false);
            _asyncConfirmAction(() => CloseSelf());
            return;
        }
        CloseSelf();
        confirmClickAction?.Invoke();
    }

    public void SetOnClickAction(Action confirmClick = null, Action cancelClick = null)
    {
        confirmClickAction = confirmClick;
        cancelClickAction = cancelClick;
        _asyncConfirmAction = null;
    }

    /// <summary>
    /// 为异步确认操作设置回调。
    /// 点击确认后按钮置灰并保持弹窗显示，当传入的 done 被调用时才关闭弹窗。
    /// 调用此方法会清除普通 confirmClickAction，两者互斥。
    /// </summary>
    public void SetAsyncConfirmAction(Action<Action> asyncConfirm)
    {
        _asyncConfirmAction = asyncConfirm;
        confirmClickAction = null;
    }

    public void SetTextAndAction(string titleText, string contentText, string confirmText, string cancelText, Action confirmClick, Action cancelClick)
    {
        SetText(titleText, contentText, confirmText, cancelText);
        SetOnClickAction(confirmClick, cancelClick);
    }
}
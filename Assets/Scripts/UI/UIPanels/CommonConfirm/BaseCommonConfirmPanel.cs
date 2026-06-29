
using System;
using UI.Base;
using UI.BaseWidgets;

/// <summary>
/// 通用确认弹窗基类
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class BaseCommonComfirmPanel<T> : BasePanel<T> where T: BasePanel<T>
{

    public bool allowBlankSpaceClose;
    private CButton blankButton;
    private Action  closeClickAction;
    public CButton CloseBtn;

    protected override void Awake()
    {
        base.Awake();
        blankButton = transform.Find("Mask")?.GetComponent<CButton>();
        blankButton?.onClick.AddListener(OnBlankClick);
        CloseBtn?.onClick.AddListener(OnCloseButtonClick);
    }

    private void OnBlankClick()
    {
        if (allowBlankSpaceClose)
        {
            closeClickAction?.Invoke();
            CloseSelf();
        }        
    }

    private void OnCloseButtonClick(){
        CloseSelf();
        closeClickAction?.Invoke();
    }


    public void SetOnCloseAction(Action closeClickAction)
    {
        this.closeClickAction = closeClickAction;
    }

    public void HideCloseBtn()
    {
        if (CloseBtn)
        {
            CloseBtn.gameObject.SetActive(false);
        }
    }

}

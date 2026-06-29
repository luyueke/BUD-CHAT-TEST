using System;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.UI;



public class CommonSingleConfirmPanel_Style2 : BasePanel<CommonSingleConfirmPanel_Style2>
{
    public CButton BgBtn;
    public CButton CloseBtn;
    public Text ContentText;
    public Text TopTitle;
    public CButton ConfirmButton;
    private Action confirmClickAction;
    private Action closeClickAction;
    private CommonSingleConfirmPanel_Style2Data _curPanelData = new CommonSingleConfirmPanel_Style2Data();

    public override void OnCreate()
    {
        base.OnCreate();
        SetText("", "", "");
    }

    public override void OnShow(params object[] args)
    {
        CommonSingleConfirmPanel_Style2Data panelData = args[0] as CommonSingleConfirmPanel_Style2Data;
        if (panelData == null)
        {
            CloseSelf();
            return;
        }
        
        _curPanelData = panelData;
        CloseBtn.gameObject.SetActive(_curPanelData.CanClose);
        CloseBtn.SetClickAble(_curPanelData.CanClose);
        BgBtn.SetClickAble(_curPanelData.CanClose);
        AddListener();
        SetText(_curPanelData.ContextString, _curPanelData.ConfirmString, _curPanelData.TopTitleString);
    }

    public void SetText(string contentText, string confirmText, string topTitleText)
    {
        ContentText.text = contentText;
        ConfirmButton.SetText(confirmText);
        TopTitle.SetText(topTitleText);
    }

    private void AddListener()
    {
        confirmClickAction = _curPanelData.ConfirmClickAction;
        closeClickAction = _curPanelData.OnCloseAction;
        
        CloseBtn.onClick.AddListener(OnCloseBtnClick);
        BgBtn.onClick.AddListener(OnCloseBtnClick);
        ConfirmButton.onClick.AddListener(OnBtnConfirmClick);
    }

    private void OnCloseBtnClick()
    {
        closeClickAction?.Invoke();
        //2.关闭弹窗
        CloseSelf();
    }
    
    private void OnBtnConfirmClick()
    {   
        //1.执行按钮回调
        confirmClickAction?.Invoke();
        //2.关闭弹窗
        CloseSelf();
    }
}
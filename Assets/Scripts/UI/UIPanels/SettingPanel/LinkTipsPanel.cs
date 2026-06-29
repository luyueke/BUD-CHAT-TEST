using System;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIWidgets;
using UnityEngine;
using UI.Base;
using UI.UIPanels.ProfilePanel;
using UnityEngine.UI;

public class LinkTipsPanel : BasePanel<LinkTipsPanel>
{
    [SerializeField] private CButton CloseBtn1;
    [SerializeField] private CButton CloseBtn2;
    [SerializeField] private CButton DeleteBtn;
    [SerializeField] private LoadingButton LinkBtn;
    
    
    public override void OnCreate()
    {
        CloseBtn1.onClick.AddListener(OnClose);
        CloseBtn2.onClick.AddListener(OnClose);
        DeleteBtn.onClick.AddListener(OnDeleteBtnClick);
        LinkBtn.onClick.AddListener(OnLinkBtnClick);
    }

    private void OnClose()
    {
        CloseSelf();
    }

   
    private void OnLinkBtnClick()
    {
        ProfilePanel profilePanel = UIManager.Inst.OpenPanel<ProfilePanel>(PanelId.ProfilePanel, AccountDataManager.Inst.Uid);
        UIManager.Inst.ClosePanel(PanelId.SettingPanel);
        CloseSelf();
    }

    private void OnDeleteBtnClick()
    {
        
        var panel = UIManager.Inst.OpenPanel<AccountDeletePanel>(PanelId.AccountDeletePanel);
        panel.DeleteSuccessAction = () => {
            UIManager.Inst.ClosePanel(PanelId.SettingPanel);
            CloseSelf();
            GameInstanceManager.Release();
            AccountDataManager.Inst.DeleteCache();
            UIManager.Inst.ClosePanel(PanelId.GameHallPanel);
            UIManager.Inst.OpenPanel(PanelId.SignInPanel);
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.logout,
                "");
        };
        CloseSelf();
    }

}

using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class SockThemeGiftPanel : BasePanel<SockThemeGiftPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private SockGiftPanelItem BtnRight;
    [SerializeField] private SockGiftPanelItem BtnLeft;

    private string budOrderId = "";

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        ShowBtn.onClick.AddListener(OnShowBtnClick);
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            if (res != null && res.waCoinPackage != null && res.waCoinPackage.Count >= 8)
            {
                BtnRight.SetData(res.waCoinPackage[0]);
                BtnLeft.SetData(res.waCoinPackage[1]);
            }
        });
    }

    private void OnShowBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.ProfileThemePreviewPanel,ProfileTheme.Sock);
    }


}

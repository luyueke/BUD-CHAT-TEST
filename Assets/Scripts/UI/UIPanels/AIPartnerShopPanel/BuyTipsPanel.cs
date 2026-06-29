using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class BuyTipsPanel : BasePanel<BuyTipsPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton ReturnBtn;
    [SerializeField] private CButton ToSetBtn;

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        ReturnBtn.onClick.AddListener(OnReturnBtnClick);
        ToSetBtn.onClick.AddListener(OnToSetBtnClick);
    }

    private void OnReturnBtnClick()
    {
        CloseSelf();
    }

    private void OnToSetBtnClick()
    {
        CloseSelf();
        // 关闭商店面板（购买流程从 AIPartnerShopPanel 发起，需先关闭它才能显示控制台）
        UIManager.Inst.ClosePanel(PanelId.AIPartnerShopPanel);
        UIManager.Inst.OpenPanel(PanelId.IncubationCabinControll);
    }

}

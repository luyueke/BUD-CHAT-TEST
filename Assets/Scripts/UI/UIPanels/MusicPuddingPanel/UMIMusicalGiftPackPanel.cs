using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class UMIMusicalGiftPackPanel : BasePanel<UMIMusicalGiftPackPanel>
{
    [SerializeField] private CButton CloseBtn;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private CButton BuyBtnLeft;
    [SerializeField] private CButton BuyBtnRight;
    [SerializeField] private CatGiftPackItem BtnRight;
    [SerializeField] private CatGiftPackItem BtnLeft;

    private string budOrderId = "";

    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        ShowBtn.onClick.AddListener(OnShowBtnClick);
        IAPDataManager.Inst.GetProductInfo(res =>
        {
            if (res != null && res.miaoCoinPackage != null && res.miaoCoinPackage.Count >= 8)
            {
                BtnRight.SetData(res.miaoCoinPackage[6]);
                BtnLeft.SetData(res.miaoCoinPackage[7]);
            }
        });
        //var datas = IAPDataManager.Inst.GetMiaoCoinPackages();
        //if(datas != null && datas.Count == 8)
        //{

        //}
    }

    private void OnShowBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CatGiftSkinShowPanel);
    }


}

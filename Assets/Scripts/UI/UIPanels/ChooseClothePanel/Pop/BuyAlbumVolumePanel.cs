using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class BuyAlbumVolumePanel : BasePanel<BuyAlbumVolumePanel>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private CButton buyBtn;
    [SerializeField] private CButton maskBtn;

    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(CloseSelf);
        maskBtn.onClick.AddListener(CloseSelf);
        buyBtn.onClick.AddListener(OnBuyBtnClick);
    }

    private void OnBuyBtnClick()
    {
        AlbumRequestCtrl.Inst.RequestBuyAlbumVolume();
        CloseSelf();
        // var commonConfirmPanel = UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
        // commonConfirmPanel.SetLocalText("提示", "是否花费60钻石购买相册容量？", "确定", "取消");
        // commonConfirmPanel.SetOnClickAction(() =>
        // {

        // }, null);
    }

}

using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class ShapeVIPHintPanel : BasePanel<ShapeVIPHintPanel>
{
    [SerializeField] private CButton backBtn;
    [SerializeField] private Button blankBtn;
    [SerializeField] private CButton vipBtn;
    [SerializeField] private CButton exitBtn;

    private bool isSave = false;

    public override void OnCreate()
    {
        base.OnCreate();
        backBtn.onClick.AddListener(OnBackBtnClick);
        blankBtn.onClick.AddListener(OnBackBtnClick);
        vipBtn.onClick.AddListener(OnVipBtnClick);
        exitBtn.onClick.AddListener(OnExitBtnClick);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        isSave = (int)args[0] == 0;//0是保存，1是退出
    }

    private void OnBackBtnClick()
    {
        CloseSelf();
    }

    private void OnVipBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.VipMonthPack);
    }

    private void OnExitBtnClick()
    {
        if (!isSave)
        {
            var saveAvatarData = AccountDataManager.Inst.UserInfo.avatarInfo;
            //for (int i = 0; i < ShapeDataMgr.Inst.shapeDataList.Count; i++)
            //{
            //    if (AccountDataManager.Inst.UserInfo.avatarInfo.bodyType == ShapeDataMgr.Inst.shapeDataList[i].Id && ShapeDataMgr.Inst.shapeDataList[i].SaleType == ShapeSaleType.Vip)
            //    {
            //        saveAvatarData.bodyType = ShapeDataMgr.Inst.shapeDataList[0].Id;
            //        AccountDataManager.Inst.SyncAvatarData(saveAvatarData);
            //        break;
            //    }
            //}
            saveAvatarData.bodyType = ShapeDataMgr.Inst.shapeDataList[0].Id;
            AccountDataManager.Inst.SyncAvatarData(saveAvatarData);
            UIManager.Inst.ClosePanel(PanelId.FittingRoomPanel);
        }
        CloseSelf();
    }
}

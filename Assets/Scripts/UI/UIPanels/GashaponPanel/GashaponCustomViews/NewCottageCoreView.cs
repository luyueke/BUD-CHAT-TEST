using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;

public class NewCottageCoreView : NewDefaultGashaponView {
    private CButton seasonBtn;

    protected void OnSeasonBtnClick() {
        UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, RechargeId.LimitedRechargeGiftPack);
    }

    protected override void OnExchageBtnClick() {
        var exchangePanel = UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            title = "兑换商店",
            gashaponData = gashaponData,
            rewardCurrency = exchageCurrency == CurrencyType.None ? gashaponData.CurrencyType : exchageCurrency,
            itemBgColor = bundleViewBgColor,
            clearTips = "",
            buyTips = ""
        });
        exchangePanel.SetBundleViewBgColor(bundleViewBgColor);
    }

    protected override void OnPriviewBtnClick() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            title = gashaponData.Name,
            gashaponData = gashaponData,
            rewardCurrency = gashaponData.CurrencyType,
            rulePath = viewCfg.RulePath,
        });
        previewPanel.SetBundleViewBgClolr(bundleViewBgColor);
    }


    protected override void InitUI() {
        base.InitUI();
        seasonBtn = GameObjectEx.FindComponentByName<CButton>(transform, "SeasonBtn");
        seasonBtn.onClick.AddListener(OnSeasonBtnClick);
    }


}

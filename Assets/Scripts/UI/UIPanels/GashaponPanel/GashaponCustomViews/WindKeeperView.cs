using GameData.Gashapon;
using System.Collections;
using System.Collections.Generic;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class WindKeeperView : NewDefaultGashaponView
{
    [SerializeField] private List<GameObject> unlocks;
    [SerializeField] private List<GameObject> getRewards;
    [SerializeField] private CButton jumpBtn;

    private Text windkeeperTip;
    private void Unlock(int index)
    {
        for (int i = 0; i < unlocks.Count; i++)
        {
            unlocks[i].gameObject.SetActive(i >= index);
            getRewards[i].gameObject.SetActive(i<index);
        }
    }

    protected override void InitBG()
    {

    }

    protected override void InitUI()
    {
        twistBtn = GameObjectEx.FindChildByName(transform, "TwistBtn").GetComponent<CButton>();
        singleIcon = GameObjectEx.FindChildByName(twistBtn.transform, "icon").GetComponent<Image>();
        singleText = GameObjectEx.FindChildByName(twistBtn.transform, "num").GetComponent<Text>();
        srcSingleText = GameObjectEx.FindChildByName(twistBtn.transform, "srcNum").GetComponent<Text>();
        singleDiscountTag = GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject;

        infoBtn = GameObjectEx.FindChildByName(transform, "InfoBtn").GetComponent<CButton>();
        previewBtn = GameObjectEx.FindChildByName(transform, "PreviewBtn").GetComponent<CButton>();

        windkeeperTip = GameObjectEx.FindChildByName(transform, "Tips").GetComponent<Text>();
        
        twistBtn.onClick.AddListener(OnTwistClick);
        infoBtn.onClick.AddListener(OnInfoClick);
        previewBtn.onClick.AddListener(OnPriviewBtnClick);
        jumpBtn.onClick.AddListener(OnJumpBtnClick);

        UpdateWidgetView();
        UpdateBtnUI();
    }

    private void UpdateBtnUI()
    {
        if (gashaponData == null) return;
        var iconSprite = PgcUtils.LoadCurrencyIcon((int)gashaponData.CurrencyType, this.gameObject);
        if (iconSprite != null)
        {
            singleIcon.sprite = iconSprite;
        }

        singleText.SetText(gashaponData.SinglePrice.ToString());
    }

    private void OnJumpBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.LimitedRechargeGiftPack);
    }

    private void OnTwistClick()
    {
        if (gashaponInfoRsp == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        SendGashaponRequestOnce(gashaponData, gashaponInfoRsp.singleDrawDiscountedPrice);
    }

    protected override void OnPriviewBtnClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = gashaponData.Name,
            gashaponData = gashaponData,
            gashaponInfoRsp = gashaponInfoRsp,
            rewardCurrency = gashaponData.CurrencyType,
            rulePath = viewCfg.RulePath,
        });
        previewPanel.SetBundleViewBgClolr(bundleViewBgColor);
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        gashaponInfoRsp = infoRsp;
        //base.OnGashaponInfoUpdate(infoRsp);
        Unlock(infoRsp.luckyProgressInfo.start);

        singleText.SetText(infoRsp.singleDrawPrice.ToString());

        if (infoRsp.luckyProgressInfo.start == infoRsp.luckyProgressInfo.end)
        {
            windkeeperTip.SetText("恭喜！你已集齐风之守护者奖池所有奖品！");
            twistBtn.gameObject.SetActive(false);
        }
        else
        {
            twistBtn.gameObject.SetActive(true);
        }
    }

    private void OnInfoClick()
    {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var rulePath = viewCfg.RulePath;
        if (string.IsNullOrEmpty(rulePath))
        {
            rulePath = "Assets/Loadable/UI/UIPanel/GashaponRulePanel/Rules/DefaultRule.json";
        }
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }
}

using System;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-10 20:47:11
/// </summary>
public class SkipSeasonConfirmPanel : BasePanel<SkipSeasonConfirmPanel>
{
    public Button Btn_Mask;
    public Button Btn_ConfirmSkip;
    public GameObject InfoPanel;

    private int costNum = 50;
    public override void OnCreate()
    {
        Btn_Mask.onClick.AddListener(OnBtnMaskClick);
        Btn_ConfirmSkip.onClick.AddListener(OnBtnConfirmSkipClick);
    }

    /// <summary>
    /// panel 显示位置
    /// </summary>
    /// <param name="wordPos">panel 显示位置</param>
    /// <param name="CostGem">消费金额</param>
    public void AdjustPosition(Vector3 wordPos, int CostGem = 50)
    {
        InfoPanel.transform.position = wordPos;
        costNum = CostGem;
    }

    private void OnBtnMaskClick()
    {
        CloseSelf();
    }

    private void OnBtnConfirmSkipClick()
    {
        var currentGem = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        if (currentGem < costNum)
        {
            UIManager.Inst.OpenPanel(PanelId.GetMoreGemsPanel, costNum - currentGem);
            return;
        }

        PayByGem();
    }

    public static void PayByGem(int level = 1, Action ac = null)
    {
        var seasonPassType = SeasonPassDataManager.Inst.CurrentSeasonPassType;
        IAPDataManager.Inst.PayByGem(BUDProductType.AdvancedSeasonPassTier, SeasonPassDataManager.Inst.GetSeasonPassName(seasonPassType), resultHandler:
        result =>
        {
            if (result)
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                if (UIManager.Inst.TryFindPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel, out var panel))
                {
                    panel.RefreshData(seasonPassType);
                }
                ac?.Invoke();
            }
        }, level);
    }
}
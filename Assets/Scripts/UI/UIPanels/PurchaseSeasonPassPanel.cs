using System;
using System.Collections.Generic;
using Network.Message;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Author:
/// Desc:
/// Date:24-07-12 19:12:51
/// </summary>
public class PurchaseSeasonPassPanel : BasePanel<PurchaseSeasonPassPanel>
{
    public Text Txt_SaleEndTime;
    public Button Btn_BgMask;
    public Button Btn_Buy;
    public Text seasonPassPrice;
    [SerializeField] private Text RewardAvatarText;
    [SerializeField] private Text RewardEmoteText;
    [SerializeField] private Text DescText;

    public Action<bool> buyCallback;

    private bool onceFlag = false;

    private void OnBuyClick()
    {
        int seasonPassGemCount = SeasonPassDataManager.Inst.PremiumSeasonPassGem;
        var gemNum = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        if (gemNum < seasonPassGemCount)
        {
            UIManager.Inst.OpenPanel<GetMoreGemsPanel>(PanelId.GetMoreGemsPanel, seasonPassGemCount - gemNum);
        }
        else
        {
            if (onceFlag)
            {
                return;
            }

            onceFlag = true;

            // IAPDataManager.Inst.PayByGem(BUDProductType.SeasonPass, productId: SeasonPassDataManager.Inst.SeasonPassName, resultHandler: result =>
            // {
            //     onceFlag = false;
            //     buyCallback?.Invoke(result);
            //     if (result)
            //     {
            //         CloseSelf();
            //         ShowReward();
            //     }
            //     else
            //     {
            //         TipPanel.ShowToast("哎呀！出错了。请重试！");
            //     }
            // });
        }
    }

    private void ShowReward()
    {
        string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
        var rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "logo", this.gameObject);
        if (rewardSp == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<CommonRewardItemData>()
        {
            new CommonRewardItemData()
            {
                IconSp = rewardSp,
                RewardAmount = 1,
                rewardName = "赛季通行证"
            }
        }, IsResize: true);
    }

    public override void OnCreate()
    {

        // var seasonPassRsp = SeasonPassDataManager.Inst.SeasonPassList;
        // if (seasonPassRsp != null && !string.IsNullOrEmpty(seasonPassRsp?.endTime))
        // {
        //     Txt_SaleEndTime.text = seasonPassRsp?.endTime;
        // }
        //
        // Btn_BgMask.onClick.AddListener(() =>
        // {
        //     CloseSelf();
        // });
        // Btn_Buy.onClick.AddListener(OnBuyClick);
        // seasonPassPrice.text = SeasonPassDataManager.Inst.PremiumSeasonPassGem.ToString();
        // string rewardAvatarName = LocalizationManager.Inst.GetLocalizedText(SeasonPassDataManager.RewardAvatarName);
        // RewardAvatarText.SetText(rewardAvatarName);
        // RewardEmoteText.SetLocalText(SeasonPassDataManager.RewardEmoteName);
        // DescText.SetLocalText("购买高级通行证立刻获得{0}套装",rewardAvatarName);
    }

    public override void OnShow(params object[] args)
    {
    }

    public override void OnHidden()
    {
    }

    protected override void OnDestroy()
    {
    }

    public override void OnWindowBeFocused()
    {
    }

    public override void OnWindowPop()
    {
    }
}


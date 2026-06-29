using Message;
using System.Collections.Generic;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;

public class SeasonPurchaseView : BasePanel<SeasonPurchaseView>
{
    [SerializeField] private CButton closeBtn;
    [SerializeField] private CButton premiumBtn;
    [SerializeField] private GameObject premiumOwnedObj;
    [SerializeField] private CButton luxuryBtn;

    bool isSending = false;

    SeasonPassListRsp rsp;
    public override void OnCreate()
    {
        base.OnCreate();
        closeBtn.onClick.AddListener(CloseSelf);
        premiumBtn.onClick.AddListener(OnPremiumClicked);
        luxuryBtn.onClick.AddListener(OnLuxuryClicked);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);

        RefreView();
    }

    void RefreView() {
        rsp = SeasonPassDataManager.Inst.GetCurSeasonData();
        if (rsp.isPaid == 0)
        {
            premiumBtn.gameObject.SetActive(true);
            premiumOwnedObj.SetActive(false);
            luxuryBtn.SetText("600");
        }
        else
        {
            premiumBtn.gameObject.SetActive(false);
            premiumOwnedObj.SetActive(true);
            luxuryBtn.SetText("300");
        }
    }

    private void OnPremiumClicked()
    {
        if (SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType))
        {
            return;
        }
        SendPayRequest(false);
    }

    private void OnLuxuryClicked()
    {
        if (SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType) && SeasonPassDataManager.Inst.GetSeasonPassPaidType(SeasonPassDataManager.Inst.CurrentSeasonPassType) == 1)
        {
            return;
        }
        SendPayRequest(true);
    }


    private void SendPayRequest(bool isLuxury)
    {
        bool isPaid = SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType);
        int seasonPassGemCount = isLuxury ? SeasonPassDataManager.Inst.LuxurySeasonPassGem : SeasonPassDataManager.Inst.PremiumSeasonPassGem;
        if (isLuxury && SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType))
        {
            seasonPassGemCount = SeasonPassDataManager.Inst.LuxurySeasonPassGem - SeasonPassDataManager.Inst.PremiumSeasonPassGem;
        }
        var gemNum = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.Gem);
        if (gemNum < seasonPassGemCount)
        {
            UIManager.Inst.OpenPanel<GetMoreGemsPanel>(PanelId.GetMoreGemsPanel, seasonPassGemCount - gemNum);
        }
        else
        {
            if (isSending)
            {
                return;
            }

            isSending = true;

            IAPDataManager.Inst.PaySeasonPassByGem(SeasonPassDataManager.Inst.GetSeasonPassName(SeasonPassDataManager.Inst.CurrentSeasonPassType), isLuxury,
                resultHandler: result =>
                {
                    isSending = false;
                    if (result)
                    {
                        ShowReward(isLuxury, isPaid);
                        CloseSelf();
                    }
                    else
                    {
                        TipPanel.ShowToast("哎呀！出错了。请重试！");
                    }
                });
        }
    }

    private void ShowReward(bool isLuxury, bool isPaid)
    {
        MessageHelper.Broadcast(MessageName.RefreshSeasonPass);
        string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";
        var rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, isLuxury ? "luxury_logo" : "logo", this.gameObject);
        if (rewardSp == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);

        var rewardList = new List<CommonRewardItemData>() {
            new CommonRewardItemData() {
                IconSp = rewardSp,
                RewardAmount = 1,
                rewardName = isLuxury ? "豪华通行证" : "高级通行证"
            },
        };

        if (!isLuxury)
        {
            rewardList.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", panel.gameObject),
                RewardAmount = 1,
                rewardName = "皮肤设子位"
            });
        }
        else
        {
            var list = SeasonPassDataManager.Inst.OnPaidSeasonPassAndGetRewardData(SeasonPassDataManager.Inst.CurrentSeasonPassType, isPaid, gameObject);
            rewardList.AddRange(list);
        }
        panel.ShowRewards(rewardList, IsResize: true);
        var seasonPanel = UIManager.Inst.FindPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel);
        seasonPanel.SwitchView(SeasonPassDataManager.Inst.CurrentSeasonPassType, "SeasonRewardView");

    }
}

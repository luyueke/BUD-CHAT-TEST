using System.Collections.Generic;
using Sirenix.OdinInspector;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 能量用完界面：引导用户购买月卡或能量套餐。
/// </summary>
public class AICreditOverPanel : BasePanel<AICreditOverPanel>
{
    public Button CloseBtn;
    public Button BuyBtn;              // 开通月卡
    public Button[] AICreditPackBtns; // 4 个能量套餐，顺序对应 AICreditPurchaseHelper.Packages

    public Sprite yuekaSprite;

    private AICreditPurchaseHelper _purchaseHelper;

    public override void OnCreate()
    {
        base.OnCreate();
        _purchaseHelper = new AICreditPurchaseHelper(this);

        if (CloseBtn != null) CloseBtn.onClick.AddListener(CloseSelf);
        if (BuyBtn   != null) BuyBtn.onClick.AddListener(OnMonthlyCardClick);

        var packages = AICreditPurchaseHelper.Packages;
        for (int i = 0; i < AICreditPackBtns.Length && i < packages.Count; i++)
        {
            if (AICreditPackBtns[i] == null) continue;
            var capturedPkg = packages[i];
            AICreditPackBtns[i].onClick.AddListener(() => OnPackageClick(capturedPkg));
        }
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void OnMonthlyCardClick()
        => _purchaseHelper.BuyIAP(AICreditPurchaseHelper.MonthlyCardGemInfo, ShowReward);

    private void OnPackageClick(AICreditPackage pkg)
    {
        var gemInfo = new ProductGemInfo
        {
            productId = pkg.productId,
            price     = (pkg.priceFen / 100).ToString(),
            name      = "AI能量",
        };
        _purchaseHelper.BuyIAP(gemInfo, () => ShowPackageReward(pkg));
    }

    private void ShowPackageReward(AICreditPackage pkg)
    {
        var items = new List<BoxRewardItemData>
        {
            new()
            {
                itemType    = BoxRewardItemData.ItemType.Common,
                specialIcon = specialIconType.aiCredit,
                commonData  = new CommonRewardItemData
                {
                    RewardAmount = pkg.energyAmount,
                    rewardName   = "AI能量",
                }
            }
        };

        if (pkg.bonusAmount > 0)
        {
            items.Add(new BoxRewardItemData
            {
                itemType    = BoxRewardItemData.ItemType.Common,
                specialIcon = specialIconType.aiCredit,
                tagType     = TagType.zengsong,
                commonData  = new CommonRewardItemData
                {
                    RewardAmount = pkg.bonusAmount,
                    rewardName   = "AI能量",
                }
            });
        }

        var rewardPanel = UIManager.Inst.OpenPanel<CommonBoxRewardPanel>(PanelId.CommonBoxRewardPanel);
        rewardPanel.ShowReward(items);
    }

    private void ShowReward()
    {
        var rewardPanel = UIManager.Inst.OpenPanel<CommonBoxRewardPanel>(PanelId.CommonBoxRewardPanel);
        rewardPanel.ShowReward(new List<BoxRewardItemData>
        {
            new()
            {
                itemType   = BoxRewardItemData.ItemType.Common,
                commonData = new CommonRewardItemData
                {
                    IconSp        = yuekaSprite,
                    rewardSpecial = "30天",
                    rewardName    = "AI能量月卡",
                }
            }
        });
    }

    [Button("展示奖励")]
    void test() => ShowReward();
}

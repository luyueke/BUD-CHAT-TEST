using System.Collections.Generic;
using Sirenix.OdinInspector;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI能量商城：购买能量套餐及月卡
/// </summary>
public class AICreditShopPanel : BasePanel<AICreditShopPanel>
{
    private Button _monthlyCardBtn;
    private Button _closeBtn;

    public GameObject TxtLijiKaiTongGo; // 立即开通文本
    public GameObject TxtXuFeiGo;       // 续费文本
    public Sprite yuekaSprite;

    private AICreditPurchaseHelper _purchaseHelper;

    public override void OnCreate()
    {
        base.OnCreate();
        _purchaseHelper = new AICreditPurchaseHelper(this);

        var ui = transform.Find("BaseLayout2D/UIContainer");

        var closeTf = ui.Find("CloseBtn");
        if (closeTf != null && closeTf.TryGetComponent<Button>(out _closeBtn))
            _closeBtn.onClick.AddListener(CloseSelf);

        BindClick(ui.Find("RulesBtn1"), () => OpenRules(AICreditRulesPanel.Tab.Chongzhi));
        BindClick(ui.Find("RulesBtn2"), () => OpenRules(AICreditRulesPanel.Tab.YueKa));

        var monthlyTf = ui.Find("Image (2)/btnBuyCard");
        if (monthlyTf != null && monthlyTf.TryGetComponent<Button>(out _monthlyCardBtn))
            _monthlyCardBtn.onClick.AddListener(OnMonthlyCardClick);

        BindClick(ui.Find("layout/btnDetail"), OnDetailClick);

        var packList = ui.Find("PackageList");
        for (int i = 0; i < AICreditPurchaseHelper.Packages.Count; i++)
        {
            var item = packList != null ? packList.Find($"packItem{i + 1}") : null;
            if (item == null) continue;
            var pkg = AICreditPurchaseHelper.Packages[i];

            if (item.Find("EnergyTxt") is { } energyTf && energyTf.TryGetComponent<Text>(out var energyT))
                energyT.text = $"x{FormatAmount(pkg.energyAmount)}";
            if (item.Find("txt") is { } priceTf && priceTf.TryGetComponent<Text>(out var priceT))
                priceT.text = $"¥{pkg.priceFen / 100f:0.##}";

            var capturedPkg = pkg;
            BindClick(item.Find("BuyBtn"), () => OnPackageClick(capturedPkg));
        }
    }

    private static void BindClick(Transform tf, UnityEngine.Events.UnityAction action)
    {
        if (tf != null && tf.TryGetComponent<Button>(out var btn))
            btn.onClick.AddListener(action);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void OnDetailClick()
        => UIManager.Inst.OpenPanel(PanelId.AICreditDetailPanel);

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

    private void OpenRules(AICreditRulesPanel.Tab tab)
        => UIManager.Inst.OpenPanel(PanelId.AICreditRulesPanel, tab);

    private static string FormatAmount(int amount) => $"{amount:N0}";

    [Button("展示奖励")]
    void test() => ShowReward();
}

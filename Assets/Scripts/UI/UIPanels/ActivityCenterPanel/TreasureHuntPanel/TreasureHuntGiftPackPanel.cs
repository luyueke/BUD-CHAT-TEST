using System.Collections.Generic;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using static ConfirmPaymentPanel;

public class TreasureHuntGiftPackPanel : MonoBehaviour
{
    [SerializeField] private CButton BuyBtn0;
    [SerializeField] private Text BuyTxt0;
    [SerializeField] private CButton BuyBtn1;
    [SerializeField] private Text BuyTxt1;
    [SerializeField] private CButton BuyBtn2;
    [SerializeField] private Text BuyTxt2;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private CButton HeadCycleBtn;
    [SerializeField] private CButton ChatBubbleBtn;
    [SerializeField] private List<CButton> ShovelBtns;
    [SerializeField] private CButton JumpBtn;

    private static readonly string[] AndroidIds =
    {
        "android_s15kidstouxiangkuang30",
        "android_s15kidsliaotiankuang68",
        "android_s15kidszhuyepifu168"
    };
    private static readonly string[] IosIds =
    {
        "ios_s15kidstouxiangkuang30",
        "ios_s15kidsliaotiankuang68",
        "ios_s15kidszhuyepifu168"
    };
    private static readonly string[] Prices = { "30", "68", "168" };
    private static readonly string[] PriceTexts = { "30元", "68元", "168元" };
    private const string PurchasedText = "已购买";

    private Text[] _buyTexts;
    private string _budOrderId;
    private int _currentBuyIndex;

    private void Awake()
    {
        _buyTexts = new[] { BuyTxt0, BuyTxt1, BuyTxt2 };

        if (BuyBtn0 != null) BuyBtn0.onClick.AddListener(() => OnBuyClick(0));
        if (BuyBtn1 != null) BuyBtn1.onClick.AddListener(() => OnBuyClick(1));
        if (BuyBtn2 != null) BuyBtn2.onClick.AddListener(() => OnBuyClick(2));
        if (ShowBtn != null) ShowBtn.onClick.AddListener(() => UIManager.Inst.OpenPanel(PanelId.TreasureHuntSkinPanel));
        if (JumpBtn != null) JumpBtn.onClick.AddListener(() => UIManager.Inst.OpenPanel<ActivityCenterPanel>(PanelId.ActivityCenterPanel, ActivityId.TreasureHunting.ToString()));
        if (HeadCycleBtn != null) HeadCycleBtn.onClick.AddListener(OnHeadCycleBtnClick);
        if (ChatBubbleBtn != null) ChatBubbleBtn.onClick.AddListener(OnChatBubbleBtnClick);
        if (ShovelBtns != null)
            foreach (var btn in ShovelBtns)
                if (btn != null) btn.onClick.AddListener(OnShovelBtnClick);

        IAPDataManager.Inst.GetProductInfo(res => { if (this != null) RefreshBtns(); });
        RefreshBtns();
    }

    private void RefreshBtns()
    {
        var list = IAPDataManager.Inst.productRes?.chasingWavesPackageList;
        string[] platformIds = Application.platform == RuntimePlatform.IPhonePlayer ? IosIds : AndroidIds;
        for (int i = 0; i < 3; i++)
        {
            if (_buyTexts[i] == null) continue;
            var pkg = list?.Find(x => x.productId == platformIds[i]);
            _buyTexts[i].text = (pkg != null && pkg.isPaid != 0) ? PurchasedText : PriceTexts[i];
        }
    }

    private ChasingWavesPackage GetPackage(int index)
    {
        var list = IAPDataManager.Inst.productRes?.chasingWavesPackageList;
        string[] platformIds = Application.platform == RuntimePlatform.IPhonePlayer ? IosIds : AndroidIds;
        return list?.Find(x => x.productId == platformIds[index]);
    }

    private void OnBuyClick(int index)
    {
        var pkg = GetPackage(index);
        if (pkg != null && pkg.isPaid != 0) return;

        string productId = Application.platform == RuntimePlatform.IPhonePlayer
            ? IosIds[index]
            : AndroidIds[index];

        ProductGemInfo gemInfo = new()
        {
            price = Prices[index],
            productId = productId,
            name = pkg.productName,
            desc = pkg.productDesc
        };

        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(
                PanelId.ConfirmPaymentPanel, gemInfo.price);
            panel.SetCallback(paymentType => StartPurchase(index, gemInfo, paymentType));
            return;
        }

        StartPurchase(index, gemInfo, PaymentType.Default);
    }

    private void StartPurchase(int index, ProductGemInfo gemInfo, PaymentType paymentType)
    {
        _currentBuyIndex = index;
        ShowPurchaseLoading();

        ChannelProductInfo channelProductInfo = gemInfo.toU8Info();
        IAPDataManager.Inst.GetProductOrderId(gemInfo.productId, null, (success, orderInfo) =>
        {
            _budOrderId = orderInfo?.budOrderId;
            if (!success || string.IsNullOrEmpty(_budOrderId))
            {
                HidePurchaseLoading();
                return;
            }
            channelProductInfo.extension = JsonConvert.SerializeObject(orderInfo);
            channelProductInfo.paymentType = (int)paymentType;
            channelProductInfo.cpOrderId = _budOrderId;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, OnBillingResult);
        UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void OnBillingResult(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        BillingResultResponse billingResult = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (billingResult.resultType == (int)BillingResultType.UserPaySuccess)
        {
            StartLooping();
        }
        else if (billingResult.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    private void StartLooping()
    {
        if (string.IsNullOrEmpty(_budOrderId)) return;

        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var processingPanel))
        {
            processingPanel.StartTimer(60, () =>
            {
                PurchaseStatusManager.Inst.StopLoop();
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }

        PurchaseStatusManager.Inst.StartLoop(_budOrderId, (orderResult, productGemInfo) =>
        {
            if (this == null) return;
            if (!orderResult) return;

            int index = _currentBuyIndex;
            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            var pkg = GetPackage(index);
            if (pkg != null) pkg.isPaid = 1;
            IAPDataManager.Inst.GetProductInfo(null);
            RefreshBtns();
            ShowReward(productGemInfo);
        });
    }

    private void OnHeadCycleBtnClick()
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        panel.PreviewAvatarFrame((AvatarFrameType)51);
    }

    private void OnChatBubbleBtnClick()
    {
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        panel.PreviewChatBubble(ChatBubblesType.ChatBubblesChasingWaves,
            "逐浪沙沙聊天气泡",
            "通过购买逐浪沙沙礼包解锁<color=#FF835D>逐浪沙沙聊天气泡</color>");
    }

    private void OnShovelBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, CurrencyType.Shovel);
    }

    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
    }

    private void ShowReward(ProductGemInfo productInfo)
    {
        if (productInfo == null) return;

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new();
        var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
        PgcUtils.CurrencyIconPath.TryGetValue(CurrencyType.Shovel, out string iconName);

        if (productInfo.productId == "android_s15kidszhuyepifu168" || productInfo.productId == "ios_s15kidszhuyepifu168")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = ProfileThemeManager.Inst.LoadThemeIcon(32, gameObject),
                RewardAmount = 1,
                rewardName = "逐浪沙沙主页皮肤"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 180,
                rewardName = "铲子",
            });
            AccountDataManager.Inst.UserInfo?.ownedHomepageSkinList?.Add(new AccountUserInfo.HomepageSkinInfo() { skinId = 32 });
        }
        else if (productInfo.productId == "android_s15kidsliaotiankuang68" || productInfo.productId == "ios_s15kidsliaotiankuang68")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = UserUIWidgetManager.Inst.GetChatBubbleIconByType(ChatBubblesType.ChatBubblesChasingWaves, gameObject),
                RewardAmount = 1,
                rewardName = "逐浪沙沙聊天气泡"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 70,
                rewardName = "铲子",
            });
        }
        else if (productInfo.productId == "android_s15kidstouxiangkuang30" || productInfo.productId == "ios_s15kidstouxiangkuang30")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = UserUIWidgetManager.Inst.GetHeadCycleImg("HeadCycle_51_Pre", gameObject),
                RewardAmount = 1,
                rewardName = "逐浪沙沙头像框"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 30,
                rewardName = "铲子",
            });
        }
        else
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = productInfo.gemNum,
                rewardName = "铲子",
            });
        }

        panel.ShowRewards(rewardItemDatas);
    }
}

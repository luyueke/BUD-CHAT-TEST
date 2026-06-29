using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class CollaborationLimitedGiftPackPanel : BasePanel<CollaborationLimitedGiftPackPanel>
{
#if UNITY_ANDROID
    private const string ProductId168 = "android_budxiazhuyepifu168";
    private const string ProductId30  = "android_budxiatouxiangkuang30";
#else
    private const string ProductId168 = "ios_budxiazhuyepifu168";
    private const string ProductId30  = "ios_budxiatouxiangkuang30";
#endif

    public CButton btn_closs;
    public Button btn_168;
    public Button btn_30;
    public GameObject cbtn_showTheme;

    private string _curOrderId;
    private Y2KSkateboardingPackage _pendingPkg;
    private string curProductid;

    public override void OnCreate()
    {
        base.OnCreate();
        if (btn_closs != null)
            btn_closs.onClick.AddListener(() => UIManager.Inst.ClosePanel(this));
        if (btn_168 != null)
            btn_168.onClick.AddListener(() => OnBuyClick(ProductId168));
        if (btn_30 != null)
            btn_30.onClick.AddListener(() => OnBuyClick(ProductId30));
        if (cbtn_showTheme != null)
            cbtn_showTheme.GetComponent<Button>().onClick.AddListener(() =>
                UIManager.Inst.OpenPanel<ProfileThemePreviewPanel>(PanelId.ProfileThemePreviewPanel, ProfileTheme.XiaXiaZai));
        IAPDataManager.Inst.GetProductInfo(res => { if (this != null) RefreshGiftPackState(); });
        RefreshGiftPackState();
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        RefreshGiftPackState();
    }

    public void RefreshGiftPackState()
    {
        var list = IAPDataManager.Inst.productRes?.babyShrimpPackageList;
        if (btn_168 != null)
        {
            var pkg168 = list?.Find(x => x.productId != null && x.productId.EndsWith(ProductId168));
            btn_168.gameObject.SetActive(pkg168 == null || pkg168.isPaid != 1);
        }
        if (btn_30 != null)
        {
            var pkg30 = list?.Find(x => x.productId != null && x.productId.EndsWith(ProductId30));
            btn_30.gameObject.SetActive(pkg30 == null || pkg30.isPaid != 1);
        }
    }

    private void OnBuyClick(string productId)
    {
        var list = IAPDataManager.Inst.productRes?.babyShrimpPackageList;
        var pkg = list?.Find(x => x.productId != null && x.productId.EndsWith(productId));
        if (pkg == null) return;
        if (pkg.isPaid == 1)
        {
            TipPanel.ShowToast("已购买过该礼包，限购1次");
            return;
        }
        

        _pendingPkg = pkg;
        curProductid = productId;
        //ShowReward();
        //return;
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, pkg.price);
            panel.SetCallback(paymentType => BuyOnePack(paymentType));
            return;
        }

        BuyOnePack(ConfirmPaymentPanel.PaymentType.Default);
    }

    private void BuyOnePack(ConfirmPaymentPanel.PaymentType paymentType)
    {
        if (_pendingPkg == null) return;
        DoPurchase(_pendingPkg, paymentType);
    }

    private void DoPurchase(Y2KSkateboardingPackage pkg, ConfirmPaymentPanel.PaymentType paymentType)
    {
        UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);

        IAPDataManager.Inst.GetProductOrderId(pkg.productId, null, (success, orderInfo) =>
        {
            _curOrderId = orderInfo?.budOrderId;
            if (!success || string.IsNullOrEmpty(_curOrderId))
            {
                HidePurchaseLoading();
                return;
            }

            var channelProductInfo = new ChannelProductInfo
            {
                productId   = pkg.productId,
                productName = pkg.productName,
                productDesc = pkg.productName,
                price       = pkg.price,
                extension   = JsonConvert.SerializeObject(orderInfo),
                cpOrderId   = _curOrderId,
                paymentType = (int)paymentType
            };

            MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, OnBillingFlowResult);
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow, JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void OnBillingFlowResult(string message)
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (string.IsNullOrEmpty(message)) { HidePurchaseLoading(); return; }

        var result = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (result == null) { HidePurchaseLoading(); return; }

        if (result.resultType == (int)BillingResultType.UserPaySuccess)
        {
            PurchaseStatusManager.Inst.StartLoop(_curOrderId, (isSuccess, productGemInfo) =>
            {
                HidePurchaseLoading();
                if (!isSuccess) return;
                AccountDataManager.Inst.BalanceInfo.Refresh();
                IAPDataManager.Inst.GetProductInfo(null);
                ShowReward();
            });
        }
        else if (result.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
        else
        {
            HidePurchaseLoading();
        }
    }

    private void HidePurchaseLoading()
    {
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
            UIManager.Inst.ClosePanel(panel);
    }

    private void ShowReward()
    {
        var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardItemDatas = new System.Collections.Generic.List<CommonRewardItemData>();
        var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
        const string iconName = "icn_common_shrimp_yuan";
        if (curProductid != null && curProductid == ProductId168)
        {
            rewardItemDatas.Add(new CommonRewardItemData
            {
                IconSp = ProfileThemeManager.Inst.LoadThemeIcon((int)ProfileTheme.XiaXiaZai,  gameObject),
                RewardAmount = 1,
                rewardName = "虾虾崽主页皮肤"
            });
            rewardItemDatas.Add(new CommonRewardItemData
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 30,
                rewardName = "虾元"
            });
            AccountDataManager.Inst.UserInfo?.ownedHomepageSkinList?.Add(
                new AccountUserInfo.HomepageSkinInfo { skinId = (int)ProfileTheme.XiaXiaZai });
        }
        else 
        {
            rewardItemDatas.Add(new CommonRewardItemData
            {
                IconSp = UserUIWidgetManager.Inst.GetHeadCycleImg("HeadCycle_52_Pre", gameObject),
                RewardAmount = 1,
                rewardName = "虾虾崽头像框"
            });
            rewardItemDatas.Add(new CommonRewardItemData
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 18,
                rewardName = "虾元"
            });
        }


        rewardPanel.ShowRewards(rewardItemDatas);

        UIManager.Inst.ClosePanel(this);
    }
}

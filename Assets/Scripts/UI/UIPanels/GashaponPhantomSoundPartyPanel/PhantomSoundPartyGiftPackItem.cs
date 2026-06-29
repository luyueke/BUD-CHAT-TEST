using System.Collections.Generic;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using static ConfirmPaymentPanel;

public class PhantomSoundPartyGiftPackItem : MonoBehaviour
{
    [SerializeField] private CButton buyBtn;
    [SerializeField] private Transform buyOverMask;

    private ProductGemInfo gemInfo = new ProductGemInfo();
    private string budOrderId = "";
    private int isPaid;

    private void Awake()
    {
        buyBtn.onClick.AddListener(OnBuyGiftClick);
    }

    public void SetData(Y2KSkateboardingPackage data)
    {
        gemInfo.price = data.price;
        gemInfo.gemNum = data.gemNum;
        gemInfo.productId = data.productId;
        gemInfo.name = data.productName;
        gemInfo.desc = data.productDesc;
        isPaid = data.isPaid;
        if (buyOverMask != null)
        {
            buyOverMask.gameObject.SetActive(data.isPaid != 0);
        }
    }

    private void OnBuyGiftClick()
    {
        if (isPaid != 0)
        {
            return;
        }
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, gemInfo.price);
            panel.SetCallback(paymentType =>
            {
                BuyCoin(paymentType);
            });
            return;
        }
        BuyCoin(PaymentType.Default);
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void BuyCoin(PaymentType paymentType)
    {
        if (gemInfo == null)
        {
            Debug.LogError("gemInfo is null");
            return;
        }
        ShowPurchaseLoading();

        ChannelProductInfo channelProductInfo = gemInfo.toU8Info();

        IAPDataManager.Inst.GetProductOrderId(gemInfo.productId, null, (b, info) =>
        {
            budOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(budOrderId))
            {
                HidePurchaseLoading();
                return;
            }

            channelProductInfo.extension = JsonConvert.SerializeObject(info);
            channelProductInfo.paymentType = (int)paymentType;
            channelProductInfo.cpOrderId = budOrderId;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }

    private void StartBillingFlow(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        BillingResultResponse billingResultResponse = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (billingResultResponse.resultType == (int)BillingResultType.UserPaySuccess)
        {
            StartLooping();
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    private void StartLooping()
    {
        if (string.IsNullOrEmpty(budOrderId))
        {
            return;
        }

        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            panel.StartTimer(60, () =>
            {
                PurchaseStatusManager.Inst.StopLoop();
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }

        PurchaseStatusManager.Inst.StartLoop(budOrderId, (orderResult, productGemInfo) =>
        {
            if (this == null)
            {
                return;
            }

            if (!orderResult)
            {
                return;
            }

            AccountDataManager.Inst.BalanceInfo.Refresh();
            HidePurchaseLoading();
            if (buyOverMask != null)
            {
                buyOverMask.gameObject.SetActive(true);
                isPaid = 1;
            }
            var giftPackPanel = GetComponentInParent<PhantomSoundPartyGiftPackPanel>();
            if (giftPackPanel != null) giftPackPanel.RefreshDataFromServer();
            ShowReward(productGemInfo);
        });
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
        if (productInfo == null)
        {
            return;
        }

        var rewardPanel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardItemDatas = new List<CommonRewardItemData>();
        var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
        PgcUtils.CurrencyIconPath.TryGetValue(CurrencyType.ZZZCoin, out string iconName);

        if (productInfo.productId == "android_budrongzhuyepifu168" || productInfo.productId == "ios_budrongzhuyepifu168")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = ProfileThemeManager.Inst.LoadThemeIcon((int)ProfileTheme.HomepageSkinZZZ, gameObject),
                RewardAmount = 1,
                rewardName = "绒天使主页皮肤"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 30,
                rewardName = "绒币",
            });
            AccountDataManager.Inst.UserInfo?.ownedHomepageSkinList?.Add(
                new AccountUserInfo.HomepageSkinInfo() { skinId = (int)ProfileTheme.HomepageSkinZZZ });
        }
        else if (productInfo.productId == "android_budrongtouxiangkuang30" || productInfo.productId == "ios_budrongtouxiangkuang30")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = UserUIWidgetManager.Inst.GetHeadCycleImg("HeadCycle_53_Pre", gameObject),
                RewardAmount = 1,
                rewardName = "绒天使头像框"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 18,
                rewardName = "绒币",
            });
        }
        else
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = productInfo.gemNum,
                rewardName = "绒币",
            });
        }
        rewardPanel.ShowRewards(rewardItemDatas);
    }
}

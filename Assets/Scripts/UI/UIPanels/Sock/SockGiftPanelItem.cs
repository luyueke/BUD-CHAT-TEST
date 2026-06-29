using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using static ConfirmPaymentPanel;

public class SockGiftPanelItem : MonoBehaviour
{
    [SerializeField] private CButton buyBtn;

    [SerializeField] private Transform buyOverMask;

    [SerializeField] private Text LimitText;

    private ProductGemInfo gemInfo = new ProductGemInfo();

    private string budOrderId = "";

    private int isPaid;

    private void Awake()
    {
        buyBtn.onClick.AddListener(OnBuyGiftClick);
    }

    public void SetData(MiaoCoinPackageData data)
    {
        gemInfo.price = data.price;
        gemInfo.gemNum = data.gemNum;
        gemInfo.productId = data.productId;
        gemInfo.name = data.name;
        gemInfo.desc = data.desc;
        isPaid = data.isPaid;
        if (buyOverMask != null)
        {
            buyOverMask.gameObject.SetActive(data.isPaid != 0);
        }
        if (LimitText != null)
        {
            LimitText.text = string.Format("限购:{0}/1", data.isPaid);
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
                BuyCatCoin(paymentType);
            });
            return;
        }

        BuyCatCoin(PaymentType.Default);
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void BuyCatCoin(PaymentType paymentType)
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
            if (LimitText != null)
            {
                LimitText.text = "限购:1/1";
            }
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

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        string iconName = "";
        var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
        PgcUtils.CurrencyIconPath.TryGetValue(CurrencyType.SockCoin, out iconName);


        if (productInfo.productId == "android_budwawazhuyepifu168" || productInfo.productId == "ios_budwawazhuyepifu168")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {

                IconSp = ProfileThemeManager.Inst.LoadThemeIcon(27, gameObject),
                RewardAmount = 1,
                rewardName = "袜袜幼稚园主页皮肤"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 30,
                rewardName = "袜币",
            });

            AccountDataManager.Inst.UserInfo?.ownedHomepageSkinList?.Add(new AccountUserInfo.HomepageSkinInfo() { skinId = 27 });
        }
        else if (productInfo.productId == "android_budwawatouxiangkuang30" || productInfo.productId == "ios_budwawatouxiangkuang30")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = UserUIWidgetManager.Inst.GetHeadCycleImg("HeadCycle_47_Pre", gameObject),
                RewardAmount = 1,
                rewardName = "袜袜幼稚园头像框"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 18,
                rewardName = "袜币",
            });
        }
        else
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = productInfo.gemNum,
                rewardName = "袜币",
            });
        }
        panel.ShowRewards(rewardItemDatas);
    }
}

using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using static ConfirmPaymentPanel;

public class XiaXiaZaiGiftPackItem : MonoBehaviour
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
        if(gemInfo == null)
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
            var giftPackPanel = GetComponentInParent<XiaXiaZaiGiftPackPanel>();
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

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> rewardItemDatas = new List<CommonRewardItemData>();
        string iconName = "";
        var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
        PgcUtils.CurrencyIconPath.TryGetValue(CurrencyType.MiaoCoin, out iconName);


        if (productInfo.productId == "android_budxiazhuyepifu168" || productInfo.productId == "ios_budxiazhuyepifu168")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {

                IconSp = ProfileThemeManager.Inst.LoadThemeIcon(33, gameObject),
                RewardAmount = 1,
                rewardName = "虾虾崽主页皮肤"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 30,
                rewardName = "虾元",
            });

            AccountDataManager.Inst.UserInfo?.ownedHomepageSkinList?.Add(new AccountUserInfo.HomepageSkinInfo() { skinId = 19 });
        }
        else if(productInfo.productId == "android_budxiatouxiangkuang30" || productInfo.productId == "ios_budxiatouxiangkuang30")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = UserUIWidgetManager.Inst.GetHeadCycleImg("HeadCycle_52_Pre", gameObject),
                RewardAmount = 1,
                rewardName = "虾虾崽头像框"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 18,
                rewardName = "虾元",
            });
        }
        else if(productInfo.productId == "android_budxiaxianichengkuang68" || productInfo.productId == "ios_budxiaxianichengkuang68")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = UserUIWidgetManager.Inst.GetNicknameBg(9, gameObject),
                RewardAmount = 1,
                rewardName = "虾虾崽昵称框"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 18,
                rewardName = "虾元",
            });
        }
        else if(productInfo.productId == "android_budxiaxiachenghao98" || productInfo.productId == "ios_budxiaxiachenghao98")
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = UserUIWidgetManager.Inst.GetTitleBg(32, gameObject),
                RewardAmount = 1,
                rewardName = "虾虾崽称号"
            });
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = 30,
                rewardName = "虾元",
            });
        }
        else
        {
            rewardItemDatas.Add(new CommonRewardItemData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject),
                RewardAmount = productInfo.gemNum,
                rewardName = "虾元",
            });
        }
        panel.ShowRewards(rewardItemDatas);
    }
}

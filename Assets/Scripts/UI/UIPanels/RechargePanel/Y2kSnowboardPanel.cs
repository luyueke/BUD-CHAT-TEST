using System.Collections.Generic;
using Game.Avatar;
using Game.Store;
using GameData.Gashapon;
using Newtonsoft.Json;
using UI.Base;
using UI.BaseWidgets;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;
using static ConfirmPaymentPanel;

public class Y2kSnowboardPanel : BasePanel<Y2kSnowboardPanel>
{
    [Header("UI相关")]
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton BuyBtn;
    [SerializeField] private CButton HaveBtn;
    [SerializeField] private RawImage bgImage;
    [SerializeField] private Text buyPriceText;
    [SerializeField] private Text discountText;

    [Header("人物展示")]
    //[SerializeField] private GameObject playerImageView;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    private bool isInit = false;
    private ProductGemInfo gemInfo = new ProductGemInfo();
    private string budOrderId = "";


    protected override void Start()
    {
        base.Start();
        PreviewBtn.onClick.AddListener(OnPriviewBtnClick);
        BuyBtn.onClick.AddListener(OnBuyClick);

        avatarCameraController.RotateTarget = characterRoot;
        avatarCameraController.cameraUpDownSpeed = -0.3f;

        IAPDataManager.Inst.GetProductInfo(res =>
        {
            if (res != null && res.y2KSkateboardingPackage != null )
            {
                Debug.Log(res.y2KSkateboardingPackage);
                res.y2KSkateboardingPackage.gemNum = 980;
                SetData(res.y2KSkateboardingPackage);
            }
        });

        //var data = new Y2KSkateboardingPackage() { desc = "测试", gemNum = 980, price = "98", productId = "a", name = "Y2k粉雾滑雪板" };
        //SetData(data);
    }

    public void SetData(Y2KSkateboardingPackage data)
    {
        gemInfo.price = data.price.ToString();
        gemInfo.gemNum = data.gemNum;
        gemInfo.productId = data.productId;
        gemInfo.name = data.productName;
        gemInfo.desc = data.productDesc;
        BuyBtn.gameObject.SetActive(data.isPaid == 0);
        HaveBtn.gameObject.SetActive(data.isPaid != 0);

        buyPriceText.text = string.Format("￥{0}  购买", data.price.ToString());
        discountText.text = data.discount;
    }



    private void OnBuyClick()
    {
        //ShowReward(gemInfo);
        //return;
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
        if (gemInfo == null || string.IsNullOrEmpty(gemInfo.productId))
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

        //var coinNum = productInfo.gemNum;
        //if (coinNum <= 0)
        //{
        //    return;
        //}
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        List<CommonRewardItemData> items = new List<CommonRewardItemData>();
        var pgcItem = new CommonRewardItemData()
        {
            RewardAmount = 1,
            rewardType = 5,
            pgcId = "11300358",
            //      rewardName = PgcUtils.GetRewardName((BUDRewardType)reward.rewardType)
        };
        items.Add(pgcItem);
        panel.ShowRewards(items);
        //var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        //panel.ShowRewards(new List<TaskRewardData>()
        //{
        //    new TaskRewardData()
        //    {
        //        num = 1,
        //        rewardType = (int)BUDRewardType.RewardY2kSnowboard,
        //    }
        //});

        BuyBtn.gameObject.SetActive(false);
        HaveBtn.gameObject.SetActive(true);
    }

    private void OnPriviewBtnClick()
    {
        var bundleShowpanel = UIManager.Inst.OpenPanel<RewardPreviewPanel>(PanelId.RewardPreviewPanel);
        bundleShowpanel.SetEventPreview(new List<string>() {
                    "11300358"}, "Y2k粉雾滑雪板","","", "","#BF8DFF", bgImage.texture, "#B8FAFD00");

    }



}
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

public class GetMoreGemsPanel : BasePanel<GetMoreGemsPanel>
{
    [SerializeField] private Button CloseBtn;
    [SerializeField] private IAPGemPackView gemPackView;
    [SerializeField] private Text tipText;

    private string budOrderId = "";
    private IapTrackData _iapTrackData = new IapTrackData();


    public override void OnShow(params object[] args)
    {
        if (args.Length > 0)
        {
            int difference = args[0] is int ? (int)args[0] : 0;
            if (difference > 0)
            {
                tipText.SetLocalText("需要<color=#905CFF>额外{0} BUD钻</color>进行该商品的购买",difference);
            }
            else
            {
                tipText.gameObject.SetActive(false);
            }
        }
        else
        {
            tipText.gameObject.SetActive(false);
        }
    }

    public override void OnCreate()
    {
        CloseBtn.onClick.RemoveAllListeners();
        CloseBtn.onClick.AddListener(CloseClick);

        gemPackView.OnClickProduct = s => { OnClickProduct(s); };
        // MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
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
            TrackEvent(billingResultResponse);
        }
        else if (billingResultResponse.resultType == (int)BillingResultType.RechargeFail)
        {
            HidePurchaseLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    private void TrackEvent(BillingResultResponse billingResultResponse)
    {
        Dictionary<string, object> trackData = new Dictionary<string, object>();
        float result;
        if (float.TryParse(_iapTrackData.price, NumberStyles.Float, CultureInfo.InvariantCulture,out result))
        {
            trackData.Add("priceNum", result);
        }

        trackData.Add("item", _iapTrackData.item);
        bool isUploadData = !AnalyticsManager.Inst.ContainOrderID(budOrderId);
        if (isUploadData)
        {
            trackData.Add("budOrderId", budOrderId ?? "");
            trackData.Add("method", "GetMoreGemsPanel");
            AnalyticsManager.Inst.Track(AnalyticsEventName.TOP_UP_SUCCESS, trackData);
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

            if (orderResult)
            {
                AccountDataManager.Inst.BalanceInfo.Refresh();
                HidePurchaseLoading();
                ShowReward(productGemInfo);
            }
        });
    }


    private void CloseClick()
    {
        CloseSelf();
    }

    private void OnClickProduct(ProductGemInfo gemInfo)
    {
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, gemInfo.price);
            panel.SetCallback(paymentType => { BuyGem(gemInfo, paymentType); });
            return;
        }

        BuyGem(gemInfo, ConfirmPaymentPanel.PaymentType.Default);
    }

    private void BuyGem(ProductGemInfo gemInfo, ConfirmPaymentPanel.PaymentType paymentType)
    {
        var productId = gemInfo.productId;
        if (string.IsNullOrEmpty(productId))
        {
            LoggerUtils.LogError($"[IAP] Not get productId {gemInfo}");
            return;
        }

        _iapTrackData.item = gemInfo.gemNum + "钻";
        _iapTrackData.price = gemInfo.price;
        _iapTrackData.channel = "";

        ShowPurchaseLoading();
        IAPDataManager.Inst.GetProductOrderId(productId, null,(b, info) =>
        {
            budOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(budOrderId))
            {
                HidePurchaseLoading();
                return;
            }

            var productInfo = gemInfo.toU8Info();
            productInfo.extension = JsonConvert.SerializeObject(info);
            productInfo.cpOrderId = budOrderId;
            productInfo.paymentType = (int)paymentType;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(productInfo));
        });
    }

    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
        // processingPanel.StartTimer(60, () => { PurchaseStatusManager.Inst.StopLoop(); });
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

        var coinNum = productInfo.gemNum;
        if (coinNum <= 0)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<TaskRewardData>()
        {
            new TaskRewardData()
            {
                num = coinNum,
                rewardType =  (int)BUDRewardType.RewardGem,
            }
        });
    }
}
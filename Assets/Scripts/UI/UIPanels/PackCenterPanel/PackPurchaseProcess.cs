using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

public class PackPurchaseProcess
{
    protected Action<string,ProductGemInfo> OnSuccess;
    protected Action<string> OnFail;
    
    public string budOrderId = "";
    private IapTrackData _iapTrackData = new IapTrackData();
    
    private const string errorTips1 = "can not find the ordierId";
    
    public void StartPurchaseProcess(ChannelProductInfo channelProductInfo,ConfirmPaymentPanel.PaymentType paymentType,string productName,Action<string,ProductGemInfo> onSuccess,Action<string> onFail = null)
    {
        OnSuccess = onSuccess;
        OnFail = onFail;
        
        string productId = channelProductInfo.productId;
        _iapTrackData.price = channelProductInfo.price;
        _iapTrackData.item = productName;
        ShowPurchaseLoading();
        IAPDataManager.Inst.GetProductOrderId(productId, null,(b, info) =>
        {
            budOrderId = info?.budOrderId;
            if (!b || string.IsNullOrEmpty(budOrderId))
            {
                HidePurchaseLoading();
                OnFail?.Invoke(errorTips1 + " :"+productId);
                return;
            }

            channelProductInfo.extension = JsonConvert.SerializeObject(info);
            channelProductInfo.cpOrderId = budOrderId;
            channelProductInfo.paymentType = (int)paymentType;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelProductInfo));
        });
    }
    
    private void ShowPurchaseLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, StartBillingFlow);
        PurchaseProcessingPanel processingPanel =
            UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }
    
    private void HidePurchaseLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var panel))
        {
            UIManager.Inst.ClosePanel(panel);
        }
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
            OnSuccess?.Invoke(budOrderId,productGemInfo);
        });
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
        trackData.Add("method", "PackPurchaseProcess");
        AnalyticsManager.Inst.Track(AnalyticsEventName.TOP_UP_SUCCESS, trackData);
    }
}

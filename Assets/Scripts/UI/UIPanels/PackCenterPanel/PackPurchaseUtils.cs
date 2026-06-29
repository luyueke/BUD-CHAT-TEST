using System;

public class PackPurchaseUtils : GlobalInstance<PackPurchaseUtils>
{
    public void StartPurchase(ChannelProductInfo channelProductInfo, ConfirmPaymentPanel.PaymentType paymentType,
        string productName,Action<string,ProductGemInfo>onSuccess,Action<string>onFail = null)
    {
        PackPurchaseProcess process = new PackPurchaseProcess();
        process.StartPurchaseProcess(channelProductInfo,paymentType,productName,onSuccess,onFail);
    }
    
    public void StartPurchase(ProductInfo productInfo, ConfirmPaymentPanel.PaymentType paymentType,
        string productName,Action<string,ProductGemInfo>onSuccess,Action<string>onFail = null)
    {
        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        PackPurchaseProcess process = new PackPurchaseProcess();
        process.StartPurchaseProcess(channelProductInfo,paymentType,productName,onSuccess,onFail);
    }
}

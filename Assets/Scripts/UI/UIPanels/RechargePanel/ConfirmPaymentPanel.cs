using System;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine.UI;


public class ConfirmPaymentPanel : BasePanel<ConfirmPaymentPanel>
{
    
    public enum PaymentType
    {
        Default,
        Alipay = 1,
        Wxpay = 2
    }
    public Text priceText;
    public CButton wxpay;
    public CButton alipay;
    public CButton closeBtn;

    private string price;

    private Action<PaymentType> _action;


    public override void OnCreate()
    {
        base.OnCreate();
        
        wxpay.onClick.AddListener(() =>
        {
            _action.Invoke(PaymentType.Wxpay);
            CloseSelf();
        });
        
        alipay.onClick.AddListener(() =>
        {
            _action.Invoke(PaymentType.Alipay);
            CloseSelf();
        });
        
        closeBtn.onClick.AddListener(() =>
        {
            CloseSelf();
        });
    }

    public override void OnShow(params object[] args)
    {
        var price = args[0] as string;
        priceText.text = "\u00a5" +price;
    }

    public void SetCallback(Action<PaymentType> paymentAction)
    {
        _action = paymentAction;
    }
    
    
}
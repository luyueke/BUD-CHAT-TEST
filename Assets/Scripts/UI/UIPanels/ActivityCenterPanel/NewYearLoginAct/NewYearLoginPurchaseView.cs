using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NewYearLoginPurchaseView : MonoBehaviour {
    [SerializeField] public Button Btn_BgMask;
    [SerializeField] public Button Btn_Buy;
    [SerializeField] private Sprite rewardSp;
    private Action<bool> onPurchaseCallBack;
    private bool onceFlag;

    private void Awake() {
        Btn_BgMask.onClick.AddListener(() => {
            onPurchaseCallBack?.Invoke(false);
        });
        Btn_Buy.onClick.AddListener(OnBuyClick);
    }

    public void SetPurchaseCallBack(Action<bool> callBack) {
        onPurchaseCallBack = callBack;
    }


    private void OnBuyClick() {
        //ShowReward();
        //onPurchaseCallBack?.Invoke(true);
        //return;
        ProductInfo productInfo = new ProductInfo() {
            productId = IAPDataManager.Inst.GetProductId(ProductIdType.product_budxinnian2026),
            productName = "福马鎏金卡",
            productDesc = "福马鎏金卡",
            price = IAPDataManager.Inst.GetPriceInfo(ProductIdType.product_budxinnian2026).price
        };
        if (IAPDataManager.Inst.IsOfficialChannel()) {
            ConfirmPaymentPanel panel =
                UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, productInfo.price);
            panel.SetCallback(paymentType => {
                Purchase(paymentType, productInfo, productInfo.productName);
            });
            return;
        }

        Purchase(ConfirmPaymentPanel.PaymentType.Default, productInfo, productInfo.productName);
    }

    private void Purchase(ConfirmPaymentPanel.PaymentType paymentType, ProductInfo productInfo, string productName) {
        ChannelProductInfo channelProductInfo = productInfo.toU8Info();
        PackPurchaseProcess process = new PackPurchaseProcess();
        process.StartPurchaseProcess(channelProductInfo, paymentType, productName,
            (string orderId, ProductGemInfo productGemInfo) => {
                ShowReward();
                onPurchaseCallBack?.Invoke(true);
            });
    }


    private void ShowReward() {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(new List<CommonRewardItemData>() {
            new CommonRewardItemData() {
                IconSp = rewardSp,
                RewardAmount = 1,
                rewardName = "福马鎏金卡"
            }
        }, IsResize: true);
    }
}

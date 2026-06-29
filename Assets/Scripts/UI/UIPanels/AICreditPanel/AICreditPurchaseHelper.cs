using Network;
using Network.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using static ConfirmPaymentPanel;

/// <summary>
/// AI 能量套餐数据（月卡 + 能量包）。
/// </summary>
[Serializable]
public class AICreditPackage
{
    public int    energyAmount; // 基础能量
    public int    bonusAmount;  // 赠送能量
    public int    priceFen;     // 价格（分）
    public string productId;    // IAP 商品 ID（平台相关）
}

/// <summary>
/// AI 能量相关购买流程公共辅助类。
/// 每个面板持有一个实例（避免跨面板回调冲突）。
/// </summary>
public class AICreditPurchaseHelper
{
    // ── 平台 product ID（iOS / Android）──────────────────────────────
    //   静态构造函数在类首次访问时执行，此时 Application.platform 已可用

    public static readonly string MonthlyCardProductId;
    public static readonly ProductGemInfo MonthlyCardGemInfo;

    // 克隆次数（供 CabinToneCloneTimePanel 使用）
    public static readonly ProductGemInfo Clone1GemInfo;
    public static readonly ProductGemInfo Clone3GemInfo;

    // 能量套餐
    public static readonly List<AICreditPackage> Packages;

    static AICreditPurchaseHelper()
    {
        bool ios = Application.platform == RuntimePlatform.IPhonePlayer;

        MonthlyCardProductId = ios ? "ios_budboxvip30" : "android_budboxvip30";
        MonthlyCardGemInfo = new ProductGemInfo
        {
            productId = MonthlyCardProductId,
            price     = "30",
            name      = "AI能量月卡",
            desc      = "月卡用户每日10000能量，永不过期",
        };

        Clone1GemInfo = new ProductGemInfo
        {
            productId = ios ? "ios_budboxcopy10" : "android_budboxcopy10",
            price     = "6",
            name      = "声音克隆次数×1",
        };

        Clone3GemInfo = new ProductGemInfo
        {
            productId = ios ? "ios_budboxcopy25" : "android_budboxcopy25",
            price     = "15",
            name      = "声音克隆次数×3",
        };

        Packages = new List<AICreditPackage>
        {
            new() { energyAmount = 60000,   bonusAmount = 0,      priceFen = 600,
                    productId = ios ? "ios_budboxenergy6"   : "android_budboxenergy6" },
            new() { energyAmount = 300000,  bonusAmount = 50000,  priceFen = 3000,
                    productId = ios ? "ios_budboxenergy30"  : "android_budboxenergy30" },
            new() { energyAmount = 680000,  bonusAmount = 120000, priceFen = 6800,
                    productId = ios ? "ios_budboxenergy68"  : "android_budboxenergy68" },
            new() { energyAmount = 1280000, bonusAmount = 320000, priceFen = 12800,
                    productId = ios ? "ios_budboxenergy128" : "android_budboxenergy128" },
        };
    }

    // ── IAP 购买流程（月卡、克隆次数等）────────────────────────────

    private readonly MonoBehaviour _context;
    private string _budOrderId;
    private Action _onSuccess;

    public AICreditPurchaseHelper(MonoBehaviour context)
    {
        _context = context;
    }

    /// <summary>
    /// 发起 IAP 购买。官方渠道先弹支付方式确认，否则直接购买。
    /// 购买成功后刷新余额并调用 onSuccess。
    /// </summary>
    public void BuyIAP(ProductGemInfo gemInfo, Action onSuccess)
    {
        _onSuccess = onSuccess;
        if (IAPDataManager.Inst.IsOfficialChannel())
        {
            var panel = UIManager.Inst.OpenPanel<ConfirmPaymentPanel>(PanelId.ConfirmPaymentPanel, gemInfo.price);
            panel.SetCallback(pt => DoIAPFlow(gemInfo, pt));
            return;
        }
        DoIAPFlow(gemInfo, PaymentType.Default);
    }

    private void DoIAPFlow(ProductGemInfo gemInfo, PaymentType paymentType)
    {
        ShowLoading();
        var channelInfo = gemInfo.toU8Info();
        IAPDataManager.Inst.GetProductOrderId(gemInfo.productId, null, (ok, info) =>
        {
            _budOrderId = info?.budOrderId;
            if (!ok || string.IsNullOrEmpty(_budOrderId))
            {
                HideLoading();
                return;
            }
            channelInfo.extension   = JsonConvert.SerializeObject(info);
            channelInfo.paymentType = (int)paymentType;
            channelInfo.cpOrderId   = _budOrderId;
            MobileInterface.Instance.SendMessage(MobileInterfaceDefine.startBillingFlow,
                JsonConvert.SerializeObject(channelInfo));
        });
    }

    private void OnBillingResult(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        var result = JsonConvert.DeserializeObject<BillingResultResponse>(message);
        if (result.resultType == (int)BillingResultType.UserPaySuccess)
            StartPolling();
        else if (result.resultType == (int)BillingResultType.RechargeFail)
        {
            HideLoading();
            PurchaseStatusManager.Inst.StopLoop();
        }
    }

    private void StartPolling()
    {
        if (string.IsNullOrEmpty(_budOrderId)) return;
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var p))
        {
            p.StartTimer(60, () =>
            {
                PurchaseStatusManager.Inst.StopLoop();
                MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
            });
        }
        PurchaseStatusManager.Inst.StartLoop(_budOrderId, (ok, _) =>
        {
            if (_context == null || !ok) return;
            AccountDataManager.Inst.BalanceInfo.Refresh();
            HideLoading();
            _onSuccess?.Invoke();
        });
    }

    private void ShowLoading()
    {
        MobileInterface.Instance.AddClientRespose(MobileInterfaceDefine.startBillingFlow, OnBillingResult);
        UIManager.Inst.OpenPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel);
    }

    private void HideLoading()
    {
        MobileInterface.Instance.DelClientResponse(MobileInterfaceDefine.startBillingFlow);
        if (UIManager.Inst.TryFindPanel<PurchaseProcessingPanel>(PanelId.PurchaseProcessingPanel, out var p))
            UIManager.Inst.ClosePanel(p);
    }

    // ── 能量套餐 HTTP 购买（无 IAP，后端直接扣款）────────────────────

    public static void BuyPackage(string productId, Action onSuccess)
    {
        NetworkManager.Inst.SendHttpRequest(
            "/ai/credit/purchase",
            HttpMethod.POST,
            JsonConvert.SerializeObject(new { productId }),
            onReceive: _ => onSuccess?.Invoke(),
            onFail:    _ => TipPanel.ShowToast("购买失败，请稍后重试"));
    }
}

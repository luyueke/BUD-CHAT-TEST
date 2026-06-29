using Sirenix.OdinInspector;
using System.Collections.Generic;
using UI.Base;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AI能量明细弹窗
/// 扣费顺序：每日 >> 赠送(临期先扣) >> 永久
/// </summary>
public class AICreditDetailPanel : BasePanel<AICreditDetailPanel>
{
    private Button _closeBtn;
    private Button _sureBtn;

    public Text txt1;        // "200<color=#3B3650>/1000</color>"
    public Text txt2;        // "今日已用800,还可用200"
    public Text txt3;        // "共xx"

    public AICreditDetailItem aICreditDetailItemGo; // 限时赠送 item 模板
    public GameObject item3Go;    // 永久充值区块，仅在 permanentAmount > 0 时显示
    public Text permanentTxt;     // 永久能量数值

    private readonly List<AICreditDetailItem> _giftItems = new();

    public override void OnCreate()
    {
        base.OnCreate();

        var closeTf = transform.Find("BaseLayout2D/UIContainer/CloseBtn");
        if (closeTf != null && closeTf.TryGetComponent(out _closeBtn))
            _closeBtn.onClick.AddListener(CloseSelf);

        var sureTf = transform.Find("BaseLayout2D/UIContainer/SureBtn");
        if (sureTf != null && sureTf.TryGetComponent(out _sureBtn))
            _sureBtn.onClick.AddListener(CloseSelf);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        FetchAndRefresh();
    }

    private void FetchAndRefresh()
    {
        AICreditManager.Inst.GetAICreditDetail(
            onSuccess: OnDetailReceived,
            onFail: err => Debug.LogError("[AICreditDetailPanel] GetAICreditDetail error: " + err)
        );
    }

    private void OnDetailReceived(AICreditDetailData detail)
    {
        if (this == null) return;

        // ── 每日免费区 ──────────────────────────────────────────────────────
        var daily = detail.daily ?? new AICreditDetailDaily();
        int dailyRemain = daily.totalAmount - daily.usedAmount;
        if (dailyRemain < 0) dailyRemain = 0;

        if (txt1 != null)
            txt1.text = $"{FormatAmount(dailyRemain)}<color=#3B3650>/{FormatAmount(daily.totalAmount)}</color>";
        if (txt2 != null)
            txt2.text = $"今日已用{FormatAmount(daily.usedAmount)},还可用{FormatAmount(dailyRemain)}";

        // ── 限时赠送区 ──────────────────────────────────────────────────────
        var expiring = detail.expiring ?? new AICreditDetailExpiring();
        if (txt3 != null)
            txt3.text = $"共{FormatAmount(expiring.totalAmount)}";

        OnExpiringItemsReceived(expiring.details ?? new List<AICreditDetailExpiringItem>());

        // ── 永久充值区 ──────────────────────────────────────────────────────
        var permanent = detail.permanent ?? new AICreditDetailPermanent();
        if (item3Go != null) item3Go.SetActive(permanent.totalAmount > 0);
        if (permanentTxt != null) permanentTxt.text = FormatAmount(permanent.totalAmount);
    }

    private void OnExpiringItemsReceived(List<AICreditDetailExpiringItem> items)
    {
        if (this == null || aICreditDetailItemGo == null) return;
        ClearGiftItems();

        var container = aICreditDetailItemGo.transform.parent;
        aICreditDetailItemGo.gameObject.SetActive(false);

        foreach (var item in items)
        {
            var go = Instantiate(aICreditDetailItemGo.gameObject, container);
            go.SetActive(true);
            if (go.TryGetComponent<AICreditDetailItem>(out var detailItem))
            {
                detailItem.Init(item);
                _giftItems.Add(detailItem);
            }
        }

        if (item3Go != null)
            UICommonUtils.RefreshLayout(item3Go.transform.parent);
    }

    private void ClearGiftItems()
    {
        foreach (var item in _giftItems)
            if (item != null) Destroy(item.gameObject);
        _giftItems.Clear();
    }

    private static string FormatAmount(int amount) => $"{amount:N0}";

    [Button("测试数据")]
    void testData()
    {
        var mockDetail = new AICreditDetailData
        {
            daily = new AICreditDetailDaily { usedAmount = 800, totalAmount = 1000 },
            expiring = new AICreditDetailExpiring
            {
                totalAmount = 53500,
                details = new List<AICreditDetailExpiringItem>
                {
                    new() { desc = "小雅礼包", amount = 28000, duration = 86400 },
                    new() { desc = "阿凯礼包", amount = 25500, duration = 86400 * 5 },
                }
            },
            permanent = new AICreditDetailPermanent { totalAmount = 128000 },
        };
        OnDetailReceived(mockDetail);
    }
}

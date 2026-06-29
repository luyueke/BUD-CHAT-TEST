using Message;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ai能量币组件(永久/每日/赠送) 显示对应数量。点击跳转能量商城,可设置是否跳转
/// </summary>
public class AICreditWidget : MonoBehaviour
{
    public enum AICreditType
    {
        Daily,      // 每日能量
        Gift,       // 赠送能量
        Permanent,  // 永久能量
    }

    [SerializeField] private Text amountTxt;
    [SerializeField] private Button shopBtn;
    [SerializeField] private Button detailBtn;
    [SerializeField] private Button rulesBtn;

    [SerializeField] private GameObject dailyGo;
    [SerializeField] private GameObject giftGo;
    [SerializeField] private GameObject permanentGo;


    public AICreditType creditType = AICreditType.Daily;
    public bool canJump = true;
    /// <summary>赠送/永久能量为 0 时是否隐藏整个组件</summary>
    public bool hideIfZero = false;

    private void Awake()
    {
        MessageHelper.AddListener(MessageName.OnAICreditChange, RefreshUI);
        if (shopBtn != null) shopBtn.onClick.AddListener(OnShopClick);
        if (detailBtn != null) detailBtn.onClick.AddListener(OnDetailClick);
        if (rulesBtn != null) rulesBtn.onClick.AddListener(OnRulesClick);
    }

    private void Start()
    {
        RefreshUI();
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.OnAICreditChange, RefreshUI);
    }

    private void RefreshUI()
    {
        var credit = AccountDataManager.Inst.BalanceInfo.AiCredit;
        if (credit == null) return;

        int amount = creditType switch
        {
            AICreditType.Daily => credit.dailyAmount,
            AICreditType.Gift => credit.expiringAmount,
            AICreditType.Permanent => credit.permanentAmount,
            _ => 0,
        };

        if (hideIfZero) gameObject.SetActive(amount > 0);
        if (amountTxt != null) amountTxt.text = FormatAmount(amount);

        if (dailyGo != null)
        {
            dailyGo?.SetActive(creditType == AICreditType.Daily);
        }
        if (giftGo != null)
        {
            giftGo?.SetActive(creditType == AICreditType.Gift);
        }
        if (permanentGo != null)
        {
            permanentGo?.SetActive(creditType == AICreditType.Permanent);
        }
    }



    private void OnShopClick()
    {
        if (canJump)
            UIManager.Inst.OpenPanel(PanelId.AICreditShopPanel);
    }

    private void OnDetailClick()
    {
        UIManager.Inst.OpenPanel(PanelId.AICreditDetailPanel);
    }

    private void OnRulesClick()
    {
        UIManager.Inst.OpenPanel(PanelId.AICreditRulesPanel);
    }

    private static string FormatAmount(int amount)
    {
        if (amount >= 10000)
            return $"{amount / 10000f:0.#}万";
        return amount.ToString();
    }
}

using System;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class EventCurrencyView : MonoBehaviour
{
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Text currencyNum;
    [SerializeField] private CButton exchangeBtn;
    [SerializeField] private CButton previewBtn;

    [SerializeField] private string currencyName;
    [SerializeField] private string currencyDesc;
    [SerializeField] private string activityId;

    public Action<int> balanceChangeAction;
    public int balance;

    /// <summary>
    /// 货币兑换比例, 兼容之前，默认为1, currency / Gem
    /// </summary>
    public float rate = 1;

    void Start()
    {
        previewBtn?.onClick.AddListener(OnShowPreview);
        exchangeBtn?.onClick.AddListener(OnExchange);
    }

    public void UpdateCurrency(int num)
    {
        balance = num;
        currencyNum.text = num.ToString();
    }

    private void OnShowPreview()
    {
        if (iconSprite == null)
        {
            return;
        }
       var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
       var ownedStr = $"当前拥有：{currencyNum.text}";
       panel.UpdateUI(iconSprite, currencyName, ownedStr, currencyDesc);
    }

    public void OnExchange()
    {
        if (string.IsNullOrEmpty(activityId) || string.IsNullOrEmpty(currencyName))
        {
            LoggerUtils.LogError("[EventCurrencyView] exChange fail");
            return;
        }

        ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
        exchangeCoinPanel.SetPianoCoinEventUI(iconSprite, activityId, i =>
        {
            if (this == null)
            {
                return;
            }
            balanceChangeAction?.Invoke(i);
            UpdateCurrency(i);
        }, currencyName, rate);
    }

}

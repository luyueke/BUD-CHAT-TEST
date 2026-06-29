using System;

using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class PetStudioCurrencyView : MonoBehaviour
{
    [SerializeField] private Sprite iconSprite;
    [SerializeField] private Text currencyNum;
    [SerializeField] private CButton exchangeBtn;
    [SerializeField] private CButton previewBtn;

    public Action<int> balanceChangeAction;

    public int balance;
    
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
       panel.UpdateUI(iconSprite, "青蛙活动币", ownedStr, "青蛙活动币，活动结束后自动失效，可通过完成活动任务领取，也可通过钻石兑换。");
    }
    
    public void OnExchange()
    {
        ExchangeCoinPanel exchangeCoinPanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
        // exchangeCoinPanel.SetPianoCoinEventUI(iconSprite, "PetStudio", i =>
        // {
        //     if (this == null)
        //     {
        //         return;
        //     }
        //     balanceChangeAction?.Invoke(i);
        //     UpdateCurrency(i);
        // });
    }
    
}

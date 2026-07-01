using System;
using UI.Manager;
using UI.Base;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class RechargeBuyTipsPanel : BasePanel<RechargeBuyTipsPanel>
{
    public CButton buy_btn;
    public CButton close_btn;
    public Text txt_dic1;
    public Text txt_dic2;
    public RectTransform dic1Rect;
    public RectTransform dic2Rect;
    public Image icon1;

    /// <summary>
    /// 余额不足时调用：txt_dic1显示缺少的货币数量，icon1显示对应货币图标，txt_dic2显示需要消耗的钻石数量
    /// </summary>
    public void Init(int missingAmount, int gemsNeeded, CurrencyType currencyType, Action onConfirm)
    {
        if (txt_dic1 != null) txt_dic1.text = "x" + missingAmount.ToString();
        if (txt_dic2 != null) txt_dic2.text = "x" + gemsNeeded.ToString();
        if (icon1 != null) icon1.sprite = PgcUtils.LoadCurrencyIcon(currencyType, gameObject);

        buy_btn.onClick.RemoveAllListeners();
        buy_btn.onClick.AddListener(() =>
        {
            CloseSelf();
            onConfirm?.Invoke();
        });

        close_btn.onClick.RemoveAllListeners();
        close_btn.onClick.AddListener(() => CloseSelf());
        LayoutRebuilder.ForceRebuildLayoutImmediate(dic1Rect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(dic2Rect);
    }
}

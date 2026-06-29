using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PriceDisplayWidget : MonoBehaviour
{
    [SerializeField]private Image priceIcon;
    [SerializeField]private Text priceNumTxt;
    [SerializeField]private Text priceUnitTxt;
    [SerializeField]private RectTransform layout;

    public void SetPrice(CurrencyType purchaseType, int price, string suffix = "")
    {
        if (purchaseIcons.TryGetValue(purchaseType, out var iconName))
        {
            var commonPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.Common);
            priceIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(commonPath, iconName, gameObject);
            // priceIcon.SetNativeSize();
        }

        priceNumTxt.text = price.ToString();
        if (string.IsNullOrEmpty(suffix))
        {
            priceUnitTxt.text = suffix;
        }
        else
        {
            priceUnitTxt.SetLocalText(suffix);
        }



        Refresh();
    }

    private Dictionary<CurrencyType, string> purchaseIcons = new Dictionary<CurrencyType, string>()
    {
        {CurrencyType.Gem, "icn_store_gem"},
        {CurrencyType.Coin, "icn_store_coin"},
        {CurrencyType.Badge, "icn_store_badge"},
        {CurrencyType.PinkCoin, "icn_common_pink_big"},
        {CurrencyType.GreenCoin, "icn_store_green"},
        {CurrencyType.LuckyCoin, "icn_common_lucky_big"},
        {CurrencyType.ChristmasCoin, "icn_common_christmas_big"},
        {CurrencyType.MagicCoin, "icn_common_magic_big"},
        {CurrencyType.GiftTicket, "icn_common_giftTicket_big"}
    };

    void Refresh()
    {
        if (layout == null)
        {
            return;
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
    }
}

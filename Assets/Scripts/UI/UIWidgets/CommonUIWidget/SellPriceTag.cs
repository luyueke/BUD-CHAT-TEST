using GameData.Base;
using GameData.Gashapon;
using UnityEngine;
using UnityEngine.UI;

public class SellPriceTag : MonoBehaviour
{
    [SerializeField] private Text priceTxt;
    [SerializeField] private Image priceIcon;

    public void SetPrice(PaymentInfo paymentInfol)
    {
        // int permissionScope = permission == null ? 0 : permission.scope;
        // if (paymentInfo == null)
        // {
        //     SetPrice(0, 0, permissionScope);
        // }
        // else
        // {
        //     SetPrice(paymentInfo.currencyType, paymentInfo.price, permissionScope);
        // }
    }

    public void SetPrice(CurrencyType purchaseType, int price, int permissionScope = 0)
    {
        // SetPrice((int)purchaseType, price, permissionScope);
    }

    public void SetPrice(int purchaseType, int price, int permissionScope = 0)
    {
        // if (permissionScope == (int)UGCShareWithView.ShareWith.Private)
        // {
        //     priceIcon.gameObject.SetActive(false);
        // }
        // else if (price <= 0)
        // {
        //     priceIcon.gameObject.SetActive(false);
        // }
        // else
        // {
        //     priceIcon.gameObject.SetActive(true);
        //     var iconPath = $"currency_small_{purchaseType}";
        //     var sprite = XAssetLoaderMgr.GetSpriteByCommonAltas(iconPath);
        //     priceIcon.sprite = sprite;
        //     priceTxt.text = DataUtils.NumToString(price);
        // }
    }

    public void SetOwnState()
    {
        priceIcon.gameObject.SetActive(false);
        priceTxt.SetLocalText("已拥有");
    }

    public void SetFree()
    {
        priceIcon.gameObject.SetActive(false);
        priceTxt.SetLocalText("免费");
    }

    public void SetPaymentInfo(PaymentInfo info)
    {
        if (info != null && info.price != 0)
        {
            priceIcon.gameObject.SetActive(true);
            priceTxt.text = info.price.ToString();
        }
        else
        {
            priceIcon.gameObject.SetActive(false);
            priceTxt.SetLocalText("免费");
        }
    }
}

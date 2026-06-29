using EventTracking;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class IAPGemItemView : MonoBehaviour
{
    [SerializeField] private Text priceTxt;
    [SerializeField] private Text gemNumTxt;
    [SerializeField] private Button mainBtn;
    [SerializeField] private RectTransform layout;
    [SerializeField] private Text unitText;
    [SerializeField] private Text bonusText;
    [SerializeField] private GameObject bonusTag;
    
    public ProductGemInfo productGem { get; private set; }
    private Action<ProductGemInfo> onClick;

    private void Start()
    {
        mainBtn?.onClick.AddListener(OnClick);
#if PACKAGE_TYPE_US
        unitText?.gameObject.SetActive(false);
#endif
    }

    public void SetData(ProductGemInfo product, Action<ProductGemInfo> clickAction)
    {
        productGem = product;
        onClick = clickAction;

        var fixedPrice = product.price.ToString();
        priceTxt.SetText(fixedPrice);
        gemNumTxt.SetText(product.gemNum.ToString());

        if (layout != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout);
        }
        
#if PACKAGE_TYPE_US
        var priceInfo = IAPDataManager.Inst.GetPriceInfo(product.productId);
        if (priceInfo != null)
        {
            priceTxt.SetText(priceInfo.priceLocal);
        }
#endif
    }

    public void SetPrice(string value)
    {
         priceTxt.SetText(value);
    }

    public void SetGemNum(int value)
    {
        gemNumTxt.SetText(value + "");
    }

    public void SetBonusNum(int num)
    {
        bonusTag.SetActive(num > 0);
        bonusText.SetText("+" + num);
    }

    private void OnClick()
    {
        if (productGem == null)
        {
            return;
        }
        if (FittingRoomPanel.curTab == MainTabs.Tab.Ugc)
        {
            LoadEvent.ReportPopupStatus("BuyDiamondsClicked", "ClickedBuygems");
        }
        onClick?.Invoke(productGem);
    }
}

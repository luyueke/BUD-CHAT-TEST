using System;
using System.Collections.Generic;
using GameData.PgcData;
using UI.BaseWidgets;
using UnityEngine;
using UnityEngine.UI;

public class BuyReduceItem : MonoBehaviour
{

    [SerializeField] private CButton AddCarBtn;
    [SerializeField] private Text BtnLabel;
    [SerializeField] private Text Price;
    [SerializeField] private Text BeforePrice;
    [SerializeField] private Text BuyLimit;
    [SerializeField] private Text Discount;
    [SerializeField] private Transform DiscountImg;
    [SerializeField] private Transform AddCountBg;
    [SerializeField] private CButton AddBtn;
    [SerializeField] private CButton ReduceBtn;
    [SerializeField] private Text AddCount;
    [SerializeField] private CButton ShowBtn;
    [SerializeField] private Image itemIcon;
    [SerializeField] private Image bg;

    public Action<BuyDiscountData> clickCallBack;

    private BuyDiscountData buyReduceData;

    private DiscountItemInfo packageData;

    private Action<DiscountItemInfo> callback;

    public const string spriteatlasPath = "Assets/Loadable/UI/UIPanel/BuyReducePanel/BuyReducePanel.spriteatlas";

    private void Awake()
    {
        AddCarBtn.onClick.AddListener(AddCarBtnClick);
        AddBtn.onClick.AddListener(AddBtnClick);
        ReduceBtn.onClick.AddListener(ReduceBtnClick);
        ShowBtn.onClick.AddListener(ShowItemInfo);

        bg.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "item_bg_1", gameObject);
    }

    public void SetData(DiscountItemInfo data, Action<DiscountItemInfo> action, Sprite icon)
    {
        if(data == null)
        {
            return;
        }
        itemIcon.sprite = icon;
        callback = action;
        packageData = data;
        buyReduceData = new BuyDiscountData();
        var discount = data.discountedPrice / (float)data.originalPrice * 10;
        Discount.text = string.Format("{0}折", Math.Ceiling( discount));
        BuyLimit.gameObject.SetActive(data.total > 1);
        BuyLimit.text = string.Format("日限{0}/{1}", data.purchasedNum, data.total);
        BtnLabel.text = data.purchasedNum == data.total ? "已售罄" : "加入购物车";
        Price.text = data.discountedPrice.ToString();
        BeforePrice.text = data.originalPrice.ToString();
        AddCarBtn.gameObject.SetActive(true);
        AddCountBg.gameObject.SetActive(false);
    }

    private void AddCarBtnClick()
    {
        if (packageData ==null || packageData.total == packageData.purchasedNum)
        {
            return;
        }
        AddCarBtn.gameObject.SetActive(false);
        AddCountBg.gameObject.SetActive(true);
        AddCount.text = string.Format("1/{0}", packageData.total);
        buyReduceData.id = packageData.id;
        buyReduceData.amount = 1;
        clickCallBack?.Invoke(buyReduceData);
    }

    private void AddBtnClick()
    {
        if (packageData == null || packageData.purchasedNum >= packageData.total || (buyReduceData.amount + packageData.purchasedNum) >= packageData.total)
        {
            return;
        }
        buyReduceData.id = packageData.id;
        buyReduceData.amount++;
        AddCount.text = string.Format("{0}/{1}", buyReduceData.amount, packageData.total);
        clickCallBack?.Invoke(buyReduceData);
    }

    private void ReduceBtnClick()
    {
        if(packageData == null)
        {
            return;
        }
        buyReduceData.amount--;
        if(buyReduceData.amount == 0)
        {
            AddCarBtn.gameObject.SetActive(true);
            AddCountBg.gameObject.SetActive(false);
        }
        buyReduceData.id = packageData.id;
        AddCount.text = string.Format("{0}/{1}", buyReduceData.amount, packageData.total);
        clickCallBack?.Invoke(buyReduceData);
    }

    private void ShowItemInfo()
    {
        if (packageData == null)
        {
            return;
        }

        callback?.Invoke(packageData);


    }
}

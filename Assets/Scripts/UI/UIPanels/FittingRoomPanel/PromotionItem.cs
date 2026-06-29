using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Com.TheFallenGames.OSA.CustomAdapters.TableView;
using Com.TheFallenGames.OSA.DataHelpers;
using Es;
using Game.Database;
using Game.Store;
using GameData.PgcData;
using Message;
using NetBusiness.Store;
using Product;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PromotionItem : MonoBehaviour
{
    [SerializeField] internal PromotionList PromotionRoot;

    [SerializeField] internal CButton ItemBtn;

    [SerializeField] internal Transform SelectBg;

    [SerializeField] internal Image ItemIcon;

    [SerializeField] internal Transform Timeinfotip;

    [SerializeField] internal Transform Timeinfoendtip;

    [SerializeField] internal Text EndTimeText;

    [SerializeField] internal Transform DiscountBg;

    [SerializeField] internal Text DiscountText;

    [SerializeField] internal Transform PriceInfo;

    [SerializeField] internal Text PriceText;

    [SerializeField] internal Text StarPriceText;

    [SerializeField] internal Transform EndTimePrice;

    [SerializeField] internal Text EndTimePriceText;

    [SerializeField] internal Transform EndSale;

    [SerializeField] private Image CurrencyImage;

    [SerializeField] private Image EndTimeCurrencyImage;

    private int ItemId;

    private OcContestHandler contestHandler;

    private AssetData StoreItemData;

    private GoodsData goodData;

    private AvatarBUDSceneHandler dataHandler;

    private List<GoodsData> goodsDatas;

    private BUDShapePropData propData;

    private StoreProductData storeProductData;

    private void Awake()
    {
        ItemBtn.onClick.AddListener(() =>
        {
            PromotionRoot.ShapeItemClickEvent(ItemId);
        });
        contestHandler = AssetsDataManager.GetData<OcContestHandler>();
        dataHandler = AssetsDataManager.GetData<AvatarBUDSceneHandler>();
    }


    public void SeItemtData(ShapeThemeProp shapeData, int itemId)
    {
        ItemId = itemId;
        SelectBg.gameObject.SetActive(false);
        storeProductData = contestHandler.GetStoreItemData(itemId.ToString());
        if (storeProductData == null || storeProductData.AssetDataList == null)
        {
            Debug.LogError("没有找到对应道具数据：" + itemId);
            return;
        }
        StoreItemData = storeProductData.AssetDataList[0];
        ItemIcon.sprite = PgcUtils.GetIconSpriteByPgcId(itemId.ToString(), gameObject);
        if (shapeData.endTime > DataUtil.GetUtcTimeStamp())//判断是否过折扣时间
        {
            Timeinfotip.gameObject.SetActive(true);
            Timeinfoendtip.gameObject.SetActive(false);
            DiscountBg.gameObject.SetActive(true);
            PriceInfo.gameObject.SetActive(true);//这里要判断一下是否拥有
            EndSale.gameObject.SetActive(false);//同上
            EndTimePrice.gameObject.SetActive(false);
            DateTime timeInfo = TimeTools.SecondsToDateTime(shapeData.endTime - DataUtil.GetUtcTimeStamp());
            EndTimeText.text = "剩余时间：" + timeInfo.Day + "天" + timeInfo.Hour + "时";//todo这里要搞个倒计时。。
            DiscountText.text = "-" + shapeData.discount + "%";
            StarPriceText.text = StoreItemData.Price.ToString();
            CurrencyImage.sprite = PgcUtils.LoadCurrencyIcon((int)StoreItemData.PurchaseType, gameObject);
            PriceText.text = (StoreItemData.Price - StoreItemData.Price * (shapeData.discount * 0.01f)).ToString();
        }
        else
        {
            Timeinfotip.gameObject.SetActive(false);
            Timeinfoendtip.gameObject.SetActive(true);
            DiscountBg.gameObject.SetActive(false);
            PriceInfo.gameObject.SetActive(false);
            EndTimePrice.gameObject.SetActive(true);
            EndSale.gameObject.SetActive(false);
            EndTimePriceText.text = StoreItemData.Price.ToString();
            EndTimeCurrencyImage.sprite = PgcUtils.LoadCurrencyIcon((int)StoreItemData.PurchaseType, gameObject);
        }

        propData = dataHandler.GetShapeGoods(shapeData.themeId);

        if (propData == null)
        {
            return;
        }

        goodsDatas = propData.GoodsList;

        for (int i = 0; i < goodsDatas.Count; i++)
        {
            if (goodsDatas[i].Id == itemId.ToString())
            {
                goodData = goodsDatas[i];
                //goodData.Price.Value = StoreItemData.Price - StoreItemData.Price * (shapeData.discount * 0.01f);
                goodData.Discount = shapeData.discount;
                if (goodData.IsOwned)
                {
                    PriceInfo.gameObject.SetActive(false);
                    EndTimePrice.gameObject.SetActive(false);
                    EndSale.gameObject.SetActive(true);
                }
            }
        }
    }


    public void ClickEvent(int id)
    {
        SelectBg.gameObject.SetActive(id == ItemId);
        if(id == ItemId)
        {
            if (storeProductData.ProductType == ProductType.Emote)
            {
                MessageHelper.Broadcast(Message.MessageName.ShapeEmoteItemEvent, goodData);
            }
            MessageHelper.Broadcast(Message.MessageName.ShapeItemEvent, goodData);

        }
    }
}

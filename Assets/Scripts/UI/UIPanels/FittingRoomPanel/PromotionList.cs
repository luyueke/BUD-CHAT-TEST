using System.Collections;
using System.Collections.Generic;
using Game.Store;
using Message;
using UnityEngine;
using UnityEngine.UI;

public class PromotionList : MonoBehaviour
{
    [SerializeField] internal PromotionItem promotionRoot;

    private List<PromotionItem> ItemList = new List<PromotionItem>();

    private RectTransform rectTransform;

    private ShapeThemeProp shapeThemeProp;

    private List<int> PromotionItemIdList = new List<int>();

    private int ThemeId;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        MessageHelper.AddListener(MessageName.BuyShapeRefresh, RefreshItem);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener(MessageName.BuyShapeRefresh, RefreshItem);
    }

    public void CreatShapeItem(int id)
    {
        ThemeId = id;
        PromotionItemIdList = GetShapeThemePropList(id, out shapeThemeProp);

        if(PromotionItemIdList == null || PromotionItemIdList.Count == 0)
        {
            for(int i = 0; i < ItemList.Count; i++)
            {
                ItemList[i].gameObject.SetActive(false);
            }
            return;
        }

        if (PromotionItemIdList.Count > ItemList.Count)
        {
            for (int i = 0; i < PromotionItemIdList.Count; i++)
            {
                if (i >= ItemList.Count)
                {
                    PromotionItem promotionItem = GameObject.Instantiate(promotionRoot, transform);
                    promotionItem.gameObject.SetActive(true);
                    ItemList.Add(promotionItem);
                }
            }
        }
        else
        {
            for (int i = 0; i < ItemList.Count; i++)
            {
                ItemList[i].gameObject.SetActive(i < PromotionItemIdList.Count);
            }
        }

        for (int i = 0; i < PromotionItemIdList.Count; i++)
        {
            ItemList[i].SeItemtData(shapeThemeProp, PromotionItemIdList[i]);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
    }

    private List<int> GetShapeThemePropList(int id,out ShapeThemeProp shapeData)
    {
        shapeData = null;
        if(AssetsDataManager.ShapeThemePropList == null || AssetsDataManager.ShapeThemePropList.Count == 0)
        {
            return null;
        }
        for (int i = 0; i < AssetsDataManager.ShapeThemePropList.Count; i++)
        {
            if(AssetsDataManager.ShapeThemePropList[i].themeId == id)
            {
                shapeData = AssetsDataManager.ShapeThemePropList[i];
                return AssetsDataManager.ShapeThemePropList[i].products;
            }
        }
        return null;
    }

    public void ShapeItemClickEvent(int id)
    {
        for (int i = 0; i < ItemList.Count; i++) 
        {
            ItemList[i].ClickEvent(id);
        }
    }

    private void RefreshItem()
    {
        if(ThemeId == 0)
        {
            return;
        }
        CreatShapeItem(ThemeId);
    }
}

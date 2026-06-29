using System;
using Basic.Utils;
using GameData;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class ContestAllPricesItem : MonoBehaviour
{
    private Text itemText;
    private Transform selectedIcon;
    private Text descText;
    private Image item;
    private Image bgImg;
    private ContestPrizeInfo info;
    public Action<ContestPrizeInfo> OnItemClick;

    void Awake()
    {
        itemText = GameObjectEx.FindChildByName(transform, "itemText").GetComponent<Text>();
        selectedIcon = GameObjectEx.FindChildByName(transform, "selectedIcon");
        item = GameObjectEx.FindChildByName(transform, "item").GetComponent<Image>();
        bgImg = GameObjectEx.FindChildByName(transform, "bgColor").GetComponent<Image>();
        descText = GameObjectEx.FindChildByName(transform, "descText").GetComponent<Text>();
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (info == null)
        {
            return;
        }
        OnItemClick?.Invoke(info);
    }

    public void Init(ContestPrizeInfo info, string bgColor)
    {
        this.info = info;
        if (info.currencyType > 0)
        {
            PgcUtils.LoadCurrencyIconAsync((CurrencyType)info.currencyType, gameObject, (sprite) =>
            {
                if (this != null && gameObject != null && item!=null && sprite != null)
                {
                    item.sprite = sprite;
                }
            });
        }
        else
        {
            PgcUtils.GetIconSpriteByPgcIdAsync(info.pgcId,gameObject, (resultSprite) =>
            {
                if (this != null && item != null && resultSprite != null)
                {
                    item.sprite = resultSprite;
                }
            });
        }
        if (info.num > 1)
        {
            itemText.gameObject.SetActive(true);
            itemText.text = $"X{info.num}";
        }
        else
        {
            itemText.gameObject.SetActive(false);
        }
        descText.SetText(info.title);
        
        if (!string.IsNullOrEmpty(bgColor))
        {
            bgImg.color = DataUtil.DeSerializeColorCheckHash(bgColor);
        }
    }
    
    public void SetSelect(bool select)
    {
        selectedIcon.gameObject.SetActive(select);
    }

    public bool SwitchSelect()
    {
        var toSet = !IsSelect();
        SetSelect(toSet);
        return toSet;
    }

    public bool IsSelect()
    {
        return selectedIcon.gameObject.activeSelf;
    }

    public ContestPrizeInfo GetBindInfo()
    {
        return info;
    }
}

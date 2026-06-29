using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using GameData;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class ContestRewardItem : MonoBehaviour
{
    [SerializeField] Image colorBackground;
    [SerializeField] Text text;
    [SerializeField] Text num;
    [SerializeField] Image assetsIcon;
    [SerializeField] GameObject selected;

    private Action<ContestPrizeInfo> onItemSelected;
    public ContestPrizeInfo Data;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnItemClick);
    }

    private void OnItemClick()
    {
        Select(this);
        onItemSelected?.Invoke(Data);
    }

    public void UpdateViews(Color color, ContestPrizeInfo data, Action<ContestPrizeInfo> action)
    {
        Data = data;
        colorBackground.color = color;
        onItemSelected = action;
        text.text = data.title;
        num.gameObject.SetActive(false);
        if (data.rewardType == (int)BUDRewardType.RewardPgcResource)
        {
            var sprite = PgcUtils.GetIconSpriteByPgcId(data.pgcId, gameObject);
            assetsIcon.sprite = sprite;
        }
        else
        {
            if (GameUtils.IsCurrencyType(data.rewardType))
            {
                var currencyType = GameUtils.ConvertRewardType(data.rewardType);
                var sprite = PgcUtils.LoadRewardIcon((BUDRewardType)data.rewardType, gameObject);
                assetsIcon.sprite = sprite;
                num.gameObject.SetActive(true);
                num.text = $"x{data.num}";
            }
        }
    }


    public static ContestRewardItem selectItem;
    public static void Select(ContestRewardItem item)
    {
        if (selectItem != null && selectItem.gameObject != null)
        {
            selectItem.selected.gameObject.SetActive(false);
        }
        item.selected.SetActive(true);
        selectItem = item;
    }
}

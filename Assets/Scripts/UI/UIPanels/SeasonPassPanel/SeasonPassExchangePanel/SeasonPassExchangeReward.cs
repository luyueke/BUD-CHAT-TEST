using Fsbm.Runtime;
using System.Collections;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassExchangeReward : ItemRenderer
{
    [SerializeField] private Image pgcIcon;
    [SerializeField] private Image currencyIcon;
    [SerializeField] private Text numText;

    protected override void Init()
    {
        base.Init();
    }

    protected override void UpdateView()
    {
        base.UpdateView();

        var info = data as SeasonExchangeConfigItem;
        pgcIcon.gameObject.SetActive(false);
        currencyIcon.gameObject.SetActive(false);
        if (info.RewardType == BUDRewardType.RewardPgcResource)
        {
            pgcIcon.sprite = PgcUtils.GetIconSpriteByPgcId(info.PgcID.ToString(), gameObject);
            pgcIcon.gameObject.SetActive(true);
        }
        else 
        {
            currencyIcon.gameObject.SetActive(true);
            currencyIcon.sprite = PgcUtils.LoadRewardIcon(info.RewardType, gameObject);
        }
        if (info.Count > 1)
        {
            numText.text = "x" + info.Count;
        }
        else
        {
            numText.text = "";
        }
    }





}
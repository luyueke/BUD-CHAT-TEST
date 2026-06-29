using System.Collections.Generic;
using System.Linq;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class SeasonPassGiftPanel : BasePanel<SeasonPassGiftPanel>
{
    public GameObject FixedGroup;

    public GameObject RandomGroup;

    public Image Icon;

    public Text Count;

    public Text Name;

    public Image SuitIcon;

    public Text SuitName;

    public Image EmoIcon;

    public Text EmoName;

    public Button CloseBtn;

    public Button BuyBtn;

    public Button ReduceBtn;

    public Button AddBtn;

    public Text BuyTxt;

    public Text FillTxt;

    public Image Fill;

    int level;

    SeasonExchangeConfig exchangeConfig;
    public override void OnCreate()
    {
        base.OnCreate();
        CloseBtn.onClick.AddListener(CloseSelf);
        BuyBtn.onClick.AddListener(OnBuy);
        ReduceBtn.onClick.AddListener(OnReduce);
        AddBtn.onClick.AddListener(OnAdd);
    }

    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        exchangeConfig = args[0] as SeasonExchangeConfig;
        if (exchangeConfig.Gift)
        {
            FixedGroup.gameObject.SetActive(false);
            RandomGroup.gameObject.SetActive(true);
            SuitIcon.sprite = PgcUtils.LoadBundleIcon(exchangeConfig.BundleID, gameObject);
            SuitIcon.SetNativeSize();
            SuitName.text = exchangeConfig.Name.Substring(0, exchangeConfig.Name.Length - 6) + "套装";
            EmoIcon.sprite = PgcUtils.GetIconSpriteByPgcId(exchangeConfig.RewardList.First().PgcID.ToString(), gameObject);
            EmoName.text = PgcUtils.GetEmoteName(exchangeConfig.RewardList.First().PgcID.ToString());
            if (exchangeConfig.BundleID == "210")
            {
                SuitIcon.transform.localScale = Vector3.one * 0.25f;
            }
        }
        else
        {
            FixedGroup.gameObject.SetActive(true);
            RandomGroup.gameObject.SetActive(false);
            Icon.sprite = PgcUtils.LoadRewardIcon(exchangeConfig.RewardList.First().RewardType,gameObject);
            Count.text = "x" + exchangeConfig.RewardList.First().Count;
            Name.text = PgcUtils.GetRewardName(exchangeConfig.RewardList.First().RewardType);
        }
        level = 1;
        UpdateList();
    }

    void UpdateList()
    {
        Fill.fillAmount = (float)level / (exchangeConfig.Count - 0);

        FillTxt.text = $"数量<color=#FFD441>{level}</color>";

        BuyTxt.text = $"{level * exchangeConfig.Price}";
    }

    void OnBuy()
    {
        var energyCoinCount = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.SeasonPassCoin);
        var count = level * exchangeConfig.Price;
        if (count > energyCoinCount)
        {
            CommonConfirmPanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmPanel>(PanelId.CommonConfirmPanel);
            commonConfirmPanel.SetLocalText("通行证币不足", "您的通行证币不足，是否立即获取", "确定", "取消");
            commonConfirmPanel.SetOnClickAction(() => {
                var exchangeCoinPanel = UIManager.Inst.OpenPanel<SeasonPassCoinPanel>(PanelId.SeasonPassCoinPanel);
                exchangeCoinPanel.SetData(CurrencyType.SeasonPassCoin, CurrencyType.Gem, (int)(count - energyCoinCount));
            }, null);
            return;
        }
        if (SeasonPassExchangeSystem.Inst.GetTime(exchangeConfig.ExchangeId) + level > exchangeConfig.Count)
        {
            TipPanel.ShowToast("已达上限");
            return;
        }
        SeasonPassExchangeSystem.Inst.ReqStore(exchangeConfig.ExchangeId, level, gameObject, () => {
            SeasonPassExchangeSystem.Inst.ReqProductInfo();
            CloseSelf(); 
        });
    }
    void OnAdd()
    {
        if (level + 0 >= exchangeConfig.Count)
        {
            return;
        }
        level++;
        UpdateList();
    }

    void OnReduce()
    {
        if (level <= 1)
        {
            return;
        }
        level--;
        UpdateList();
    }

}
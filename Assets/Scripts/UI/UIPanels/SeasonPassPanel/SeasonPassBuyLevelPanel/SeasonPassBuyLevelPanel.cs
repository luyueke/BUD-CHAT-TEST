using Basic.Extensions;
using Fsbm.Runtime;
using Message;
using System.Collections;
using System.Collections.Generic;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;
using View.UI.PopupPanelSystem.Data;

public class SeasonPassBuyLevelPanel : BasePanel<SeasonPassBuyLevelPanel>
{
    public Button CloseBtn;

    public Button OpenBtn;

    public Button OpenBtn2;

    public Button BuyBtn;

    public Button ReduceBtn;

    public Button AddBtn;

    public GameObject Mask;

    public GameObject Mask2;

    public Text BuyTxt;

    public Text ContentTxt;

    public Text FillTxt;

    public Image Fill;

    public ScrollList ScrollListFree;

    public ScrollList ScrollListBuy1;

    public ScrollList ScrollListBuy2;

    int level;

    SeasonPassListRsp data => SeasonPassDataManager.Inst.GetCurSeasonData();

    public override void OnCreate()
    {
        base.OnCreate();

        CloseBtn.onClick.AddListener(CloseSelf);
        OpenBtn.onClick.AddListener(OnOpen);
        OpenBtn2.onClick.AddListener(OnOpen);
        BuyBtn.onClick.AddListener(OnBuy);
        ReduceBtn.onClick.AddListener(OnReduce);
        AddBtn.onClick.AddListener(OnAdd);
    }


    public override void OnShow(params object[] args)
    {
        base.OnShow(args);
        level = 1;
        UpdateList();
    }

    public void UpdateLevel(int l) {
        level = l;
        UpdateList();
    }

    void UpdateList()
    {
        OpenBtn.gameObject.SetActive(data.isPaid != 1);
        Mask.gameObject.SetActive(data.isPaid != 1);
        OpenBtn2.gameObject.SetActive(data.paidType != 1);
        Mask2.gameObject.SetActive(data.paidType != 1);

        var r1 = ScrollListBuy1.transform as RectTransform;
        r1.sizeDelta = new Vector2(data.isPaid != 1 ?724:924,158);

        var r2 = ScrollListBuy2.transform as RectTransform;
        r2.sizeDelta = new Vector2(data.paidType != 1 ? 724 : 924, 158);

        var lockCount = 0;

        var free = new List<SeasonPassItemInfo>();
        for (int i = data.progressInfo.currentTier + 1; i <= level + data.progressInfo.currentTier; i++)
        {
            free.Add(data.rewardList[i]);
            lockCount++;
        }

        var paid = new List<SeasonPassItemInfo>();
        var ls = new List<SeasonPassItemInfo>();
        for (int i = data.progressInfo.currentTier + 1; i <= level + data.progressInfo.currentTier; i++)
        {
            paid.Add(data.paidRewardList[i]);
            ls.Add(data.paidRewardList[i]);
            if (data.isPaid == 1)
            {
                lockCount++;
            }
        }

        if (data.isPaid != 1)
        {
            var v = new SeasonPassItemInfo();
            v.rewardInfo = new SeasonPassRewardInfo();
            v.rewardInfo.itemList = new List<SeasonPassRewardData>();
            v.rewardInfo.itemList.Add(new SeasonPassRewardData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", gameObject),
                amount = 1,
                name = "皮肤设子位"
            });
            paid.Add(v);
        }

        if (data.paidType != 1)
        {
            var v = new SeasonPassItemInfo();
            v.rewardInfo = new SeasonPassRewardInfo();
            v.rewardInfo.itemList = new List<SeasonPassRewardData>();
            v.rewardInfo.itemList.Add(new SeasonPassRewardData()
            {
                IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(SpriteAtlasType.Common, "SlotIcon", gameObject),
                amount = 2,
                name = "皮肤设子位"
            });
            var v1 = new SeasonPassItemInfo();
            v1.rewardInfo = new SeasonPassRewardInfo();
            v1.rewardInfo.itemList = new List<SeasonPassRewardData>();
            v1.rewardInfo.itemList.Add(new SeasonPassRewardData()
            {
                IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardYouYouCoin, gameObject),
                amount = 15,
                name = "优优币"
            });
            var v2 = new SeasonPassItemInfo();
            v2.rewardInfo = new SeasonPassRewardInfo();
            v2.rewardInfo.itemList = new List<SeasonPassRewardData>();
            v2.rewardInfo.itemList.Add(new SeasonPassRewardData()
            {
                IconSp = PgcUtils.LoadRewardIcon(BUDRewardType.RewardExperience, gameObject),
                amount = 1000,
                name = "通行证经验"
            });
            ls.Add(v);
            ls.Add(v1);
            ls.Add(v2);
        }

        ScrollListFree.datas = free;

        ScrollListBuy1.datas = paid;

        ScrollListBuy2.datas = ls;

        Fill.fillAmount = (float)level / (data.progressInfo.end - data.progressInfo.currentTier);

        ContentTxt.text = $"升至<color=#FFD441>{data.progressInfo.currentTier + level}级</color>，可立即解锁<color=#FFD441>{lockCount}件</color>奖励";

        FillTxt.text = $"购买<color=#FFD441>{level}级</color>级别，升至{data.progressInfo.currentTier + level}级";

        BuyTxt.text = $"{50 * level}购买";
    }


    void OnOpen() 
    {
        UIManager.Inst.OpenPanel(PanelId.SeasonPurchaseView);
    }

    void OnBuy() 
    {
        SkipSeasonConfirmPanel.PayByGem(level, () => {
            TipPanel.ShowToast("购买成功");
            CloseSelf();
        });
    }
    void OnAdd()
    {
        if (level + data.progressInfo.currentTier >= data.progressInfo.end)
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
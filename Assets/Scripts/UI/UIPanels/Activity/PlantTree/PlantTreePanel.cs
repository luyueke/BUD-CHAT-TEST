using Basic.Utils;
using Com.TheFallenGames.OSA.Util.IO;
using Es;
using Game.Avatar;
using Game.Event;
using Game.Store;
using GameData.PgcData;
using Message;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Product;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;


public class PlantTreePanel : ActivityBaseView
{

    [HideInInspector] public ActivityInfo activityInfo;

    public List<PlantTreePanelPlayerItem> PlayerItems;

    public List<PlantTreePanelRewardItem> RewardItems;

    public GameObject RankNone;

    public CButton RewardBtn;

    public CButton GetSeedBtn;

    public CButton GetWaterBtn;

    public CButton WaterBtnBtn;

    public Button RuleBtn;

    public Image Fill;

    public Text JoinWaterCount;

    public Text FlowerCount;
    public Text FlowerRank;

    public Text RemainWaterCount;

    public Image Icon;

    public RemoteImageBehaviour RemoteIcon;

    public GameObject RedPoint;
    public GameObject ValueRankInfo;

    PlantTreeSystem data => PlantTreeSystem.Inst;
    public override void Init(ActivityInfo info)
    {
        base.Init(info);

        RewardBtn.onClick.AddListener(OnRewardBtn);

        GetSeedBtn.onClick.AddListener(OnGetSeedBtn);

        GetWaterBtn.onClick.AddListener(OnGetWaterBtn);

        WaterBtnBtn.onClick.AddListener(OnWaterBtnBtn);

        RuleBtn.onClick.AddListener(OnRuleBtn);

        activityInfo = info;

        for (int i = 0; i < PlayerItems.Count; i++)
        {
            PlayerItems[i].gameObject.SetActive(false);
        }

        MessageHelper.AddListener(MessageName.PlantTreeUpdate, RefreshView);
        MessageHelper.AddListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<CurrencyType>(MessageName.OnPlayerInfoAccountChange, OnPlayerInfoAccountChange);
        MessageHelper.RemoveListener(MessageName.PlantTreeUpdate, RefreshView);
    }

    private void OnEnable()
    {
        data.ActivityReq();
    }

    public override void RefrashData(ActivityInfo info)
    {
        base.RefrashData(info);

        activityInfo = info;
    }

    void OnPlayerInfoAccountChange(CurrencyType currencyType) 
    {
        RemainWaterCount.text = $"可浇水次数:{AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingWater)}";
    }

    void RefreshView() {
        if (data.WaterInfo == null)
        {
            return;
        }
        //var now = TcpTimeSystem.Inst.ServerDataTime;
        //bool isMarch = now.Year == 2026 && now.Month == 3 && (now.Day == 28 || now.Day == 29);
        //if (isMarch)
        //{
        //    GetWaterBtn.gameObject.SetActive(false);
        //    GetSeedBtn.gameObject.SetActive(false);
        //    WaterBtnBtn.gameObject.SetActive(false);
        //}
        //else
        {

            if (data.WaterInfo.treePlantingDayInfo != null && !string.IsNullOrEmpty(data.WaterInfo.treePlantingDayInfo.plantingCover))
            {
                WaterBtnBtn.gameObject.SetActive(true);
                ValueRankInfo.gameObject.SetActive(true);
                GetWaterBtn.gameObject.SetActive(true);
                GetSeedBtn.gameObject.SetActive(false);
            }
            else
            {
                WaterBtnBtn.gameObject.SetActive(false);
                ValueRankInfo.gameObject.SetActive(false);
                GetWaterBtn.gameObject.SetActive(false);
                GetSeedBtn.gameObject.SetActive(true);
            }
        }

        if (data.WaterInfo != null)
        {
            var cur = data.WaterInfo.currencyAmount;
            var ls = new List<int> { 0, 5, 10, 20, 30, 50 };
            var lsF = new List<float> { 0, 0.06f, 0.3f, 0.53f, 0.76f, 1 };
            ls.Reverse();
            lsF.Reverse();
            for (int i = 0; i < ls.Count; i++)
            {
                if (cur >= ls[i])
                {
                    if (i == 0)
                    {
                        Fill.fillAmount = 1;
                    }
                    else
                    {
                        Fill.fillAmount = lsF[i] + (lsF[i - 1] - lsF[i]) * (cur - ls[i]) / (ls[i - 1] - ls[i]);
                    }
                    break;
                }
            }
            JoinWaterCount.text = cur.ToString();
        }
        else
        {
            JoinWaterCount.text = "0";
            Fill.fillAmount = 0;
        }

        if (data.WaterInfo.treePlantingDayInfo != null)
        {
            FlowerCount.text = data.WaterInfo.treePlantingDayInfo.growthValue.ToString();
            FlowerRank.text = data.WaterInfo.treePlantingDayInfo.growthRank.ToString();

            if(data.WaterInfo.treePlantingDayInfo.isEndVote == 1)
            {
                ValueRankInfo.gameObject.SetActive(true);
                GetWaterBtn.gameObject.SetActive(false);
                GetSeedBtn.gameObject.SetActive(false);
                WaterBtnBtn.gameObject.SetActive(false);
            }
        }
        else
        {
            FlowerCount.text = "0";
            FlowerRank.text = "0";
        }

        RemainWaterCount.text = $"可浇水次数:{AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.TreePlantingWater)}";

        if (data.WaterInfo.treePlantingDayInfo != null)
        {
            var url = data.WaterInfo.treePlantingDayInfo.plantingCover;
            if (!string.IsNullOrEmpty(url))
            {
                if (url.StartsWith("http"))
                {
                    RemoteIcon.gameObject.SetActive(true);
                    Icon.gameObject.SetActive(false);
                    RemoteIcon.Load(url);
                }
                else
                {
                    RemoteIcon.gameObject.SetActive(false);
                    Icon.gameObject.SetActive(true);
                    var atlasPath = XAssetLoaderMgr.Inst.GetSpriteAltasPath(SpriteAtlasType.PgcPropSprite);
                    var spriteAtlas = XAssetLoaderMgr.Inst.LoadResource<SpriteAtlas>(atlasPath, gameObject);
                    Icon.sprite = spriteAtlas.GetSprite(url);
                }
            }
        }
        else
        {
            Icon.gameObject.SetActive(false);
            RemoteIcon.gameObject.SetActive(false);
        }

        RefreshPlayer();
        RefreshReward();
        RefreshTaskRedPoint();
    }

    void RefreshReward() {
        for (int i = 0; i < RewardItems.Count; i++)
        {
            RewardItems[i].SetData();
        }
    }

    void RefreshPlayer() 
    {
        data.RankListReq((_ls) => {
            if (_ls.list != null)
            {
                for (int i = 0; i < PlayerItems.Count; i++)
                {
                    if (_ls.list.Count > i)
                    {
                        PlayerItems[i].gameObject.SetActive(true);
                        PlayerItems[i].SetData(_ls.list[i]);
                    }
                    else
                    {
                        PlayerItems[i].gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                for (int i = 0; i < PlayerItems.Count; i++)
                {
                    PlayerItems[i].gameObject.SetActive(false);
                }
            }
        });
    }

    public void RefreshRedDot()
    {
        UpdateRedDot();
    }

    private void RefreshTaskRedPoint()
    {
        if (RedPoint == null) return;

        bool hasClaimable = false;

        // 日常任务：eventStatus == Unlocked 表示可领取
        var daily = data.DailyInfo;
        if (daily?.list != null)
        {
            for (int i = 0; i < daily.list.Count && !hasClaimable; i++)
            {
                var group = daily.list[i];
                var events = group?.eventList;
                if (events == null) continue;
                for (int j = 0; j < events.Count; j++)
                {
                    var e = events[j];
                    if (e == null) continue;
                    if ((ClaimStatus)e.eventStatus == ClaimStatus.Unlocked)
                    {
                        hasClaimable = true;
                        break;
                    }
                }
            }
        }

        // 充值任务：eventStatus == Unlocked 表示可领取
        var recharge = data.RechargeInfo;
        if (!hasClaimable && recharge?.eventList != null)
        {
            for (int i = 0; i < recharge.eventList.Count; i++)
            {
                var e = recharge.eventList[i];
                if (e == null) continue;
                if ((ClaimStatus)e.eventStatus == ClaimStatus.Unlocked)
                {
                    hasClaimable = true;
                    break;
                }
            }
        }

        RedPoint.SetActive(hasClaimable);
        //UpdateRedDot();
    }

    private void ShowPackReward(List<LimitPackageRewardData> rewardList)
    {
        if (rewardList != null && rewardList.Count > 0)
        {
            var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
            panel.ShowPgcRewards(rewardList[0].pgcIdList, "醒狮汤圆套装");
        }

        ReddotManagerUtils.Inst.RefreshRedDot();
        MessageHelper.Broadcast(MessageName.OnPurchaseLimitedPackageSuccess);
    }


    #region Button事件

    private void OnRuleBtn()
    {
        UIManager.Inst.OpenPanel<ActivityRulePanel>(PanelId.ActivityRulePanel, "Assets/Loadable/UI/ActivityCenterPanel/PlantTree/Rule.json");
    }

    private void OnGetWaterBtn()
    {
        UIManager.Inst.OpenPanel(PanelId.PlantTaskPanel);
    }

    private void OnWaterBtnBtn() {
        UIManager.Inst.OpenPanel(PanelId.PlantInfoPanel);
    }

    private void OnGetSeedBtn()
    {
        UIManager.Inst.OpenPanel(PanelId.PlantSeedPanel);
    }

    private void OnRewardBtn()
    {
        UIManager.Inst.OpenPanel(PanelId.PlantRankRewardPanel);
    }

    #endregion

}
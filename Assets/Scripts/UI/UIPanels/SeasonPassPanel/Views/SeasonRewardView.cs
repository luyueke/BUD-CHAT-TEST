using Game.Event;
using Game.Store;
using Message;
using System;
using System.Collections.Generic;
using System.Linq;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.FittingRoom;
using UnityEngine;
using UnityEngine.UI;

public class SeasonRewardView : BaseSeasonView
{
    public CButton ExchangeBtn;

    public Texture2D Texture2DBg;
    public Button btn_RightBanner;
    public Button btn_TopTimeContent;
    public Button btn_BuySeasonPass;
    public Text txt_CurDay;
    public Text txt_CurProgress;

    public Scrollbar TopProgressScrollBar;
    public SeasonPassTipsView _tipsView;
    public CButton infoBtn;
    public CButton sendBtn;
    public CButton allClaimBtn;

    public ScrollRect ScRect;
    public CButton skipBtn;

    public RectTransform SeasonPassScContent;
    public Transform topPaidContent;
    public Transform bottomFreeContent;
    public GameObject paidTipsGo;

    public SeasonPassProgressContent SeasonPassProgressContent;

    public GameObject paidItemPath;
    public GameObject freeItemPath;

    //private string paidItemPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/Prefab/SeasonPassPaidItem.prefab";
    //private string freeItemPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/Prefab/SeasonPassFreeItem.prefab";

    private List<SeasonPassFreeItem> SeasonPassFreeItems = new List<SeasonPassFreeItem>();
    private List<SeasonPassPaidItem> seasonPassPaidItems = new List<SeasonPassPaidItem>();
    private string atlasPath = "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassPanel.spriteatlas";

    float ClaimCnt = 0;
    public override void OnCreate()
    {
        SeasonPassProgressContent = this.GetComponentInChildren<SeasonPassProgressContent>();

        _tipsView.gameObject.SetActive(false);
        //txt_Title.SetLocalText(SeasonPassDataManager.Inst.GetSeasonPassConfig(SeasonPassType, this.gameObject).Title);

        ExchangeBtn.onClick.AddListener(OnExchange);
        btn_RightBanner.onClick.AddListener(() => { ShowRewardPreview(); });
        btn_BuySeasonPass.onClick.AddListener(OnBuySeasonPass);
        //SeasonPassItemScView.onValueChanged.AddListener(OnSeasonPassItemScViewValueChange);
        infoBtn.onClick.AddListener(OnRuleBtnClick);
        sendBtn.onClick.AddListener(OnSendBtnClick);
        SeasonPassProgressContent.ConfirmSkipAction = panelPos =>
        {
            if (this == null)
            {
                return;
            }

            ShowSkipConfirmPanel(panelPos);
        };
        btn_TopTimeContent.onClick.AddListener(() =>
        {
            var seasonPassRsp = SeasonPassDataManager.Inst.GetCurSeasonData();
            if (SeasonPassDataManager.Inst.FinishAllProgress(seasonPassRsp?.rewardList))
            {
                return;
            }

            var leftTime = seasonPassRsp?.progressInfo?.end - seasonPassRsp?.progressInfo?.start ?? 0;
            _tipsView.gameObject.SetActive(true);
            _tipsView.SetData(seasonPassRsp.progressInfo.start, leftTime);
        });

        allClaimBtn.onClick.AddListener(OnClaimAll);
        skipBtn.onClick.AddListener(OnBtnDropClick);
        ScRect.onValueChanged.AddListener(OnScrollValueChanged);


        InitSeasonPassItem();
    }

    private void InitSeasonPassItem()
    {
        var datas = SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassDataManager.Inst.CurrentSeasonPassType);
        if (datas == null)
        {
            return;
        }

        var paidItems = datas.paidRewardList;
        if (paidItems != null)
        {
            seasonPassPaidItems.Clear();
            for (int i = 0; i < paidItems.Count; i++)
            {
                var itemObj = Instantiate(paidItemPath, topPaidContent);
                var itemComp = itemObj.GetComponent<SeasonPassPaidItem>();
                itemComp.Init(paidItems[i], OnClickPaidItem);
                seasonPassPaidItems.Add(itemComp);
            }
        }

        var normalItems = datas.rewardList;
        if (normalItems != null)
        {
            SeasonPassFreeItems.Clear();
            for (int i = 0; i < normalItems.Count; i++)
            {
                var itemObj = Instantiate(freeItemPath, bottomFreeContent);
                var itemComp = itemObj.GetComponent<SeasonPassFreeItem>();

                itemComp.Init(normalItems[i], OnClickFreeItem);
                SeasonPassFreeItems.Add(itemComp);

            }
        }
    }

    void CheckedNormalValue()
    {
        ClaimCnt = 0;
        foreach (var item in SeasonPassFreeItems)
        {
            if (item.curData.BudRewardStatus == BudRewardStatus.Claimed)
            {
                ClaimCnt++;
            }
        }
        ScRect.horizontalNormalizedPosition = (float)(ClaimCnt / (float)SeasonPassFreeItems.Count);
    }

    private void OnBtnDropClick()
    {

        // HorizontalLayoutGroup ScRect;
        // 滑动到底部
        ScRect.horizontalNormalizedPosition = 0.55f;
        //verticalNormlizedPosition = 0f;
        // 隐藏按钮
        skipBtn.gameObject.SetActive(false);

    }

    private void OnScrollValueChanged(Vector2 pos)
    {
        // 检查是否滚动到顶部，显示Drop按钮
        if (ScRect.horizontalNormalizedPosition >= 0.55f)
        {
            skipBtn.gameObject.SetActive(false);
        }
        // 检查是否滚动到底部，隐藏Drop按钮
        else if (ScRect.horizontalNormalizedPosition < 0.55f)
        {
            skipBtn.gameObject.SetActive(true);
        }
    }
    private void OnSendBtnClick()
    {
        var sendpanel = UIManager.Inst.OpenPanel<SendGiftPanel>(PanelId.SendGiftPanel);
        sendpanel.JumpTo(SendGiftMainTabs.Tab.Tool);
    }

    private void OnRuleBtnClick()
    {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, "Assets/Loadable/UI/UIPanel/SeasonPassPanel/SeasonPassRule.json");
    }

    private void OnExchange()
    {
        //UIManager.Inst.OpenPanel<SeasonPassExchangePanel>(PanelId.SeasonPassExchangePanel, new GashaponData());
        UIManager.Inst.OpenPanel<SeasonPassExchangePanel>(PanelId.SeasonPassExchangePanel);
    }

    private void ShowSkipConfirmPanel(Vector3 pos)
    {
        var panel = UIManager.Inst.OpenPanel<SkipSeasonConfirmPanel>(PanelId.SkipSeasonConfirmPanel);
        panel.AdjustPosition(pos);
    }

    private void ShowSeasonPassRandomPackPreview()
    {
        var rewardSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "icon_36_min", this.gameObject);
        var panel = UIManager.Inst.OpenPanel<CurrencyTipsPanel>(PanelId.CurrencyTipsPanel);
        panel.UpdateUI(rewardSp, "通行证随机礼包", null, "打开后有机会获得以下奖励之一：30个通行证币；10个通行证币；10个徽章；200个金币");
    }

    private PaidPackRewardPanel ShowRewardPreview(bool isPaid = true)
    {
        var panel = UIManager.Inst.OpenPanel<PaidPackRewardPanel>(PanelId.PaidPackRewardPanel);
        panel.bundleBgColorStr = "#FFFFFF";
        panel.bundleContentBgColorStr = "#FF9B9B";
        panel.SetStyle(Texture2DBg, "#FF93BA");
        panel.SetEventPreview(SeasonPassDataManager.Inst.GetSeasonPassRewardDatas(SeasonPassDataManager.Inst.CurrentSeasonPassType, isPaid), "", null);
        return panel;
    }

    private void OnBuySeasonPass()
    {
        if (SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType) && SeasonPassDataManager.Inst.GetSeasonPassPaidType(SeasonPassDataManager.Inst.CurrentSeasonPassType) == 1)
        {
            return;
        }

        //var panel = UIManager.Inst.FindPanel<NewSeasonPassPanel>(PanelId.NewSeasonPassPanel);
        //if (panel != null)
        //{
        //    panel.SwitchView(SeasonPassType, "SeasonPurchaseView");
        //}
        UIManager.Inst.OpenPanel(PanelId.SeasonPurchaseView);
    }

    private void ScrollToEnd()
    {
        SeasonPassScContent.anchoredPosition = new Vector2(-42570f, 0);
    }

    private void ScrollToPGC()
    {
        SeasonPassScContent.anchoredPosition = new Vector2(-22969, 0);
    }

    private void OnSeasonPassItemScViewValueChange(Vector2 curPos)
    {
        //btn_ToEnd.gameObject.SetActive(SeasonPassScContent.anchoredPosition.x > -16430);
    }

    private void OnClickPaidItem(SeasonPassItemInfo data)
    {
        if (data.BudRewardStatus == BudRewardStatus.Unlocked)
        {
            OnClaimDirect(data, true);
        }
        else if (data.BudRewardStatus == BudRewardStatus.Lock)
        {
            if (data.IsPgcCloth())
            {
                var panel = ShowRewardPreview();
                var pgcId = data.rewardInfo?.itemList?.First()?.pgcIdList?.First() ?? "";
                if (pgcId == "40100533" || pgcId == "40100567")
                {
                    foreach (var item in panel.itemViews)
                    {
                        if (item.PaidPackRewardData.pgcIds[0] == "40100533" || item.PaidPackRewardData.pgcIds[0] == "40100567")
                        {
                            panel.OnClickItem(item.PaidPackRewardData);
                            break;
                        }
                    }
                }
            }
            else if (data.BudRewardType == BUDRewardType.RewardSeasonPassRandomPack)
            {
                ShowSeasonPassRandomPackPreview();
            }
            else
            {
                if (SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType))
                {
                    TipPanel.ShowToast("完成任务以解锁此奖励！");
                }
                else
                {
                    TipPanel.ShowToast("购买高级通行证即可解锁高级奖励");
                }
            }
        }
    }

    private void OnClickFreeItem(SeasonPassItemInfo data)
    {
        if (data.BudRewardStatus == BudRewardStatus.Unlocked)
        {
            OnClaimDirect(data, false);
        }
        else if (data.BudRewardStatus == BudRewardStatus.Lock)
        {
            if (data.IsPgcCloth())
            {
                ShowRewardPreview(false);
            }
            else
            {
                TipPanel.ShowToast("完成任务以解锁此奖励！");
            }
        }
    }

    private void OnClaimDirect(SeasonPassItemInfo data, bool isPaid = false)
    {
        SeasonPassDataManager.Inst.Claim(SeasonPassDataManager.Inst.CurrentSeasonPassType, data, (b, rsp) =>
        {
            if (b && this != null)
            {
                HandleClaimSuccess(data, rsp, isPaid);
            }
        });
    }

    private void OnClaimAll()
    {
        SeasonPassDataManager.Inst.ClaimAll(SeasonPassDataManager.Inst.CurrentSeasonPassType, (b, rsp) =>
        {
            if (b && this != null)
            {
                OnClaimedSuccess(rsp);
            }
        });
    }

    private void OnClaimedSuccess(SeasonPassClaimRsp rsp)
    {
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardList = new List<CommonRewardItemData>() { };
        bool bo14 = false;
        bool bo15 = false;
        foreach (var rewardInfo in rsp.rewardList)
        {
            if (rewardInfo.pgcId == "10400508" || rewardInfo.pgcId == "11300369")
            {
                bo14 = true;
                continue;
            }
            if (rewardInfo.pgcId == "10400513" || rewardInfo.pgcId == "12100153" || rewardInfo.pgcId == "11400090")
            {
                bo15 = true;
                continue;
            }
            var rewardItemData = new CommonRewardItemData();
            rewardItemData.rewardType = rewardInfo.rewardType;
            rewardItemData.RewardAmount = rewardInfo.amount;
            rewardItemData.pgcId = rewardInfo.pgcId;
            if (rewardInfo.rewardType == (int)BUDRewardType.RewardPgcResource)
            {
                rewardItemData.rewardName = "";
            }
            else
            {
                rewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)rewardInfo.rewardType);
            }
            rewardItemData.isConverted = rewardInfo.isReplaced == 1;
            rewardList.Add(rewardItemData);
        }
        if (bo14)
        {
            var rewardItemData = new CommonRewardItemData();
            rewardItemData.rewardName = "焦糖布丁喵套装";
            rewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "icon_ma", this.gameObject);
            rewardList.Insert(0, rewardItemData);
        }
        if (bo15)
        {
            var rewardItemData = new CommonRewardItemData();
            rewardItemData.rewardName = "天使小羊套装";
            rewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "icon_ma1", this.gameObject);
            rewardList.Insert(0, rewardItemData);
        }
        panel.ShowRewards(rewardList);

        if (SeasonPassDataManager.Inst.GetSeasonPassIsPaid(SeasonPassDataManager.Inst.CurrentSeasonPassType))
        {
            panel.ShowReplaceTex(rsp.replaceRewardList);
        }
        else
        {

            // 如果用户购买了季票，可以获得的奖励总和
            var datas = SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassDataManager.Inst.CurrentSeasonPassType);
            if (datas != null)
            {
                // 获取已到达的高级奖励
                var currentTier = SeasonPassDataManager.Inst.GetSeasonPassList(SeasonPassDataManager.Inst.CurrentSeasonPassType).progressInfo.currentTier;

                var unlockedPaidRewards = datas.paidRewardList
                    .Take(currentTier)  // 只取前currentTier个奖励
                    .ToList();

                if (unlockedPaidRewards.Count > 0)
                {
                    // 计算购买季票后可获得的奖励总和
                    var seasonPassPotentialReward = ConvertToRewardItemData(unlockedPaidRewards);
                    panel.ShowSeasonPassRewards(seasonPassPotentialReward, true);
                }
                else
                {
                    panel.ShowReplaceTex(rsp.replaceRewardList);
                }

            }

        }
        CheckedNormalValue();
        AccountDataManager.Inst.BalanceInfo.Refresh();
        MessageHelper.Broadcast(MessageName.RefreshSeasonPass);
    }

    /// <summary>
    /// 将到达的进度的季票奖励转换为CommonRewardItemData列表
    /// </summary>
    /// <param name="unlockedPaidRewards">已得到但未解锁的高级奖励</param>
    /// <returns>奖励列表</returns>
    private List<CommonRewardItemData> ConvertToRewardItemData(List<SeasonPassItemInfo> unlockedPaidRewards)
    {
        List<CommonRewardItemData> result = new List<CommonRewardItemData>();

        // 遍历所有解锁的奖励
        bool bo14 = false;
        bool bo15 = false;
        foreach (var reward in unlockedPaidRewards)
        {
            if (reward.rewardInfo == null || reward.rewardInfo.itemList == null)
                continue;

            foreach (var item in reward.rewardInfo.itemList)
            {
                var rewardItemData = new CommonRewardItemData();
                rewardItemData.rewardType = item.rewardType;
                rewardItemData.RewardAmount = item.amount;
                rewardItemData.pgcId = item.pgcIdList != null && item.pgcIdList.Count > 0 ? item.pgcIdList.First() : "";
                if (item.rewardType == (int)BUDRewardType.RewardPgcResource)
                {
                    if (rewardItemData.pgcId == "10400508" || rewardItemData.pgcId == "11300369")
                    {
                        bo14 = true;
                        continue;
                    }
                    if (rewardItemData.pgcId == "10400513" || rewardItemData.pgcId == "12100153" || rewardItemData.pgcId == "11400090")
                    {
                        bo15 = true;
                        continue;
                    }
                    if ( rewardItemData.pgcId == "11300350")
                    {
                        rewardItemData.bundleId = "87";
                        rewardItemData.rewardType = (int)BUDRewardType.RewardPgcBundle;
                    }
                    if (rewardItemData.pgcId == "10100054")
                    {
                        rewardItemData.bundleId = "81";
                        rewardItemData.rewardType = (int)BUDRewardType.RewardPgcBundle;
                    }
                    if (rewardItemData.pgcId == "10400497")
                    {
                        rewardItemData.bundleId = "199";
                        rewardItemData.rewardType = (int)BUDRewardType.RewardPgcBundle;
                    }
                }

                //rewardItemData.bundleId = item.rewardType == (int)BUDRewardType.RewardPgcResource && rewardItemData.pgcId == "10100054" ? "81": "";

                if (item.rewardType == (int)BUDRewardType.RewardPgcResource)
                {
                    rewardItemData.rewardName = "";
                }
                else
                {
                    rewardItemData.rewardName = PgcUtils.GetRewardName((BUDRewardType)item.rewardType);
                }
                rewardItemData.isConverted = item.isReplaced == 1;
                result.Add(rewardItemData);
            }
        }
        if (bo14)
        {
            var rewardItemData = new CommonRewardItemData();
            rewardItemData.rewardName = "焦糖布丁喵套装";
            rewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "icon_ma", this.gameObject);
            result.Insert(0, rewardItemData);
        }
        if (bo15)
        {
            var rewardItemData = new CommonRewardItemData();
            rewardItemData.rewardName = "天使小羊套装";
            rewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, "icon_ma1", this.gameObject);
            result.Insert(0, rewardItemData);
        }

        return result;
    }

    private void HandleClaimSuccess(SeasonPassItemInfo data, SeasonPassClaimRsp rsp, bool isPaid = false)
    {
        if (data == null)
        {
            return;
        }

        if (data.BudRewardType != BUDRewardType.RewardPgcResource)
        {
            AccountDataManager.Inst.BalanceInfo.Refresh();
        }

        if (isPaid)
        {
            var itemView = seasonPassPaidItems.Find(x => x.curData.rewardId == data.rewardId);
            if (itemView != null)
            {
                itemView.SyncRewardState(data.rewardId);
            }
        }
        else
        {
            var itemView = SeasonPassFreeItems.Find(x => x.curData.rewardId == data.rewardId);
            if (itemView != null)
            {
                itemView.SyncRewardState(data.rewardId);
            }
        }

        OnClaimedSuccess(rsp);

        //if (data.BudRewardType == BUDRewardType.RewardPgcResource) {
        //    ShowPgcResourceReward(data);
        //} else if (data.BudRewardType == BUDRewardType.RewardGashaponVoucher) {
        //    Sprite rewardSp;

        //    if (isPaid) {
        //        var itemView = seasonPassPaidItems.Find(x => x.curData.rewardId == data.rewardId);
        //        rewardSp = itemView.rewardSp;
        //    } else {
        //        var itemView = SeasonPassFreeItems.Find(x => x.curData.rewardId == data.rewardId);
        //        rewardSp = itemView.rewardSp;
        //    }

        //    if (rewardSp == null) {
        //        return;
        //    }

        //    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        //    panel.ShowRewards(new List<CommonRewardItemData>() {
        //        new CommonRewardItemData() {
        //            IconSp = rewardSp,
        //            RewardAmount = data.RewardAmount(),
        //            rewardName = "扭蛋券"
        //        }
        //    });
        //} else if (data.BudRewardType == BUDRewardType.RewardCoin || data.BudRewardType == BUDRewardType.RewardBadge || data.BudRewardType == BUDRewardType.RewardGem || data.BudRewardType == BUDRewardType.RewardPinkCoin || data.BudRewardType == BUDRewardType.RewardLuckyCoin || data.BudRewardType == BUDRewardType.RewardYouYouCoin) {
        //    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        //    panel.ShowRewards(new List<TaskRewardData>() {
        //        new TaskRewardData() {
        //            num = data.RewardAmount(),
        //            rewardType = (int)data.BudRewardType,
        //        }
        //    });
        //} else if (data.BudRewardType == BUDRewardType.RewardSeasonPassRandomPack) {
        //    var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        //    var rewardList = new List<CommonRewardItemData>() {};
        //    foreach (var rewardData in rsp.rewardList) {
        //        rewardList.Add(new CommonRewardItemData() {
        //            rewardType = rewardData.rewardType,
        //            rewardName = PgcUtils.GetRewardName(rewardData.BudRewardType),
        //            RewardAmount = rewardData.amount,
        //            IconSp = PgcUtils.LoadRewardIcon(rewardData.BudRewardType, panel.gameObject),
        //        });
        //    }
        //    panel.ShowRewards(rewardList);
        //}
        //MessageHelper.Broadcast(MessageName.RefreshSeasonPass);
    }

    private void ShowPgcResourceReward(SeasonPassItemInfo data)
    {
        var pgcIdList = data?.rewardInfo?.itemList?.First()?.pgcIdList;
        if (pgcIdList == null || pgcIdList.Count == 0)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        var rewardId = pgcIdList.First();
        var rewardName = SeasonPassDataManager.Inst.GetSeasonPassPgcNames(SeasonPassDataManager.Inst.CurrentSeasonPassType)[rewardId];
        panel.ShowPgcRewards(pgcIdList, rewardName);
    }

    private bool CanClaim(SeasonPassListRsp seasonPassRsp)
    {
        if (seasonPassRsp == null || seasonPassRsp.paidRewardList == null || seasonPassRsp.rewardList == null)
        {
            return false;
        }
        var reward = seasonPassRsp.paidRewardList.Find(p => p.BudRewardStatus == BudRewardStatus.Unlocked);
        if (reward != null) return true;
        var reward1 = seasonPassRsp.rewardList.Find(p => p.BudRewardStatus == BudRewardStatus.Unlocked);
        if (reward1 != null) return true;

        return false;
    }


    public override void RefreshData(SeasonPassListRsp seasonPassRsp)
    {
        allClaimBtn.interactable = CanClaim(seasonPassRsp);
        btn_BuySeasonPass.gameObject.SetActive(seasonPassRsp.isPaid != 1 || seasonPassRsp.paidType != 1);
        paidTipsGo.SetActive(seasonPassRsp.isPaid != 1);
        //paidActivateGo.SetActive(seasonPassRsp.isPaid == 1 && seasonPassRsp.paidType == 1);
        //txt_BuySeasonPassBtn.SetLocalText(seasonPassRsp.isPaid == 1 && seasonPassRsp.paidType == 0 ? "升级豪华通行证" : "解锁高级通行证");

        if (seasonPassRsp.progressInfo != null)
        {
            txt_CurProgress.text = seasonPassRsp.progressInfo.start.ToString() + "/" +
                                   LocalizationManager.Inst.GetLocalizedText("{0}经验", seasonPassRsp.progressInfo.end);
            TopProgressScrollBar.size = (float)seasonPassRsp.progressInfo.start / seasonPassRsp.progressInfo.end;
            SeasonPassProgressContent.InitProgress(seasonPassRsp?.rewardList, seasonPassRsp.progressInfo.currentTier);
            txt_CurDay.SetLocalText("{0}级", seasonPassRsp.progressInfo.currentTier);
        }

        var CurDay = SeasonPassDataManager.Inst.GetCurDay(seasonPassRsp?.rewardList);
        var nextLevel = CurDay + 1;
        nextLevel = Math.Min(nextLevel, 50);

        //btn_ToEnd.gameObject.SetActive(CurDay <= 10);

        if (seasonPassPaidItems.Count == 0)
        {
            InitSeasonPassItem();
        }
        else
        {
            seasonPassPaidItems.ForEach(x => x.SyncData(seasonPassRsp?.paidRewardList));
            SeasonPassFreeItems.ForEach(x => x.SyncData(seasonPassRsp?.rewardList));
        }

        CheckedNormalValue();
    }
}

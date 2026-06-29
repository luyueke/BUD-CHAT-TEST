using System;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class JingleBellsView : NewDefaultGashaponView
{
    private Text LuckyProgress;

    [SerializeField] private Transform extraRewardContent;
    [SerializeField] private ExtraRewardItem extraRewardItemPrefab;
    [SerializeField] private GameObject extraProgressNode;
    [SerializeField] private Image extraProgressImg;


    private List<ExtraRewardConfig> extraConfigList = new List<ExtraRewardConfig>();
    private List<ExtraRewardItem> extraItemNodeList = new List<ExtraRewardItem>();
    private string viewName;
    private string viewPath;
    private string atlasPath;
    private string rulePath;
    private string fullBgPath;
    private CurrencyType rewardCurrency;
    private string exchangeItemColor;
    private string exchangeTitle;
    private string exchangeClearTips;
    private string exchangeBuyTips;
    private string previewTitle;

    public override void OnCreate(string gashaponId)
    {
        InitConfig();
        base.OnCreate(gashaponId);
        gashaponData = dataHandler.GetGashaponData(gashaponId);
        InitData(gashaponData);
    }

    protected override void InitBG() {
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgRootNode);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomTextureBg("Assets/Loadable/UI/UIPanel/GashaponPanel/JingleBells/mainview_bg.png");

        item.gameObject.SetActive(true);

    }

    private void InitConfig()
    {
        viewName = GashaponType.JingleBells.ToString();
        viewPath  = GashaponUtils.ViewBasePath + viewName + "/";
        atlasPath = viewPath + "JingleBells.spriteatlas";
        rulePath  = viewPath + "Rule.json";
        rewardCurrency = CurrencyType.PurpleDreamTicket;
        fullBgPath = viewPath + "preview_bg.jpg";
        exchangeItemColor = "#FF9B9B";
        exchangeTitle = "兑换商店";
        //exchangeClearTips = "活动结束后铃铛券和圣诞币会自动失效，记得在活动结束前使用哦!";
        exchangeClearTips = "";
        exchangeBuyTips = "活动时间：12/20 00:00 - 01/12 23:59";
        previewTitle = "圣诞响叮当";
        extraConfigList.Clear();
        extraConfigList.Add(new ExtraRewardConfig{Id = 1,atlasPath = this.atlasPath,iconName = "reward_coin", currencyType = CurrencyType.PurpleDreamCoin,RewardNum = 10,CountNum = 10});
        extraConfigList.Add(new ExtraRewardConfig{Id = 2,atlasPath = this.atlasPath,iconName = "reward_picket", currencyType = CurrencyType.PurpleDreamTicket,RewardNum = 15,CountNum = 20});
        extraConfigList.Add(new ExtraRewardConfig{Id = 3,atlasPath = this.atlasPath,iconName = "reward_coin", currencyType = CurrencyType.PurpleDreamCoin, RewardNum = 15,CountNum = 50});
        extraConfigList.Add(new ExtraRewardConfig{Id = 4,atlasPath = this.atlasPath,iconName = "reward_picket", currencyType = CurrencyType.PurpleDreamTicket, RewardNum = 25,CountNum = 100});
        extraConfigList.Add(new ExtraRewardConfig{Id = 5,atlasPath = this.atlasPath,iconName = "reward_picket", currencyType = CurrencyType.PurpleDreamTicket, RewardNum = 40,CountNum = 160});

    }

    protected override void InitUI()
    {
        base.InitUI();
        LuckyProgress = GameObjectEx.FindChildByName(transform, "LuckyProgress").GetComponent<Text>();

        InitExtraItems(extraConfigList);
    }

    private void InitData(GashaponData data)
    {
        if (data == null) return;
       // data.CurrencyType = CurrencyType.PurpleDreamCoin;
        var iconSprite = PgcUtils.LoadCurrencyIcon((int)data.CurrencyType, this.gameObject);
        if (iconSprite != null)
        {
            singleIcon.sprite = iconSprite;
            tenIcon.sprite = iconSprite;
        }

        singleText.SetText(data.SinglePrice.ToString());
        tenText.SetText(data.TenDrawPrice.ToString());
    }

    protected override void UpdateWidgetView() {
        if (gashaponData == null) return;
        widgets ??= gameObject.GetComponentsInChildren<AccountWidget>(true);

        foreach (var widget in widgets) {
            if (widget.type == CurrencyType.PurpleDreamCoin || widget.type == CurrencyType.PurpleDreamTicket) {
                widget.gameObject.SetActive(true);
            } else {
                widget.gameObject.SetActive(false);
            }
        }
    }


    private void InitExtraItems(List<ExtraRewardConfig> configList)
    {
        extraItemNodeList.Clear();
        foreach (var config in configList)
        {
            var itemNode = Instantiate(extraRewardItemPrefab,extraRewardContent);
            itemNode.InitData(config);
            itemNode.AddClickListener(() =>
            {
                OnExtraRewardItemClick(itemNode,config);
            });
            itemNode.SetState((int)BudRewardStatus.Lock);
            extraItemNodeList.Add(itemNode);
        }

        //默认不可见，等服务端数据到了才可见
        extraRewardContent.gameObject.SetActive(false);
    }

    private void UpdateExtraData(List<GashaponExtraTaskInfo> taskList)
    {
        if (extraItemNodeList != null && extraItemNodeList.Count > 0)
        {
            extraRewardContent.gameObject.SetActive(true);
            extraProgressNode.gameObject.SetActive(true);

            int canRewardCount = 0;
            foreach (var extraItemNode in extraItemNodeList)
            {
                var config = extraItemNode.GetBindData();
                var serverInfo = GashaponUtils.GetExtraServerInfo(taskList,config.Id);
                if (serverInfo != null)
                {
                    extraItemNode.SetState(serverInfo.rewardStatus);
                    if (serverInfo.rewardStatus == (int)BudRewardStatus.Unlocked ||
                        serverInfo.rewardStatus == (int)BudRewardStatus.Claimed)
                    {
                        canRewardCount++;
                    }
                }
            }

            float max = Math.Max(taskList.Count - 1,0);
            float value = Math.Max(canRewardCount - 1, 0);
            float progress = value/max;
            extraProgressImg.fillAmount = progress;
        }
    }

    private void OnExtraRewardItemClick(ExtraRewardItem itemNode,ExtraRewardConfig config)
    {
        if (itemNode.GetState() == (int)BudRewardStatus.Lock)
        {
            UIManager.Inst.OpenPanel(PanelId.CurrencyTipsPanel, config.currencyType);
        }
        else if (itemNode.GetState() == (int)BudRewardStatus.Unlocked)
        {
            GashaponDataManager.Inst.RequestClaimTaskReward(gashaponData.Id,config.Id,OnUpdateExtraTask);
        }
    }



    protected override void OnExchageBtnClick()
    {
        var exchangePanel = UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            bgPath = fullBgPath,
            title = exchangeTitle,
            gashaponData = gashaponData,
            rewardCurrency = rewardCurrency,
            itemBgColor = exchangeItemColor,
            clearTips = exchangeClearTips,
            buyTips = exchangeBuyTips
        });
        exchangePanel.SetAnimPreviewBtnColor(new Color32(210,73,73,255), new Color32(255,192,31,255), new Color32(255,86,96,255));
        exchangePanel.SetBundleViewBgColor(bundleViewBgColor);
    }

    #region 网络

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp) {
        base.OnGashaponInfoUpdate(infoRsp);
        var start = infoRsp.luckyProgressInfo?.start ?? 0;
        var end = infoRsp.luckyProgressInfo?.end ?? 80;
        // 幸运值进度：0/80
        var fixedLuck = Math.Min(end, start);
        fixedLuck = Math.Max(0, fixedLuck);
        LuckyProgress.SetLocalText("幸运值进度：{0}/{1}",fixedLuck,end);

        if (infoRsp.taskList != null && infoRsp.taskList.Count > 0)
        {
            UpdateExtraData(infoRsp.taskList);
        }

        singleText.SetText(infoRsp.singleDrawDiscountedPrice.ToString());
        //更新按钮价格
        if (infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice) {
            singleDiscountTag.SetActive(true);
            srcSingleText.gameObject.SetActive(true);
            srcSingleText.SetText(infoRsp.singleDrawPrice.ToString());
        } else {
            singleDiscountTag.SetActive(false);
            srcSingleText.gameObject.SetActive(false);
        }

        tenText.SetText(infoRsp.tenDrawDiscountedPrice.ToString());
        if (infoRsp.tenDrawDiscountedPrice != infoRsp.tenDrawPrice) {
            tenDiscountTag.SetActive(true);
            srcTenText.gameObject.SetActive(true);
            srcTenText.SetText(infoRsp.tenDrawPrice.ToString());
        } else {
            tenDiscountTag.SetActive(false);
            srcTenText.gameObject.SetActive(false);
        }
    }



    private void OnUpdateExtraTask(GashaponTaskRewardRsp rewardRsp)
    {
        if (!this || rewardRsp == null) return;
        if (rewardRsp.taskList != null && rewardRsp.taskList.Count > 0)
        {
            UpdateExtraData(rewardRsp.taskList);
        }

        List<CommonRewardData> rewardsItems = rewardRsp.rewardList;
        var pairList = rewardRsp.backpackData?.pairList;
        if ((rewardsItems == null || rewardsItems.Count <= 0) && (pairList == null || pairList.Count <= 0))
        {
            return;
        }
        GashaponDataManager.Inst.ShowGashaponReward(this.gameObject,rewardsItems,pairList);
    }


    #endregion

}

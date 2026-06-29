using System;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class BudChroniclesView : GashaponBaseView
{
    //通用按钮，用代码获取，方便后面复制
    private CButton backBtn;
    private CButton twistBtn;
    private CButton twist10Btn;
    private Image singleIcon;
    private Image tenIcon;
    private Text singleText;//单抽价格
    private Text tenText;//十连抽价格
    private CButton infoBtn;
    private CButton previewBtn;
    private CButton exchangeBtn;
    private Text LuckyProgress;
    private GashaponTwistAnimView TwistAnimView;//抽奖动画页

    [SerializeField] private GameObject TitleImgCN;
    [SerializeField] private GameObject TitleImgUS;
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


    private GashaponData gashaponData;
    public override void OnCreate(string gashaponId)
    {
        base.OnCreate(gashaponId);
        InitConfig();
        InitUI();
        gashaponData = dataHandler.GetGashaponData(gashaponId);
        InitData(gashaponData);
    }

    private void InitConfig()
    {
        viewName = GashaponType.BudChronicles.ToString();
        viewPath  = GashaponUtils.ViewBasePath + viewName + "/";
        atlasPath = viewPath + "BudChronicles.spriteatlas";
        rulePath  = viewPath + "Rule.json";
        rewardCurrency = CurrencyType.LuckyTicket;
        fullBgPath = viewPath + "preview_bg.jpg";
        exchangeItemColor = "#C58663";
        exchangeTitle = "兑换商店";
        exchangeClearTips = "幸运券和幸运币不会清空，可在后续上新的幸运币盲盒中持续兑换哦";
        exchangeBuyTips = "可以使用幸运券进行奖品兑换哦";
        previewTitle = "BUD编年史";
        extraConfigList.Clear();
        extraConfigList.Add(new ExtraRewardConfig{Id = 1,atlasPath = this.atlasPath,iconName = "lucky_1",currencyType = CurrencyType.LuckyTicket,RewardNum = 10,CountNum = 10});
        extraConfigList.Add(new ExtraRewardConfig{Id = 2,atlasPath = this.atlasPath,iconName = "lucky_2",currencyType = CurrencyType.LuckyTicket,RewardNum = 15,CountNum = 20});
        extraConfigList.Add(new ExtraRewardConfig{Id = 3,atlasPath = this.atlasPath,iconName = "lucky_3",currencyType = CurrencyType.LuckyTicket,RewardNum = 20,CountNum = 50});
        extraConfigList.Add(new ExtraRewardConfig{Id = 4,atlasPath = this.atlasPath,iconName = "lucky_4",currencyType = CurrencyType.LuckyTicket,RewardNum = 30,CountNum = 100});
        extraConfigList.Add(new ExtraRewardConfig{Id = 5,atlasPath = this.atlasPath,iconName = "lucky_5",currencyType = CurrencyType.LuckyTicket,RewardNum = 40,CountNum = 160});

    }

    private void InitUI()
    {
        backBtn = GameObjectEx.FindChildByName(transform, "BackBtn").GetComponent<CButton>();
        twistBtn = GameObjectEx.FindChildByName(transform,"TwistBtn").GetComponent<CButton>();
        twist10Btn = GameObjectEx.FindChildByName(transform,"Twist10Btn").GetComponent<CButton>();
        singleIcon = GameObjectEx.FindChildByName(twistBtn.transform, "icon").GetComponent<Image>();
        tenIcon = GameObjectEx.FindChildByName(twist10Btn.transform, "icon").GetComponent<Image>();
        singleText = GameObjectEx.FindChildByName(twistBtn.transform, "num").GetComponent<Text>();
        tenText = GameObjectEx.FindChildByName(twist10Btn.transform, "num").GetComponent<Text>();
        infoBtn = GameObjectEx.FindChildByName(transform, "InfoBtn").GetComponent<CButton>();
        previewBtn = GameObjectEx.FindChildByName(transform, "PreviewBtn").GetComponent<CButton>();
        exchangeBtn = GameObjectEx.FindChildByName(transform, "ExchangeBtn").GetComponent<CButton>();
        LuckyProgress = GameObjectEx.FindChildByName(transform, "LuckyProgress").GetComponent<Text>();
        TwistAnimView = GameObjectEx.FindChildByName(transform, "GashaponTwistAnimView").GetComponent<GashaponTwistAnimView>();

        backBtn.onClick.AddListener(OnBackBtnClick);
        twistBtn.onClick.AddListener(OnTwistClick);
        twist10Btn.onClick.AddListener(OnTwist10Click);
        infoBtn.onClick.AddListener(OnInfoClick);
        previewBtn.onClick.AddListener(OnPriviewBtnClick);
        exchangeBtn.onClick.AddListener(OnExchageBtnClick);
        InitExtraItems(extraConfigList);
        TitleImgCN.SetActive(true);
        TitleImgUS.SetActive(false);
#if PACKAGE_TYPE_US
        TitleImgCN.SetActive(false);
        TitleImgUS.SetActive(true);
#endif
        TwistAnimView.gameObject.SetActive(false);
    }

    private void InitData(GashaponData data)
    {
        if (data == null) return;
        var iconSprite = PgcUtils.LoadCurrencyIcon((int)data.CurrencyType, this.gameObject);
        if (iconSprite != null)
        {
            singleIcon.sprite = iconSprite;
            tenIcon.sprite = iconSprite;
        }

        singleText.SetText(data.SinglePrice.ToString());
        tenText.SetText(data.TenDrawPrice.ToString());
        TwistAnimView.InitTwistAnimStyle(data.Id);
        GashaponDataManager.Inst.RequestGashaponInfo(data.Id,OnUpdateGashaponInfo);
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
        extraProgressNode.gameObject.SetActive(false);
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

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        if (!this || gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        TwistAnimView.gameObject.SetActive(true);
        TwistAnimView.PlayOneTwistAnimation(gashaponRsp.rewardList, () =>
        {
            OnGashaTwistAnimComplete(gashaponRsp);

        });
    }

    public override void OnGashaTenRsp(GashaponRsp gashaponRsp)
    {
        if (!this || gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }
        TwistAnimView.gameObject.SetActive(true);
        TwistAnimView.PlayTenTwistAnimation(gashaponRsp.rewardList, () =>
        {
            OnGashaTwistAnimComplete(gashaponRsp);
        });
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp)
    {
        // OnUpdateGashaponInfo(gashaponRsp.lotteryInfo);
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponData.Id,OnUpdateGashaponInfo);
        TwistAnimView.gameObject.SetActive(false);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponData.Id, gashaponRsp);
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

    private void OnBackBtnClick()
    {
        mainPanel?.CloseSelf();
    }


    private void OnTwistClick()
    {
        SendGashaponRequestOnce(gashaponData);
    }


    private void OnTwist10Click()
    {
        SendGashaponRequestTenTimes(gashaponData);
    }

    private void OnInfoClick()
    {
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel,rulePath);
    }

    private void OnPriviewBtnClick()
    {
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = fullBgPath,
            title = previewTitle,
            gashaponData = gashaponData,
            rewardCurrency = rewardCurrency,
            rulePath = rulePath,
        });
        previewPanel.SetAnimPreviewBtnColor(new Color32(210,73,73,255), new Color32(255,192,31,255), new Color32(255,86,96,255));
        previewPanel.SetBundleViewBgClolr(exchangeItemColor);
    }

    private void OnExchageBtnClick()
    {
        var exchangePanel =  UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
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

    }

    #region 网络

    private void OnUpdateGashaponInfo(GashaponInfoRsp infoRsp)
    {
        if (!this || infoRsp == null) return;
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

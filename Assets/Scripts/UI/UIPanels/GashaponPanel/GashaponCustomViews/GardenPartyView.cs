using System;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GradenPartyView : GashaponBaseView
{
    //通用按钮，用代码获取，方便后面复制
    private CButton backBtn;
    private CButton twistBtn;
    private CButton twist10Btn;
    private Image singleIcon;
    private Image tenIcon;
    private Text singleText;//单抽价格
    private Text tenText;//十连抽价格
    private Text srcTenText;//十连抽原始价格
    private Text dicText;//折扣%
    private GameObject discountTag;//折扣tag节点
    private CButton infoBtn;
    private CButton previewBtn;
    private CButton exchangeBtn;
    private Text LuckyProgress;
    private GashaponTwistAnimView TwistAnimView;//抽奖动画页
    
    [SerializeField] private GameObject TitleImgCN;
    [SerializeField] private GameObject TitleImgUS;
    
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
        viewName = GashaponType.GardenParty.ToString();
        viewPath  = GashaponUtils.ViewBasePath + viewName + "/";
        atlasPath = viewPath + "GardenParty.spriteatlas";
        rulePath  = viewPath + "Rule.json";
        rewardCurrency = CurrencyType.CoinTicket;
        fullBgPath = viewPath + "preview_bg.jpg";
        exchangeItemColor = "#254EFA";
        exchangeTitle = "兑换商店";
        exchangeClearTips = "金币券和金币不会清空，可在后续上新的幸运币盲盒中持续兑换哦";
        exchangeBuyTips = "可以使用金币券进行奖品兑换哦";
        previewTitle = "捣蛋游园会";
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
        srcTenText = GameObjectEx.FindChildByName(twist10Btn.transform, "srcNum").GetComponent<Text>();
        discountTag = GameObjectEx.FindChildByName(twist10Btn.transform, "TagDiscount").gameObject;
        dicText = GameObjectEx.FindChildByName(discountTag.transform, "discount").GetComponent<Text>();
        
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
        
        TwistAnimView.InitTwistAnimStyle(data.Id);
        GashaponDataManager.Inst.RequestGashaponInfo(data.Id,OnUpdateGashaponInfo);
        
        singleText.SetText(data.SinglePrice.ToString());
        srcTenText.SetText(data.TenDrawPrice.ToString());
        bool isShowDiscount = data.Discount != 0;
        srcTenText.gameObject.SetActive(isShowDiscount);
        discountTag?.SetActive(isShowDiscount);
        string priceTxt = ((100 - data.Discount) * 0.01 * data.TenDrawPrice).ToString();
        tenText.SetText(priceTxt);
        dicText.SetText(string.Format("-{0}%",data.Discount));
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
        OnUpdateGashaponInfo(gashaponRsp.lotteryInfo);
        TwistAnimView.gameObject.SetActive(false);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponData.Id, gashaponRsp);
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
        previewPanel.SetBundleViewBgClolr(exchangeItemColor);

    }
    
    private void OnExchageBtnClick()
    {
        UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            bgPath = fullBgPath,
            title = exchangeTitle,
            gashaponData = gashaponData,
            rewardCurrency = rewardCurrency,
            itemBgColor = exchangeItemColor,
            clearTips = exchangeClearTips,
            buyTips = exchangeBuyTips
        });
        
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
        
    }
    
    #endregion

}

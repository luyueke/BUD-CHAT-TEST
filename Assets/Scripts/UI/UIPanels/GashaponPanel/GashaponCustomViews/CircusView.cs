using System;
using System.Collections.Generic;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class CircusView : GashaponBaseView
{
    //通用按钮，用代码获取，方便后面复制
    private CButton backBtn;
    private CButton twistBtn;
    private CButton twist10Btn;
    private Image singleIcon;
    private Image tenIcon;
    private Text singleText;//单抽价格
    private Text srcSingleText;//单抽原价格
    private GameObject singleDiscountTag;//十连抽打折
    private Text tenText;//十连抽价格
    private Text srcTenText;//十连抽原价格
    private GameObject tenDiscountTag;//十连抽打折
    private CButton infoBtn;
    private CButton previewBtn;
    private CButton exchangeBtn;
    private Text LuckyProgress;
    private GashaponTwistAnimView TwistAnimView;//抽奖动画页
    
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
    private GashaponInfoRsp curInfoRsp;
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
        viewName = GashaponType.JingleBells.ToString();
        viewPath  = GashaponUtils.ViewBasePath + viewName + "/";
        atlasPath = viewPath + "JingleBells.spriteatlas";
        rulePath  = viewPath + "Rule.json";
        rewardCurrency = CurrencyType.ChristmasTicket;
        fullBgPath = viewPath + "preview_bg.jpg";
        exchangeItemColor = "#FF9B9B";
        exchangeTitle = "兑换商店";
        exchangeClearTips = "活动结束后铃铛券和圣诞币会自动失效，记得在活动结束前使用哦!";
        exchangeBuyTips = "活动时间：12/20 00:00 - 01/12 23:59";
        previewTitle = "圣诞响叮当";
    }

    private void InitUI()
    {
        backBtn = GameObjectEx.FindChildByName(transform, "BackBtn").GetComponent<CButton>();

        twistBtn = GameObjectEx.FindChildByName(transform,"TwistBtn").GetComponent<CButton>();
        singleIcon = GameObjectEx.FindChildByName(twistBtn.transform, "icon").GetComponent<Image>();
        singleText = GameObjectEx.FindChildByName(twistBtn.transform, "num").GetComponent<Text>();
        srcSingleText = GameObjectEx.FindChildByName(twistBtn.transform, "srcNum").GetComponent<Text>();
        singleDiscountTag = GameObjectEx.FindChildByName(twistBtn.transform, "TagDiscount").gameObject;

        twist10Btn = GameObjectEx.FindChildByName(transform, "Twist10Btn").GetComponent<CButton>();
        tenIcon = GameObjectEx.FindChildByName(twist10Btn.transform, "icon").GetComponent<Image>();
        tenText = GameObjectEx.FindChildByName(twist10Btn.transform, "num").GetComponent<Text>();
        srcTenText = GameObjectEx.FindChildByName(twist10Btn.transform, "srcNum").GetComponent<Text>();
        tenDiscountTag = GameObjectEx.FindChildByName(twist10Btn.transform, "TagDiscount").gameObject;

        infoBtn = GameObjectEx.FindChildByName(transform, "InfoBtn").GetComponent<CButton>();
        previewBtn = GameObjectEx.FindChildByName(transform, "PreviewBtn").GetComponent<CButton>();
        exchangeBtn = GameObjectEx.FindChildByName(transform, "ExchangeBtn").GetComponent<CButton>();
        TwistAnimView = GameObjectEx.FindChildByName(transform, "GashaponTwistAnimView").GetComponent<GashaponTwistAnimView>();

        backBtn.onClick.AddListener(OnBackBtnClick);
        twistBtn.onClick.AddListener(OnTwistClick);
        twist10Btn.onClick.AddListener(OnTwist10Click);
        infoBtn.onClick.AddListener(OnInfoClick);
        previewBtn.onClick.AddListener(OnPriviewBtnClick);
        exchangeBtn.onClick.AddListener(OnExchageBtnClick);
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

    private void OnBackBtnClick()
    {
        mainPanel?.CloseSelf();
    }


    private void OnTwistClick()
    {
        if (curInfoRsp == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }

        SendGashaponRequestOnce(gashaponData,curInfoRsp.singleDrawDiscountedPrice);
    }


    private void OnTwist10Click()
    {
        if (curInfoRsp == null)
        {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        SendGashaponRequestTenTimes(gashaponData,curInfoRsp.tenDrawDiscountedPrice);
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

    }

    #region 网络

    private void OnUpdateGashaponInfo(GashaponInfoRsp infoRsp)
    {
        if (!this || infoRsp == null) return;
        curInfoRsp = infoRsp;
        var start = infoRsp.luckyProgressInfo?.start ?? 0;
        var end = infoRsp.luckyProgressInfo?.end ?? 80;
        // 幸运值进度：0/80
        var fixedLuck = Math.Min(end, start);
        fixedLuck = Math.Max(0, fixedLuck);
        LuckyProgress.SetLocalText("幸运值进度：{0}/{1}",fixedLuck,end);
        
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
    #endregion

}

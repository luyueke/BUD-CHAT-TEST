using System;
using System.Collections.Generic;
using Game.Avatar;
using Game.Store;
using GameData.Gashapon;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class RainbowView : GashaponBaseView
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
    private CButton previewBtn;
    private CButton exchangeBtn;
    private Text LuckyProgress;
    private GashaponTwistAnimView TwistAnimView;//抽奖动画页
    protected CButton infoBtn;

    private Image singleDiscountTagSpr;
    private Image tenDiscountTagSpr;
    private Image infoBtnSpr;
    private Image previewBtnSpr;
    private Image exchangeBtnSpr;
    private Image previewSpr;
    private Image exchangeSpr;


    private string viewName;
    private string viewPath;
    private string rulePath;
    private string fullBgPath;
    private CurrencyType rewardCurrency;
    private string exchangeItemColor;
    private string exchangeTitle;
    private string exchangeClearTips;
    private string exchangeBuyTips;
    private string previewTitle;
    private CreatorCenterPanel centerPanel;
    

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
        viewName = GashaponType.Rainbow.ToString();
        viewPath  = GashaponUtils.ViewBasePath + viewName + "/";
        rulePath  = "Assets/Loadable/UI/UIPanel/Rainbow/Rule.json";
        rewardCurrency = CurrencyType.PopularityTicket;
        exchangeItemColor = "#B0A4F4";
        exchangeTitle = "兑换商店";
        exchangeClearTips = "";
        exchangeBuyTips = "";
        previewTitle = "创作者扭蛋";
    }
    public void SetCreatorPanel(CreatorCenterPanel panel)
    {
        centerPanel = panel;
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
        
        previewBtn = GameObjectEx.FindChildByName(transform, "PreviewBtn").GetComponent<CButton>();
        exchangeBtn = GameObjectEx.FindChildByName(transform, "ExchangeBtn").GetComponent<CButton>();
        TwistAnimView = GameObjectEx.FindChildByName(transform, "GashaponTwistAnimView").GetComponent<GashaponTwistAnimView>();
        infoBtn = GameObjectEx.FindChildByName(transform, "InfoBtn").GetComponent<CButton>();

        singleDiscountTagSpr = singleDiscountTag?.GetComponent<Image>();
        tenDiscountTagSpr = tenDiscountTag?.GetComponent<Image>();
        infoBtnSpr = infoBtn?.GetComponent<Image>();
        previewBtnSpr = previewBtn?.GetComponent<Image>();
        exchangeBtnSpr = exchangeBtn?.GetComponent<Image>();
        if (previewBtn != null && previewBtn.transform.Find("BtnIcon") != null)
        {
            previewSpr = previewBtn.transform.Find("BtnIcon").GetComponent<Image>();
        }
        if (exchangeBtn != null && exchangeBtn.transform.Find("BtnIcon") != null)
        {
            exchangeSpr = exchangeBtn.transform.Find("BtnIcon").GetComponent<Image>();
        }
        string spriteatlasPath = "Assets/Loadable/UI/UIPanel/GashaponPanel/GashaponPanelAtlas.spriteatlas";
        Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "ic_info", gameObject);
        //Sprite sp2 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "newdiscount", gameObject);
        Sprite sp3 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "icon_preview", gameObject);
        Sprite sp5 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "icon_shop", gameObject);
        Sprite sp4 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "btn_white_30", gameObject);

        
        if (sp1 != null && infoBtnSpr != null)
        {
            infoBtnSpr.sprite = sp1;
        }
        //if (sp2 != null && singleDiscountTagSpr != null && tenDiscountTagSpr != null)
        //{
        //    singleDiscountTagSpr.sprite = sp2;
        //    tenDiscountTagSpr.sprite = sp2;
        //}
        if (sp3 != null && previewSpr != null)
        {
            previewSpr.sprite = sp3;
        }
        if (sp5 != null && exchangeSpr != null)
        {
            exchangeSpr.sprite = sp5;
        }
        if (sp3 != null && previewSpr != null)
        {
            previewSpr.sprite = sp3;
        }
        if (sp4 != null && previewBtnSpr != null && exchangeBtnSpr!= null)
        {
            previewBtnSpr.sprite = sp4;
            exchangeBtnSpr.sprite = sp4;
        }


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
        TwistAnimView.SetBg(null);
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
        centerPanel?.CloseSelf();
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
            title = previewTitle,
            gashaponData = gashaponData,
            rewardCurrency = rewardCurrency,
            rulePath = rulePath,
        });
        previewPanel.SetAnimPreviewBtnColor(new Color32(78,103,165,255), new Color32(255,192,31,255), new Color32(252,158,195,255));
        previewPanel.SetBundleViewBgClolr(exchangeItemColor);
    }

    private void OnExchageBtnClick()
    {
        var exchangePanel = UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            title = exchangeTitle,
            gashaponData = gashaponData,
            rewardCurrency = rewardCurrency,
            itemBgColor = exchangeItemColor,
            clearTips = exchangeClearTips,
            buyTips = exchangeBuyTips
        });
        exchangePanel.SetAnimPreviewBtnColor(new Color32(78,103,165,255), new Color32(255,192,31,255), new Color32(78,103,165,255));
        exchangePanel.SetBundleViewBgColor(exchangeItemColor);
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

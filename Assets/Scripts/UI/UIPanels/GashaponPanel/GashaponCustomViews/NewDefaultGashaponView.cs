using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using GameData.Gashapon;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class NewDefaultGashaponView : BaseGashaponView {



    [SerializeField] protected Transform bgRootNode;
    [SerializeField]
    protected CurrencyType exchageCurrency = CurrencyType.None;

    [SerializeField] protected string bundleViewBgColor = "#FFFFFF";

    protected CButton twistBtn;
    protected CButton twist10Btn;
    protected Image singleIcon;
    protected Image tenIcon;
    protected Text singleText; //单抽价格
    protected Text srcSingleText; //单抽原价格
    protected GameObject singleDiscountTag; //十连抽打折
    protected Text tenText; //十连抽价格
    protected Text srcTenText; //十连抽原价格
    protected GameObject tenDiscountTag; //十连抽打折
    protected CButton infoBtn;
    protected CButton previewBtn;
    protected CButton exchangeBtn;

    protected AccountWidget[] widgets;
    private Action onPreviewBackCallBack;


    private Image singleDiscountTagSpr;
    private Image tenDiscountTagSpr;
    private Image infoBtnSpr;
    private Image previewBtnSpr;
    private Image previewSpr;
    private Image shopSpr;


    [SerializeField] private AvatarCameraController avatarCameraController;
    public bool hasPlayerModel = false;
    public GameObject modelRoot; // 用于放置角色模型的根节点
    public string remoteid;

    protected string bgPath;
    public override void OnCreate(string id) {
        base.OnCreate(id);
        InitBG();
        InitUI();
    }

    protected virtual void InitBG() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        if (viewCfg == null) {
            return;
        }
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgRootNode);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        if (string.IsNullOrEmpty(viewCfg.BgPath)) {
            item.InitCustomBgItem(viewCfg.BgColor, viewCfg.AtlasPath, viewCfg.BgSpriteIds);
        } else {
            item.InitCustomTextureBg(viewCfg.BgPath);
        }

        item.gameObject.SetActive(true);
        if (hasPlayerModel)
        {
            InitPlayerModle();
        }
    }

    void InitPlayerModle()
    {   
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo.Clone();
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, modelRoot.transform);
        var characterCtcl = characterWrapper.Avatar.GetComponent<PlayerAnimationCtrl>();
        avatarCameraController.RotateTarget = modelRoot.transform;
        characterCtcl.PlaySingleEmoteForUICharacter(remoteid);
    }
    protected virtual void InitUI() {
        twistBtn = GameObjectEx.FindChildByName(transform, "TwistBtn").GetComponent<CButton>();
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

        twistBtn.onClick.AddListener(OnTwistClick);
        twist10Btn.onClick.AddListener(OnTwist10Click);
        infoBtn.onClick.AddListener(OnInfoClick);
        previewBtn.onClick.AddListener(OnPriviewBtnClick);
        exchangeBtn.onClick.AddListener(OnExchageBtnClick);

        UpdateAltas();

        UpdateWidgetView();
        UpdateBtnUI();
    }

    public void UpdateAltas()
    {
        singleDiscountTagSpr = singleDiscountTag?.GetComponent<Image>();
        tenDiscountTagSpr = tenDiscountTag?.GetComponent<Image>();
        infoBtnSpr = infoBtn?.GetComponent<Image>();
        previewBtnSpr = previewBtn?.GetComponent<Image>();

        if (previewBtn != null && previewBtn.transform.Find("BtnIcon") != null)
        {
            previewSpr = previewBtn.transform.Find("BtnIcon").GetComponent<Image>();
        }
        if (exchangeBtn != null && exchangeBtn.transform.Find("BtnIcon") != null)
        {
            shopSpr = exchangeBtn.transform.Find("BtnIcon").GetComponent<Image>();
        }

        string spriteatlasPath = "Assets/Loadable/UI/UIPanel/GashaponPanel/GashaponPanelAtlas.spriteatlas";
        Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "ic_info", gameObject);
        Sprite sp2 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "newdiscount", gameObject);
        Sprite sp3 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "icon_preview", gameObject);
        Sprite sp4 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "btn_white_30", gameObject);
        Sprite sp5 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "icon_shop", gameObject);


        if (sp1 != null && infoBtnSpr != null)
        {
            infoBtnSpr.sprite = sp1;
        }
        if (sp2 != null && singleDiscountTagSpr != null && tenDiscountTagSpr != null)
        {
            //singleDiscountTagSpr.sprite = sp2;
            //tenDiscountTagSpr.sprite = sp2;
        }
        if (sp3 != null && previewSpr != null)
        {
            previewSpr.sprite = sp3;
        }
        if (sp4 != null && previewBtnSpr != null)
        {
            previewBtnSpr.sprite = sp4;
        }
        if (sp5 != null && shopSpr != null)
        {
            shopSpr.sprite = sp5;
        }
    }


    private void UpdateBtnUI()
    {
        if (gashaponData == null) return;
        int Type = (int)gashaponData.CurrencyType;
        if (Type == 0)
        {
            Type = (int)CurrencyType.YouYouCoin;
        }
        var iconSprite = PgcUtils.LoadCurrencyIcon(Type, this.gameObject);

        if (iconSprite != null)
        {
            singleIcon.sprite = iconSprite;
            tenIcon.sprite = iconSprite;
        }

        singleText.SetText(gashaponData.SinglePrice.ToString());
        tenText.SetText(gashaponData.TenDrawPrice.ToString());

    
    
    }

    protected virtual void UpdateWidgetView() {
        if (gashaponData == null) return;
        widgets ??= gameObject.GetComponentsInChildren<AccountWidget>(true);

        foreach (var widget in widgets) {
            if ((gashaponData.CurrencyType == CurrencyType.Badge || gashaponData.CurrencyType == CurrencyType.Coin) &&
                widget.type == CurrencyType.EnergyCoin) {
                widget.gameObject.SetActive(true);
                continue;
            }

            widget.gameObject.SetActive((int)widget.type == (int)gashaponData.CurrencyType);
        }
    }

    private void OnInfoClick() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var rulePath = viewCfg.RulePath;
        if (string.IsNullOrEmpty(rulePath)) {
            rulePath = "Assets/Loadable/UI/UIPanel/GashaponRulePanel/Rules/DefaultRule.json";
        }
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    private void OnTwist10Click() {
        if (gashaponInfoRsp == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        SendGashaponRequestTenTimes(gashaponData, gashaponInfoRsp.tenDrawDiscountedPrice);

    }

    private void OnTwistClick() {
        if (gashaponInfoRsp == null) {
            TipPanel.ShowToast("数据异常，请关闭重试");
            return;
        }
        SendGashaponRequestOnce(gashaponData, gashaponInfoRsp.singleDrawDiscountedPrice);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp) {
        base.OnGashaOnceRsp(gashaponRsp);
        OnGashaRsp(gashaponRsp, true);
    }

    public override void OnGashaTenRsp(GashaponRsp gashaponRsp) {
        base.OnGashaTenRsp(gashaponRsp);
        OnGashaRsp(gashaponRsp, false);
    }

    protected virtual void OnExchageBtnClick() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var exchangePanel = UIManager.Inst.OpenPanel<GashaponExchangePanel>(PanelId.GashaponExchangePanel, new GashaponExchangeParam
        {
            bgPath = viewCfg.BgPath,
            title = "兑换商店",
            gashaponData = gashaponData,
            rewardCurrency = exchageCurrency == CurrencyType.None ? gashaponData.CurrencyType : exchageCurrency,
            itemBgColor = bundleViewBgColor,
            clearTips = "",
            buyTips = ""
        });
        exchangePanel.SetAnimPreviewBtnColor(new Color32(210,73,73,255), new Color32(255,192,31,255), new Color32(255,86,96,255));
        exchangePanel.SetBundleViewBgColor(bundleViewBgColor);
    }

    protected virtual void OnPriviewBtnClick() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = gashaponData.Name,
            gashaponData = gashaponData,
            rewardCurrency = gashaponData.CurrencyType,
            rulePath = viewCfg.RulePath,
            onBackCallBack = onPreviewBackCallBack,
        });
        previewPanel.SetBundleViewBgClolr(bundleViewBgColor);
    }

    protected void SetOnPreviewBackCallBack(Action act)
    {
        onPreviewBackCallBack = act;
    }

    private void OnGashaRsp(GashaponRsp gashaponRsp, bool isOnce = false) {
        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
            new GashaponTwistAnimParam() {
                gashaponId = gashaponData.Id,
                bgPath = bgPath
            });
        if (isOnce) {
            panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () => {
                OnGashaTwistAnimComplete(gashaponRsp);
            });
        } else {
            panel.PlayTenTwistAnimation(gashaponRsp.rewardList, () => {
                OnGashaTwistAnimComplete(gashaponRsp);
            });
        }

    }


    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp) {
        if (!this || infoRsp == null) return;
        base.OnGashaponInfoUpdate(infoRsp);
        if (infoRsp.tenDrawDiscountedPrice == infoRsp.tenDrawPrice) {
            GameObjectEx.FindChildByName(twist10Btn.transform, "TagDiscount")?.gameObject.SetActive(false);
            GameObjectEx.FindChildByName(twist10Btn.transform, "Content/srcNum")?.gameObject.SetActive(false);
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "Content/num")?.SetText(infoRsp.tenDrawDiscountedPrice.ToString());
        } else {
            GameObjectEx.FindChildByName(twist10Btn.transform, "TagDiscount")?.gameObject.SetActive(true);
            GameObjectEx.FindChildByName(twist10Btn.transform, "Content/srcNum")?.gameObject.SetActive(true);
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "Content/srcNum")?
                .SetText(infoRsp.tenDrawPrice.ToString());
            GameObjectEx.FindComponentByName<Text>(twist10Btn.transform, "Content/num")?
                .SetText(infoRsp.tenDrawDiscountedPrice.ToString());
        }
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp)
    {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        GashaponDataManager.Inst.RequestGashaponInfo(gashaponId, OnGashaponInfoUpdate);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponData.Id, gashaponRsp);
    }

}

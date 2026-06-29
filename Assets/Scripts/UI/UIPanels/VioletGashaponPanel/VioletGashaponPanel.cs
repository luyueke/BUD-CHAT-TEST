using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using GameData.Gashapon;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using UnityEngine;
using UnityEngine.UI;

public class VioletGashaponPanel : BaseGashaponView
{
    [SerializeField] private Transform ActivityBg0;
    [SerializeField] private Transform ActivityBg1;
    [SerializeField] private Transform ModelBg0;
    [SerializeField] private Transform ModelBg1;
    [SerializeField] private Transform Logo0;
    [SerializeField] private Transform Logo1;
    [SerializeField] private CButton TabBtn0;
    [SerializeField] private CButton TabBtn1;
    [SerializeField] private Transform TabSelect0;
    [SerializeField] private Transform TabSelect1;
    [SerializeField] private CButton TwistBtn;
    [SerializeField] private CButton Twist10Btn;
    [SerializeField] private Text OncePrice;
    [SerializeField] private Text OnceBeforePrice;
    [SerializeField] private Text OnceDiscountText;
    [SerializeField] private Transform OnceDiscount;
    [SerializeField] private Text TenPrice;
    [SerializeField] private Text TenBeforePrice;
    [SerializeField] private Text TenDiscountText;
    [SerializeField] private Transform TenDiscount;
    [SerializeField] private CButton InfoBtn;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton SeasonBtn;
    [SerializeField] private CButton RewardShowBtn;
    [SerializeField] private Text tipsText;
    [SerializeField] private Transform CharacterRoot;
    [SerializeField] private Transform CharacterShow0;
    [SerializeField] private Transform CharacterShow1;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    [Header("奖励道具展示")]
    [SerializeField] private Image RewardIcon0;
    [SerializeField] private Text RewardName0;
    [SerializeField] private Transform OwenMask0;
    [SerializeField] private Image RewardIcon1;
    [SerializeField] private Text RewardName1;
    [SerializeField] private Transform OwenMask1;
    [SerializeField] private Image RewardIcon2;
    [SerializeField] private Text RewardName2;
    [SerializeField] private Transform OwenMask2;
    [SerializeField] private Image RewardIcon3;
    [SerializeField] private Text RewardName3;
    [SerializeField] private Transform OwenMask3;

    //人物3d预览
    private int m_CurSelectType;
    string bundleViewBgColor = "#FF9B9B";

    private List<string> firstSuitItemList = new List<string>()
    {
        "10900502",
        "11100040",
        "10300047",
        "10600133",
        "10800128",
        "11300362",
        "10400500"
    };

    private List<string> secSuitItemList = new List<string>()
    {
        "12100147",
        "11100041",
        "10300048",
        "10600134",
        "10800129",
        "11300363",
        "10400501"
    };

    override public void OnCreate(string id)
    {
        base.OnCreate(id);
        InitData();
        avatarCameraController.RotateTarget = CharacterRoot;

        TabBtn0.onClick.AddListener(OnTabBtn0Click);
        TabBtn1.onClick.AddListener(OnTabBtn1Click);
        PreviewBtn.onClick.AddListener(OnPriviewBtnClick);
        SeasonBtn.onClick.AddListener(OnSeasonBtnClick);
        InfoBtn.onClick.AddListener(OnInfoClick);
        TwistBtn.onClick.AddListener(OnTwistClick);
        Twist10Btn.onClick.AddListener(OnTenTwistClick);
        RewardShowBtn.onClick.AddListener(OnPriviewBtnClick);
    }



    override public void OnShow()
    {
        base.OnShow();

        var pendingId = GashaponDataManager.Inst.pendingSubLotteryId;
        GashaponDataManager.Inst.pendingSubLotteryId = null;

        if (pendingId == "lottery.VioletGashapon" || pendingId == "lottery.nightButterflyDream")
        {
            OnTabBtn0Click();
        }
        if (pendingId == "lottery.violetFragrance")
        {
            OnTabBtn1Click();
        }

        GetSeverRefresh();
    }

    override public void OnHide()
    {
        base.OnHide();
    }

    private void InitData(int type = 0)
    {
        m_CurSelectType = type;
        var CurRewardList = GetCurTypeIdList();
        PgcUtils.LoadBundleIconAsync(CurRewardList.SuitRewardId_0, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                RewardIcon0.sprite = iconSprite;
                RewardName0.text = m_CurSelectType == 1 ? "紫云临渊套装" : "夜蝶幽梦套装";
            }
        });
        PgcUtils.LoadAvatarIconAsync(CurRewardList.SuitRewardId_1, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                RewardIcon1.sprite = iconSprite;
                var cfg = DataTables.GetPgcNameData(CurRewardList.SuitRewardId_1);
                RewardName1.text = cfg.Name;
            }
        });
        PgcUtils.LoadAvatarIconAsync(CurRewardList.SuitRewardId_2, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                RewardIcon2.sprite = iconSprite;
                var cfg = DataTables.GetPgcNameData(CurRewardList.SuitRewardId_2);
                RewardName2.text = cfg.Name;
            }
        });
        PgcUtils.LoadEmoteIconAsync(CurRewardList.SuitRewardId_3, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                RewardIcon3.sprite = iconSprite;
                var cfg = DataTables.GetEmoUIConfig(CurRewardList.SuitRewardId_3);
                RewardName3.text = cfg.name;
            }
        });

        ActivityBg0.gameObject.SetActive(m_CurSelectType == 0);
        ActivityBg1.gameObject.SetActive(m_CurSelectType == 1);
        ModelBg0.gameObject.SetActive(m_CurSelectType == 0);
        ModelBg1.gameObject.SetActive(m_CurSelectType == 1);
        Logo0.gameObject.SetActive(m_CurSelectType == 0);
        Logo1.gameObject.SetActive(m_CurSelectType == 1);
        TabSelect0.gameObject.SetActive(m_CurSelectType == 0);
        TabSelect1.gameObject.SetActive(m_CurSelectType == 1);
        CharacterShow0.gameObject.SetActive(m_CurSelectType == 0);
        CharacterShow1.gameObject.SetActive(m_CurSelectType == 1);

        gashaponData = GashaponDataManager.Inst.gashaponData(GetGashaponIdByType());

        string spriteatlasPath = "Assets/Loadable/UI/UIPanel/GashaponPanel/GashaponPanelAtlas.spriteatlas";
        Sprite sp1 = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "ic_info", gameObject);
        var infoBtnSpr = InfoBtn?.GetComponent<Image>();
        if (sp1 != null && infoBtnSpr != null)
        {
            infoBtnSpr.sprite = sp1;
        }
    }

    private string GetGashaponIdByType()
    {
        string typeId = "";
        switch (m_CurSelectType)
        {
            case 0:
                typeId = "lottery.nightButterflyDream";
                break;
            case 1:
                typeId = "lottery.violetFragrance";
                break;
        }
        return typeId;
    }

    private VioletSuitRewardInfo GetCurTypeIdList()
    {
        switch(m_CurSelectType)
        {
            case 0: 
                return new VioletSuitRewardInfo()
                {
                    SuitRewardId_0 = "122",
                    SuitRewardId_1 = "11000266",
                    SuitRewardId_2 = "11700035",
                    SuitRewardId_3 = "40300507",
                };
            case 1: 
                return new VioletSuitRewardInfo()
                {
                    SuitRewardId_0 = "121",
                    SuitRewardId_1 = "11000267",
                    SuitRewardId_2 = "11700036",
                    SuitRewardId_3 = "40300127",
                };
        }

        return null;
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
        if (infoRsp == null)
        {
            return;
        }
        gashaponInfoRsp = infoRsp;
        OnceBeforePrice.text = infoRsp.singleDrawPrice.ToString();
        OncePrice.text = infoRsp.singleDrawDiscountedPrice.ToString();
        OnceBeforePrice.gameObject.SetActive(infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice);
        TenBeforePrice.text = infoRsp.tenDrawPrice.ToString();
        TenPrice.text = infoRsp.tenDrawDiscountedPrice.ToString();
        TenBeforePrice.gameObject.SetActive(infoRsp.singleDrawDiscountedPrice != infoRsp.singleDrawPrice);
        float discount = infoRsp.singleDrawPrice > 0 ? (infoRsp.singleDrawDiscountedPrice / (float)infoRsp.singleDrawPrice) : 1f;
        TenDiscount.gameObject.SetActive(discount != 1);
        OnceDiscount.gameObject.SetActive(discount != 1);
        OnceDiscountText.text = string.Format("限时{0}折", discount * 10);
        TenDiscountText.text = string.Format("限时{0}折", discount * 10);

        LayoutRebuilder.ForceRebuildLayoutImmediate(OncePrice.transform.parent.GetComponent<RectTransform>());
        LayoutRebuilder.ForceRebuildLayoutImmediate(TenPrice.transform.parent.GetComponent<RectTransform>());

        if(infoRsp.singleDrawPrice == 0 && infoRsp.singleDrawDiscountedPrice == 0)
        {
            tipsText.text = "恭喜！你已集齐当前奖池所有商品！";
            TwistBtn.gameObject.SetActive(false);
            Twist10Btn.gameObject.SetActive(false);
        }
        else
        {
            tipsText.text = "抽取1轮提前获得大奖会返还优优币，前三轮限时享受专属折扣.";
            TwistBtn.gameObject.SetActive(true);
            Twist10Btn.gameObject.SetActive(true);
        }
        var CurRewardList = GetCurTypeIdList();
        for (int i = 0; i < gashaponData.RewardList.Count; i++)
        {
            if(gashaponData.RewardList[i].Id == CurRewardList.SuitRewardId_1)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OwenMask1.gameObject.SetActive(isOwned);
            }
            else if (gashaponData.RewardList[i].Id == CurRewardList.SuitRewardId_2)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OwenMask2.gameObject.SetActive(isOwned);
            }
            else if (gashaponData.RewardList[i].Id == CurRewardList.SuitRewardId_3)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OwenMask3.gameObject.SetActive(isOwned);
            }
            else
            {
                if(m_CurSelectType == 0)
                {
                    if(firstSuitItemList.Contains(gashaponData.RewardList[i].Id))
                    {
                        bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                        OwenMask0.gameObject.SetActive(isOwned);
                    }
                }
                else if(m_CurSelectType == 1)
                {
                    if(secSuitItemList.Contains(gashaponData.RewardList[i].Id))
                    {
                        bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                        OwenMask0.gameObject.SetActive(isOwned);
                    }
                }
            }
        }
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        base.OnGashaOnceRsp(gashaponRsp);
        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
        new GashaponTwistAnimParam()
        {
            gashaponId = gashaponData.Id
        });
        panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () => {
            OnGashaTwistAnimComplete(gashaponRsp);
        });
        InitData(m_CurSelectType);
        GetSeverRefresh();
        if(gashaponRsp.gachaTimes != 0)
        {
            TipPanel.ShowToast(string.Format("已扭蛋{0}次，返还{1}优优币", gashaponRsp.gachaTimes, gashaponRsp.gachaReturn?.amount));
        }
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp)
    {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        if (gashaponRsp == null || gashaponRsp.rewardList == null)
        {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponId, gashaponRsp);

        if (UIManager.Inst.TryFindPanel<CreatorRewardPanel>(WindowId.CreatorWindow, PanelId.CreatorCenterPanel,
                out var creatorRewardPanel) && gashaponData.CurrencyType == CurrencyType.GreenCoin)
        {
            creatorRewardPanel.Refresh();
        }
    }

    private void GetSeverRefresh()
    {
        string typeId = GetGashaponIdByType();
        if(string.IsNullOrEmpty(typeId))
        {
            return;
        }
        GashaponDataManager.Inst.RequestGashaponInfo(typeId, OnGashaponInfoUpdate);
    }

    private void OnTabBtn0Click()
    {
        m_CurSelectType = 0;
        InitData(0);
        GetSeverRefresh();
    }

    private void OnTabBtn1Click()
    {
        m_CurSelectType = 1;
        InitData(1);
        GetSeverRefresh();
    }
    private void OnPriviewBtnClick()
    {
        if (gashaponData == null)
        {
            Debug.LogError("gashaponData is null!");
            return;
        }
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(GetGashaponIdByType());
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = GetGashaponNameByType(),
            gashaponData = gashaponData,
            rewardCurrency = CurrencyType.YouYouCoin,
            rulePath = viewCfg.RulePath,
            onBackCallBack = ShowCloseInfo,
        });
        if (previewPanel != null)
            previewPanel.SetBundleViewBgClolr(bundleViewBgColor);

        CharacterRoot.gameObject.SetActive(false);
    }

    private void ShowCloseInfo()
    {
        CharacterRoot.gameObject.SetActive(true);
    }

    private string GetGashaponNameByType()
    {
        string name = "";
        switch (m_CurSelectType)
        {
            case 0:
                name = "夜蝶幽梦";
                break;
            case 1:
                name = "紫云临渊";
                break;
        }
        return name;

    }

    private void OnSeasonBtnClick()
    {
        // UIManager.Inst.OpenPanel(PanelId.UMIMusicalGiftPackPanel);
        UIManager.Inst.OpenPanel<RechargePanel>(PanelId.RechargePanel, RechargeId.LimitedRechargeGiftPack);
    }

    private void OnTwistClick()
    {
        if(gashaponInfoRsp == null)
        {
            return;
        }
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.YouYouCoin);
        if (gashaponInfoRsp.singleDrawDiscountedPrice > num)
        {
            ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
            badgePanel.SetData(CurrencyType.YouYouCoin, CurrencyType.Gem, gashaponInfoRsp.singleDrawDiscountedPrice - num);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.YouYouCoin, gashaponInfoRsp.singleDrawDiscountedPrice))
        {
            var val = gashaponInfoRsp.singleDrawDiscountedPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.YouYouCoin, val);
            return;
        }

        var gId = GetGashaponIdByType();
        GashaponDataManager.Inst.RequestGashapon(gId, OneTimeGasha, OnGashaOnceRsp);
    }

    private void OnTenTwistClick()
    {
        if (gashaponInfoRsp == null)
        {
            return;
        }
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.YouYouCoin);
        if(gashaponInfoRsp.tenDrawDiscountedPrice > num)
        {
            ExchangeCoinPanel badgePanel = UIManager.Inst.OpenPanel<ExchangeCoinPanel>(PanelId.ExchangeCoinPanel);
            badgePanel.SetData(CurrencyType.YouYouCoin, CurrencyType.Gem, gashaponInfoRsp.tenDrawDiscountedPrice - num);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.YouYouCoin, gashaponInfoRsp.singleDrawPrice))
        {
            var val = gashaponInfoRsp.singleDrawPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.YouYouCoin, val);
            return;
        }

        var gId = GetGashaponIdByType();
        GashaponDataManager.Inst.RequestGashapon(gId, 8, OnGashaOnceRsp);
    }
    
    private void OnInfoClick()
    {
        var rulePath = "Assets/Loadable/UI/UIPanel/VioletGashaponPanel/Rule.json";

        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }
}

public class VioletSuitRewardInfo
{
    public string SuitRewardId_0;

    public string SuitRewardId_1;

    public string SuitRewardId_2;

    public string SuitRewardId_3;
}
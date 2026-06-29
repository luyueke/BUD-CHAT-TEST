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

public class GashaponSockPanel : BaseGashaponView
{
    [SerializeField] protected string bundleViewBgColor = "#FF9B9B";

    [Header("按钮相关")]
    [SerializeField] private CButton InfoBtn;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton SeasonBtn;
    [SerializeField] private CButton TwistBtn;
    [SerializeField] private CButton Twist10Btn;
    [SerializeField] private Toggle SuitToggle_0;
    [SerializeField] private Toggle SuitToggle_1;
    [SerializeField] private CButton BigRewardBtn;

    [Header("界面展示")]
    [SerializeField] private Transform CharacterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    [SerializeField] private List<SuitRewardItem> RewardItemList;
    [SerializeField] private Text OncePrice;
    [SerializeField] private Text OnceBeforePrice;
    [SerializeField] private Text OnceDiscountText;
    [SerializeField] private Transform OnceDiscount;
    [SerializeField] private Text TenPrice;
    [SerializeField] private Text TenBeforePrice;
    [SerializeField] private Text TenDiscountText;
    [SerializeField] private Transform TenDiscount;
    [SerializeField] private Image BigRewardIcon_0;
    [SerializeField] private Text BigRewardText_0;
    [SerializeField] private Image BigRewardIcon_1;
    [SerializeField] private Text BigRewardText_1;
    [SerializeField] private Image BigRewardIcon_2;
    [SerializeField] private Text BigRewardText_2;
    [SerializeField] private Image BigRewardIcon_3;
    [SerializeField] private Text BigRewardText_3;
    [SerializeField] private Transform OverMask0;
    [SerializeField] private Transform OverMask1;
    [SerializeField] private Transform OverMask2;
    [SerializeField] private Text tipsText;
    [SerializeField] private CButton GetTaskBtn;
    [SerializeField] private List<CButton> ShowInfoBtnList;
    private GameObject CharacterShow0;
    private GameObject CharacterShow1;

    //人物3d预览
    private CharacterWrap characterWrap;

    private int m_CurSelectType = 0;

    public override void OnCreate(string id)
    {
        base.OnCreate(id);
        m_CurSelectType = 0;
        InitData();
        InitPreviewPlayer();
        SuitToggle_0.onValueChanged.AddListener(SuitToggleOneClick);
        SuitToggle_1.onValueChanged.AddListener(SuitToggleSecClick);
        PreviewBtn.onClick.AddListener(OnPriviewBtnClick);
        SeasonBtn.onClick.AddListener(OnSeasonBtnClick);
        TwistBtn.onClick.AddListener(OnTwistClick);
        Twist10Btn.onClick.AddListener(OnTenTwistClick);
        InfoBtn.onClick.AddListener(OnInfoClick);
        BigRewardBtn.onClick.AddListener(OnPriviewBtnClick);
        SuitToggleOneClick(true);
        GetTaskBtn.onClick.AddListener(() =>
        {
            GashaponDataManager.Inst.RequestClaimTaskReward(GetGashaponIdByType(), 3, OnUpdateExtraTask);
        });
        for (int i = 0; i < ShowInfoBtnList.Count; i++)
        {
            ShowInfoBtnList[i].onClick.AddListener(OnPriviewBtnClick);
        }

        SetImage();
    }

    private void SetImage()
    {
        //var spriteatlasPath = "Assets/Loadable/UI/UIPanel/MusicPuddingPanel/MusicPuddingAltas.spriteatlas";
        //var spriteatlasPath1 = "Assets/Loadable/UI/UIPanel/SecMusicPuddingPanel/SecMusicPuddingAltas.spriteatlas";
        //var rewardbg = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, "Suit_Reward_Bg", gameObject);
        //for (int i = 0; i < RewardItemList.Count; i++)
        //{
        //    RewardItemList[i].GetComponent<Image>().sprite = rewardbg;
        //    RewardItemList[i].transform.Find("Icon").GetComponent<Image>().sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath1, $"bottom_item_icon_{i}", gameObject);
        //}
    }

    public override void OnShow()
    {
        base.OnShow();

        var pendingId = GashaponDataManager.Inst.pendingSubLotteryId;
        GashaponDataManager.Inst.pendingSubLotteryId = null;

        if (pendingId == "lottery.wawaKindergartenSuit" || pendingId == "lottery.wawaKindergarten.pinkTail")
        {
            SuitToggle_0.isOn = true;
        }
        if (pendingId == "lottery.wawaKindergarten.warmOrange")
        {
            SuitToggle_1.isOn = true;
        }

        GetSeverRefresh();
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    private void InitPreviewPlayer()
    {
        avatarCameraController.RotateTarget = CharacterRoot;

        var firstSuitItemList = new List<string>() { "11000274", "11100043", "10300050", "11200046", "10600136",
            "10800131", "11000268", "10200069", "12100149", "10900506", "11300366", "10400505" };
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, CharacterRoot.transform);
        CharacterShow0 = characterWrapper.Avatar.gameObject;
        CharacterShow0.gameObject.SetActive(false);
        foreach (var pgcId in firstSuitItemList)
        {
            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrapper.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
            characterWrapper.ChangeColor(classType, config.defaultColor);
            characterWrapper.Move(classType, config.pDef);
            characterWrapper.Rotate(classType, config.rDef);
            characterWrapper.Scale(classType, config.sDef);
            characterWrapper.HVScale(classType, config.vhSDef);
            characterWrapper.SetLeftOrRight(classType, config.leftRightType);
        }
        characterWrapper.Avatar?.GetComponentInChildren<CustomBodyTypeController>()?.ApplyBodyType(CustomBodyTypeController.BodyType.None);

        var secSuitItemList = new List<string>() { "11200045", "11100042", "10300049", "10600135",
            "12100148", "11600038", "10200068", "10800130", "11300365", "10400504", "11000275","10900515" };
        var saveCharacterData2 = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper2 = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, CharacterRoot.transform);
        CharacterShow1 = characterWrapper2.Avatar.gameObject;
        CharacterShow1.gameObject.SetActive(false);
        foreach (var pgcId in secSuitItemList)
        {
            var pgcConfig = PgcUtils.GetPgcConfigData(pgcId);
            var config = DataTables.GetAvatarCommonData(pgcId);
            var classType = UniqueType.GetAvatar(pgcId);
            characterWrapper2.ChangePart(UniqueType.GetAvatar((AvatarSubType)pgcConfig.SubType), pgcId);
            characterWrapper2.ChangeColor(classType, config.defaultColor);
            characterWrapper2.Move(classType, config.pDef);
            characterWrapper2.Rotate(classType, config.rDef);
            characterWrapper2.Scale(classType, config.sDef);
            characterWrapper2.HVScale(classType, config.vhSDef);
            characterWrapper2.SetLeftOrRight(classType, config.leftRightType);
        }
        characterWrapper2.Avatar?.GetComponentInChildren<CustomBodyTypeController>()?.ApplyBodyType(CustomBodyTypeController.BodyType.None);
    }

    private void GetSeverRefresh()
    {
        string typeId = GetGashaponIdByType();
        if (string.IsNullOrEmpty(typeId))
        {
            return;
        }
        GashaponDataManager.Inst.RequestGashaponInfo(typeId, OnGashaponInfoUpdate);
    }

    private void InitData(int type = 0)
    {
        m_CurSelectType = type;

        var CurRewardList = GetCurTypeIdList();
        PgcUtils.LoadEmoteIconAsync(CurRewardList.SuitRewardId_0, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_1.sprite = iconSprite;
                //     BigRewardIcon_1.SetNativeSize();
                var cfg = DataTables.GetEmoUIConfig(CurRewardList.SuitRewardId_0);
                BigRewardText_1.text = cfg.name;
            }
        });
        PgcUtils.LoadEmoteIconAsync(CurRewardList.SuitRewardId_1, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_2.sprite = iconSprite;
                //   BigRewardIcon_2.SetNativeSize();
                var cfg = DataTables.GetEmoUIConfig(CurRewardList.SuitRewardId_1);
                BigRewardText_2.text = cfg.name;
            }
        });
        PgcUtils.LoadEmoteIconAsync(CurRewardList.SuitRewardId_2, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_3.sprite = iconSprite;
                //     BigRewardIcon_3.SetNativeSize();
                var cfg = DataTables.GetEmoUIConfig(CurRewardList.SuitRewardId_2);
                BigRewardText_3.text = cfg.name;
            }
        });
        PgcUtils.LoadBundleIconAsync(CurRewardList.BundleId, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_0.sprite = iconSprite;
                BigRewardText_0.text = m_CurSelectType == 1 ? "暖橙晕晕套装" : "粉绒小尾套装";
            }
        });
        gashaponData = GashaponDataManager.Inst.gashaponData(GetGashaponIdByType());

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
        float discount = infoRsp.singleDrawDiscountedPrice / (float)infoRsp.singleDrawPrice;
        TenDiscount.gameObject.SetActive(discount != 1);
        OnceDiscount.gameObject.SetActive(discount != 1);
        OnceDiscountText.text = string.Format("限时{0}折", discount * 10);
        TenDiscountText.text = string.Format("限时{0}折", discount * 10);

        if (infoRsp.taskList != null)
        {
            if (infoRsp != null && infoRsp.taskList != null)
            {
                for (int i = 0; i < RewardItemList.Count; i++)
                {
                    if (infoRsp.taskList.Count <= i)
                    {
                        return;
                    }
                    RewardItemList[i].SetData(infoRsp.taskList[i]);
                }
            }
        }
        SetImage();
        if (infoRsp.singleDrawPrice == 0 && infoRsp.singleDrawDiscountedPrice == 0)
        {
            tipsText.text = "恭喜！你已集齐当前奖池所有商品！";
            TwistBtn.gameObject.SetActive(false);
            Twist10Btn.gameObject.SetActive(false);
        }
        else
        {
            tipsText.text = "抽取1轮提前获得大奖会返还袜币，前三轮限时享受专属折扣.";
            TwistBtn.gameObject.SetActive(true);
            Twist10Btn.gameObject.SetActive(true);
        }

        var CurRewardList = GetCurTypeIdList();
        for (int i = 0; i < gashaponData.RewardList.Count; i++)
        {
            if (gashaponData.RewardList[i].Id == CurRewardList.SuitRewardId_0)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OverMask0.gameObject.SetActive(isOwned);
            }
            else if (gashaponData.RewardList[i].Id == CurRewardList.SuitRewardId_1)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OverMask1.gameObject.SetActive(isOwned);
            }
            else if (gashaponData.RewardList[i].Id == CurRewardList.SuitRewardId_2)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OverMask2.gameObject.SetActive(isOwned);
            }
        }
    }

    private SuitRewardInfo GetCurTypeIdList()
    {
        switch (m_CurSelectType)
        {
            case 0:
                return new SuitRewardInfo()
                {
                    CenterRewarId = "",
                    BundleId = "138",
                    SuitRewardId_0 = "40200535",
                    SuitRewardId_1 = "40200527",
                    SuitRewardId_2 = "40200526",
                };
            case 1:
                return new SuitRewardInfo()
                {
                    CenterRewarId = "",
                    BundleId = "139",
                    SuitRewardId_0 = "40200536",
                    SuitRewardId_1 = "40200525",
                    SuitRewardId_2 = "40200524",
                };
        }

        return null;
    }

    private void OnPriviewBtnClick()
    {
        if (gashaponData == null)
        {
            Debug.LogError("gashaponData is null!");
            return;
        }
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var previewPanel = UIManager.Inst.OpenPanel<GashaponPreviewPanel>(PanelId.GashaponPreviewPanel, new GashaponPreviewParam
        {
            bgPath = viewCfg.BgPath,
            title = GetGashaponNameByType(),
            gashaponData = gashaponData,
            gashaponInfoRsp = gashaponInfoRsp,
            rewardCurrency = CurrencyType.SockCoin,
            rulePath = viewCfg.RulePath,
            onBackCallBack = ShowCloseInfo,
        });
        if (previewPanel != null)
            previewPanel.SetBundleViewBgClolr(bundleViewBgColor);

        CharacterRoot.gameObject.SetActive(false);
    }

    private void OnSeasonBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.SockThemeGiftPanel);
    }

    private void OnTwistClick()
    {
        if (gashaponInfoRsp == null)
        {
            return;
        }
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.SockCoin);
        if (gashaponInfoRsp.singleDrawDiscountedPrice > num)
        {
            int lessNum = gashaponInfoRsp.singleDrawDiscountedPrice - num;
            //UIManager.Inst.OpenPanel(PanelId.CatRechargePanel, lessNum);
            UIManager.Inst.OpenPanel(PanelId.SockRechargePanel, lessNum);
            //UIManager.Inst.OpenPanel(PanelId.RechargePanel,(int)RechargeId.WaCoinPack);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.SockCoin, gashaponInfoRsp.singleDrawDiscountedPrice))
        {
            var val = gashaponInfoRsp.singleDrawDiscountedPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.SockCoin, val);
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
        //判断当前喵币是否大于十抽金额
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.SockCoin);
        if (gashaponInfoRsp.tenDrawDiscountedPrice > num)
        {
            int lessNum = gashaponInfoRsp.tenDrawDiscountedPrice - num;
            //UIManager.Inst.OpenPanel(PanelId.CatRechargePanel, lessNum);
            UIManager.Inst.OpenPanel(PanelId.SockRechargePanel, lessNum);
            //UIManager.Inst.OpenPanel(PanelId.RechargePanel,(int)RechargeId.WaCoinPack);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.SockCoin, gashaponInfoRsp.singleDrawPrice))
        {
            var val = gashaponInfoRsp.singleDrawPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.SockCoin, val);
            return;
        }

        var gId = GetGashaponIdByType();
        GashaponDataManager.Inst.RequestGashapon(gId, 8, OnGashaOnceRsp);
    }

    private void OnInfoClick()
    {
        var rulePath = "Assets/Loadable/UI/UIPanel/GashaponSock/Rule.json";

        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        base.OnGashaOnceRsp(gashaponRsp);
        Debug.Log("UMIMusicalNoteSuitPanel.OnGashaOnceRsp: " + gashaponRsp);
        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
        new GashaponTwistAnimParam()
        {
            bgPath = "Assets/Loadable/UI/UIPanel/GashaponSock/preview_bg1.png",
            gashaponId = gashaponData.Id
        });
        panel.PlayOneTwistAnimation(gashaponRsp.rewardList, () =>
        {
            OnGashaTwistAnimComplete(gashaponRsp);
        });
        InitData(m_CurSelectType);
        GetSeverRefresh();

        if (gashaponRsp.gachaTimes != 0 && gashaponRsp.gachaReturn != null)
        {
            TipPanel.ShowToast(string.Format("已抽取{0}次，返还{1}袜币", gashaponRsp.gachaTimes, gashaponRsp.gachaReturn.amount));
            var tippanel = UIManager.Inst.FindPanel(WindowId.CommonWindow, PanelId.TipPanel);
            if (tippanel != null)
            {
                var canvas = tippanel.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 1000;
            }
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

    private void OnUpdateExtraTask(GashaponTaskRewardRsp rewardRsp)
    {
        if (!this || rewardRsp == null) return;

        List<CommonRewardData> rewardsItems = rewardRsp.rewardList;
        var pairList = rewardRsp.backpackData?.pairList;
        if ((rewardsItems == null || rewardsItems.Count <= 0) && (pairList == null || pairList.Count <= 0))
        {
            return;
        }
        var rewardList = new List<CommonRewardItemData>();
        CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        var altas = "Assets/Loadable/UI/UIWidgets/UserInfoBubbleView/UserInfoBubbleView.spriteatlas";
        commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(altas, "ShowBubble_28", gameObject);
        commonRewardItemData.rewardName = "粉绒小尾聊天气泡";
        commonRewardItemData.RewardAmount = 1;
        rewardList.Add(commonRewardItemData);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardList);
        //GashaponDataManager.Inst.ShowGashaponReward(this.gameObject, rewardsItems, pairList);
        for (int i = 0; i < RewardItemList.Count; i++)
        {
            if (rewardRsp.taskList.Count <= i)
            {
                return;
            }
            RewardItemList[i].SetData(rewardRsp.taskList[i]);
        }
        SetImage();
    }

    private string GetGashaponIdByType()
    {
        string typeId = "";
        switch (m_CurSelectType)
        {
            case 0:
                typeId = "lottery.wawaKindergarten.pinkTail";
                break;
            case 1:
                typeId = "lottery.wawaKindergarten.warmOrange";
                break;
        }
        return typeId;
    }

    private string GetGashaponNameByType()
    {
        string name = "";
        switch (m_CurSelectType)
        {
            case 0:
                name = "粉绒小尾套装";
                break;
            case 1:
                name = "暖橙晕晕套装";
                break;
        }
        return name;
    }

    private void SuitToggleOneClick(bool isOn)
    {
        if (!isOn)
        {
            return;
        }
        InitData(0);
        GetSeverRefresh();
        CharacterShow0.gameObject.SetActive(true);
        CharacterShow1.gameObject.SetActive(false);
    }

    private void SuitToggleSecClick(bool isOn)
    {
        if (!isOn)
        {
            return;
        }
        InitData(1);
        GetSeverRefresh();
        CharacterShow0.gameObject.SetActive(false);
        CharacterShow1.gameObject.SetActive(true);
    }

    private void ShowCloseInfo()
    {
        CharacterRoot.gameObject.SetActive(true);
    }
}

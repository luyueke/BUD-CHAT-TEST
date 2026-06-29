using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using Message;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class UMIMusicalNoteSuitPanel : BaseGashaponView
{

    [SerializeField] protected string bundleViewBgColor = "#FF9B9B";

    [Header("按钮相关")]
    [SerializeField] private CButton InfoBtn;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton SeasonBtn;
    [SerializeField] private CButton TwistBtn;
    [SerializeField] private CButton Twist10Btn;
    [SerializeField] private CButton GetBubbleBtn;
    [SerializeField] private Toggle SuitToggle_0;
    [SerializeField] private Toggle SuitToggle_1;
    [SerializeField] private Toggle SuitToggle_2;
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
    [SerializeField] private Transform CharacterShow0;
    [SerializeField] private Transform CharacterShow1;

    private PlayerAnimationCtrl animationCtrl;

    //private CharacterWrap otherCharacterWrap;

    //人物3d预览
    private CharacterWrap characterWrap;

    private int m_CurSelectType;

    private BaseAvatarData saveAvatarInfo;

    public override void OnCreate(string id)
    {
        base.OnCreate(id);
        SuitToggle_0.onValueChanged.AddListener(SuitToggleOneClick);
        SuitToggle_1.onValueChanged.AddListener(SuitToggleSecClick);
        SuitToggle_2.onValueChanged.AddListener(SuitToggleThirdClick);
        PreviewBtn.onClick.AddListener(OnPriviewBtnClick);
        SeasonBtn.onClick.AddListener(OnSeasonBtnClick);
        TwistBtn.onClick.AddListener(OnTwistClick);
        Twist10Btn.onClick.AddListener(OnTenTwistClick);
        InfoBtn.onClick.AddListener(OnInfoClick);
        BigRewardBtn.onClick.AddListener(OnPriviewBtnClick);
        GetTaskBtn.onClick.AddListener(() =>
        {
            GashaponDataManager.Inst.RequestClaimTaskReward(GetGashaponIdByType(), 4, OnUpdateExtraTask);
        });
        for (int i = 0; i < ShowInfoBtnList.Count; i++)
        {
            ShowInfoBtnList[i].onClick.AddListener(OnPriviewBtnClick);
        }
        InitData();
        InitPreviewPlayer();
        ChangeAvatarInfo("11400083");
    }

    public override void OnShow()
    {
        base.OnShow();

        var pendingId = GashaponDataManager.Inst.pendingSubLotteryId;
        GashaponDataManager.Inst.pendingSubLotteryId = null;

        if (pendingId == "lottery.musicalNoteSuit" || pendingId == "lottery.musicalPudding.dreamingGhost")
        {
            SuitToggle_0.isOn = true;
        }
        if (pendingId == "lottery.musicalPudding.kumoGhost")
        {
            SuitToggle_1.isOn = true;
        }
        if (pendingId == "lottery.musicalPudding.kuroGhost")
        {
            SuitToggle_2.isOn = true;
        }

        GetSeverRefresh();
    }

    public override void OnHide()
    {
        base.OnHide();
    }

    private void InitPreviewPlayer()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData != null && characterWrap == null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(CharacterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = CharacterRoot;
            animationCtrl.gameObject.SetActive(true);
        }
        saveAvatarInfo = saveCharacterData;

    }

    private void ChangeAvatarInfo(string pgcId)
    {
        if (characterWrap == null)
        {
            return;
        }
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrap.ChangePart(classType, pgcId);
        characterWrap.ChangeColor(classType, config.defaultColor);
        characterWrap.Move(classType, config.pDef);
        characterWrap.Rotate(classType, config.rDef);
        characterWrap.Scale(classType, config.sDef);
        characterWrap.HVScale(classType, config.vhSDef);
        characterWrap.SetLeftOrRight(classType, config.leftRightType);
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

    private void InitData(int type = 0)
    {
        m_CurSelectType = type;
        var CurRewardList = GetCurTypeIdList();
        PgcUtils.LoadEmoteIconAsync(CurRewardList.SuitRewardId_0, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_1.sprite = iconSprite;
                var cfg = DataTables.GetEmoUIConfig(CurRewardList.SuitRewardId_0);
                BigRewardText_1.text = cfg.name;
            }
        });
        PgcUtils.LoadEmoteIconAsync(CurRewardList.SuitRewardId_1, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_2.sprite = iconSprite;
                var cfg = DataTables.GetEmoUIConfig(CurRewardList.SuitRewardId_1);
                BigRewardText_2.text = cfg.name;
            }
        });
        PgcUtils.LoadEmoteIconAsync(CurRewardList.SuitRewardId_2, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_3.sprite = iconSprite;
                var cfg = DataTables.GetEmoUIConfig(CurRewardList.SuitRewardId_2);
                BigRewardText_3.text = cfg.name;
            }
        });
        if(string.IsNullOrEmpty(CurRewardList.BundleId))
        {
            PgcUtils.LoadAvatarIconAsync(CurRewardList.CenterRewarId, gameObject, (iconSprite) =>
            {
                if (iconSprite != null)
                {
                    BigRewardIcon_0.sprite = iconSprite;
                    BigRewardText_0.text = "梦绕幽灵咪";
                }
            });
        }
        else
        {
            PgcUtils.LoadBundleIconAsync(CurRewardList.BundleId, gameObject, (iconSprite) =>
            {
                if (iconSprite != null)
                {
                    BigRewardIcon_0.sprite = iconSprite;
                    BigRewardText_0.text = m_CurSelectType == 1 ? "kumo幽灵咪套装" : "kuro幽灵咪套装";
                }
            });
        }

        gashaponData = GashaponDataManager.Inst.gashaponData(GetGashaponIdByType());
        
    }

    protected override void OnGashaponInfoUpdate(GashaponInfoRsp infoRsp)
    {
        base.OnGashaponInfoUpdate(infoRsp);
        if (infoRsp == null || infoRsp.taskList == null)
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

        if(infoRsp.singleDrawPrice == 0 && infoRsp.singleDrawDiscountedPrice == 0)
        {
            tipsText.text = "恭喜！你已集齐当前奖池所有商品！";
            TwistBtn.gameObject.SetActive(false);
            Twist10Btn.gameObject.SetActive(false);
        }
        else
        {
            tipsText.text = "抽取1轮提前获得大奖会返还喵币，前三轮限时享受专属折扣.";
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
            else if(gashaponData.RewardList[i].Id == CurRewardList.SuitRewardId_1)
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
        switch(m_CurSelectType)
        {
            case 0: 
                return new SuitRewardInfo()
                {
                    CenterRewarId = "11400083",
                    BundleId = "",
                    SuitRewardId_0 = "40100516",
                    SuitRewardId_1 = "40100517",
                    SuitRewardId_2 = "40100518",
                };
            case 1: 
                return new SuitRewardInfo()
                {
                    CenterRewarId = "10900495",
                    BundleId = "115",
                    SuitRewardId_0 = "40100519",
                    SuitRewardId_1 = "40100520",
                    SuitRewardId_2 = "40100521",
                };
            case 2:
                return new SuitRewardInfo()
                {
                    CenterRewarId = "10900496",
                    BundleId = "116",
                    SuitRewardId_0 = "40100522",
                    SuitRewardId_1 = "40100523",
                    SuitRewardId_2 = "40100524",
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
            rewardCurrency = CurrencyType.MiaoCoin,
            rulePath = viewCfg.RulePath,
            onBackCallBack = ShowCloseInfo,
        });
        if (previewPanel != null)
            previewPanel.SetBundleViewBgClolr(bundleViewBgColor);

        CharacterRoot.gameObject.SetActive(false);
    }

    private void OnSeasonBtnClick()
    {
        UIManager.Inst.OpenPanel(PanelId.SecUMIMusicalGiftPackPanel);
    }

    private void OnTwistClick()
    {
        if(gashaponInfoRsp == null)
        {
            return;
        }
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.MiaoCoin);
        if (gashaponInfoRsp.singleDrawDiscountedPrice > num)
        {
            int lessNum = gashaponInfoRsp.singleDrawDiscountedPrice - num;
            UIManager.Inst.OpenPanel(PanelId.CatRechargePanel, lessNum);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.MiaoCoin, gashaponInfoRsp.singleDrawDiscountedPrice))
        {
            var val = gashaponInfoRsp.singleDrawDiscountedPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.MiaoCoin, val);
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
        int num = AccountDataManager.Inst.BalanceInfo.GetAccountCount(CurrencyType.MiaoCoin);
        if(gashaponInfoRsp.tenDrawDiscountedPrice > num)
        {
            int lessNum = gashaponInfoRsp.tenDrawDiscountedPrice - num;
            UIManager.Inst.OpenPanel(PanelId.CatRechargePanel, lessNum);
            return;
        }

        if (!GashaponUtils.CurrencyIsEnough((int)CurrencyType.MiaoCoin, gashaponInfoRsp.singleDrawPrice))
        {
            var val = gashaponInfoRsp.singleDrawPrice;
            GashaponDataManager.Inst.ShowCurrencyNoEnough((int)CurrencyType.MiaoCoin, val);
            return;
        }

        var gId = GetGashaponIdByType();
        GashaponDataManager.Inst.RequestGashapon(gId, 8, OnGashaOnceRsp);
    }

    private void OnInfoClick()
    {
        //var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        //var rulePath = viewCfg.RulePath;
        //if (string.IsNullOrEmpty(rulePath))
        //{
        //    rulePath = "Assets/Loadable/UI/UIPanel/MusicPuddingPanel/Rule2.json";
        //}
        var rulePath = "Assets/Loadable/UI/UIPanel/MusicPuddingPanel/Rule2.json";

        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        base.OnGashaOnceRsp(gashaponRsp);
        Debug.Log("UMIMusicalNoteSuitPanel.OnGashaOnceRsp: " + gashaponRsp);
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
        commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(altas, "HallChatBubble_19", gameObject);
        commonRewardItemData.rewardName = "音符布丁限定聊天气泡";
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
    }

    private string GetGashaponIdByType()
    {
        string typeId = "";
        switch (m_CurSelectType)
        {
            case 0:
                typeId = "lottery.musicalPudding.dreamingGhost";
                break;
            case 1:
                typeId = "lottery.musicalPudding.kumoGhost";
                break;
            case 2:
                typeId = "lottery.musicalPudding.kuroGhost";
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
                name = "梦绕幽灵咪";
                break;
            case 1:
                name = "kumo幽灵咪";
                break;
            case 2:
                name = "kuro幽灵咪";
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
        characterWrap.RefreshAvatar(saveAvatarInfo as CharacterData);
        ChangeAvatarInfo("11400083");
        characterWrap.Avatar.gameObject.SetActive(true);
        CharacterShow0.gameObject.SetActive(false);
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
        ShowAvatar();
        characterWrap.Avatar.gameObject.SetActive(false);
        CharacterShow0.gameObject.SetActive(true);
        CharacterShow1.gameObject.SetActive(false);
    }

    private void SuitToggleThirdClick(bool isOn)
    {
        if (!isOn)
        {
            return;
        }
        InitData(2);
        GetSeverRefresh();
        ShowAvatar();
        characterWrap.Avatar.gameObject.SetActive(false);
        CharacterShow0.gameObject.SetActive(false);
        CharacterShow1.gameObject.SetActive(true);
    }

    private void ShowAvatar()
    {
        characterWrap.RefreshAvatar(saveAvatarInfo as CharacterData);
        if (gashaponData.RewardList != null && gashaponData.RewardList.Count > 0)
        {
            for(int i = 0; i < gashaponData.RewardList.Count; i++)
            {
                if (string.IsNullOrEmpty(gashaponData.RewardList[i].BundleId))
                {
                    continue;
                }
                List<string> pgcIds = GashaponUtils.ToPgcIdList(gashaponData.RewardList[i].PgcDatas);
                ChangeAvatarInfo(pgcIds[0]);
            }
        }
    }

    private void ShowCloseInfo()
    {
        CharacterRoot.gameObject.SetActive(true);
    }
}

public class SuitRewardInfo
{
    public string CenterRewarId;

    public string BundleId;

    public string SuitRewardId_0;

    public string SuitRewardId_1;

    public string SuitRewardId_2;
}

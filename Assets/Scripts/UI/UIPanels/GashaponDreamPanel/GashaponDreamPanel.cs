using System.Collections.Generic;
using Es;
using Game.Avatar;
using GameData.Gashapon;
using GameData.PgcData;
using Newtonsoft.Json;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponDreamPanel : BaseGashaponView
{
    [SerializeField] protected string bundleViewBgColor = "#FF9B9B";

    [Header("按钮相关")]
    [SerializeField] private CButton InfoBtn;
    [SerializeField] private CButton PreviewBtn;
    [SerializeField] private CButton ShowRewardBtn;
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
    [SerializeField] private Transform OverMask3;
    [SerializeField] private Text tipsText;
    [SerializeField] private CButton GetTaskBtn;
    [SerializeField] private List<CButton> ShowInfoBtnList;

    private GameObject CharacterShow0;
    private GameObject CharacterShow1;
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
        ShowRewardBtn.onClick.AddListener(OnPriviewBtnClick);
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
    }

    public override void OnShow()
    {
        base.OnShow();

        var pendingId = GashaponDataManager.Inst.pendingSubLotteryId;
        GashaponDataManager.Inst.pendingSubLotteryId = null;

        if (pendingId == "lottery.whiteDewDrowningStar")
        {
            SuitToggle_0.isOn = true;
        }
        if (pendingId == "lottery.orangeRainBubbles")
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

        var firstSuitItemList = new List<string>() { "10400512","11300373","10700132","10800137","12100151","10100122","11000281","11200052","10600142","11100049","10900513" };
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        var characterWrapper = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, CharacterRoot.transform);
        CharacterShow0 = characterWrapper.Avatar.gameObject;
        CharacterShow0.SetActive(false);
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
        var bodyCtrl0 = characterWrapper.Avatar.GetComponentInChildren<CustomBodyTypeController>();
        if (bodyCtrl0 != null) bodyCtrl0.ApplyBodyType(CustomBodyTypeController.BodyType.None);

        var secSuitItemList = new List<string>() { "10400511","11300372","10900511","12100155","11000277","10800136","12000037","10500083","11200051","10300055",
        "10600141","11100048","11400089","11000278" };
        var characterWrapper2 = AvatarController.Inst.CreateUIAvatarWithIKController(saveCharacterData, CharacterRoot.transform);
        CharacterShow1 = characterWrapper2.Avatar.gameObject;
        CharacterShow1.SetActive(false);
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
        var bodyCtrl1 = characterWrapper2.Avatar.GetComponentInChildren<CustomBodyTypeController>();
        if (bodyCtrl1 != null) bodyCtrl1.ApplyBodyType(CustomBodyTypeController.BodyType.None);
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

        var curRewardList = GetCurTypeIdList();
        PgcUtils.LoadBundleIconAsync(curRewardList.SuitRewardId_0, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_0.sprite = iconSprite;
                BigRewardText_0.text = m_CurSelectType == 1 ? "橘雨泡泡套装" : "白露溺星套装";
            }
        });
        PgcUtils.LoadAvatarIconAsync(curRewardList.SuitRewardId_1, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_1.sprite = iconSprite;
                var cfg = DataTables.GetPgcNameData(curRewardList.SuitRewardId_1);
                BigRewardText_1.text = cfg.Name;
            }
        });
        PgcUtils.LoadEmoteIconAsync(curRewardList.SuitRewardId_2, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_2.sprite = iconSprite;
                var cfg = DataTables.GetEmoUIConfig(curRewardList.SuitRewardId_2);
                BigRewardText_2.text = cfg.name;
            }
        });
        PgcUtils.LoadAvatarIconAsync(curRewardList.SuitRewardId_3, gameObject, (iconSprite) =>
        {
            if (iconSprite != null)
            {
                BigRewardIcon_3.sprite = iconSprite;
                var cfg = DataTables.GetPgcNameData(curRewardList.SuitRewardId_3);
                BigRewardText_3.text = cfg.Name;
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
        float discount = infoRsp.singleDrawPrice > 0 ? infoRsp.singleDrawDiscountedPrice / (float)infoRsp.singleDrawPrice : 1f;
        TenDiscount.gameObject.SetActive(discount != 1);
        OnceDiscount.gameObject.SetActive(discount != 1);
        OnceDiscountText.text = string.Format("限时{0}折", discount * 10);
        TenDiscountText.text = string.Format("限时{0}折", discount * 10);

        if (infoRsp.taskList != null)
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

        if (infoRsp.singleDrawPrice == 0 && infoRsp.singleDrawDiscountedPrice == 0)
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

        var curRewardList = GetCurTypeIdList();
        for (int i = 0; i < gashaponData.RewardList.Count; i++)
        {
            if (gashaponData.RewardList[i].BundleId == curRewardList.SuitRewardId_0)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OverMask0.gameObject.SetActive(isOwned);
            }
            else if (gashaponData.RewardList[i].Id == curRewardList.SuitRewardId_1)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OverMask1.gameObject.SetActive(isOwned);
            }
            else if (gashaponData.RewardList[i].Id == curRewardList.SuitRewardId_2)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OverMask2.gameObject.SetActive(isOwned);
            }
            else if (gashaponData.RewardList[i].Id == curRewardList.SuitRewardId_3)
            {
                bool isOwned = GashaponUtils.IsOwnedReward(gashaponData.RewardList[i]);
                OverMask3.gameObject.SetActive(isOwned);
            }
        }
    }

    private VioletSuitRewardInfo GetCurTypeIdList()
    {
        switch (m_CurSelectType)
        {
            case 0:
                return new VioletSuitRewardInfo()
                {
                    SuitRewardId_0 = "142",      // TODO: 填入白露溺星套装 BundleId
                    SuitRewardId_1 = "10900513", // TODO
                    SuitRewardId_2 = "40300514", // TODO
                    SuitRewardId_3 = "12100152", // TODO
                };
            case 1:
                return new VioletSuitRewardInfo()
                {
                    SuitRewardId_0 = "141",      // TODO: 填入橘雨泡泡套装 BundleId
                    SuitRewardId_1 = "11000278", // TODO11000278
                    SuitRewardId_2 = "40100569", // TODO40100569
                    SuitRewardId_3 = "11000283", // TODO11000283
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
            rewardCurrency = CurrencyType.YouYouCoin,
            rulePath = viewCfg.RulePath,
            onBackCallBack = ShowCloseInfo,
        });
        if (previewPanel != null)
            previewPanel.SetBundleViewBgClolr(bundleViewBgColor);

        CharacterRoot.gameObject.SetActive(false);
    }

    private void OnTwistClick()
    {
        if (gashaponInfoRsp == null)
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
        if (gashaponInfoRsp.tenDrawDiscountedPrice > num)
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
        var rulePath = "Assets/Loadable/UI/UIPanel/GashaponDreamPanel/Rule.json";
        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp)
    {
        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
        new GashaponTwistAnimParam()
        {
            bgPath = "Assets/Loadable/UI/UIPanel/GashaponDreamPanel/preview_bg.png",
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
            TipPanel.ShowToast(string.Format("已抽取{0}次，返还{1}优优币", gashaponRsp.gachaTimes, gashaponRsp.gachaReturn.amount));
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
        var altas = "Assets/Loadable/UI/UIWidgets/Nickname/UserNickNameView.spriteatlas";
        commonRewardItemData.IconSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(altas, "NicknameIcon7", gameObject);
        commonRewardItemData.rewardName = "误入梦核境域昵称框";
        commonRewardItemData.RewardAmount = 1;
        rewardList.Add(commonRewardItemData);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardList);
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
        switch (m_CurSelectType)
        {
            case 0: return "lottery.whiteDewDrowningStar";
            case 1: return "lottery.orangeRainBubbles";
        }
        return "";
    }

    private string GetGashaponNameByType()
    {
        switch (m_CurSelectType)
        {
            case 0: return "白露溺星套装";
            case 1: return "橘雨泡泡套装";
        }
        return "";
    }

    private void SuitToggleOneClick(bool isOn)
    {
        if (!isOn) return;
        InitData(0);
        GetSeverRefresh();
        CharacterShow0.SetActive(true);
        CharacterShow1.SetActive(false);
    }

    private void SuitToggleSecClick(bool isOn)
    {
        if (!isOn) return;
        InitData(1);
        GetSeverRefresh();
        CharacterShow0.SetActive(false);
        CharacterShow1.SetActive(true);
    }

    private void ShowCloseInfo()
    {
        CharacterRoot.gameObject.SetActive(true);
    }
}

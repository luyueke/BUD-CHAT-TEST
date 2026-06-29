using System;
using System.Collections.Generic;
using System.Linq;
using Basic.Utils;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Config;
using Game.Pet;
using Game.Store;
using GameData.Gashapon;
using GameData.PgcData;
using UI.BaseWidgets;
using UI.Manager;
using UI.UIPanels.CreaterRewardPanel;
using UI.UIPanels.FittingRoom;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class DefaultGashaponView : BaseGashaponView {
    [SerializeField] private Transform bgNode;

    [Header("抽奖按钮相关")]
    [SerializeField] private CButton twistBtn;
    [SerializeField] private CButton twist10Btn;
    [SerializeField] private Image singleIcon;
    [SerializeField] private Image tenIcon;
    [SerializeField] private Text singleText; //单抽价格
    [SerializeField] private Text tenText; //十连抽价格
    [SerializeField] private Text tenDicText; //折后十连抽价格
    [SerializeField] private Text dicText; //折扣文本
    [SerializeField] private GameObject discountNumLine;
    [SerializeField] private GameObject discountTag; //折扣tag节点
    [SerializeField] private CButton infoBtn;
    [SerializeField] private CButton musicPreviewBtn;
    [SerializeField] private CButton changeOtherOcBtn;


    [Header("列表相关")]
    [SerializeField] private Transform cacheNode;
    [SerializeField] private Transform scrollContent;
    [SerializeField] private GashaponPriceItem itemPrefab;

    [Header("道具信息")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text itemNameText;
    [SerializeField] private Image currencyRewardImage;
    [SerializeField] private Text currencyRewardNum;

    [Header("人物展示")]
    [SerializeField] private GameObject playerImageView;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;


    [Header("各个根节点")]
    [SerializeField] private GameObject previewRoot; //人物预览页
    [SerializeField] private GameObject twistBtnsRoot; //按钮页
    [SerializeField] private GashaponLuckValueView gashaponLuckValueView;

    [Header("右上角活动货币")]
    [SerializeField] private GameObject Go_EventCurrencyView;
    [SerializeField] private Text Txt_EventCurrency;


    private List<GashaponPriceItem> items = new List<GashaponPriceItem>();
    private LinkedList<GashaponPriceItem> cacheItems = new LinkedList<GashaponPriceItem>();

    //人物3d预览
    internal CharacterWrap characterWrap;

    // 宠物3D 预览
    internal PetWrap petWrap;

    internal BaseAvatarWrapper avatarWrapper;
    internal CharacterWrap otherCharacterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;
    internal PetAnimationCtrl petAnimationCtrl;

    private List<AccountWidget> widgets;


    public override void OnCreate(string id) {
        base.OnCreate(id);
        InitWrapper();
        InitUI();
    }

    private void InitUI() {
        InitBg();
        InitBtn();
        SwitchStyle(GashaponThemePageStyle.PreviewStyle);
        InitData();
    }

    public override void OnShow() {
        base.OnShow();
        Invoke(nameof(DefClickFirst), 0.2f);
    }

    public override void OnHide() {
        base.OnHide();
        StopAvatarAnim();
    }


    private void InitBg() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        if (viewCfg == null) {
            return;
        }

        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(bgNode);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem(viewCfg.BgColor, viewCfg.AtlasPath, viewCfg.BgSpriteIds);
        item.gameObject.SetActive(true);
    }

    private void InitBtn() {
        twistBtn.onClick.AddListener(OnTwistClick);
        twist10Btn.onClick.AddListener(OnTwist10Click);
        infoBtn.onClick.AddListener(OnInfoClick);
        musicPreviewBtn?.onClick.AddListener(OnMusicalInstrumentsPreview);
        changeOtherOcBtn?.onClick.AddListener(ChangeOtherOc);
    }

    private void InitWrapper() {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData != null && characterWrap == null) {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;
            animationCtrl.gameObject.SetActive(true);

            otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.otherAvatarInfo);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }

        var savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;
        if (savePetData != null && petWrap == null) {
            petWrap = PetAvatarController.Inst.CreateUIAvatar(savePetData);
            petWrap.SetParent(characterRoot, true);
            petAnimationCtrl = petWrap.Avatar.GetComponentInChildren<PetAnimationCtrl>();
            petAnimationCtrl.gameObject.SetActive(false);
            avatarCameraController.RotateTarget = characterRoot;
        }
    }

    private void InitData() {
        if (gashaponData == null) {
            LoggerUtils.LogError("GashaponPanel InitData Error curGashaponData is null id:" + gashaponId);
            return;
        }
        UpdateBtnUI();
        UpdateWidgetView();
        UpdateListview();
        gashaponLuckValueView.Refresh(gashaponId, gashaponData.CurrencyType);
    }


    public void SwitchStyle(GashaponThemePageStyle target) {
        switch (target) {
            case GashaponThemePageStyle.PreviewStyle:
                previewRoot.SetActive(true);
                twistBtnsRoot.SetActive(true);
                gashaponLuckValueView.gameObject.SetActive(true);
                infoBtn.gameObject.SetActive(true);
                break;
            case GashaponThemePageStyle.TwistAnimStyle:
                previewRoot.gameObject.SetActive(false);
                twistBtnsRoot.SetActive(false);
                gashaponLuckValueView.gameObject.SetActive(false);
                infoBtn.gameObject.SetActive(false);
                StopAvatarAnim();
                break;
            case GashaponThemePageStyle.ContinueTwist:
                previewRoot.gameObject.SetActive(false);
                twistBtnsRoot.SetActive(true);
                gashaponLuckValueView.gameObject.SetActive(true);
                infoBtn.gameObject.SetActive(true);
                break;
            case GashaponThemePageStyle.EvnetPreview:
                previewRoot.SetActive(true);
                twistBtnsRoot.SetActive(false);
                infoBtn.gameObject.SetActive(true);
                gashaponLuckValueView.gameObject.SetActive(true);
                gashaponLuckValueView.Refresh(gashaponId, CurrencyType.Coin);
                break;
        }
    }

    private void StopAvatarAnim() {
        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        petAnimationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();
    }

    private void UpdateBtnUI() {
        if (gashaponData == null) return;
        var iconSprite = PgcUtils.LoadCurrencyIcon((int)gashaponData.CurrencyType, this.gameObject);
        if (iconSprite != null) {
            singleIcon.sprite = iconSprite;
            tenIcon.sprite = iconSprite;
        }

        titleText.SetLocalText(gashaponData.Name);
        singleText.SetText(gashaponData.SinglePrice.ToString());
        tenText.SetText(gashaponData.TenDrawPrice.ToString());

        bool isShowDiscount = gashaponData.Discount != 0;
        discountNumLine?.SetActive(isShowDiscount);
        discountTag?.SetActive(isShowDiscount);
        string priceTxt = ((100 - gashaponData.Discount) * 0.01 * gashaponData.TenDrawPrice).ToString();
        tenDicText.SetText(priceTxt);
        dicText.SetText($"-{gashaponData.Discount}%");
    }

    private void UpdateWidgetView() {
        if (gashaponData == null) return;
        widgets ??= gameObject.GetComponentsInChildren<AccountWidget>(true).ToList();

        foreach (var widget in widgets) {
            if ((gashaponData.CurrencyType == CurrencyType.Badge || gashaponData.CurrencyType == CurrencyType.Coin) &&
                widget.type == CurrencyType.EnergyCoin) {
                widget.gameObject.SetActive(true);
                continue;
            }

            widget.gameObject.SetActive((int)widget.type == (int)gashaponData.CurrencyType);
        }
    }

    private void UpdateListview() {
        ClearItems();
        if (gashaponData == null || gashaponData.RewardList == null || gashaponData.RewardList.Count <= 0) return;
        for (int i = 0; i < gashaponData.RewardList.Count; i++) {
            GashaponRewardData priceData = gashaponData.RewardList[i];
            GashaponPriceItem itemScript = GetItem();
            itemScript.Init(priceData, OnItemClick);
            items.Add(itemScript);
        }
    }

    private GashaponPriceItem GetItem() {
        if (cacheItems != null && cacheItems.Count != 0) {
            GashaponPriceItem cache = cacheItems.Last.Value;
            cacheItems.RemoveLast();
            cache.gameObject.SetActive(true);
            cache.transform.SetParent(scrollContent);
            return cache;
        }

        GashaponPriceItem newIns = Instantiate(itemPrefab, scrollContent);
        return newIns;
    }

    private void ClearItems() {
        if (items != null) {
            for (int i = 0; i < items.Count; i++) {
                RecycleItem(items[i]);
            }

            items.Clear();
        }
    }

    private void RecycleItem(GashaponPriceItem item) {
        if (cacheItems == null) {
            cacheItems = new LinkedList<GashaponPriceItem>();
        }

        cacheItems.AddLast(item);
        item.gameObject.SetActive(false);
        item.SetSelectStatus(false);
        item.transform.SetParent(cacheNode);
    }


    private void OnItemClick(GashaponPriceItem item, GashaponRewardData info) {
        bool isSelect = false;

        string pgcId = "";
        if (GashaponUtils.HasPGCData(info)) {
            pgcId = info.PgcDatas[0].Id;
        }

        itemNameText.gameObject.SetActive(false);
        musicPreviewBtn?.gameObject.SetActive(false);
        changeOtherOcBtn?.gameObject.SetActive(false);

        characterWrap.Avatar.SetActive(false);
        petWrap.Avatar.SetActive(false);
        otherCharacterWrap.Avatar.SetActive(false);

        if (!string.IsNullOrEmpty(pgcId)) {
            GameResData resData = Es.DataTables.GetGameResData(pgcId);
            if (resData.ResourceType == (int)ResourceType.Avatar ||
                resData.ResourceType == (int)ResourceType.UgcAvatar) {
                bool isMusic = resData.SubType == (int)AvatarSubType.MusicalInstrument;
                musicPreviewBtn?.gameObject.SetActive(isMusic);
                avatarWrapper = characterWrap;
                avatarWrapper.Avatar.gameObject.SetActive(true);
                SetCharacterAvatarCamera();
            } else if (resData.ResourceType == (int)ResourceType.PGCPetAvatar ||
                       resData.ResourceType == (int)ResourceType.UGCPetAvatar) {
                avatarWrapper = petWrap;
                avatarWrapper.Avatar.gameObject.SetActive(true);
                SetPetAvatarCamera();
            } else if (resData.ResourceType == (int)ResourceType.Emote) {
                var emoteSubType = ((EmoteSubType)resData.SubType);
                if (emoteSubType.IsPet()) {
                    avatarWrapper = petWrap;
                    SetPetAvatarCamera();
                } else {
                    avatarWrapper = characterWrap;
                    SetCharacterAvatarCamera();
                }

                avatarWrapper.Avatar.gameObject.SetActive(true);
                changeOtherOcBtn?.gameObject.SetActive(emoteSubType.IsDouble() && !emoteSubType.IsPet());
            }
        }

        var currencyType = GameUtils.ConvertRewardType((int)info.RewardType);
        if (GameUtils.IsCurrencyType((int)currencyType)) {
            SetRewardRawImageShow(true);
            var iconSprite = PgcUtils.LoadCurrencyIcon(currencyType, currencyRewardImage.gameObject);
            if (iconSprite != null) {
                currencyRewardImage.sprite = iconSprite;
            }

            currencyRewardNum.SetText(info.Num > 1 ? "x" + info.Num : "");
        } else {
            SetRewardRawImageShow(false);
            CancelTryOn();
            TryOn(info.PgcDatas[0], item);
            PreviewEmote(info.PgcDatas[0]);
        }

        if (GashaponUtils.HasPGCData(info)) {
            itemNameText.gameObject.SetActive(true);
            itemNameText.SetLocalText(info.PgcDatas[0].Name);
        } else {
            itemNameText.gameObject.SetActive(false);
        }


        for (int i = 0; i < items.Count; i++) {
            items[i].SetSelectStatus(false);
        }
    }

    private void SetRewardRawImageShow(bool isShow) {
        playerImageView.SetActive(!isShow);
        currencyRewardImage.gameObject.SetActive(isShow);
    }

    private void HideItemsLoading() {
        foreach (var item in items) {
            item.SetLoadingVisible(false);
        }
    }

    private void CancelTryOn() {
        StopAvatarAnim();
        ResetCharacterRotate();
        petWrap?.RefreshAvatar(AccountDataManager.Inst.PetInfo.avatarInfo);
        characterWrap?.RefreshAvatar(AccountDataManager.Inst.UserInfo.avatarInfo);
    }

    internal void TryOn(AssetsData asset, GashaponPriceItem itemNode) {
        HideItemsLoading();
        if (asset == null ||
            (asset.ResourceType != ResourceType.Avatar && asset.ResourceType != ResourceType.PGCPetAvatar)) return;

        itemNode?.SetLoadingVisible(true);
        var pgcAssets = asset as PGCAssetsData;
        var classType = asset.ResourceType == ResourceType.Avatar
            ? UniqueType.GetAvatar(pgcAssets.AvatarSubType)
            : UniqueType.GetPGCPetAvatar(pgcAssets.AvatarSubType);
        var config = asset.ResourceType == ResourceType.Avatar
            ? DataTables.GetAvatarCommonData(asset.Id)
            : DataTables.GetPetAvatarCommonData(asset.Id);
        avatarWrapper.ChangePart(classType, pgcAssets.Id, () => {
            itemNode?.SetLoadingVisible(false);
        });
        avatarWrapper.ChangeColor(classType, config.defaultColor);
        avatarWrapper.Move(classType, config.pDef);
        avatarWrapper.Rotate(classType, config.rDef);
        avatarWrapper.Scale(classType, config.sDef);
        avatarWrapper.HVScale(classType, config.vhSDef);
        avatarWrapper.SetLeftOrRight(classType, config.leftRightType);
        avatarCameraController.SetCameraZoom(classType);
    }

    internal void PreviewEmote(AssetsData asset) {
        if (asset == null || asset.ResourceType != ResourceType.Emote) return;

        var emoteAssets = asset as EmoteAssetsData;
        switch (emoteAssets.EmoteSubType) {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id);
                break;
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                animationCtrl.PlayDoubleEmoteForUICharacter(emoteAssets.Id, otherAnimationCtrl);
                break;
            case EmoteSubType.PetSingle:
            case EmoteSubType.PetSingleLoop:
                petAnimationCtrl.PlaySingleEmoteForUICharacter(emoteAssets.Id);
                break;
            case EmoteSubType.PetWithPlayer:
            case EmoteSubType.PetWithPlayerLoop:
                petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(emoteAssets.Id, animationCtrl);
                break;
        }

        var classType = UniqueType.Get(emoteAssets.ResourceType, (int)emoteAssets.EmoteSubType);
        avatarCameraController.SetCameraZoom(classType);
        avatarCameraController.SetEmoteView(emoteAssets.Id);
    }

    private void ResetCharacterRotate() {
        characterRoot.eulerAngles = new Vector3(0, -180, 0);
        animationCtrl.transform.localPosition = Vector3.zero;
    }


    private void SetPetAvatarCamera() {
        avatarCameraController.ZoomUpperPosY = -0.1f;
        avatarCameraController.ZoomUppereCameraSize = 0.4f;
        avatarCameraController.ZoomWholePosY = -0.2f;
        avatarCameraController.ZoomWholeCameraSize = 0.7f;
        avatarCameraController.ZoomFootPosY = -0.46f;
        avatarCameraController.ZoomFootCameraSize = 0.3f;
        avatarCameraController.customEmoteCameraScale = 0.7f;
    }

    private void SetCharacterAvatarCamera() {
        avatarCameraController.ZoomUpperPosY = 0.3f;
        avatarCameraController.ZoomUppereCameraSize = 0.6f;
        avatarCameraController.ZoomWholePosY = 0;
        avatarCameraController.ZoomWholeCameraSize = 1f;
        avatarCameraController.ZoomFootPosY = -0.375f;
        avatarCameraController.ZoomFootCameraSize = 0.75f;
        avatarCameraController.customEmoteCameraScale = 1.0f;
    }


    private void StopAllEmoteSound() {
        //关闭的时候清除所有音效
        if (animationCtrl != null && animationCtrl.gameObject != null) {
            AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
        }

        if (otherAnimationCtrl != null && otherAnimationCtrl.gameObject != null) {
            AkSoundManager.Inst.StopAll(otherAnimationCtrl.gameObject);
        }

        if (petAnimationCtrl != null && petAnimationCtrl.gameObject != null) {
            AkSoundManager.Inst.StopAll(petAnimationCtrl.gameObject);
        }
    }


    private void DefClickFirst() {
        StopAllEmoteSound();

        if (!gameObject.activeSelf) {
            return;
        }
        if (items.Count > 0) {
            items[0].OnItemClick();
        }
    }


    private void OnInfoClick() {
        var viewCfg = GashaponDataManager.Inst.GetGashaponView(gashaponId);
        var rulePath = viewCfg.RulePath;
        if (string.IsNullOrEmpty(rulePath)) {
            if (gashaponData.CurrencyType != CurrencyType.GreenCoin && gashaponData.CurrencyType != CurrencyType.Coin) {
                rulePath = "Assets/Loadable/UI/UIPanel/GashaponRulePanel/Rules/DefaultLimitRule.json";
            } else {
                rulePath = "Assets/Loadable/UI/UIPanel/GashaponRulePanel/Rules/DefaultRule.json";
            }
        }

        UIManager.Inst.OpenPanel<GashaponRulePanel>(PanelId.GashaponRulePanel, rulePath);
    }

    private void OnMusicalInstrumentsPreview() {
        UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, ((CharacterWrap)avatarWrapper).ChaData.Clone());
    }

    private void ChangeOtherOc() {
        UIManager.Inst.OpenPanelTakeAni<OcChangePanel>(PanelId.OcChangePanel, OcChangeScene.DoubleEmote).OnCloseAction =
            OnChangeOtherOc;
    }

    private void OnChangeOtherOc(BaseAvatarData baseAvatarData) {
        if (baseAvatarData != null) {
            try {
                var data = (CharacterData)baseAvatarData;
                otherCharacterWrap.SetCharacterData(data);
                PlayerPrefs.SetString(GameConsts.EmoteOtherPlayerOcKey + AccountDataManager.Inst.Uid,
                    CharacterData.SerializeObject(data));
            } catch (Exception e) {
                LoggerUtils.LogError("OnChangeOtherOc:" + e.Message + "\n" + e.StackTrace + "\n");
            }
        }
    }

    private void OnTwist10Click() {
        SendGashaponRequestTenTimes(gashaponData);
    }

    public override void OnGashaOnceRsp(GashaponRsp gashaponRsp) {
        base.OnGashaOnceRsp(gashaponRsp);
        OnGashaRsp(gashaponRsp, true);
    }

    private void OnGashaTwistAnimComplete(GashaponRsp gashaponRsp) {
        UIManager.Inst.ClosePanel(PanelId.GashaponTwistAnimPanel);
        gashaponLuckValueView?.AdjustUI(gashaponRsp.lotteryInfo, gashaponData.CurrencyType);
        if (gashaponRsp == null || gashaponRsp.rewardList == null) {
            return;
        }

        var panel = UIManager.Inst.OpenPanel<GashaponRewardPanel>(PanelId.GashaponRewardPanel);
        panel.ShowRewards(gashaponId, gashaponRsp);

        if (UIManager.Inst.TryFindPanel<CreatorRewardPanel>(WindowId.CreatorWindow, PanelId.CreatorCenterPanel,
                out var creatorRewardPanel) && gashaponData.CurrencyType == CurrencyType.GreenCoin) {
            creatorRewardPanel.Refresh();
        }
    }

    private void OnTwistClick() {
        SendGashaponRequestOnce(gashaponData);
    }

    public override void OnGashaTenRsp(GashaponRsp gashaponRsp) {
        base.OnGashaTenRsp(gashaponRsp);
        OnGashaRsp(gashaponRsp, false);
    }

    private void OnGashaRsp(GashaponRsp gashaponRsp, bool isOnce = false) {
        StopAvatarAnim();
        var panel = UIManager.Inst.OpenPanel<GashaponTwistAnimPanel>(PanelId.GashaponTwistAnimPanel,
            new GashaponTwistAnimParam() {
                gashaponId = gashaponData.Id
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

    public override void OnDataChange(AssetsData[] changes) {
        base.OnDataChange(changes);
        UpdateListview();
        DefClickFirst();
    }
}

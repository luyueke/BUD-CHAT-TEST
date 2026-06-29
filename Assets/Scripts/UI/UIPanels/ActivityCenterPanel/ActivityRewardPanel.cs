using System;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Event;
using Game.Store;
using GameData.PgcData;
using Network;
using Network.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UI.Base;
using UI.BaseWidgets;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public  class ActivityRewardPanel : BasePanel<ActivityRewardPanel>
{
    [SerializeField] private RawImage Tex_Bg;
    [SerializeField] private Transform BG;
    [SerializeField] private Button BackBtn;
    [SerializeField] private Transform Content;
    [SerializeField] private MusicCurrencyView currencyView;
    [SerializeField] private Text RewardName;

    [Header("人物形象")] [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    [SerializeField] private ActivityRewardItemView _item;
    [SerializeField] private Text EndTimeText;
    [SerializeField] private Text Txt_SubTitle;

    [SerializeField] private CButton wearBtn;
    [SerializeField] private LoadingButton payBtn;
    [SerializeField] private Image payIcon;
    [SerializeField] private CButton tryOnBtn;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Text titleTxt;


    [SerializeField] internal GameObject AvatarRootObj;
    [SerializeField] internal Image CurrencyObj;
    [SerializeField] internal Transform TitleRoot;
    [SerializeField] internal Text rewardTipText;

    [SerializeField] internal GameObject operationRoot;

    private List<ActivityRewardItemView> itemViews = new List<ActivityRewardItemView>();

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;

    internal CharacterData saveCharacterData;

    private AmbientLightSetting _srcLightSetting;
    private bool _srcHallLightVisible;
    private bool _srcGameSceneLightVisible;
    private bool _srcPreviewSceneLightVisible;

    private string pgcId;

    private static int allConsume;

    private Action<int> balanceChange;

    private ActivityId activityId = ActivityId.NotesJump;
    private bool isOnlyPreview = false;

    private string altasPath;
    private Vector2 _defaultCurrencyObjSize;

    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(OnBack);

        currencyView.balanceChangeAction = i => { balanceChange?.Invoke(i); };

        tryOnBtn?.onClick.AddListener(OnClickTryPlay);
        payBtn?.onClick.AddListener(OnClickPayItem);
        wearBtn?.onClick.AddListener(OnClickWear);
        _defaultCurrencyObjSize = CurrencyObj.rectTransform.sizeDelta;
    }

    private void OnBack()
    {
        CloseSelf();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    public override void OnShow(params object[] args)
    {
        if (args.Length > 0)
        {
            if (args[0] is ActivityId)
            {
                activityId = (ActivityId)args[0];
            }
            else if (args[0] is string && Enum.TryParse((string)args[0], out ActivityId tmpId))
            {
                activityId = tmpId;
            }
            else
            {
                activityId = ActivityId.NotesJump;
            }
        }
        else
        {
            activityId = ActivityId.NotesJump;
        }
   
        InitUI();
        _srcPreviewSceneLightVisible = AmbientLightManager.Inst.ShowPreviewDirLight();
        _srcLightSetting = AmbientLightManager.Inst.OpenUILight();
        _srcHallLightVisible = AmbientLightManager.Inst.HideHallLight();
        _srcGameSceneLightVisible = AmbientLightManager.Inst.HideGameSceneLight();

        InitRewardView();
        AccountDataManager.Inst.BalanceInfo.Refresh();
    }

    private void InitUI()
    {
        if (BG == null)
        {
            return;
        }
        string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        switch (activityId)
        {
            case ActivityId.NotesJump:
                item.InitCustomBgItem("#FFB7EC", atlasPath, new List<string>()
                {
                    "MusicalInstrument_1", "MusicalInstrument_2", "MusicalInstrument_3"
                });
                break;
            case ActivityId.PetStudio:
                item.InitCustomBgItem("#DDE2E8", atlasPath, new List<string>()
                {
                    "pet_studio_bg1", "pet_studio_bg2", "pet_studio_bg3", "pet_studio_bg4"
                });
                break;
            case ActivityId.AnimationStudio:
                item.InitCustomBgItem("#FFDBA5", atlasPath, new List<string>()
                {
                    "AnimationStudio_1", "AnimationStudio_2", "AnimationStudio_3"
                });
                break;
            case ActivityId.WinterCarnival:
                item.InitCustomBgItem("#CFE8FF", atlasPath, new List<string>()
                {
                    "WinterCarnival_1", "WinterCarnival_2", "WinterCarnival_3", "WinterCarnival_4"
                });
                break;
        }

        item.gameObject.SetActive(true);
        currencyView.SetData(activityId);
        var spriteatlasPath = "Assets/Loadable/UI/UIPanel/CommonSprite/CommonSprite.spriteatlas";
        string iconName = "icn_common_piano_big";
        string title = "套装上线活动奖品兑换";
        if (this.activityId == ActivityId.AnimationStudio)
        {
            iconName = "icn_reward_movie_big";
            title = "创意动作节全新上线";
            rewardTipText.gameObject.SetActive(false);
        }
        else if (this.activityId == ActivityId.WinterCarnival)
        {
            iconName = "icn_reward_movie_big";
            title = "冬日趣玩派对";
            rewardTipText.gameObject.SetActive(false);
            payBtn.gameObject.SetActive(false);
            wearBtn.gameObject.SetActive(false);
            tryOnBtn.gameObject.SetActive(false);
            currencyView.gameObject.SetActive(false);
        }

        payIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(spriteatlasPath, iconName, gameObject);
        titleTxt.text = title;
    }

    private void InitRewardView()
    {
        saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData == null)
            saveCharacterData = AvatarDataManager.Inst.GetDefaultDataByGender(1);
        if (saveCharacterData != null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;

            var otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }
    }

    private ActivityInfo activityInfo;


    public void SetPreviewBg(Sprite sprite)
    {
        if (sprite == null) return;
        var bgItem = GetComponentInChildren<ActivityCenterBgItem>(true);
        if (bgItem != null)
            bgItem.InitCustomTextureBg(sprite.texture);
    }

    /// <summary>
    /// 仅仅显示奖励预览, 不现实其他货币及不包含购买逻辑
    /// </summary>
    /// <param name="info"></param>
    public void SetPreviewData(ActivityInfo info)
    {
        activityInfo = info;
        isOnlyPreview = true;
        if (info.rewardPanelCfg != null)
        {
            string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
            var bgItem = GetComponentInChildren<ActivityCenterBgItem>(true);
            if (!string.IsNullOrEmpty(info.rewardPanelCfg.rewardBg))
            {
                bgItem.InitCustomTextureBg(info.rewardPanelCfg.rewardBg);
            }
            else if (!string.IsNullOrEmpty(info.rewardPanelCfg.rewardBgColor))
            {
                bgItem.InitCustomBgItem(info.rewardPanelCfg.rewardBgColor, atlasPath, info.rewardPanelCfg.rewardIcons);
                bgItem.GetComponent<ColorBgPanel>().RefreshSprite();
            }
        }


        currencyView.gameObject.SetActive(false);
        string itemBgColorStr = "#E577CD";
        if (activityId == ActivityId.PetStudio)
        {
            itemBgColorStr = "#2f4cf1";
        }
        else if (activityId == ActivityId.AnimationStudio)
        {
            itemBgColorStr = "#E39659";
        }

        if (info.rewardPanelCfg != null && !string.IsNullOrEmpty(info.rewardPanelCfg.rewardItemBgColor))
        {
            itemBgColorStr = info.rewardPanelCfg.rewardItemBgColor;
        }

        Debug.LogError(JsonConvert.SerializeObject(activityInfo));
        if (info.rewardPanelCfg != null && !string.IsNullOrEmpty(info.rewardPanelCfg.endTimeTip))
        {
            EndTimeText.SetLocalText(info.rewardPanelCfg.endTimeTip);
        }
        else
        {
            EndTimeText.SetLocalText("距活动结束还有: {0}", activityInfo.leftTime);
        }

        rewardTipText.gameObject.SetActive(false);

        Color itemBgColor = DataUtil.DeSerializeColorCheckHash(itemBgColorStr);

        var rewardList = activityInfo.preRewardList == null ? activityInfo.rewardList : activityInfo.preRewardList;
        if (rewardList != null)
        {
            foreach (var element in rewardList)
            {
                var item = GameObject.Instantiate(_item, Content);
                item.SetPreviewData(activityId, itemBgColor, element, OnClickItem,this.altasPath);
                itemViews.Add(item);
            }

            scrollRect.verticalNormalizedPosition = 1f;

            if (rewardList.Count > 0)
            {
                OnClickItem(rewardList[0]);
            }
        }
        operationRoot.SetActive(false);
        titleTxt.SetLocalText(info.activityTital);
    }

    public void SetEventPreview(ActivityInfo info, string subTitle, Texture bg,string atlasPath = "")
    {
        this.altasPath = atlasPath;
        SetPreviewData(info);
        EndTimeText.gameObject.SetActive(false);
        Txt_SubTitle.gameObject.SetActive(true);
        Tex_Bg.gameObject.SetActive(true);
        Txt_SubTitle.text = subTitle;
        Tex_Bg.texture = bg;
        var sizeToFit = Tex_Bg.GetComponentInParent<UIBGSizeToFit>();
        sizeToFit.Resize();
    }

    public void SetData(ActivityInfo activityInfo, int balanceValaue, Action<int> balanceChange)
    {
        isOnlyPreview = false;
        this.activityInfo = activityInfo;
        this.balanceChange = balanceChange;
        var leftTime = activityInfo.leftTime;
        if (!string.IsNullOrEmpty(activityInfo.leftTime))
        {
            EndTimeText.SetLocalText("距活动结束还有: {0}", activityInfo.leftTime);
        }

        currencyView.UpdateCurrency(balanceValaue);

        var rewardList = activityInfo.preRewardList == null ? activityInfo.rewardList : activityInfo.preRewardList;
        if (rewardList != null)
        {
            foreach (var element in rewardList)
            {
                var item = GameObject.Instantiate(_item, Content);
                // item.gameObject.SetActive(true);
                item.SetData(activityId, element, OnClickItem);
                itemViews.Add(item);
            }

            scrollRect.verticalNormalizedPosition = 1f;

            if (rewardList.Count > 0)
            {
                OnClickItem(rewardList[0]);
            }
        }
    }


    private ActivityRewardInfo activeData;

    private void OnClickItem(ActivityRewardInfo data)
    {
        activeData = data;
        CurrencyObj.rectTransform.sizeDelta = _defaultCurrencyObjSize;
        foreach (var element in itemViews)
        {
            element.SetSelect(element.RewardId == data.rewardId);
        }

        var isOwned = false;
        string pgcId = null;
        BUDRewardType budRewardType = (BUDRewardType)data.budRewardType;
        bool showAvatar = budRewardType == BUDRewardType.RewardPgcResource ||
                                budRewardType == BUDRewardType.ErrRewardType || budRewardType == BUDRewardType.RewardPgcBundle || budRewardType == BUDRewardType.RewardTypeCameraPose;
        bool showTitle = budRewardType == BUDRewardType.RewardTypeTitle;
        AvatarRootObj.SetActive(showAvatar);
        CurrencyObj.gameObject.SetActive(!showAvatar && !showTitle);
        if (TitleRoot != null) TitleRoot.gameObject.SetActive(showTitle);

        if (budRewardType == BUDRewardType.ErrRewardType)
        {
            pgcId = data.rewardType;
        }
        else if (budRewardType == BUDRewardType.RewardPgcResource)
        {
            pgcId = data.pgcId;
        }
        else if (budRewardType == BUDRewardType.RewardAvatarFrame)
        {
            pgcId = data.pgcId;
        }
        else if (budRewardType == BUDRewardType.RewardPgcBundle)
        {
            pgcId = data.pgcId;
        }
        else if (budRewardType == BUDRewardType.RewardTypeCameraPose)
        {
            pgcId = data.pgcId;
        }
        else if (budRewardType == BUDRewardType.RewardTypeNicknameFrame)
        {
            pgcId = data.pgcId;
        }
        else if (budRewardType == BUDRewardType.RewardTypeTitle)
        {
            pgcId = data.pgcId;
        }

        var rewardName = data.rewardName;
        if (!string.IsNullOrEmpty(pgcId))
        {
            if (budRewardType == BUDRewardType.RewardAvatarFrame)
            {
                UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(data.pgcId, gameObject,
                    sp => { CurrencyObj.sprite = sp; });
            }
            else if(budRewardType == BUDRewardType.RewardPgcBundle)
            {
                //Debug.LogError("OnClick pgcids=" + pgcId);
                string[] pgcIds = pgcId.Split(",");
                TryOn(pgcIds);
                isOwned = pgcIds.Length >0;
                foreach(var pId in pgcIds)
                {
                    isOwned = isOwned && AssetsDataManager.IsOwned(pId) ;
                    GameResData resData = Es.DataTables.GetGameResData(pId);
                    bool isMusic = resData.SubType == (int)AvatarSubType.MusicalInstrument;
                    tryOnBtn.gameObject.SetActive(isMusic);
                }
            }
            else if (budRewardType == BUDRewardType.RewardTypeCameraPose)
            {
                PoseInit(pgcId);
            }
            else if (budRewardType == BUDRewardType.RewardTypeNicknameFrame)
            {
                UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(pgcId, gameObject,
                    sp => { 
                        CurrencyObj.sprite = sp; 
                        CurrencyObj.SetNativeSize();
                         });
                CancelTryOn();
            }
            else if (budRewardType == BUDRewardType.RewardTypeTitle)
            {
                if (TitleRoot != null)
                {
                    for (int i = TitleRoot.childCount - 1; i >= 0; i--)
                        Destroy(TitleRoot.GetChild(i).gameObject);
                    var titleData = UserUIWidgetManager.Inst.GetTitleDataByPgcId(pgcId);
                    if (titleData != null && !string.IsNullOrEmpty(titleData.Prefab))
                    {
                        var prefab = Loader.Load<GameObject>(titleData.Prefab, gameObject);
                        if (prefab != null)
                            Instantiate(prefab, TitleRoot);
                    }
                }
                CancelTryOn();
            }
            else
            {
                isOwned = AssetsDataManager.IsOwned(pgcId);
                TryOn(pgcId);
                GameResData resData = Es.DataTables.GetGameResData(pgcId);
                bool isMusic = resData.SubType == (int)AvatarSubType.MusicalInstrument;
                tryOnBtn.gameObject.SetActive(isMusic);
            }
        }
        else
        {
            if (budRewardType == BUDRewardType.RewardTypeSelfDefine && !string.IsNullOrEmpty(altasPath))
            {
                CurrencyObj.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(altasPath, data.previewIcon, gameObject);
            }
            else
            {
                CurrencyObj.sprite = PgcUtils.LoadRewardPreviewIcon(budRewardType, gameObject);
            }
            
            isOwned = data.rewardStatus == 1;
            CancelTryOn();
            if (data.rewardNum == 1 && (budRewardType == BUDRewardType.RewardVipFreeTrail || budRewardType == BUDRewardType.RewardYouYouCoinNewYearPack)) {
            } else {
                rewardName = $"{data.rewardNum} {rewardName}";
            }

        }

        RewardName.text = rewardName;
        payBtn.gameObject.SetActive(!isOwned);
        var PriceText = GameObjectEx.FindChildByName(payBtn.gameObject, "Text").GetComponent<Text>();
        PriceText.text = data.spendNum.ToString();

        if (this.activityId == ActivityId.WinterCarnival)
        {
            rewardTipText.gameObject.SetActive(false);
            payBtn.gameObject.SetActive(false);
            wearBtn.gameObject.SetActive(false);
            tryOnBtn.gameObject.SetActive(false);
        }
    }

    private void OnClickWear()
    {
    }

    private void OnClickPayItem()
    {
        if (activeData == null)
        {
            return;
        }

        if (currencyView.balance < activeData.spendNum)
        {
            currencyView.OnExchange();
            return;
        }

        if (isSending)
        {
            return;
        }

        isSending = true;
        payBtn.ShowLoading();
        ConvertReward(activityInfo.activityId, activeData.rewardId, (b, response) =>
        {
            if (this == null)
            {
                return;
            }

            payBtn.HideLoading();
            payBtn.gameObject.SetActive(false);
            isSending = false;

            if (b)
            {
                OnConvertSuccess(response);
            }
        });
    }

    private void OnClickTryPlay()
    {
        UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, characterWrap.ChaData.Clone());
    }

    private bool isSending = false;

    private void ConvertReward(string activityId, int rewardId,
        Action<bool, ActivityRewardConvertResponse> resultAction)
    {
        if (string.IsNullOrEmpty(activityId))
        {
            resultAction?.Invoke(false, null);
            return;
        }

        JObject jObject = new JObject()
        {
            ["activityId"] = activityId,
            ["rewardId"] = rewardId
        };
        NetworkManager.Inst.SendHttpRequest(HttpUrlDefine.ActivityRedeemReward, HttpMethod.POST,
            JsonConvert.SerializeObject(jObject), (content) =>
            {
                ActivityRewardConvertResponse response =
                    JsonConvert.DeserializeObject<ActivityRewardConvertResponse>(content);
                isSending = false;

                resultAction?.Invoke(true, response);
            },
            (error) => { resultAction?.Invoke(false, null); });
    }

    private void OnConvertSuccess(ActivityRewardConvertResponse response)
    {
        if (response == null)
        {
            return;
        }

        currencyView.UpdateCurrency(response.currencyAmount);
        balanceChange?.Invoke(response.currencyAmount);
        var itemView = itemViews.Find(x => x.RewardId == response.rewardId);
        if (itemView != null)
        {
            itemView.SetOwnedUI();
        }

        var rewardInfo = activityInfo.rewardList.Find(x => x.rewardId == response.rewardId);
        if (rewardInfo == null)
        {
            return;
        }

        var rewardList = new List<CommonRewardItemData>();
        CommonRewardItemData commonRewardItemData = new CommonRewardItemData();
        if (rewardInfo.budRewardType == (int)BUDRewardType.ErrRewardType)
        {
            commonRewardItemData.IconSp = PgcUtils.GetIconSpriteByPgcId(rewardInfo.rewardType, gameObject);
        }
        else if (rewardInfo.budRewardType == (int)BUDRewardType.RewardPgcResource)
        {
            commonRewardItemData.IconSp = PgcUtils.GetIconSpriteByPgcId(rewardInfo.pgcId, gameObject);
        }
        else
        {
            commonRewardItemData.IconSp = PgcUtils.LoadRewardIcon((BUDRewardType)rewardInfo.budRewardType, gameObject);
        }

        commonRewardItemData.rewardName = rewardInfo.rewardName;
        commonRewardItemData.RewardAmount = rewardInfo.rewardNum == 0 ? 1 : rewardInfo.rewardNum;
        rewardList.Add(commonRewardItemData);
        var panel = UIManager.Inst.OpenPanel<CommonRewardPanel>(PanelId.CommonRewardPanel);
        panel.ShowRewards(rewardList);
    }

    internal void CancelTryOn()
    {
        characterWrap.RefreshAvatar(saveCharacterData);
    }

    /// <summary>
    /// 试穿 重置参数
    /// </summary>
    /// <param name="goodsData"></param>
    internal void TryOn(string pgcId)
    {
        CancelTryOn();

        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();

        GameResData config = Es.DataTables.GetGameResData(pgcId);
        if (config == null)
        {
            return;
        }

        switch ((ResourceType)config.ResourceType)
        {
            case ResourceType.Avatar:
                OnWearAvatar(pgcId);
                break;
            case ResourceType.Emote:
                PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                break;
            case ResourceType.CameraSelfiePose:
                ApplySelfiePreview(pgcId);
                break;
        }
    }

    internal void TryOn(string[] pgcIds)
    {
        CancelTryOn();

        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();

        foreach(string pgcId in pgcIds)
        {
            GameResData config = Es.DataTables.GetGameResData(pgcId);
            if (config == null)
            {
                continue;
            }

            switch ((ResourceType)config.ResourceType)
            {
                case ResourceType.Avatar:
                    OnWearAvatar(pgcId);
                    break;
                case ResourceType.Emote:
                    PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                    break;
                case ResourceType.CameraSelfiePose:
                    ApplySelfiePreview(pgcId);
                    break;
            }
        }

    }

    internal void OnWearAvatar(string pgcId)
    {
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


    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType)
    {
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();
        avatarCameraController.SetEmoteView(pgcId);
        switch (emoteSubType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(pgcId, null);
                break;
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                animationCtrl.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl, null);
                break;
        }
    }

    #region 姿勢
    private const string LeftEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/effect_l";
    private const string RightEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/effect_r";
    private const string SelfieStickPrefabPath = "Assets/Loadable/AnimationsExpress/Feat/selfiestick_effect/selfiestick_effect.prefab";

    private GameObject selfieNode;

    private bool isInit;
    private string selfiePoseId;

    public void PoseInit(string selfiePoseId)
    {
        this.selfiePoseId = selfiePoseId;
        if (!isInit)
        {
            isInit = true;
        }

        RefreshSingleSelfieItem();
        ApplySelfiePreview(selfiePoseId);
    }

    private void RefreshSingleSelfieItem()
    {
        if (string.IsNullOrEmpty(selfiePoseId))
        {
            return;
        }

        var cfg = DataTables.GetCameraSelfiePose(selfiePoseId);
        if (cfg == null)
        {
            return;
        }

    }


    private void ApplySelfiePreview(string poseId)
    {
        if (string.IsNullOrEmpty(poseId))
        {
            return;
        }

        if (characterWrap == null || animationCtrl == null)
        {
            return;
        }

        var cfg = DataTables.GetCameraSelfiePose(poseId);
        if (cfg == null)
        {
            return;
        }

        selfiePoseId = poseId;
        characterWrap.Avatar.SetActive(true);
        ApplySelfieAnim(cfg);
        CreateOrRefreshSelfieStick(cfg);
    }

    private void ApplySelfieAnim(CameraSelfiePose cfg)
    {
        animationCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
        animationCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
        animationCtrl.OverrideAnimationClip("selfiestick_idle", null);

        if (!string.IsNullOrEmpty(cfg.resourcePath))
        {
            var clipWrapper = Loader.Load<AnimationClip>(cfg.resourcePath + ".anim");
            var clipRes = clipWrapper != null ? clipWrapper.RetainAsset(gameObject) : null;
            if (clipRes != null)
            {
                var clip = AnimationClip.Instantiate(clipRes, gameObject.transform);
                animationCtrl.OverrideAnimationClip("selfiestick_idle", clip);
            }
        }


        animationCtrl.SetPlayerState(PlayerState.CameraMode);
        animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
    }

    private void CreateOrRefreshSelfieStick(CameraSelfiePose cfg)
    {
        var parent = characterWrap.Avatar.transform.Find(cfg.stickHand == 1 ? RightEffectPath : LeftEffectPath);
        if (parent == null)
        {
            return;
        }

        if (selfieNode == null)
        {
            var selfiePrefab = Loader.Load<GameObject>(SelfieStickPrefabPath)?.RetainAsset(gameObject);
            if (selfiePrefab == null)
            {
                return;
            }
            selfieNode = GameObject.Instantiate(selfiePrefab, parent);
        }
        else if (selfieNode.transform.parent != parent)
        {
            selfieNode.transform.SetParent(parent, false);
        }

        var selfieTransform = selfieNode.transform;
        selfieTransform.localPosition = new Vector3(0f, 0f, 0.02f);
        selfieTransform.localScale = Vector3.one;
        selfieTransform.localEulerAngles = cfg.stickRot;
        selfieNode.SetActive(true);

        var selfieAnimator = selfieNode.GetComponent<Animator>();
        if (selfieAnimator != null)
        {
            selfieAnimator.SetInteger("BoardState", 2);
        }
    }

    protected override void OnDisable()
    {
        if (animationCtrl != null)
        {
            animationCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
            animationCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
            animationCtrl.OverrideAnimationClip("selfiestick_idle", null);
            animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
        }
        base.OnDisable();
    }
    #endregion
}

using System;
using System.Collections.Generic;
using Es;
using Game.Audio;
using Game.Avatar;
using GameData;
using GameData.PgcData;
using Message;
using UI.Base;
using UI.Manager;
using UnityEngine;
using UnityEngine.UI;

public class RewardPreviewPanel : BasePanel<RewardPreviewPanel>
{
    [SerializeField] private RawImage Tex_Bg;
    [SerializeField] private Transform BG;
    [SerializeField] private Button BackBtn;
    [SerializeField] private Image RewardIcon;
    [SerializeField] private Text RewardName;
    [SerializeField] private Text RewardNameSkin;
    [SerializeField] private RewardBundleList RewardBundleList;
    [SerializeField] private SpecialAnimContainer SpecialAnimContainer;
    [SerializeField] private Image BundleBg;
    [SerializeField] private Image BG_Img;
    [SerializeField] private SwitchAnimView switchAnimView;
    [SerializeField] private Transform SpecialImage;
    [SerializeField] private Camera avatarCamera;

    [Header("人物形象")] [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;

    internal CharacterWrap characterWrap;
    internal PlayerAnimationCtrl animationCtrl;
    internal PlayerAnimationCtrl otherAnimationCtrl;

    internal CharacterData saveCharacterData;
    bool isInit = false;
    public override void OnCreate()
    {
        BackBtn.onClick.AddListener(OnBack);
    }

    private void OnBack()
    {
        CloseSelf();
        AccountDataManager.Inst.BalanceInfo.Refresh();
        MessageHelper.Broadcast(MessageName.ResumeActivityEmote);
    }

    public override void OnShow(params object[] args)
    {
        InitRewardView();
        AccountDataManager.Inst.BalanceInfo.Refresh();
        MessageHelper.Broadcast(MessageName.SpecialProps, false);
        isInit = true;
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

        SpecialAnimContainer.SetCallBack(OnSpecialPGCClick);
        switchAnimView?.SetCallBack(OnSwitchAnimItemClick);
    }
    
    private void OnSwitchAnimItemClick(SpecialAnim anim)
    {
        var emoteId = switchAnimView.GetPgcId();
        if (!string.IsNullOrEmpty(emoteId))
        {
            animationCtrl.PlayLinkEmoteForUICharacter(emoteId, anim,otherAnimationCtrl);
        }
    }

    private ActivityInfo activityInfo;

    /// <summary>
    /// 仅仅显示奖励预览, 不现实其他货币及不包含购买逻辑
    /// </summary>
    /// <param name="info"></param>
    public void SetPreviewData(ActivityInfo info)
    {
        activityInfo = info;

        if (info.rewardPanelCfg != null)
        {
            string atlasPath = "Assets/Loadable/UI/UIPanel/ActivityCenterPanel/ActivityCenterPanel.spriteatlas";
            var bgItem = GetComponentInChildren<ActivityCenterBgItem>(true);
            if (!string.IsNullOrEmpty(info.rewardPanelCfg.rewardBg))
            {
                bgItem.InitCustomBg(atlasPath, info.rewardPanelCfg.rewardBg);
            }
            else if (!string.IsNullOrEmpty(info.rewardPanelCfg.rewardBgColor))
            {
                bgItem.InitCustomBgItem(info.rewardPanelCfg.rewardBgColor, atlasPath, info.rewardPanelCfg.rewardIcons);
                bgItem.GetComponent<ColorBgPanel>().RefreshSprite();
            }
        }

        ShowReward();
    }

    private void ShowReward()
    {
        if (activityInfo.rewardList.Count == 1)
        {
            var reward = activityInfo.rewardList[0];
            RewardIcon.sprite = PgcUtils.GetIconSpriteByPgcId(reward.pgcId, gameObject);
            RewardName.text = reward.rewardName;
            RewardBundleList.gameObject.SetActive(false);
            TryOn(reward.pgcId);
        }
        else
        {
            RewardBundleList.gameObject.SetActive(true);
            RewardBundleList.SetTarget(activityInfo, "");

            foreach(var reward in activityInfo.rewardList)
            {
                TryOn(reward.pgcId);
            }
        }
    }

    public void SetEventPreview(ActivityInfo info, string subTitle, Texture bg)
    {
        SetPreviewData(info);
        Tex_Bg.gameObject.SetActive(true);
        Tex_Bg.texture = bg;
    }
    public void SetNewyearEventPreview(List<string> pgcIds, string bundleName, string bundleImg,string atlasPath,string subTitle,string bundleBgColor)
    {
        ShowReward(pgcIds, bundleName, bundleImg, atlasPath);
        InitBgView();
        if (!string.IsNullOrEmpty(bundleBgColor))
        {
            BundleBg.color = DataUtil.DeSerializeColorByHex(bundleBgColor);
        }
    }
    
    private void InitBgView()
    {
        if (BG == null)
        {
            return;
        }

        string atlasPath = RechargePanel.RechargePanelAtlas;
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem("#9859FF", atlasPath, new List<string>()
        {
            "s4_limit_bg_1", "s4_limit_bg_2", "s4_limit_bg_3"
        });
        item.gameObject.SetActive(true);
    }

    public void SetEventPreview(List<string> pgcIds, string bundleName, string bundleImg,string atlasPath,string subTitle,string bundleBgColor, Texture bg, string cameraColor = "")
    {
        if (!isInit)
        {
            InitRewardView();
            AccountDataManager.Inst.BalanceInfo.Refresh();
            isInit = true;
        }
        
        // 重置状态
        CancelTryOn();
        if (animationCtrl != null)
        {
            avatarCameraController.ResetEmoteView();
            animationCtrl.ResetEmoteForUICharacter();
            otherAnimationCtrl.gameObject.SetActive(false);
            otherAnimationCtrl.ResetEmoteForUICharacter();
        }
        
        ShowReward(pgcIds, bundleName, bundleImg, atlasPath);
        Tex_Bg.gameObject.SetActive(true);
        Tex_Bg.texture = bg;
        if (!string.IsNullOrEmpty(bundleBgColor))
        {
            BundleBg.color = DataUtil.DeSerializeColorByHex(bundleBgColor);
        }
        if (!string.IsNullOrEmpty(cameraColor))
        {
            avatarCamera.clearFlags = CameraClearFlags.SolidColor;
            avatarCamera.backgroundColor = DataUtil.DeSerializeColorByHex(cameraColor);
        }
    }

    public void SetEventPreview(List<string> pgcIds, string bundleName, string bundleSkinName, string bundleImg, string atlasPath, string subTitle, string bundleBgColor, Texture bg,bool showImg)
    {
        RewardNameSkin.text = bundleSkinName;
        ShowReward(pgcIds, bundleName, bundleImg, atlasPath);
        Tex_Bg.gameObject.SetActive(true);
        Tex_Bg.texture = bg;
        if (!string.IsNullOrEmpty(bundleBgColor))
        {
            BundleBg.color = DataUtil.DeSerializeColorByHex(bundleBgColor);
        }
        SpecialImage.gameObject.SetActive(showImg);
        RewardName.gameObject.SetActive(false);
    }
    public void SetEventPreview(List<string> pgcIds, string bundleName, string bundleSkinName, Sprite sprite, string bundleBgColor, Texture bg)
    {
        RewardNameSkin.text = bundleSkinName;
        ShowReward(pgcIds, bundleName, sprite);
        Tex_Bg.gameObject.SetActive(true);
        Tex_Bg.texture = bg;
        if (!string.IsNullOrEmpty(bundleBgColor))
        {
            BundleBg.color = DataUtil.DeSerializeColorByHex(bundleBgColor);
        }
        SpecialImage.gameObject.SetActive(true);
        RewardName.gameObject.SetActive(false);
    }
    public void SetEventPreview(List<string> pgcIds, string Name, Sprite sprite , string bundleBgColor, Texture bg)
    {
        RewardNameSkin.text = Name;
        ShowReward(pgcIds, Name, sprite);
        Tex_Bg.gameObject.SetActive(true);
        Tex_Bg.texture = bg;
        if (!string.IsNullOrEmpty(bundleBgColor))
        {
            BundleBg.color = DataUtil.DeSerializeColorByHex(bundleBgColor);
        }
        RewardName.gameObject.SetActive(false);
    }
    public void SetEventPreview(List<string> pgcIds, string bundleName, string bundleImg,string atlasPath,string subTitle,string bundleBgColor, string bgName)
    {
        ShowReward(pgcIds, bundleName, bundleImg, atlasPath);
        Sprite bgSp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, bgName, Tex_Bg.gameObject);
        Tex_Bg.gameObject.SetActive(false);
        BG_Img.sprite = bgSp;
        if (!string.IsNullOrEmpty(bundleBgColor))
        {
            BundleBg.color = DataUtil.DeSerializeColorByHex(bundleBgColor);
        }
    }
    public void SetEventPreviewBg(List<string> pgcIds ,string bundleName ,string atlasPath , string color , List<string>Icons)
    {
        ShowReward(pgcIds, bundleName, "", atlasPath);
        if (!string.IsNullOrEmpty(color))
        {
            BundleBg.color = DataUtil.DeSerializeColorByHex(color);
        }
        if (BG == null)
        {
            return;
        }
        var itemObj = Loader
            .Load<GameObject>("Assets/Loadable/UI/UIPanel/CommonBgPanel/ActivityCenterBg.prefab")
            .Instantiate(BG);
        var item = itemObj.GetComponent<ActivityCenterBgItem>();
        item.InitCustomBgItem(color, atlasPath, Icons);
        item.gameObject.SetActive(true);
    }
    private void ShowReward(List<string> pgcIds, string bundleName, string bundleImg, string atlasPath)
    {
        if (pgcIds.Count == 0)
        {
            RewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, bundleImg, gameObject);
            RewardName.text = bundleName;
            RewardBundleList.gameObject.SetActive(false);
        }
        else if (pgcIds.Count == 1)
        {
            var pgcId = pgcIds[0];
            RewardIcon.sprite = GetRewardIconSprite(pgcId);
            RewardName.text = bundleName;
            RewardBundleList.gameObject.SetActive(false);
            TryOn(pgcId);
        }
        else
        {
            RewardIcon.sprite = XAssetLoaderMgr.Inst.LoadSpriteInAltas(atlasPath, bundleImg, gameObject);
            RewardName.text = bundleName;
            RewardBundleList.gameObject.SetActive(true);
            RewardBundleList.SetTarget(pgcIds, bundleName);

            foreach(var pgcId in pgcIds)
            {
                TryOn(pgcId);
            }
        }
    }
    private void ShowReward(List<string> pgcIds, string bundleName, Sprite sprite)
    {
        if (pgcIds.Count == 0)
        {
            RewardIcon.sprite = sprite;
            RewardName.text = bundleName;
            RewardBundleList.gameObject.SetActive(false);
        }
        else if (pgcIds.Count == 1)
        {
            var pgcId = pgcIds[0];
            RewardIcon.sprite = GetRewardIconSprite(pgcId);
            RewardName.text = bundleName;
            RewardBundleList.gameObject.SetActive(false);
            TryOn(pgcId);
        }
        else
        {
            RewardIcon.sprite = sprite;
            RewardName.text = bundleName;
            RewardBundleList.gameObject.SetActive(true);
            RewardBundleList.SetTarget(pgcIds, bundleName);

            foreach (var pgcId in pgcIds)
            {
                TryOn(pgcId);
            }
        }
    }
    private void OnClickTryPlay()
    {
        UIManager.Inst.SwapPanel(PanelId.TryMusicalInstrumentPanel, characterWrap.ChaData.Clone());
    }

    internal void CancelTryOn()
    {
        characterWrap.RefreshAvatar(saveCharacterData);
        switchAnimView?.gameObject.SetActive(false);
    }

    /// <summary>
    /// 试穿 重置参数
    /// </summary>
    /// <param name="goodsData"></param>
    internal void TryOn(string pgcId)
    {
        // CancelTryOn();

        avatarCameraController.ResetEmoteView();
        animationCtrl.ResetEmoteForUICharacter();
        otherAnimationCtrl.gameObject.SetActive(false);
        otherAnimationCtrl.ResetEmoteForUICharacter();

        GameResData config = Es.DataTables.GetGameResData(pgcId);
        if (config == null)
        {
            if (DataTables.GetCameraSelfiePose(pgcId) != null)
                PreviewSelfiePose(pgcId);
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
                PreviewSelfiePose(pgcId);
                break;
        }
        CheckSwitchAnimView(pgcId);
    }
    
    private void CheckSwitchAnimView(string pgcId) {
        GameResData resData = Es.DataTables.GetGameResData(pgcId);
        if (resData == null) {
            LoggerUtils.LogError("resData == null:" + pgcId);
            return;
        }
        var specialSkinConfig = DataTables.GetSpecialSkinConfig(pgcId);

        if (specialSkinConfig != null) {
            SpecialAnimContainer?.gameObject.SetActive(true);
            SpecialAnimContainer?.ResetIdle();
        } else if (resData.ResourceType == (int)ResourceType.Emote && resData.SubType == (int)EmoteSubType.LinkEmote) {
            switchAnimView?.gameObject.SetActive(true);
            switchAnimView?.SetPgcId(pgcId);
        }
    }

    internal void OnWearAvatar(string pgcId)
    {
        var config = DataTables.GetAvatarCommonData(pgcId);
        if(config == null)
        {
            Debug.LogError("AvatarCommonData 配置空了 拉一下商城数据 pgcId ：" + pgcId);
            
            return;
        }
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrap.ChangePart(classType, pgcId);
        if(!string.IsNullOrEmpty(config.defaultColor))
            characterWrap.ChangeColor(classType, config.defaultColor);
        characterWrap.Move(classType, config.pDef);
        characterWrap.Rotate(classType, config.rDef);
        characterWrap.Scale(classType, config.sDef);
        characterWrap.HVScale(classType, config.vhSDef);
        characterWrap.SetLeftOrRight(classType, config.leftRightType);

        var specialConfig = DataTables.GetSpecialSkinConfig(pgcId);
        if (specialConfig != null) ShowSpecialContainer();
    }

    public void ShowSpecialContainer()
    {
        SpecialAnimContainer.gameObject.SetActive(true);
        SpecialAnimContainer.ResetIdle();
    }

    public void OnSpecialPGCClick(SpecialAnim anim)
    {
        if (string.IsNullOrEmpty(animationCtrl.specialAnimPgcId)) return;
        animationCtrl.CheckAndOverrideSpecialAnim();
        if (string.IsNullOrEmpty(animationCtrl.specialAnimPgcId))
        {
            return;
        }
        var specialSkinConfig = DataTables.GetSpecialSkinConfig(animationCtrl.specialAnimPgcId);
        SpecialAnimCameraInfo cameraInfo = null;
        //var resConfig = DataTables.GetGameResData(animationCtrl.specialAnimPgcId);
        //AvatarSubType subType = (AvatarSubType)resConfig.SubType;
        //BodyNode bodyNode = BodyNode.BackDeckNode;
        //if (subType == AvatarSubType.Hats)
        //{
        //    bodyNode = BodyNode.HatNode;
        //}
        Animator pgcAnimator = animationCtrl.specialAnimRoot?.GetComponentInChildren<Animator>(true);
        var switchEventName = "";
        var pgcAnimName = "";
        switch (anim)
        {
            case SpecialAnim.Idle:
                animationCtrl.SetPlayerAniState(PlayerAniState.Idle);
                switchEventName = specialSkinConfig.previewIdleAnimInfo.audio;
                pgcAnimName = "idle";
                cameraInfo = specialSkinConfig.previewIdleCameraInfo;
                break;
            case SpecialAnim.Run:
                animationCtrl.SetPressJoystickTime(0.0f);
                animationCtrl.SetPlayerAniState(PlayerAniState.Run);
                switchEventName = specialSkinConfig.previewRunAnimInfo.audio;
                pgcAnimName = "run";
                cameraInfo = specialSkinConfig.previewRunCameraInfo;
                break;
            case SpecialAnim.FastRun:
                animationCtrl.SetPressJoystickTime(2.5f);
                animationCtrl.SetPlayerAniState(PlayerAniState.Run);
                switchEventName = specialSkinConfig.previewFastRunAnimInfo.audio;
                pgcAnimName = "fast_run";
                cameraInfo = specialSkinConfig.previewFastRunCameraInfo;
                break;
            case SpecialAnim.Jump:
                animationCtrl.SetPlayerState(PlayerState.Leisure);
                animationCtrl.SetPlayerAniState(PlayerAniState.Jump);
                switchEventName = specialSkinConfig.previewJumpAnimInfo.audio;
                pgcAnimName = "preview_jump";
                cameraInfo = specialSkinConfig.previewJumpCameraInfo;
                break;
        }
        if (pgcAnimator != null && !string.IsNullOrEmpty(pgcAnimName))
        {
            pgcAnimator.Play(pgcAnimName);
        }
        AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
        if (!string.IsNullOrEmpty(switchEventName))
        {

            AkSoundManager.Inst.PlaySound($"Emote_Group_{specialSkinConfig.SoundVersion}", switchEventName, $"Play_Emote_{specialSkinConfig.SoundVersion}_1P", animationCtrl.gameObject);
        }

        if (cameraInfo != null)
        {
            avatarCameraController.SetSpecialSkinView(cameraInfo);
        }

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
            case EmoteSubType.LinkEmote:
                animationCtrl.PlayLinkEmoteForUICharacter(pgcId, SpecialAnim.Idle, otherAnimationCtrl);
                break;
        }
    }

    private Sprite GetRewardIconSprite(string pgcId)
    {
        var sp = PgcUtils.GetIconSpriteByPgcId(pgcId, gameObject);
        if (sp != null) return sp;

        var selfieCfg = DataTables.GetCameraSelfiePose(pgcId);
        if (selfieCfg != null && !string.IsNullOrEmpty(selfieCfg.iconString))
        {
            const string cameraModeAtlas = "Assets/Loadable/UI/UIPanel/CameraModePanel/CameraModePanel.spriteatlas";
            sp = XAssetLoaderMgr.Inst.LoadSpriteInAltas(cameraModeAtlas, selfieCfg.iconString, gameObject);
            if (sp != null) return sp;
            if (selfieCfg.iconString.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                sp = XAssetLoaderMgr.Inst.LoadResource<Sprite>(selfieCfg.iconString, gameObject);
        }
        return sp;
    }

    #region 自拍动作
    private const string LeftEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/effect_l";
    private const string RightEffectPath = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/effect_r";
    private const string SelfieStickPrefabPath = "Assets/Loadable/AnimationsExpress/Feat/selfiestick_effect/selfiestick_effect.prefab";

    private GameObject selfieNode;

    private void PreviewSelfiePose(string pgcId)
    {
        if (characterWrap == null || animationCtrl == null) return;

        var cfg = DataTables.GetCameraSelfiePose(pgcId);
        if (cfg == null) return;

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
    public void StopAudio()
    {
         if (animationCtrl != null)
            AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
    }
    private void CreateOrRefreshSelfieStick(CameraSelfiePose cfg)
    {
        var parent = characterWrap.Avatar.transform.Find(cfg.stickHand == 1 ? RightEffectPath : LeftEffectPath);
        if (parent == null) return;

        if (selfieNode == null)
        {
            var selfiePrefab = Loader.Load<GameObject>(SelfieStickPrefabPath)?.RetainAsset(gameObject);
            if (selfiePrefab == null) return;
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

    public override void OnHidden()
    {
        if (animationCtrl != null)
            AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
        base.OnHidden();
    }

    protected override void OnDisable()
    {
        if (animationCtrl != null)
        {
            animationCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
            animationCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
            animationCtrl.OverrideAnimationClip("selfiestick_idle", null);
        }
        base.OnDisable();
    }
    #endregion
}

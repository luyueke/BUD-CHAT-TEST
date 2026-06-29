using System;
using System.Collections.Generic;
using Basic.Utils;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Pet;
using Game.Store;
using Game.Vehicle.PGCVehicle;
using GameData;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using Product;
using UI.Manager;
using UI.UIPanels.GashaponPanel;
using UnityEngine;
using UnityEngine.UI;

public class GashaponCharacterPreview : MonoBehaviour
{
    [Header("人物展示")]
    [SerializeField] private GameObject playerImageView;
    [SerializeField] internal Transform characterRoot;
    [SerializeField] internal AvatarCameraController avatarCameraController;
    [SerializeField] private Image currencyRewardImage;
    [SerializeField] private Text currencyRewardNum;
    [SerializeField] private Image SpecialRewardImage;
    [SerializeField] private SwitchAnimView switchAnimView;
    [SerializeField] private SpecialAnimContainer specialAnimContainer;
    [SerializeField] private Transform effectPreview;

    //人物3d预览
    public CharacterWrap characterWrap;
    // 宠物3D 预览
    public PetWrap petWrap;
    public CharacterWrap otherCharacterWrap;
    [HideInInspector] public PlayerAnimationCtrl animationCtrl;
    [HideInInspector] public PlayerAnimationCtrl otherAnimationCtrl;
    [HideInInspector] public PetAnimationCtrl petAnimationCtrl;

    private Action<bool> previewAnimViewCallBack;
    // huhuyun 路径记录上次按下的 SpecialAnim：相同的再点不再调用 PlaySpecialAnimForUICharacter，避免重复触发让本体/云相位错开
    private SpecialAnim? _lastSwitchSpecialAnim;
    // huhu/wuwu 套装预览要求"不要云"，但 TakeOffSpecialSkin 可能在 Unity Start() 跑之前就被外部调用（characterWrap 还没创建 → 等于 no-op）。
    // 用这个 latch 把请求记住，InitPreviewPlayer 创建完 characterWrap 后立即兑现，挡住玩家自己装备的特殊皮肤（云）加载出来。
    private bool _suppressSpecialSkinOnInit;

    private bool isInit = false;

    void Start()
    {
        InitPreviewPlayer();
        InitPreviewPet();
        isInit = true;
        switchAnimView?.SetCallBack(OnSwitchAnimItemClick);
        specialAnimContainer?.SetCallBack(OnSwitchSpecialAnimItemClick);
        MessageHelper.AddListener<int>(MessageName.OnPhantomSoundPartyBigRewardToggleChanged, OnPhantomSoundPartyBigRewardToggleChanged);
    }

    public void InitPreviewPlayer()
    {
        var saveCharacterData = AccountDataManager.Inst.UserInfo.avatarInfo;
        if (saveCharacterData != null && characterWrap == null)
        {
            characterWrap = AvatarController.Inst.CreateUIAvatar(saveCharacterData);
            characterWrap.SetParent(characterRoot, true);
            animationCtrl = characterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            avatarCameraController.RotateTarget = characterRoot;
            animationCtrl.gameObject.SetActive(true);
            // huhu/wuwu 预览：外部在 Start 之前就已请求 TakeOff（_suppressSpecialSkinOnInit=true），这里 character 刚创建就立即兑现，
            // 同帧把 avatarPartDatas[SpecialSkin] 置 "0"、ResetCurrentID + TakeOff，让 SpecialSkinPartAdapter 后续的异步加载回调走 id != curEffectId 分支被丢掉，云不会被实例化。
            if (_suppressSpecialSkinOnInit) characterWrap.TakeOffSpecialSkin();

            otherCharacterWrap = AvatarController.Inst.CreateUIAvatar(AccountDataManager.Inst.UserInfo.otherAvatarInfo);
            otherCharacterWrap.SetParent(characterRoot, true);
            otherAnimationCtrl = otherCharacterWrap.Avatar.GetComponentInChildren<PlayerAnimationCtrl>();
            otherCharacterWrap.Avatar.gameObject.SetActive(false);
        }
    }

    // 卸下角色身上的特殊皮肤（云）。InitPreviewPlayer 用的是玩家实际 avatarInfo，若玩家正装备 huhuyun 等特殊皮肤，
    // 进预览时云会跟着加载出来；像 huhu/wuwu 这种普通套装预览不需要云，由外部按需调用此方法清掉。
    // OpenPanel 后立刻调用时 characterWrap 还没创建（Unity Start 下一帧才跑），故同时把 latch 打开，InitPreviewPlayer 创建完角色立即兑现。
    public void TakeOffSpecialSkin()
    {
        _suppressSpecialSkinOnInit = true;
        if (characterWrap != null) characterWrap.TakeOffSpecialSkin();
    }

    public void InitPreviewPet()
    {
        var savePetData = AccountDataManager.Inst.PetInfo.avatarInfo;
        if (savePetData != null && petWrap == null)
        {
            petWrap = PetAvatarController.Inst.CreateUIAvatar(savePetData);
            petWrap.SetParent(characterRoot, true);
            petAnimationCtrl = petWrap.Avatar.GetComponentInChildren<PetAnimationCtrl>();
            petAnimationCtrl.gameObject.SetActive(false);
            avatarCameraController.RotateTarget = characterRoot;
        }
    }

    public void StopAllEmoteSound()
    {
        //关闭的时候清除所有音效
        if (animationCtrl != null && animationCtrl.gameObject != null)
        {
            AkSoundManager.Inst.StopAll(animationCtrl.gameObject);
        }

        if (otherAnimationCtrl != null && otherAnimationCtrl.gameObject != null)
        {
            AkSoundManager.Inst.StopAll(otherAnimationCtrl.gameObject);
        }

        if (petAnimationCtrl != null && petAnimationCtrl.gameObject != null)
        {
            AkSoundManager.Inst.StopAll(petAnimationCtrl.gameObject);
        }

    }

    public void ShowCurrency(GashaponBaseData info)
    {
        var currencyType = GameUtils.ConvertRewardType((int)info.RewardType);
        if (currencyType == CurrencyType.None)
        {
            return;
        }

        SetRewardRawImageShow(true);
        var iconSprite = PgcUtils.LoadCurrencyIcon(currencyType, currencyRewardImage.gameObject);
        if (iconSprite != null)
        {
            currencyRewardImage.sprite = iconSprite;
        }
        currencyRewardNum.gameObject.SetActive(true);
        currencyRewardNum.SetText(info.Num > 1 ? "x" + info.Num : "");
    }

    public void ShowAvatarFrame(GashaponBaseData info)
    {
        SetRewardRawImageShow(true);
        currencyRewardNum.gameObject.SetActive(false);
        UserUIWidgetManager.Inst.GetHeadCycleImgByPgcIdAsync(info.Id, currencyRewardImage.gameObject, (sp) =>
        {
            currencyRewardImage.sprite = sp;
        });
    }
    public void ShowHomeSkipFrame(GashaponBaseData info)
    {
        SetRewardRawImageShow(true);
        currencyRewardNum.gameObject.SetActive(false);
        currencyRewardImage.sprite = info.iconSprite;
    }
    public void ShowUGCTemplate(GashaponBaseData info)
    {
        SetRewardRawImageShow(true);
        currencyRewardNum.gameObject.SetActive(false);
        PgcUtils.LoadPetUGCTemplateAsync(info.Id, currencyRewardImage.gameObject, (sp) =>
        {
            currencyRewardImage.sprite = sp;
        });
    }
    public void ShowChatBubble(GashaponBaseData info)
    {
        SetRewardRawImageShow(true);
        currencyRewardNum.gameObject.SetActive(false);
        UserUIWidgetManager.Inst.GetChatBubbleIconByPgcIdAsync(info.Id, currencyRewardImage.gameObject, (sp) =>
        {
            currencyRewardImage.sprite = sp;
        });
    }

    public void ShowRewardIcon(GashaponBaseData info)
    {
        SetRewardRawImageShow(true);
        currencyRewardNum.gameObject.SetActive(false);
        var iconSprite = PgcUtils.LoadRewardIcon((BUDRewardType)info.RewardType, currencyRewardImage.gameObject);
        if (iconSprite != null)
        {
            currencyRewardImage.sprite = iconSprite;
        }
        currencyRewardNum.gameObject.SetActive(true);
        currencyRewardNum.SetText(info.Num > 1 ? "x" + info.Num : "");
    }

    public void ShowTitleIcon(GashaponBaseData info)
    {
        SetSpecialRewardImageShow();
        currencyRewardNum.gameObject.SetActive(false);
        UserUIWidgetManager.Inst.GetTitleImgByPgcIdAsync(info.Id, gameObject, (iconSprite) =>
        {
            if (this != null && SpecialRewardImage != null && iconSprite != null)
            {
                SpecialRewardImage.gameObject.SetActive(true);
                SpecialRewardImage.sprite = iconSprite;
                SpecialRewardImage.SetNativeSize();
            }
        });
    }

    public void ShowNicknameFrameIcon(GashaponBaseData info)
    {
        SetSpecialRewardImageShow();
        currencyRewardNum.gameObject.SetActive(false);
        UserUIWidgetManager.Inst.GetNicknameBgByPgcIdAsync(info.Id, gameObject, (iconSprite) =>
        {
            if (this != null && SpecialRewardImage != null && iconSprite != null)
            {
                SpecialRewardImage.gameObject.SetActive(true);
                SpecialRewardImage.sprite = iconSprite;
                SpecialRewardImage.SetNativeSize();
            }
        });
    }

    public void ShowCurrencyIcon(BUDRewardType RewardType, int Count)
    {
        SetRewardRawImageShow(true);
        var iconSprite = PgcUtils.LoadRewardIcon((BUDRewardType)RewardType, currencyRewardImage.gameObject);
        if (iconSprite != null)
        {
            currencyRewardImage.sprite = iconSprite;
        }
        currencyRewardNum.gameObject.SetActive(true);
        currencyRewardNum.SetText(Count > 1 ? "x" + Count : "");
    }

    public void StartPreview(GashaponBaseData info, Action<List<string>> onAllPartsDressed = null)
    {
        StopAllAnim();
        CancelTryOn();

        if ((int)info.RewardType == (int)BUDRewardType.RewardPgcOptionalBox)
        {
            SetRewardRawImageShow(false);
            return;
        }

        var currencyType = GameUtils.ConvertRewardType((int)info.RewardType);
        //显示货币
        if (!GashaponUtils.HasPGCData(info) && currencyType != CurrencyType.None)
        {
            ShowCurrency(info);
            return;
        }

        if (info.RewardType == RewardType.RewardAvatarFrame)
        {
            ShowAvatarFrame(info);
            return;
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardUgcTemplateResource)
        {
            ShowUGCTemplate(info);
            return;
        }
        if (info.RewardType == RewardType.RewardChatBubbles)
        {
            ShowChatBubble(info);
            return;
        }
        if (info.RewardType == RewardType.RewardHomepageSkin)
        {
            ShowHomeSkipFrame(info);
            return;
        }
        if ((int)info.RewardType == (int)BUDRewardType.RewardSkinSlot || (int)info.RewardType == (int)BUDRewardType.RewardAiBuddySlot || (int)info.RewardType == (int)BUDRewardType.RewardVipFreeTrail
        || (int)info.RewardType == (int)BUDRewardType.RewardCrystal || (int)info.RewardType == (int)BUDRewardType.RewardCrystalShards)
        {
            Debug.LogError("?");
            ShowRewardIcon(info);
            return;
        }
        if((int)info.RewardType == (int)BUDRewardType.RewardTypeTitle)
        {
            ShowTitleEffect(info);
            return;
        }
        if((int)info.RewardType == (int)BUDRewardType.RewardTypeNicknameFrame)
        {
            ShowNicknameFrameEffect(info);
            return;
        }
        string pgcId = "";
        if (GashaponUtils.HasPGCData(info))
        {
            pgcId = info.PgcDatas[0].Id;
        }

        if (string.IsNullOrEmpty(pgcId)) return;


        //判断是要套装还是普通单件
        if (info.PgcDatas.Count > 1)
        {
            List<string> pgcIds = GashaponUtils.ToPgcIdList(info.PgcDatas);
            StartPreview(pgcIds, onAllPartsDressed);
        }
        else
        {
            StartPreview(pgcId, (resultId) =>
            {
                List<string> pgcIds = new List<string>();
                pgcIds.Add(resultId);
                onAllPartsDressed?.Invoke(pgcIds);
            });
        }
    }

    public void SetAnimPreviewBtnColor(Color outlineColor, Color selectColor, Color normalColor)
    {

    }


    public void StartPreview(string pgcId, Action<string> changePardCallback = null)
    {
        SetRewardRawImageShow(false);
        HideAllWarpper();
        StopAllAnim();
        CancelTryOn();
        TryOn(pgcId, changePardCallback);
    }

    // 顺序加载：先等角色刷新完，再加载部件，两者都完成后播待机动画
    // 仅供有此需求的面板（如虾虾崽）通过 GashaponPreviewHandleView.EnableIdleSync 开启
    public void StartPreviewWithIdleSync(string pgcId, Action<string> partCallback = null)
    {
        SetRewardRawImageShow(false);
        HideAllWarpper();
        StopAllAnim();
        specialAnimContainer?.gameObject.SetActive(false);
        switchAnimView?.gameObject.SetActive(false);
        ResetCharacterPosAndRot();
        petWrap?.RefreshAvatar(AccountDataManager.Inst.PetInfo.avatarInfo);
        PGCVehicleManager.Inst.RemoveUIPGCVehicle();
        // 虾虾崽 huhuyun 等单件特殊皮肤预览：要求本体待机持续播 preview_idle_exhibit（和云同款，不要 idle/idle_exhibit 循环切换）。
        // 提前打开标记，让随后 SpecialSkinPartAdapter 异步加载特效后调用 CheckAndOverrideSpecialAnim 时直接把 "idle" AOC 改成 exhibit 片段；
        // 非特殊皮肤 specialPgcAniDic 里没有 idle_exhibit 项 → flag 是 no-op，零影响。
        if (animationCtrl != null) animationCtrl.PreferExhibitIdleForPreview = true;
        if (characterWrap == null)
        {
            TryOn(pgcId, resultId => { partCallback?.Invoke(resultId); animationCtrl?.ForceIdleForUICharacter(); });
            return;
        }
        characterWrap.RefreshAvatar(AccountDataManager.Inst.UserInfo.avatarInfo, () =>
        {
            TryOn(pgcId, resultId =>
            {
                partCallback?.Invoke(resultId);
                animationCtrl?.ForceIdleForUICharacter();
            });
        });
    }

    public void StartPreviewSpecialAnim(string pgcId, SpecialAnim specialAnimType, Action<string> changePardCallback = null)
    {
        SetRewardRawImageShow(false);
        HideAllWarpper();
        StopAllAnim();
        CancelTryOn();
        TryOn(pgcId, changePardCallback);
        OnSwitchAnimItemClick(specialAnimType);
        switchAnimView?.gameObject.SetActive(false);
        avatarCameraController.ZoomCustom(new Vector3(0, 90, 0), 1.2f);
    }

    public void StartPreview(List<string> pgcIdList, Action<List<string>> onAllPartsDressed = null)
    {
        SetRewardRawImageShow(false);
        HideAllWarpper();
        StopAllAnim();
        CancelTryOn();

        if (pgcIdList == null || pgcIdList.Count == 0)
        {
            onAllPartsDressed?.Invoke(pgcIdList);
            return;
        }

        int remainingParts = pgcIdList.Count; // 计数器，记录剩余部位数量

        foreach (string pgcId in pgcIdList)
        {
            TryOn(pgcId, (resultPgcId) =>
            {
                remainingParts--;
                if (remainingParts == 0)
                {
                    onAllPartsDressed?.Invoke(pgcIdList);
                }
            });
        }
        avatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
    }

    private void TryOn(string pgcId, Action<string> changePardCallback = null)
    {
        SwitchWarpperAndCamera(pgcId);
        var config = DataTables.GetGameResData(pgcId);
        if (config == null)
        {
            LoggerUtils.LogError("pgcId ======", pgcId);
            return;
        }
        switch ((ResourceType)config.ResourceType)
        {
            case ResourceType.Avatar:
                TryOnAvatar(pgcId, changePardCallback);
                break;
            case ResourceType.PGCPetAvatar:
                TryOnPet(pgcId, changePardCallback);
                break;
            case ResourceType.Emote:
                PreviewEmote(pgcId, (EmoteSubType)config.SubType);
                break;
            case ResourceType.Vehicle:
                TryOnVehicle(int.Parse(pgcId), changePardCallback);
                break;
        }
        CheckSwitchAnimView(pgcId);

    }

    private void HideAllWarpper()
    {
        characterWrap?.Avatar?.SetActive(false);
        otherCharacterWrap?.Avatar?.SetActive(false);
        petWrap?.Avatar?.SetActive(false);

    }

    private void SwitchWarpperAndCamera(string pgcId)
    {
        GameResData resData = Es.DataTables.GetGameResData(pgcId);
        if (resData == null)
        {
            LoggerUtils.LogError("resData == null:" + pgcId);
            return;
        }
        if (resData.ResourceType == (int)ResourceType.Avatar || resData.ResourceType == (int)ResourceType.UgcAvatar)
        {
            characterWrap.Avatar.SetActive(true);
            SetCharacterAvatarCamera();
        }
        else if (resData.ResourceType == (int)ResourceType.PGCPetAvatar || resData.ResourceType == (int)ResourceType.UGCPetAvatar)
        {
            petWrap.Avatar.SetActive(true);
            SetPetAvatarCamera();
        }
        else if (resData.ResourceType == (int)ResourceType.Emote)
        {
            var emoteSubType = ((EmoteSubType)resData.SubType);
            if (emoteSubType.IsPet())
            {
                petWrap.Avatar.SetActive(true);
                SetPetAvatarCamera();
            }
            else
            {
                characterWrap.Avatar.SetActive(true);
                SetCharacterAvatarCamera();
            }
        }
        else if (resData.ResourceType == (int)ResourceType.Vehicle && (pgcId == "160200004" || pgcId == "160200005"))
        {
            characterWrap.Avatar.SetActive(true);
            otherCharacterWrap.Avatar.SetActive(true);
            SetCharacterAvatarCamera();
        }

    }

    private void CheckSwitchAnimView(string pgcId)
    {
        GameResData resData = Es.DataTables.GetGameResData(pgcId);
        if (resData == null)
        {
            LoggerUtils.LogError("resData == null:" + pgcId);
            return;
        }
        var specialSkinConfig = DataTables.GetSpecialSkinConfig(pgcId);

        if (specialSkinConfig != null)
        {
            specialAnimContainer?.gameObject.SetActive(true);
            specialAnimContainer?.ResetIdle();
        }
        else if (resData.ResourceType == (int)ResourceType.Emote && resData.SubType == (int)EmoteSubType.LinkEmote)
        {
            switchAnimView?.gameObject.SetActive(true);
            switchAnimView?.SetPgcId(pgcId);
        }
    }

    internal void TryOnVehicle(int pgcId, Action<string> changePardCallback = null)
    {
        // characterWrap.ChangeUGCVehicle(AccountDataManager.Inst.UserInfo.uid, pgcId);
        var vehicleConfig = DataTables.GetPgcVehicleConfig(pgcId);
        PGCVehicleManager.Inst.CreateUIPGCVehicle(pgcId, characterRoot, new PlayerAnimationCtrl[]{animationCtrl, otherAnimationCtrl}, (vehicleController) =>
        {
            avatarCameraController.SetVehicleViewByConfig(vehicleConfig);
            animationCtrl.transform.localPosition = new Vector3(animationCtrl.transform.localPosition.x, animationCtrl.transform.localPosition.y + 0.5f,animationCtrl.transform.localPosition.z);
            otherAnimationCtrl.transform.localPosition = new Vector3(otherAnimationCtrl.transform.localPosition.x, otherAnimationCtrl.transform.localPosition.y + 0.5f,otherAnimationCtrl.transform.localPosition.z);
            //这里由于创建UI载具的时候Y轴-0.5f,这里加回来，不然人物模型会穿出载具
            changePardCallback?.Invoke(pgcId.ToString());
        });
    }
    internal void TryOnAvatar(string pgcId, Action<string> changePardCallback = null)
    {
        if (characterWrap == null)
        {
            return;
        }
        var config = DataTables.GetAvatarCommonData(pgcId);
        var classType = UniqueType.GetAvatar(pgcId);
        characterWrap.ChangePart(classType, pgcId, () =>
        {
            changePardCallback?.Invoke(pgcId);
        });
        characterWrap.ChangeColor(classType, config.defaultColor);
        characterWrap.Move(classType, config.pDef);
        characterWrap.Rotate(classType, config.rDef);
        characterWrap.Scale(classType, config.sDef);
        characterWrap.HVScale(classType, config.vhSDef);
        characterWrap.SetLeftOrRight(classType, config.leftRightType);
        avatarCameraController.SetCameraZoom(classType);



        // var specialSkinConfig = DataTables.GetSpecialSkinConfig(pgcId);
        // if (specialSkinConfig != null && gashaponPreviewAnimView != null) {
        //     gashaponPreviewAnimView.gameObject.SetActive(true);
        //     previewAnimViewCallBack?.Invoke(true);
        //     avatarCameraController.SetCameraZoom(ViewType.ZoomWholeBody);
        // } else {
        //     avatarCameraController.SetCameraZoom(classType);
        // }

    }

    private void TryOnPet(string pgcId, Action<string> changePardCallback = null)
    {
        if (petWrap == null)
        {
            return;
        }

        var petConfig = DataTables.GetPetAvatarCommonData(pgcId);
        var petSubType = UniqueType.GetPGCPetAvatar((AvatarSubType)petConfig.SubType);

        petWrap.ChangePart(petSubType, pgcId, () =>
        {
            changePardCallback?.Invoke(pgcId);
        });
        petWrap.ChangeColor(petSubType, petConfig.defaultColor);
        petWrap.Move(petSubType, petConfig.pDef);
        petWrap.Rotate(petSubType, petConfig.rDef);
        petWrap.Scale(petSubType, petConfig.sDef);
        petWrap.HVScale(petSubType, petConfig.vhSDef);
        petWrap.SetLeftOrRight(petSubType, petConfig.leftRightType);
        avatarCameraController.SetCameraZoom(petSubType);
    }

    private void CancelTryOn()
    {
        specialAnimContainer?.gameObject.SetActive(false);
        switchAnimView?.gameObject.SetActive(false);
        ResetCharacterPosAndRot();
        petWrap?.RefreshAvatar(AccountDataManager.Inst.PetInfo.avatarInfo);
        characterWrap?.RefreshAvatar(AccountDataManager.Inst.UserInfo.avatarInfo);
        // huhu/wuwu 套装预览：RefreshAvatar 把玩家 avatarInfo 里的特殊皮肤（含云）整套塞回来，
        // 套装本身 idList 并没配云，所以每次点 item 都得再次卸掉，避免 SpecialSkinPartAdapter.PutOn 再次启动云加载
        if (_suppressSpecialSkinOnInit && characterWrap != null) characterWrap.TakeOffSpecialSkin();
        PGCVehicleManager.Inst.RemoveUIPGCVehicle();
    }

    internal void PreviewEmote(string pgcId, EmoteSubType emoteSubType)
    {
        switch (emoteSubType)
        {
            case EmoteSubType.Single:
            case EmoteSubType.SingleLoop:
                animationCtrl.PlaySingleEmoteForUICharacter(pgcId);
                break;
            case EmoteSubType.Double:
            case EmoteSubType.DoubleLoop:
                animationCtrl.PlayDoubleEmoteForUICharacter(pgcId, otherAnimationCtrl);
                break;
            case EmoteSubType.PetSingle:
            case EmoteSubType.PetSingleLoop:
                petAnimationCtrl.PlaySingleEmoteForUICharacter(pgcId);
                break;
            case EmoteSubType.PetWithPlayer:
            case EmoteSubType.PetWithPlayerLoop:
                petAnimationCtrl.PlayPetWithPlayerEmoteForUICharacter(pgcId, animationCtrl);
                break;
            case EmoteSubType.LinkEmote:
                animationCtrl.PlayLinkEmoteForUICharacter(pgcId, SpecialAnim.Idle, otherAnimationCtrl);
                break;

        }
        avatarCameraController.SetCameraZoom(ViewType.ZoomEmote);
        avatarCameraController.SetEmoteView(pgcId);
    }

    private void ResetCharacterPosAndRot()
    {
        characterRoot.eulerAngles = new Vector3(0, -180, 0);
        animationCtrl.transform.localPosition = Vector3.zero;
    }

    public void StopAllAnim()
    {
        animationCtrl?.CheckAndOverrideSpecialAnim();
        animationCtrl?.DriveSpecialEffectPreviewIdle(); // 切换/重置时特效会被打回 base，重新驱动 preview（非特殊皮肤 no-op）
        avatarCameraController?.ResetEmoteView();
        animationCtrl?.ResetEmoteForUICharacter();
        petAnimationCtrl?.ResetEmoteForUICharacter();
        otherAnimationCtrl?.gameObject.SetActive(false);
        otherAnimationCtrl?.ResetEmoteForUICharacter();
        if(effectPreview?.childCount >0)
        {
            DestroyImmediate(effectPreview.GetChild(0).gameObject);
        }
    }

    private void SetPetAvatarCamera()
    {
        avatarCameraController.ZoomUpperPosY = -0.1f;
        avatarCameraController.ZoomUppereCameraSize = 0.4f;
        avatarCameraController.ZoomWholePosY = -0.2f;
        avatarCameraController.ZoomWholeCameraSize = 0.7f;
        avatarCameraController.ZoomFootPosY = -0.46f;
        avatarCameraController.ZoomFootCameraSize = 0.3f;

        avatarCameraController.customEmoteCameraScale = 0.7f;
    }

    private void SetCharacterAvatarCamera()
    {
        avatarCameraController.ZoomUpperPosY = 0.3f;
        avatarCameraController.ZoomUppereCameraSize = 0.6f;
        avatarCameraController.ZoomWholePosY = 0;
        avatarCameraController.ZoomWholeCameraSize = 1f;
        avatarCameraController.ZoomFootPosY = -0.375f;
        avatarCameraController.ZoomFootCameraSize = 0.75f;
        avatarCameraController.customEmoteCameraScale = 1.0f;
    }


    public void SetRewardRawImageShow(bool isShow)
    {
        playerImageView.SetActive(!isShow);
        currencyRewardImage.gameObject.SetActive(isShow);
        SpecialRewardImage.gameObject.SetActive(false);
    }

    public void SetSpecialRewardImageShow()
    {
        playerImageView.SetActive(false);
        SpecialRewardImage.gameObject.SetActive(true);
        currencyRewardImage.gameObject.SetActive(false);
    }

    private void OnSwitchAnimItemClick(SpecialAnim anim)
    {
        var emoteId = switchAnimView.GetPgcId();
        if (!string.IsNullOrEmpty(emoteId))
        {
            animationCtrl.PlayLinkEmoteForUICharacter(emoteId, anim, otherAnimationCtrl);
        }
    }

    private void OnSwitchSpecialAnimItemClick(SpecialAnim anim)
    {
        // huhuyun 预览：当前已选中 jump/idle 再点同一个直接吞掉，避免重复触发让本体/云相位错开。其他面板（flag=false）维持原行为
        if (animationCtrl != null && animationCtrl.PreferExhibitIdleForPreview && _lastSwitchSpecialAnim == anim) return;
        _lastSwitchSpecialAnim = anim;
        animationCtrl.PlaySpecialAnimForUICharacter(anim);
    }

    private void OnDisable()
    {
        StopAllEmoteSound();
    }

    private void OnPhantomSoundPartyBigRewardToggleChanged(int pgcId)
    {
        PGCVehicleManager.Inst.RemoveUIPGCVehicle();
        TryOnVehicle(pgcId, (_) => { });
    }

    private void OnDestroy()
    {
        StopAllEmoteSound();
        MessageHelper.RemoveListener<int>(MessageName.OnPhantomSoundPartyBigRewardToggleChanged, OnPhantomSoundPartyBigRewardToggleChanged);
        UIManager.Inst.ClosePanel(PanelId.PhantomSoundPartyBigRewardShowPanel);
    }


    public void SetCameraColor(Color color)
    {
        if (avatarCameraController != null && avatarCameraController.roleCamera != null)
        {
            avatarCameraController.roleCamera.backgroundColor = color;
        }
    }
    public void ShowTitleEffect(GashaponBaseData info)
    {
        SetSpecialRewardImageShow();
        SpecialRewardImage.gameObject.SetActive(false);
        currencyRewardNum.gameObject.SetActive(false);
        var config = UserUIWidgetManager.Inst.GetTitleDataByPgcId(info.Id);
        if (this != null && effectPreview != null && config != null && !string.IsNullOrEmpty(config.PreviewPath))
        {
            var o = Loader.Load<GameObject>(config.PreviewPath, gameObject);
            if(o != null)
            {
                var go = GameObject.Instantiate(o, effectPreview.transform);
                go.transform.localScale = Vector3.one * 2;
            
            }else
            {
                ShowTitleIcon(info);
            }
     

        }else
        {
            ShowTitleIcon(info);
        }
    }

    public void ShowNicknameFrameEffect(GashaponBaseData info)
    {
        SetSpecialRewardImageShow();
        SpecialRewardImage.gameObject.SetActive(false);
        currencyRewardNum.gameObject.SetActive(false);
        var config = UserUIWidgetManager.Inst.GetNicknameByPgcId(info.Id);
        if (this != null && effectPreview != null && config != null && !string.IsNullOrEmpty(config.Prefab))
        {
            var o = Loader.Load<GameObject>(config.Prefab, gameObject);
            if (o != null)
            {
                //o.transform.localScale = Vector3.one * 2;
                 GameObject.Instantiate(o, effectPreview.transform);
            }else
            {
                ShowNicknameFrameIcon(info);
            }

        }else
        {
            ShowNicknameFrameIcon(info);
        }
    }



}

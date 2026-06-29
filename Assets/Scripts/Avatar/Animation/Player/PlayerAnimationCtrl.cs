using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Basic.Extensions;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Vehicle.PGCVehicle;
using Game.Config;
using GameData;
using GameData.Manager;
using GameData.PgcData;
using Message;
using UIAgent;
using UnityEngine;
using xasset;
using Random = UnityEngine.Random;

public class PlayerAnimationCtrl : PlayAnimation
{
    protected IParameterAnimation<PlayerAniPrameter> m_ParameterAnimation;
    public IParameterAnimation<PlayerAniPrameter> ParameterAnimation
    {
        get
        {
            if (m_ParameterAnimation == null)
            {
                m_ParameterAnimation = new PlayerParameterAnimation(m_Animator);
            }

            return m_ParameterAnimation;
        }
    }

    public PlayerAniState CurAniState { get; private set; }
    public PlayerAniState PreAniState { get; private set; }
    public PlayerChildAniState CurChildAniState { get; private set; }
    public PlayerChildAniState PreChildAniState { get; private set; }

    public PlayerState CurPlayState => curPlayerState;


    protected PlayerState curPlayerState;
    protected PlayerState prePlayerState;
    public Action<string> OnUGCPartsPlayAnim { get; set; }
    public Action OnUGCPartsResetAnim { get; set; }
    //private string curEyeAni = AniPath + "Eye/eye_1/FACE_EYE_1";

    public BaseAvatarWrapper Wrap;
    private Dictionary<string, AnimationClip> specialPgcAniDic = new Dictionary<string, AnimationClip>();

    public string specialAnimPgcId;
    [HideInInspector] public GameObject specialAnimRoot;
    // UI 预览模式标记：置 true 后，特效每次被(重新)创建时自动切 preview_idle（应对网络刷新重建特效）。仅 UI 面板设置，局内为 false。
    [HideInInspector] public bool UIPreviewIdleMode;
    // 虾虾崽 huhuyun 预览专用：置 true 后，CheckAndOverrideSpecialAnim 会把本体 "idle" AOC 也指向 idle_exhibit 的片段，
    // 让 idle/idle_exhibit 两个 state 播同一支 preview_idle_exhibit，避免 state 机循环切换造成本体相位与云错乱。
    // 仅 GashaponCharacterPreview.StartPreviewWithIdleSync 入口设置，其他面板默认 false，零影响。
    [HideInInspector] public bool PreferExhibitIdleForPreview;
    // huhuyun 路径下串行化 switch container 的 SetSwitch+PostEvent：每次新请求等一帧再执行，避免连续切按钮时前一个 SetSwitch 被后一个覆盖、两次 PostEvent 都读到最新 state（都播 Fast）
    private Coroutine _previewAudioCoroutine;
    // UI 预览下是否正在播 emote：true 时特效应隐藏（emote 与特效会穿模）。特效可能在 emote 开始后才异步创建，故创建/驱动时统一据此判定显隐。
    private bool _uiEmotePlaying;
    protected string playerId;
    public string PlayerId { get { return playerId; } }
    public bool IsFootSoundEnable = true;

    public void Init(BaseAvatarWrapper wrap)
    {
        Wrap = wrap;
        m_Animator = GetComponent<Animator>();
        EnterTempClip = EnterTempClipState;
        CurAniState = PlayerAniState.Idle;
        PreAniState = PlayerAniState.Idle;
        Init();
    }
    private void Start()
    {
        MessageHelper.AddListener<bool>(MessageName.SpecialProps, SetSpecialActivity);
    }
    private void OnDestroy()
    {
        MessageHelper.RemoveListener<bool>(MessageName.SpecialProps, SetSpecialActivity);
    }


    public void RefreshSkinInfo(int resType, string skinId) {

        SpecialSkinConfig specialSkinConfig = null;

        if (resType == UniqueType.GetAvatar(AvatarSubType.SpecialSkin) && (string.IsNullOrEmpty(skinId) || skinId.Equals("0"))) {
            specialPgcAniDic.Clear();
            specialAnimPgcId = null;
            return;
        }

        if (string.IsNullOrEmpty(skinId) || skinId.Equals("0")) {
            return;
        }

        specialSkinConfig = DataTables.GetSpecialSkinConfig(skinId);
        if (specialSkinConfig == null) {
            return;
        }

        specialPgcAniDic.Clear();
        specialAnimPgcId = null;
        bool isOwnedSpecialSkin = false;
        if (!string.IsNullOrEmpty(specialSkinConfig.idleAnimInfo.anim)) {
            var skinIdle = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.idleAnimInfo.anim, gameObject);
            if (skinIdle != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.Idle.GetString(), skinIdle);
            }
        }

        if (!string.IsNullOrEmpty(specialSkinConfig.runAnimInfo.anim)) {
            var skinRun = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.runAnimInfo.anim, gameObject);
            if (skinRun != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.Run.GetString(), skinRun);
            }

        }
        if (!string.IsNullOrEmpty(specialSkinConfig.fastRunAnimInfo.anim)) {
            var skinRunFast = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.fastRunAnimInfo.anim, gameObject);
            if (skinRunFast != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.FastRun.GetString(), skinRunFast);
            }
        }

        if (!string.IsNullOrEmpty(specialSkinConfig.jumpRunAnimInfo.anim)) {
            var skinJumpRun = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.jumpRunAnimInfo.anim, gameObject);
            if (skinJumpRun != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.JumpRun.GetString(), skinJumpRun);
            }
        }

        if (!string.IsNullOrEmpty(specialSkinConfig.jumpFastRunAnimInfo.anim)) {
            var skinJumpFastRun = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.jumpFastRunAnimInfo.anim, gameObject);
            if (skinJumpFastRun != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.JumpFastRun.GetString(), skinJumpFastRun);
            }
        }

        if (!string.IsNullOrEmpty(specialSkinConfig.jumpAnimInfo.anim)) {
            var skinJump = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.jumpAnimInfo.anim, gameObject);
            if (skinJump != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.Jump.GetString(), skinJump);
            }
        }

        if (!string.IsNullOrEmpty(specialSkinConfig.previewIdleAnimInfo.anim)) {
            var skinIdle = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.previewIdleAnimInfo.anim, gameObject);
            if (skinIdle != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.PreviewIdle.GetString(), skinIdle);
            }
        }

        if (!string.IsNullOrEmpty(specialSkinConfig.previewRunAnimInfo.anim)) {
            var skinRun = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.previewRunAnimInfo.anim, gameObject);
            if (skinRun != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.PreviewRun.GetString(), skinRun);
            }
        }

        if (!string.IsNullOrEmpty(specialSkinConfig.previewFastRunAnimInfo.anim)) {
            var skinRunFast = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.previewFastRunAnimInfo.anim, gameObject);
            if (skinRunFast != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.PreviewFastRun.GetString(), skinRunFast);
            }
        }


        if (!string.IsNullOrEmpty(specialSkinConfig.previewJumpAnimInfo.anim)) {
            var skinJump = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.previewJumpAnimInfo.anim, gameObject);
            if (skinJump != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.PreviewJump.GetString(), skinJump);
            }
        }


        if (!string.IsNullOrEmpty(specialSkinConfig.landAnimInfo.anim)) {
            var skinLanded = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.landAnimInfo.anim, gameObject);
            if (skinLanded != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.Landed.GetString(), skinLanded);
            }
        }


        if (!string.IsNullOrEmpty(specialSkinConfig.exhibitIdleAnimInfo.anim) || !string.IsNullOrEmpty(specialSkinConfig.idleAnimInfo.anim) ) {
            var exhibitIdleAnim = specialSkinConfig.exhibitIdleAnimInfo.anim;
            if (string.IsNullOrEmpty(exhibitIdleAnim)) {
                exhibitIdleAnim = specialSkinConfig.idleAnimInfo.anim;
            }
            var skinExhibitIdle= Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + exhibitIdleAnim, gameObject);
            if (skinExhibitIdle != null) {
                isOwnedSpecialSkin = true;
                specialPgcAniDic.Add(SpecialAnim.ExhibitIdle.GetString(), skinExhibitIdle);
            }
        }


        // UI 预览模式：把本体显示键(idle/run/fastRun/jump/exhibit)替换成 preview 专用片段，没配则保持 base。
        // 场景/局内 UIPreviewIdleMode=false → 整段跳过，本体维持 base，零影响。
        if (UIPreviewIdleMode) {
            ReplaceWithPreviewClip(SpecialAnim.Idle.GetString(), specialSkinConfig.previewIdleAnimInfo.anim);
            ReplaceWithPreviewClip(SpecialAnim.Run.GetString(), specialSkinConfig.previewRunAnimInfo.anim);
            ReplaceWithPreviewClip(SpecialAnim.FastRun.GetString(), specialSkinConfig.previewFastRunAnimInfo.anim);
            ReplaceWithPreviewClip(SpecialAnim.Jump.GetString(), specialSkinConfig.previewJumpAnimInfo.anim);
            // ExhibitIdle 用 previewexhibitIdleAnimInfo（如 huhucloud/preview_idle_exhibit.anim），
            // 配合云端 controller 的 preview_idle_exhibit state，让本体/云都按这一对文件展示，相位一致。
            // 其它皮肤这两个字段本来就指向同一文件，行为无变化。
            ReplaceWithPreviewClip(SpecialAnim.ExhibitIdle.GetString(), specialSkinConfig.previewexhibitIdleAnimInfo.anim);
        }

        if (isOwnedSpecialSkin) {
            specialAnimPgcId = skinId;
        }
    }

    // UI 预览：用 preview 片段覆盖 specialPgcAniDic 的某个本体显示键（preview 路径为空或加载失败则保持原 base）。
    private void ReplaceWithPreviewClip(string bodyClipKey, string previewAnimPath) {
        if (string.IsNullOrEmpty(previewAnimPath)) return;
        var clip = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + previewAnimPath, gameObject);
        if (clip != null) specialPgcAniDic[bodyClipKey] = clip;
    }



    public void SetPlayerID(string playerId)
    {
        this.playerId = playerId;
    }


    public void CheckAndOverrideSpecialAnim() {
        if (specialPgcAniDic != null && specialPgcAniDic.Count > 0) {
            foreach (var item in specialPgcAniDic) {
                if (item.Value != null) {
                    m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, item.Key, item.Value);
                    if (item.Key == "jump_run") m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, "runjump24", item.Value);
                }
            }
            // 虾虾崽 huhuyun 预览：把本体 "idle" 也指向 idle_exhibit 片段，让 idle/idle_exhibit 都播同款 preview_idle_exhibit，本体相位和云一致
            if (PreferExhibitIdleForPreview
                && specialPgcAniDic.TryGetValue(SpecialAnim.ExhibitIdle.GetString(), out var exhibitClip)
                && exhibitClip != null)
            {
                m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, SpecialAnim.Idle.GetString(), exhibitClip);
            }
        } else {
            if (!PGCVehicleManager.Inst.HasHallUIVehicleFor(this))
                ClearOverrideSpecialAnim();
        }
        ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
    }

    public void ClearOverrideSpecialAnim() {
        foreach (SpecialAnim anim in Enum.GetValues(typeof(SpecialAnim))) {
            m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, anim.GetString(), null);
			if (anim.GetString() == "jump_run") m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, "runjump24", null);
        }
    }

    public void OverrideAnimationClip(string stateName,AnimationClip clip)
    {
       // Debug.LogError($"OverrideAnimationClip stateName={stateName},clip.name={clip.name}");
        m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, stateName, clip);
    }

    // UI 预览专用：把本体某个显示用 clip 键覆盖成 preview 系列动画。仅在 UI 实例上调用，局内实例不受影响。
    // animPath 为空 → 不覆盖，沿用 base（兼容没配 preview 的皮肤）。
    private void OverrideUIPreviewClip(string bodyClipKey, string animPath)
    {
        if (string.IsNullOrEmpty(animPath)) return;
        var clip = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + animPath, gameObject);
        if (clip != null)
        {
            OverrideAnimationClip(bodyClipKey, clip);
        }
    }

    // UI 预览专用：特效 animator 优先播 "preview_<baseState>"，控制器里没有该 state 就回退 base。
    // 多数特效控制器只有 preview_jump，没有 preview_idle/preview_run/preview_fast_run，故大多数会回退（= 原行为，不回归）。
    private void PlayEffectPreferPreview(Animator effectAnim, string baseState)
    {
        if (effectAnim == null || string.IsNullOrEmpty(baseState)) return;
        string previewState = "preview_" + baseState;
        string target = effectAnim.HasState(0, Animator.StringToHash(previewState)) ? previewState : baseState;
        // 优先 Play + CrossFadeInFixedTime 兜底：装备特殊皮肤后 SetActive(true) 引起的 Animator rebind 会把 Play 排队的 state
        // 又重置回 controller 默认 state（"idle"）。CrossFadeInFixedTime 显式建立 transition，绕开这种 short-circuit。
        effectAnim.Play(target, 0, 0f);
        effectAnim.CrossFadeInFixedTime(target, 0f, 0, 0f);
    }

    // UI 展示场景（大厅等不走 PlaySpecialAnimForUICharacter 的面板）专用：在 CheckAndOverrideSpecialAnim 之后调用，
    // 把本体 "idle" 覆盖成 preview 待机，使大厅特殊皮肤也显示 preview 而非 base。仅作用于该 UI 实例，局内不受影响；
    // previewIdleAnimInfo.anim 为空则不覆盖（沿用 base）。
    private Coroutine uiPreviewEffectCoroutine;

    public void ApplyUIPreviewIdleOverride()
    {
        if (string.IsNullOrEmpty(specialAnimPgcId)) return;
        var cfg = DataTables.GetSpecialSkinConfig(specialAnimPgcId);
        if (cfg == null) return;
        UIPreviewIdleMode = true; // 标记 UI 预览：后续特效(重新)创建时自动切 preview
        // 回到特殊待机：清除 emote 标记并恢复特效显示，保证任何"返回特殊待机"的路径都能复原特效（避免被 emote 隐藏后卡住）
        _uiEmotePlaying = false;
        SetSpecialEffectActive(true);
        // 本体同步存在，立即切 preview idle
        OverrideUIPreviewClip(SpecialAnim.Idle.GetString(), cfg.previewIdleAnimInfo.anim);
        // 特效(specialAnimRoot)是异步加载的，进面板那一刻可能还没就绪。
        // 用协程等到特效 animator 就绪后再驱动一次 preview_idle（带超时），比固定等帧/秒稳：就绪即触发、不卡死。
        if (uiPreviewEffectCoroutine != null) StopCoroutine(uiPreviewEffectCoroutine);
        if (gameObject.activeInHierarchy)
        {
            uiPreviewEffectCoroutine = StartCoroutine(DriveEffectPreviewIdleWhenReady());
        }
    }

    private IEnumerator DriveEffectPreviewIdleWhenReady()
    {
        const float timeout = 3f;
        float elapsed = 0f;
        Animator effectAnim = null;
        while (elapsed < timeout)
        {
            if (specialAnimRoot != null)
            {
                effectAnim = specialAnimRoot.GetComponentInChildren<Animator>(true);
                if (effectAnim != null && effectAnim.runtimeAnimatorController != null) break;
                effectAnim = null;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (effectAnim != null)
        {
            PlayEffectPreferPreview(effectAnim, "idle");
        }
        uiPreviewEffectCoroutine = null;
    }


    /// <summary>
    /// 设置人物动画状态
    /// </summary>
    /// <param name="aniState"></param>
    public void SetPlayerAniState(PlayerAniState aniState, bool isForceChange = true)
    {
        //if (CurAniState == aniState && !isForceChange) return;
        if (CurAniState == aniState ) return; //去掉isForceChange,会导致死循环
        if (specialAnimCoroutine != null)
        {
            StopCoroutine(specialAnimCoroutine);
            specialAnimCoroutine = null;
        }
        if (!isForceChange)
        {
            // 只允许base之间的动画状态切换
            if (CurAniState > PlayerAniState.Land && aniState <= PlayerAniState.Land) return;
        }

        PreAniState = CurAniState;
        CurAniState = aniState;
        StateEventManager.Inst.TriggerStateEvent(playerId, StateEvent.PlayerAniState, CurAniState);
        ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
        ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
        ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);
    }

    // 强制触发 Idle，不受 CurAniState == Idle 的 early-return 限制，专供 UI 预览面板使用
    public void ForceIdleForUICharacter()
    {
        PreAniState = CurAniState;
        CurAniState = PlayerAniState.Idle;
        ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
        ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
        ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);
    }

    /// <summary>强制从头重播 idle（绕过 CurAniState==Idle 的 early-return）。
    /// 用于让被娃娃机抓着的驾驶员 avatar idle 与载具 idle 同帧同相位起播（float 对齐）。</summary>
    public void ForceReplayIdle()
    {
        PreAniState = CurAniState;
        CurAniState = PlayerAniState.Idle;
        ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
        ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
        ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);
    }

    /// <summary>
    /// 设置人物动画状态再次触发,目前针对连跳的情况，请确保动画机中不会无限循环
    /// </summary>
    /// <param name="aniState"></param>
    public void SetPlayerAniTriggerAgain(PlayerAniState aniState){
        if (specialAnimCoroutine != null)
        {
            StopCoroutine(specialAnimCoroutine);
            specialAnimCoroutine = null;
        }

        //如果当前不是此动画状态，配置为此动画状态，相当于第一次进入
        //预防覆盖了PreAniState的情况
        if(CurAniState != aniState){
            PreAniState = CurAniState;
            CurAniState = aniState;
        }

        StateEventManager.Inst.TriggerStateEvent(playerId, StateEvent.PlayerAniState, CurAniState);
        ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
        ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
        ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);
    }

    /// <summary>
    /// 设置人物子动画状态
    /// </summary>
    /// <param name="aniState"></param>
    public void SetPlayerChildAniState(PlayerChildAniState aniState)
    {
        PreChildAniState = CurChildAniState;
        CurChildAniState = aniState;

        ParameterAnimation.SetInteger(PlayerAniPrameter.PreChildAniState, (int)PreChildAniState);
        ParameterAnimation.SetInteger(PlayerAniPrameter.CurChildAniState, (int)CurChildAniState);
    }

    /// <summary>
    /// 设置人物状态
    /// </summary>
    /// <param name="curPlayerState"></param>
    /// <param name="prePlayerState"></param>
    public void SetPlayerState(PlayerState stateID)
    {
        prePlayerState = curPlayerState;
        curPlayerState = stateID;

        if(ParameterAnimation.GetParametersLength() == 0)
        {
              ParameterAnimation.InitAnimatorParameters();
        }
        ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
        ParameterAnimation.SetInteger(PlayerAniPrameter.PrePlayerState, (int)prePlayerState);
        ParameterAnimation.SetInteger(PlayerAniPrameter.CurPlayerState, (int)curPlayerState);
    //    Debug.LogError("SetPlayerState curPlayerState=" + (int)curPlayerState+",path="+PrintClassPath()+",instanceid="+transform.GetInstanceID());
    }

    private string PrintClassPath()
    {
        string n = "";
        GameObject go = gameObject;
        while(go.transform.parent != null)
        {
            go = go.transform.parent.gameObject;
            n += go.name + "/";
        }
        return n;
    }
    /// <summary>
    /// 设置移动速度
    /// </summary>
    /// <param name="velocity"></param>
    public void SetMoveSpeed(Vector3 velocity)
    {
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        ParameterAnimation.SetFloat(PlayerAniPrameter.MoveSpeed, speed);
        ParameterAnimation.SetFloat(PlayerAniPrameter.YMoveSpeed, velocity.y);
    }

    public void SetVehicleMoveSpeed(Vector3 velocity)
    {
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        ParameterAnimation.SetFloat(PlayerAniPrameter.MoveSpeed, speed);
        ParameterAnimation.SetFloat(PlayerAniPrameter.YMoveSpeed, 0);
        var vehicleInfo = GameDataManager.Inst.mapGlobalData.vehicleInfo;
        if (vehicleInfo == null)
        {
            return;
        }
        var animator = transform.parent.parent.GetComponent<Animator>();
        if(animator == null)
        {
            return;
        }
        if (speed > 0)
        {
            if(vehicleInfo.runAniType == 0)
            {
                animator.enabled = false;
                return;
            }
            animator.enabled = true;
            animator.Play(string.Format("ugc_car0{0}_run", vehicleInfo.runAniType));
        }
        else
        {
            if(vehicleInfo.endAniType == 0)
            {
                animator.enabled = false;
                return;
            }
            animator.enabled = true;
            animator.Play(string.Format("ugc_car0{0}_idle", vehicleInfo.endAniType));
        }

    }

    /// <summary>
    /// 设子特殊PGC ID
    /// </summary>
    /// <param name="pgcId"></param>
    public void SetSpecialPgcId(string pgcId) {
        if (int.TryParse(pgcId, out var id)) {
            ParameterAnimation.SetInteger(PlayerAniPrameter.SpecialAnimPGC, id);
        }
    }
    /// <summary>
    /// 设置移动时间
    /// </summary>
    /// <param name="speed"></param>
    public void SetPressJoystickTime(float speed)
    {
        ParameterAnimation.SetFloat(PlayerAniPrameter.PressJoystickTime, speed);
    }

    public float GetPressJoystickTime()
    {
        return ParameterAnimation.GetFloat(PlayerAniPrameter.PressJoystickTime);
    }

    public void SetPlayerABType(int abType)
    {
        ParameterAnimation.SetInteger(PlayerAniPrameter.PlayerABType, abType);
    }

    public int GetPlayerABType()
    {
        return ParameterAnimation.GetInteger(PlayerAniPrameter.PlayerABType);
    }

    /// <summary>
    /// 根据配置播放动画
    /// </summary>
    /// <param name="animId"></param>
    public IAnimationEvent PlayAnimFromConfig(AnimId animId, float normalizeTime = 0)
    {
        var animConfig = DataTables.GetAnimationConfig(animId.ToString());

        if (animConfig.IsLoad)
        {
            return LoadPlay(animConfig.LoadPath, animConfig.layerIndex);
        }
        else
        {
            return Play(animConfig.AnimationName, animConfig.layerIndex);
        }
    }

    /// <summary>
    /// 根据配置播放动画
    /// </summary>
    /// <param name="animId"></param>
    public IAnimationEvent CrossFadeAnimFromConfig(AnimId animId, float normalizedTransitionDuration, float normalizedTimeOffset = 0, float normalizedTransitionTime = 0)
    {
        var animConfig = DataTables.GetAnimationConfig(animId.ToString());

        if (animConfig.IsLoad)
        {
            return LoadCrossFade(animConfig.LoadPath, normalizedTransitionDuration, animConfig.layerIndex, normalizedTimeOffset, normalizedTransitionTime);
        }
        else
        {
            return LoadCrossFade(animConfig.AnimationName, normalizedTransitionDuration, animConfig.layerIndex, normalizedTimeOffset, normalizedTransitionTime);
        }
    }

    /// <summary>
    /// 根据配置播放动画
    /// </summary>
    /// <param name="animId"></param>
    public IAnimationEvent CrossFadeFixedAnimFromConfig(AnimId animId, float fixedTransitionDuration, float fixedTimeOffset = 0, float normalizedTransitionTime = 0)
    {
        var animConfig = DataTables.GetAnimationConfig(animId.ToString());

        if (animConfig.IsLoad)
        {
            return LoadCrossFade(animConfig.LoadPath, fixedTransitionDuration, animConfig.layerIndex, fixedTimeOffset, normalizedTransitionTime);
        }
        else
        {
            return LoadCrossFade(animConfig.AnimationName, fixedTransitionDuration, animConfig.layerIndex, fixedTimeOffset, normalizedTransitionTime);
        }
    }

    protected void EnterTempClipState(int layer)
    {
        if (layer == 0)
        {
            PreAniState = CurAniState;
            CurAniState = PlayerAniState.TempClip;

            ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
            ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);
        }
    }

    #region PlayEmoteAnimation

    protected static readonly string AniPath = "Assets/Loadable/Animations/";
    protected static readonly string EffectPath = "Assets/Loadable/AnimationsExpress/";

    protected List<GameObject> uiEmoteExpressionGo;
    protected Dictionary<string, GameObject> uiEmoteExpressionGoDict = new();

    private List<PlayAniType> singleLoopAni = new List<PlayAniType>() { PlayAniType.SingleLoopStart, PlayAniType.SingleLooping, PlayAniType.SingleLoopEnd };
    protected virtual List<PlayAniType> GetSingleLoopAni()
    {
        return singleLoopAni;
    }




    /// <summary>
    /// 用于UI界面播放单人Emote
    /// </summary>
    /// <param name="emoteID"></param>
    public void PlaySingleEmoteForUICharacter(string emoteID, Action OnCompleteSingleEmote = null, bool isPlaySound = true, Action OnDownloadOver = null, Func<bool> loopNeedFinish = null)
    {
        PlaySingleEmoteByPlayerId(AccountDataManager.Inst.Uid,emoteID,OnCompleteSingleEmote,isPlaySound,OnDownloadOver,loopNeedFinish);
    }

    public void PlaySingleEmoteByPlayerId(string id,string emoteID, Action OnCompleteSingleEmote = null, bool isPlaySound = true, Action OnDownloadOver = null, Func<bool> loopNeedFinish = null)
    {
        playerId = id;
        SetSpecialEffectActive(false); // 播 emote 时隐藏特殊皮肤特效避免穿模（ResetEmoteForUICharacter 回到 idle 时恢复）；非特殊皮肤 no-op
        ClearExpression(uiEmoteExpressionGo);
        ResetAnimation();
        DownloadAnimationAB(emoteID, (success) =>
        {
            OnDownloadOver?.Invoke();
            if (!success) return;

            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
            if (emoAniDataList == null || emoAniDataList.Count == 0)
            {
                LoggerUtils.LogError($"获取Emote配置失败，请确认配置表上是否存在EmoteID : {emoteID}");
                return;
            }

            bool isLoop = emoAniDataList.Count > 1;

            if (isLoop)
            {
                var anis = GetSingleLoopAni();
                var emoAniConfig = PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(), anis[0], anis[1], (aniConfig) =>
                {
                    var expression = CreateExpression(aniConfig);
                    SetUIEmoteExpression(expression);

                    return uiEmoteExpressionGo;
                }, isPlaySound, loopNeedFinish);
                uiEmoteExpressionGo = CreateExpression(emoAniConfig);
            }
            else
            {
                var emoAniConfig = emoAniDataList[0].ConvertToAniConfig();
                uiEmoteExpressionGo = CreateExpression(emoAniConfig);
                if (emoAniDataList[0].randomCount > 0)
                {
                    int random = emoAniDataList[0].randomCount;
                    int randomResult = random == 0 ? 0 : Random.Range(1, random + 1);
                    SetRandomMove(uiEmoteExpressionGo, emoAniConfig.randomTexPath, randomResult);
                }
                PlayConfigAni(emoAniConfig, () =>
                {
                    ResetEmoteForUICharacter();
                    OnCompleteSingleEmote?.Invoke();
                }, isPlaySound);
            }
        });
    }

    public void PlayerChangeClothesForUICharacer(Action OnCompleteSingleEmote = null, bool isPlaySound = true)
    {
        playerId = AccountDataManager.Inst.Uid;
        var featAniConfig = DataTables.GetFeatAniConfigList().Find((aniConfig) => aniConfig.StateID == (int)PlayerState.ChangeClothesAni).ConvertToAniConfig();
        uiEmoteExpressionGo = CreateExpression(featAniConfig);
        PlayConfigAni(featAniConfig, () =>
        {
            ResetEmoteForUICharacter();
            OnCompleteSingleEmote?.Invoke();
        }, isPlaySound);
    }

    public void PlayerChangeVehicleForUICharacer(Action OnCompleteSingleEmote = null, bool isPlaySound = true)
    {
        playerId = AccountDataManager.Inst.Uid;
        var featAniConfig = DataTables.GetFeatAniConfigList().Find((aniConfig) => aniConfig.StateID == (int)PlayerState.ChangeVehicleAni).ConvertToAniConfig();
        uiEmoteExpressionGo = CreateExpression(featAniConfig);
        StartCoroutine(ResetEmoteExpressionGo());
    }
    IEnumerator ResetEmoteExpressionGo()
    {
        yield return new WaitForSeconds(2f);
        ResetEmoteForUICharacter();
    }
    public void SetUIEmoteExpression(List<GameObject> expression)
    {
        if (expression != null && uiEmoteExpressionGo != null)
        {
            expression.ForEach(e => uiEmoteExpressionGo.Remove(e));
        }
        ClearExpression(uiEmoteExpressionGo);
        uiEmoteExpressionGo = expression;
    }

    protected readonly PlayAniType[] DoublePlayerA = new PlayAniType[] { PlayAniType.DoubleStart, PlayAniType.DoublePlayerA, PlayAniType.DoubleStart, PlayAniType.DoubleLoop };
    protected readonly PlayAniType[] DoublePlayerB = new PlayAniType[] { PlayAniType.DoublePlayerB };

    protected readonly PlayAniType[] DoubleLoopPlayerA = new PlayAniType[] { PlayAniType.DoubleStart, PlayAniType.DoubleLoopPlayerAStart, PlayAniType.DoubleLoopPlayerALoop };
    protected readonly PlayAniType[] DoubleLoopPlayerB = new PlayAniType[] { PlayAniType.DoubleLoopPlayerBStart, PlayAniType.DoubleLoopPlayerBLoop };

    /// <summary>
    /// 用于UI界面播放双人Emote
    /// </summary>
    /// <param name="emoteID"></param>
    /// <param name="playerBAnimCtrl"></param>
    /// <param name="OnCompleteDoubleEmote"></param>
    /// <param name="isPlaySound"></param>
    public void PlayDoubleEmoteForUICharacter(string emoteID, PlayerAnimationCtrl playerBAnimCtrl, Action OnCompleteDoubleEmote = null, bool isPlaySound = true, Action OnDownloadOver = null, Func<bool> loopNeedFinish = null)
    {
        SetSpecialEffectActive(false); // 播 emote 时隐藏特殊皮肤特效避免穿模；非特殊皮肤 no-op
        playerId = AccountDataManager.Inst.Uid;
        playerBAnimCtrl.SetPlayerID(AccountDataManager.Inst.Uid);

        ClearExpression(uiEmoteExpressionGo);
        ResetAnimation();

        DownloadAnimationAB(emoteID, (success) =>
        {
            OnDownloadOver?.Invoke();
            if (!success) return;

            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
            if (emoAniDataList == null || emoAniDataList.Count == 0)
            {
                LoggerUtils.LogError($"获取Emote配置失败，请确认配置表上是否存在EmoteID : {emoteID}");
                return;
            }

            var aniConfigList = emoAniDataList.ConvertToAniConfig();

            bool isLoop = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerALoop.ToString())) != null;
            if (isLoop)
            {
                PlayADoubleEmoteList(aniConfigList, DoubleLoopPlayerA, PlayAniType.DoubleLoopPlayerAStart, () =>
                {
                    playerBAnimCtrl.PlayBDoubleEmoteList(transform, aniConfigList, DoubleLoopPlayerB, OnCompleteDoubleEmote, isPlaySound);
                }, isPlaySound, loopNeedFinish);
            }
            else
            {
                PlayADoubleEmoteList(aniConfigList, DoublePlayerA, PlayAniType.DoublePlayerA, () =>
                {
                    playerBAnimCtrl.PlayBDoubleEmoteList(transform, aniConfigList, DoublePlayerB, OnCompleteDoubleEmote, isPlaySound);
                }, isPlaySound, loopNeedFinish);
            }
        });
    }

    private Coroutine specialAnimCoroutine;

    public void StopSpecialIdleAnim() {
        if (specialAnimCoroutine != null) {
            StopCoroutine(specialAnimCoroutine);
            specialAnimCoroutine = null;
        }

        if (string.IsNullOrEmpty(specialAnimPgcId)) {
            ClearOverrideSpecialAnim();
            return;
        }

        CheckAndOverrideSpecialAnim();

    }
    void CreateKCC(string kcc)
    {
    }
    // 特效(specialAnimRoot)显隐：播 emote 时隐藏避免穿模，播特殊动作/idle 时显示。
    // 仅 UI 预览实例(UIPreviewIdleMode)生效——这些 emote/idle 方法局内(AIYandere/OCTheatre/乐器等)也会调用，必须隔离，避免改动局内特效行为。
    public void SetSpecialEffectActive(bool show)
    {
        if (!UIPreviewIdleMode) return;
        if (specialAnimRoot != null) specialAnimRoot.SetActive(show);
    }

    // 供不经标准 emote 方法的播放路径（如 UGC 表情 UgcIdleBehaviour）调用：标记进入 emote 并隐藏特效；
    // 恢复由 ResetEmoteForUICharacter / ApplyUIPreviewIdleOverride 统一处理。仅 UI 预览生效，非特殊皮肤 no-op。
    public void NotifyUIEmoteBegin()
    {
        if (!UIPreviewIdleMode) return;
        _uiEmotePlaying = true;
        SetSpecialEffectActive(false);
    }

    // 特效(重新)创建/恢复后立即切 preview_idle（控制器没有则回退 idle）。
    // 仅 UI 预览实例生效；局内 UIPreviewIdleMode=false → no-op，特效维持 base，零影响。
    public void DriveSpecialEffectPreviewIdle()
    {
        if (!UIPreviewIdleMode) return;
        if (specialAnimRoot == null) return;
        var effectAnim = specialAnimRoot.GetComponentInChildren<Animator>(true);
        if (effectAnim != null)
        {
            // 虾虾崽 huhuyun 预览：本体 "idle" 已被 PreferExhibitIdleForPreview 改成 preview_idle_exhibit，
            // 云这里也直接进 idle_exhibit（PlayEffectPreferPreview 内部优先 preview_idle_exhibit），避免刚加载完先播一遍 preview_idle 再切换造成两边不同步。
            PlayEffectPreferPreview(effectAnim, PreferExhibitIdleForPreview ? "idle_exhibit" : "idle");
            // 若刚 SetActive 激活：同帧 Play 会被 Animator rebind 的默认 state 覆盖，立即 Update(0) 强制应用该 state
            if (effectAnim.gameObject.activeInHierarchy) effectAnim.Update(0f);

            // 云是异步加载的，加载完成时本体 idle 已经播了一段。在云首次起跑的同一帧把本体当前 state 的 playback time 也重置到 0，让两边同帧从头开始。
            if (PreferExhibitIdleForPreview && m_Animator != null && m_Animator.gameObject.activeInHierarchy)
            {
                m_Animator.Update(0f);
                var info = m_Animator.GetCurrentAnimatorStateInfo(0);
                m_Animator.Play(info.fullPathHash, 0, 0f);
                m_Animator.Update(0f);
            }
        }
    }

    public void PlaySpecialAnimForUICharacter(SpecialAnim anim, Action<SpecialAnimCameraInfo> updateCameraCallBack = null) {
            if (string.IsNullOrEmpty(specialAnimPgcId)) {
                ClearOverrideSpecialAnim();
                if (specialAnimCoroutine != null) {
                    StopCoroutine(specialAnimCoroutine);
                    specialAnimCoroutine = null;
                }
                return;
            }
            if (specialAnimCoroutine != null) {
                StopCoroutine(specialAnimCoroutine);
                specialAnimCoroutine = null;
            }
            CheckAndOverrideSpecialAnim();
            SetSpecialEffectActive(true); // 播特殊动作/idle 时确保特效显示（emote 期间会被隐藏）
        var specialSkinConfig = DataTables.GetSpecialSkinConfig(specialAnimPgcId);
            var avatarConfigData = Es.DataTables.GetAvatarData(specialAnimPgcId);
            var type = 10028;
            var classType = UniqueType.GetAvatar(specialAnimPgcId);
            SpecialAnimCameraInfo cameraInfo = null;
        try
        {
            Animator pgcAnimator = specialAnimRoot?.GetComponentInChildren<Animator>(true);
            CreateKCC(specialSkinConfig.KCC);
            var switchEventName = "";
            var pgcAnimName = "";
            switch (anim)
            {
                case SpecialAnim.Idle:
                    SetPlayerState(PlayerState.Default);
                    SetPressJoystickTime(0.0f);
                    CheckAndPlayIdle(pgcAnimator, specialSkinConfig, updateCameraCallBack, out switchEventName, out pgcAnimName, out cameraInfo);
                    if (specialSkinConfig.specialHandlePos == 1)
                    {
                        Wrap.Move(type, specialSkinConfig.idleItemPos);
                        Wrap.Rotate(type, specialSkinConfig.idleItemRot);
                    }
                    break;
                case SpecialAnim.Run:
                    SetPlayerState(PlayerState.Default);
                    SetPressJoystickTime(0.0f);
                    SetPlayerAniState(PlayerAniState.Run);
                    switchEventName = specialSkinConfig.previewRunAnimInfo.audio;
                    pgcAnimName = "run";
                    cameraInfo = specialSkinConfig.previewRunCameraInfo;
                    OverrideUIPreviewClip(SpecialAnim.Run.GetString(), specialSkinConfig.previewRunAnimInfo.anim);
                    if (specialSkinConfig.specialHandlePos == 1)
                    {
                        Wrap.Move(type, specialSkinConfig.runItemPos);
                        Wrap.Rotate(type, specialSkinConfig.runItemRot);
                    }
                    break;
                case SpecialAnim.FastRun:
                    SetPlayerState(PlayerState.Default);
                    SetPressJoystickTime(2.5f);
                    SetPlayerAniState(PlayerAniState.Run);
                    switchEventName = specialSkinConfig.previewFastRunAnimInfo.audio;
                    pgcAnimName = "fast_run";
                    cameraInfo = specialSkinConfig.previewFastRunCameraInfo;
                    OverrideUIPreviewClip(SpecialAnim.FastRun.GetString(), specialSkinConfig.previewFastRunAnimInfo.anim);
                    if (specialSkinConfig.specialHandlePos == 1)
                    {
                        Wrap.Move(type, specialSkinConfig.runFastItemPos);
                        Wrap.Rotate(type, specialSkinConfig.runFastItemRot);
                    }
                    break;
                case SpecialAnim.Jump:
                    SetPlayerState(PlayerState.Leisure);
                    SetPlayerAniState(PlayerAniState.Jump);
                    switchEventName = specialSkinConfig.previewJumpAnimInfo.audio;
                    pgcAnimName = "preview_jump";
                    cameraInfo = specialSkinConfig.previewJumpCameraInfo;
                    OverrideUIPreviewClip(SpecialAnim.Jump.GetString(), specialSkinConfig.previewJumpAnimInfo.anim);
                    if (specialSkinConfig.specialHandlePos == 1)
                    {
                        Wrap.Move(type, specialSkinConfig.idleItemPos);
                        Wrap.Rotate(type, specialSkinConfig.idleItemRot);
                    }
                    break;
            }
            // Idle 情况下特效的 idle / idle_exhibit 完全由 RandomSpecialIdleAnim 协程驱动；
            // 此处若再 Play 一次 "idle"，当 delayTime≈0、协程首帧已同步播了 idle_exhibit 时，会把特效打回 idle，
            // 造成本体演 idle_exhibit、特效却停在 idle 的错位。故 Idle 跳过入口播放，交由协程独占驱动以保证对齐。
            if (anim != SpecialAnim.Idle && pgcAnimator != null && !string.IsNullOrEmpty(pgcAnimName))
            {
                PlayEffectPreferPreview(pgcAnimator, pgcAnimName);
            }
            else if (anim == SpecialAnim.Idle && specialAnimCoroutine == null && pgcAnimator != null)
            {
                // 兜底：Idle 路径下协程没能启动（试穿房点击穿戴时 Character(Clone) 短暂 inactive，
                // CheckAndPlayIdle 的 activeInHierarchy 守卫跳过 StartCoroutine），云端就没人驱动 preview_idle_exhibit。
                // 这里直接 PlayEffectPreferPreview 兜底，让云至少进入正确的 state；
                // 等 Animator 重新 active 时会按 Play 排队的 state 正确播放。
                PlayEffectPreferPreview(pgcAnimator, PreferExhibitIdleForPreview ? "idle_exhibit" : "idle");
            }
            AkSoundManager.Inst.StopAll(gameObject);
            if (!string.IsNullOrEmpty(switchEventName))
            {
                var soundVer = specialSkinConfig.SoundVersion;
                var grp = $"Emote_Group_{soundVer}";
                var evt = $"Play_Emote_{soundVer}_1P";
                if (PreferExhibitIdleForPreview && isActiveAndEnabled)
                {
                    // huhuyun 路径串行化：cancel 上一个，启动新协程等一帧再 SetSwitch+PostEvent，让 Wwise 在两次请求之间有时间处理，
                    // 避免连续切按钮（走/跑）时前一个 SetSwitch 被后一个覆盖、两次 PostEvent 都读到最新 state（都播 Fast）
                    if (_previewAudioCoroutine != null) StopCoroutine(_previewAudioCoroutine);
                    _previewAudioCoroutine = StartCoroutine(PlayPreviewAudioNextFrame(grp, switchEventName, evt));
                }
                else
                {
                    // 非预览路径，或者预览路径但对象处于 inactive（试穿房穿戴特殊道具时 Character(Clone) 会短暂 SetActive(false) 重挂部件）—
                    // 此时无法 StartCoroutine，直接同步播放避免抛 "GameObject is inactive" 错误。
                    AkSoundManager.Inst.PlaySound(grp, switchEventName, evt, gameObject);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }
            updateCameraCallBack?.Invoke(cameraInfo);
        
    }

    private IEnumerator PlayPreviewAudioNextFrame(string switchGroup, string switchStateName, string eventName)
    {
        // 等一帧让 Wwise 把上一次的 SetSwitch+PostEvent 渲染完，再触发新的，避免 switch state 被覆盖
        yield return null;
        AkSoundManager.Inst.PlaySound(switchGroup, switchStateName, eventName, gameObject);
        _previewAudioCoroutine = null;
    }

    // 播放特殊待机
    private void CheckAndPlayIdle(Animator pgcAnimator, SpecialSkinConfig specialSkinConfig, Action<SpecialAnimCameraInfo> updateCameraCallBack,  out string eventName, out string pgcAnimName, out SpecialAnimCameraInfo cameraInfo) {
        if (specialAnimCoroutine != null) {
            StopCoroutine(specialAnimCoroutine);
            specialAnimCoroutine = null;
        }
    
        eventName = specialSkinConfig.previewIdleAnimInfo.audio;
        pgcAnimName = "idle";
        cameraInfo = specialSkinConfig.previewIdleCameraInfo;
      //  if (specialSkinConfig.exhibitIdleAnimInfo != null && !string.IsNullOrEmpty(specialSkinConfig.exhibitIdleAnimInfo.anim) && gameObject.activeInHierarchy) {
        if ( gameObject.activeInHierarchy)
            {
            //       var previewAnimClip = specialPgcAniDic[SpecialAnim.Idle.GetString()];
            float remaintime = 0;
            // PreferExhibitIdleForPreview 路径下不要等云的当前 state 跑完一个 loop，否则刚装备完云会停在 controller 默认 "idle"
            // state 等满 1.6s 才进 exhibit。直接以 0 启动，协程首轮立刻 PlayEffectPreferPreview 把云切到 preview_idle_exhibit。
            if(pgcAnimator != null && !PreferExhibitIdleForPreview)
            {
                AnimatorStateInfo info = pgcAnimator.GetCurrentAnimatorStateInfo(0);
                remaintime = info.length * (1 - info.normalizedTime);
               // Debug.LogError("lefttime =" + remaintime);
            }


            specialAnimCoroutine = StartCoroutine(RandomSpecialIdleAnim(pgcAnimator, specialSkinConfig, updateCameraCallBack, remaintime));
        }
    }

    private IEnumerator RandomSpecialIdleAnim(Animator pgcAnimator, SpecialSkinConfig specialSkinConfig, Action<SpecialAnimCameraInfo> updateCameraCallBack, float delayTime) {
        // UI 普通 idle 改用 preview 专用动画：此协程只在 UI 预览路径运行，局内待机走 BaseSpecialKCC，互不影响。
        // 进协程加载一次缓存；previewIdleAnimInfo.anim 为空则回退 base idle，避免没配 preview 的皮肤异常。
        AnimationClip uiIdleClip = null;
        if (!string.IsNullOrEmpty(specialSkinConfig.previewIdleAnimInfo.anim)) {
            uiIdleClip = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + specialSkinConfig.previewIdleAnimInfo.anim, gameObject);
        }
        if (uiIdleClip == null) {
            uiIdleClip = specialPgcAniDic[SpecialAnim.Idle.GetString()];
        }
        // 在等待 delayTime 之前先立即把本体切到 preview idle，避免加载/切换时等待期间露出 base idle。
        // 声音/相机/特效仍由下方循环首轮处理；这里只负责本体显示，不改 delayTime 语义。
        // PreferExhibitIdleForPreview 路径（huhuyun 等）下不覆盖到 preview_idle 片段 ——
        // CheckAndOverrideSpecialAnim 已把 idle slot 设成 exhibit clip，跟云端 preview_idle_exhibit state 一致；
        // 这里若再覆盖成 preview_idle，会在 0 ~ delayTime 期间让本体先播 preview_idle 然后在 delayTime 时刻又切到 exhibit，
        // 视觉上就是用户说的"几秒后动画不同步"。保留 state/trigger 仍能让 mecanim 进入 idle 态，不影响"切换到待机"路径。
        if (uiIdleClip != null) {
            if (!PreferExhibitIdleForPreview) {
                OverrideAnimationClip(SpecialAnim.Idle.GetString(), uiIdleClip);
            }
            PreAniState = CurAniState;
            CurAniState = PlayerAniState.Idle;
            ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
            ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
            ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);
        }
        if (delayTime > float.Epsilon) {
            yield return new WaitForSeconds(delayTime);
        }
        bool playPreview = false;
        bool firstRound = true; // 首轮强制普通 idle，避免一加载就播 idle_exhibit
        while (true) {
            //if (CurAniState != PlayerAniState.Idle) {
            //    specialAnimCoroutine = null;
            //    yield break;
            //}
            CheckAndOverrideSpecialAnim();
            float random = UnityEngine.Random.Range(0, 100);
            // 虾虾崽 huhuyun：永远走 exhibit 分支（else），不走 preview_idle 分支，避免按一下 idle 按钮后又回到 idle/idle_exhibit 循环切换
            if (!PreferExhibitIdleForPreview && (firstRound || random > 90)) {
                firstRound = false;
                
                if (specialSkinConfig.KCC == "ZongziKCC")
                {
                    specialAnimRoot.SetActive(false);
                }
                var previewAnimClip = uiIdleClip;
                var pgcAnimName = "idle";
                var switchEventName = specialSkinConfig.previewIdleAnimInfo.audio;
                if (pgcAnimator != null && !string.IsNullOrEmpty(pgcAnimName)) {
                    PlayEffectPreferPreview(pgcAnimator, pgcAnimName);
                    OverrideAnimationClip(SpecialAnim.Idle.GetString(), previewAnimClip);
                    ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);
                    PreAniState = CurAniState;
                    CurAniState = PlayerAniState.Idle;
                    ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
                    ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);
                }
                AkSoundManager.Inst.StopAll(gameObject);
                if (!string.IsNullOrEmpty(switchEventName)) {
                    AkSoundManager.Inst.PlaySound($"Emote_Group_{specialSkinConfig.SoundVersion}", switchEventName, $"Play_Emote_{specialSkinConfig.SoundVersion}_1P", gameObject);
                }
                updateCameraCallBack?.Invoke(specialSkinConfig.previewIdleCameraInfo);
                playPreview = false;
                yield return new WaitForSeconds(previewAnimClip.length);
            } else {
                // PreferExhibitIdleForPreview 路径下没有 preview_idle 分支与之交替，不能用 playPreview 早退（否则永远 continue 死循环）
                if (!PreferExhibitIdleForPreview && playPreview) {
                    continue;
                }
                var pgcAnimName = "idle_exhibit";
                if (specialSkinConfig.KCC == "ZongziKCC")
                {
                    specialAnimRoot.SetActive(false);
                    specialAnimRoot.SetActive(true);
                    pgcAnimName = "preview_idle";
                }
                // 展示态(idle_exhibit) UI 专用：优先 previewexhibitIdleAnimInfo，没配则回退共用的 exhibitIdleAnimInfo（局内仍用 exhibit）
                bool useUIExhibit = !string.IsNullOrEmpty(specialSkinConfig.previewexhibitIdleAnimInfo.anim);
                var exhibitAnim = useUIExhibit ? specialSkinConfig.previewexhibitIdleAnimInfo.anim : specialSkinConfig.exhibitIdleAnimInfo.anim;
                var exhibitIdleAnimClip = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + exhibitAnim, gameObject);
                if (exhibitIdleAnimClip == null) {
                    // 该皮肤没有可用展示动画：保持普通(preview) idle 循环，不要 yield break。
                    // 否则协程死掉、而本轮 CheckAndOverrideSpecialAnim 已把 idle 重置回 base，会卡死在 base idle。
                    if (uiIdleClip == null) {
                        specialAnimCoroutine = null;
                        yield break;
                    }
                    OverrideAnimationClip(SpecialAnim.Idle.GetString(), uiIdleClip);
                    playPreview = false;
                    yield return new WaitForSeconds(uiIdleClip.length);
                    continue;
                }
     
                if (specialSkinConfig.KCC == "HolyangeKCC" ||(specialSkinConfig.KCC == "GhostCatKCC" && specialSkinConfig.Id == "11400084")) //pgcAnimator是effect，所以还需要播翅膀动作
                {
                    var backRoot = Wrap.GetBandNode((int)BodyNode.SpecialBackDeckNode);
                    Animator  backRootAnimator = backRoot.GetComponentInChildren<Animator>();
                    backRootAnimator?.Play(pgcAnimName);
                }

                OverrideAnimationClip(SpecialAnim.Idle.GetString(), exhibitIdleAnimClip);
                ParameterAnimation.SetTrigger(PlayerAniPrameter.StateOn);

                PreAniState = CurAniState;
                CurAniState = PlayerAniState.Idle;
                ParameterAnimation.SetInteger(PlayerAniPrameter.PreAniState, (int)PreAniState);
                ParameterAnimation.SetInteger(PlayerAniPrameter.CurAniState, (int)CurAniState);

                if (pgcAnimator != null) {
                    // 特效展示态优先播 preview_<state>（preview_idle_exhibit），没有则回退 idle_exhibit；zongzi 的 preview_idle 不受影响
                    // 先 Update(0) 吃掉装备时 SetActive(true) 引起的 rebind 残留（mecanim 还在 default state "idle"），再 Play 想要的 state，再 Update(0) 应用
                    if (pgcAnimator.gameObject.activeInHierarchy) pgcAnimator.Update(0f);
                    PlayEffectPreferPreview(pgcAnimator, pgcAnimName);
                    if (pgcAnimator.gameObject.activeInHierarchy) pgcAnimator.Update(0f);
                }
                if(specialAnimRoot != null)
                {
                    var pgcAnimatorOnEffect = specialAnimRoot.GetComponentInChildren<Animator>(true);
                    if(pgcAnimatorOnEffect != null && pgcAnimatorOnEffect != pgcAnimator)
                    {
                        if (pgcAnimatorOnEffect.gameObject.activeInHierarchy) pgcAnimatorOnEffect.Update(0f);
                        PlayEffectPreferPreview(pgcAnimatorOnEffect, pgcAnimName);
                        if (pgcAnimatorOnEffect.gameObject.activeInHierarchy) pgcAnimatorOnEffect.Update(0f);
                    }
                }
                // PreferExhibitIdleForPreview 路径下显式把本体切到 "idle" state（AOC 已经把 idle slot 改成 exhibit clip），
                // 不依赖 SetTrigger(StateOn) 的延迟 mecanim 处理 —— 用户从 Run/Jump 切回 Idle 时，SetTrigger 处理时机
                // 不可控可能让本体停在原 state；直接 Play("idle", 0, 0f) 强制切到 idle state 同时跟云回到 time 0 对齐。
                if (PreferExhibitIdleForPreview && m_Animator != null && m_Animator.gameObject.activeInHierarchy)
                {
                    m_Animator.Play(SpecialAnim.Idle.GetString(), 0, 0f);
                    m_Animator.Update(0f);
                }
                var switchEventName = useUIExhibit ? specialSkinConfig.previewexhibitIdleAnimInfo.audio : specialSkinConfig.exhibitIdleAnimInfo.audio;
                AkSoundManager.Inst.StopAll(gameObject);
                if (!string.IsNullOrEmpty(switchEventName)) {
                    AkSoundManager.Inst.PlaySound($"Emote_Group_{specialSkinConfig.SoundVersion}", switchEventName, $"Play_Emote_{specialSkinConfig.SoundVersion}_1P", gameObject);
                }
                updateCameraCallBack?.Invoke(useUIExhibit ? specialSkinConfig.previewexhibitIdleCameraInfo : specialSkinConfig.exhibitIdleCameraInfo);
                playPreview = true;
                yield return new WaitForSeconds(exhibitIdleAnimClip.length);
            }
        }

    }

    public void PlayLinkEmoteForUICharacter(string emoteID, SpecialAnim animType,PlayerAnimationCtrl playerBAnimCtrl,Action OnDownloadOver = null)
    {
        SetSpecialEffectActive(false); // 播 emote 时隐藏特殊皮肤特效避免穿模；非特殊皮肤 no-op
        playerId = AccountDataManager.Inst.Uid;
        playerBAnimCtrl.SetPlayerID(AccountDataManager.Inst.Uid);

        ClearExpression(uiEmoteExpressionGo);
        ResetAnimation();
        AkSoundManager.Inst.StopAll(gameObject);
        DownloadAnimationAB(emoteID, (success) =>
        {
            OnDownloadOver?.Invoke();
            if (!success) return;

            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
            if (emoAniDataList == null || emoAniDataList.Count == 0)
            {
                LoggerUtils.LogError($"获取Emote配置失败，请确认配置表上是否存在EmoteID : {emoteID}");
                return;
            }

            var aniConfigList = emoAniDataList.ConvertToAniConfig();

            PlayAniType aniTypeA = PlayAniType.LinkIdleA;
            PlayAniType aniTypeB = PlayAniType.LinkIdleB;

            if (animType == SpecialAnim.Jump)
            {
                aniTypeA = PlayAniType.PreviewJumpA;
                aniTypeB = PlayAniType.PreviewJumpB;
            }
            else if(animType == SpecialAnim.Run)
            {
                aniTypeA = PlayAniType.LinkRunA;
                aniTypeB = PlayAniType.LinkRunB;
            }
            else if (animType == SpecialAnim.FastRun)
            {
                aniTypeA = PlayAniType.LinkRunFastA;
                aniTypeB = PlayAniType.LinkRunFastB;
            }


            var emoAniConfigA = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(aniTypeA.ToString()));
            var emoAniConfigB = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(aniTypeB.ToString()));


            var expression = CreateExpression(emoAniConfigA);
            if (expression != null)
            {
                SetUIEmoteExpression(expression);
            }

            PlayLoopAni(emoAniConfigA, uiEmoteExpressionGo, true);

            if (emoAniConfigB != null)
            {
                playerBAnimCtrl.gameObject.SetActive(true);
                var pos = transform.position + transform.TransformDirection(emoAniConfigB.interactPos / 1.7f * transform.lossyScale.x);
                var interactRot = emoAniConfigB.interactRot - new Vector3(0, 180, 0);//场景内配置和UI配置差了180度
                var rot = Quaternion.LookRotation(-transform.forward) * Quaternion.Euler(interactRot);
                playerBAnimCtrl.transform.SetPositionAndRotation(pos, rot);
                playerBAnimCtrl.PlayLoopAni(emoAniConfigB,uiEmoteExpressionGo,false);
            }
        });
    }

    /// <summary>
    /// 重置Emote动画，默认恢复到idle动画
    /// </summary>
    /// <param name="isLoadPlay"></param>
    /// <param name="stateName"></param>
    public void ResetEmoteForUICharacter()
    {
        ClearExpression(uiEmoteExpressionGo);
        ResetAnimation();
        Play("idle");
        SetPlayerAniState(PlayerAniState.Idle);
        // emote 结束回到 idle：恢复特殊皮肤特效并重新驱动 preview（非特殊皮肤 specialAnimRoot 为 null → no-op）
        SetSpecialEffectActive(true);
        DriveSpecialEffectPreviewIdle();
    }
    public void SetSpecialActivity(bool isShow)
    {
        if(specialAnimPgcId!= "11000238" && specialAnimPgcId!= "11400081")
        {
            return;
        }
        if (!gameObject.activeInHierarchy)
        {
            // 如果对象未激活，直接设置
            Animator pgcAnimator = specialAnimRoot?.GetComponentInChildren<Animator>(true);
            if (pgcAnimator != null)
            {
                pgcAnimator.gameObject.SetActive(isShow);
            }
            return;
        }
        StartCoroutine(FindSpeacialProp(isShow));
    }
    IEnumerator FindSpeacialProp(bool isShow)
    {
        float timeout = 5f;
        float timer = 0;
        Animator pgcAnimator = specialAnimRoot?.GetComponentInChildren<Animator>(true);
        while(pgcAnimator == null && timer < timeout)
        {
            yield return new WaitForSeconds(1f);
            pgcAnimator = specialAnimRoot?.GetComponentInChildren<Animator>(true);
            timer += 1;
        }
        if (pgcAnimator != null){
            pgcAnimator.gameObject.SetActive(isShow);
        }
        else
        {
            Debug.LogError(gameObject.name);
        }
    }
    protected string actionEmoteId;
    protected int downloadNum;
    protected bool downloadResult;
    protected Action<bool> downloadCallBack;
    public void DownloadAnimationAB(string emoteId, Action<bool> action)
    {
        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
        if (emoAniDataList.Count == 0)
        {
            action?.Invoke(false);
            return;
        }

        var hashEmoteId = emoteId + DateTime.Now.ToLongTimeString();
        downloadNum = 0;

        foreach (var config in emoAniDataList)
        {
            if (!string.IsNullOrEmpty(config.bodyPath))
            {
                var aniPath = AniPath + config.bodyPath + ".anim";
                if (!Assets.IsDownloaded(aniPath))
                {
                    var aniWrapper = Loader.LoadAsync<AnimationClip>(aniPath);
                    aniWrapper.completed = (success) =>
                    {
                        OnDownloadCallBack(hashEmoteId, success);
                    };
                    downloadNum++;
                }
            }
        }

        foreach (var config in emoAniDataList)
        {
            if (config.effectPath != null && config.effectPath.Count > 0)
            {
                var effectPath = EffectPath + config.effectPath[0] + ".prefab";
                if (!Assets.IsDownloaded(effectPath))
                {
                    var wrapper = Loader.LoadAsync<GameObject>(effectPath);
                    wrapper.completed = (success) =>
                    {
                        OnDownloadCallBack(hashEmoteId, success);
                    };
                    downloadNum++;
                }
                break;
            }
        }

        if (downloadNum == 0)
        {
            action?.Invoke(true);
            return;
        }

        actionEmoteId = hashEmoteId;
        downloadCallBack = action;
        downloadResult = true;
    }

    public void DownloadAnimationAB(List<PlayerAniConfig> emoAniDataList, Action<bool> action)
    {
        if (emoAniDataList.Count == 0)
        {
            action?.Invoke(false);
            return;
        }

        var emoAniData = emoAniDataList.FirstOrDefault();
        string emoteId = emoAniData.aniId;
        var hashEmoteId = emoteId + DateTime.Now.ToLongTimeString();
        downloadNum = 0;

        foreach (var config in emoAniDataList)
        {
            if (!string.IsNullOrEmpty(config.bodyPath))
            {
                var aniPath = AniPath + config.bodyPath + ".anim";
                if (!Assets.IsDownloaded(aniPath))
                {
                    var aniWrapper = Loader.LoadAsync<AnimationClip>(aniPath);
                    aniWrapper.completed = (success) =>
                    {
                        OnDownloadCallBack(hashEmoteId, success);
                    };
                    downloadNum++;
                }
            }
        }

        foreach (var config in emoAniDataList)
        {
            if (config.effectPath != null && config.effectPath.Count > 0)
            {
                var effectPath = EffectPath + config.effectPath[0] + ".prefab";
                if (!Assets.IsDownloaded(effectPath))
                {
                    var wrapper = Loader.LoadAsync<GameObject>(effectPath);
                    wrapper.completed = (success) =>
                    {
                        OnDownloadCallBack(hashEmoteId, success);
                    };
                    downloadNum++;
                }
                break;
            }
        }

        if (downloadNum == 0)
        {
            action?.Invoke(true);
            return;
        }

        actionEmoteId = hashEmoteId;
        downloadCallBack = action;
        downloadResult = true;
    }

    protected void OnDownloadCallBack(string emoteId, bool success)
    {
        // 异步回调触发时检查 MonoBehaviour 是否已被销毁，避免在无效对象上继续执行
        if (this == null)
            return;

        if (emoteId != actionEmoteId) return;
        downloadNum--;
        downloadResult = downloadResult && success;
        if (downloadNum == 0) downloadCallBack?.Invoke(downloadResult);
    }

    internal void PlayADoubleEmoteList(List<PlayerAniConfig> aniConfigList, PlayAniType[] playList, PlayAniType startPlayerBAniType, Action OnStartPlayerB, bool isPlaySound = true, Func<bool> loopNeedFinish = null)
    {
        PlayDoubleEmote(aniConfigList, playList, 0, (index) =>
        {
            if (playList.Length == index)
            {
                var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playList[playList.Length - 1].ToString()));
                PlayLoopAni(emoAniConfig, uiEmoteExpressionGo, false,loopNeedFinish);

                return false;
            }
            else
            {
                if (playList[index] == startPlayerBAniType)
                {
                    OnStartPlayerB?.Invoke();
                }
                return true;
            }
        }, isPlaySound);
    }

    internal void PlayPetADoubleEmoteList(List<PlayerAniConfig> aniConfigList, PlayAniType[] playList, bool isPlaySound = true)
    {
        PlayDoubleEmote(aniConfigList, playList, 0, (index) =>
        {
            if (playList.Length == index)
            {
                var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playList[playList.Length - 1].ToString()));
                if (emoAniConfig.aniType.Contains("Loop"))
                {
                    PlayLoopAni(emoAniConfig, uiEmoteExpressionGo, false);
                }
                else
                {
                    ResetEmoteForUICharacter();
                }
                return false;
            }
            else
            {
                return true;
            }
        }, isPlaySound);
    }

    internal void PlayBDoubleEmoteList(Transform playerATransform, List<PlayerAniConfig> aniConfigList, PlayAniType[] playList, Action OnCompleteDoubleEmote = null, bool isPlaySound = true)
    {
        PlayAniType playAniType = playList[0];
        var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playAniType.ToString()));
        if (emoAniConfig == null)
        {
            emoAniConfig = GetInteractAniConfig(aniConfigList, playAniType);
            if (emoAniConfig == null) return;
        }

        var pos = playerATransform.position + playerATransform.TransformDirection(emoAniConfig.interactPos / 1.7f * playerATransform.lossyScale.x);
        var rot = Quaternion.LookRotation(-playerATransform.forward) * Quaternion.Euler(emoAniConfig.interactRot);
        transform.SetPositionAndRotation(pos, rot);
        gameObject.SetActive(true);

        PlayDoubleEmote(aniConfigList, playList, 0, (index) =>
        {
            if (playList.Length == index)
            {
                var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playList[playList.Length - 1].ToString()));
                if (emoAniConfig.aniType.Contains("Loop"))
                {
                    PlayLoopAni(emoAniConfig, uiEmoteExpressionGo, false);
                }
                else
                {
                    gameObject.SetActive(false);
                    OnCompleteDoubleEmote?.Invoke();
                }

                return false;
            }
            else
            {
                return true;
            }
        }, isPlaySound);
    }

    internal void PlayPlayerADoubleEmoteList(Transform petATransform, List<PlayerAniConfig> aniConfigList, PlayAniType[] playList, Action OnCompleteDoubleEmote = null, bool isPlaySound = true, Func<bool> loopNeedFinish = null)
    {
        PlayAniType playAniType = playList[0];
        var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playAniType.ToString()));
        if (emoAniConfig == null)
        {
            emoAniConfig = GetInteractAniConfig(aniConfigList, playAniType);
            if (emoAniConfig == null) return;
        }

        var pos = petATransform.position + petATransform.TransformDirection(-emoAniConfig.interactPos / 1.7f *
            petATransform.lossyScale.x / petATransform.localScale.x);
        var rot = Quaternion.LookRotation(-petATransform.forward) * Quaternion.Euler(-emoAniConfig.interactRot);
        transform.SetPositionAndRotation(pos, rot);
        gameObject.SetActive(true);

        PlayDoubleEmote(aniConfigList, playList, 0, (index) =>
        {
            if (playList.Length == index)
            {
                var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playList[playList.Length - 1].ToString()));
                if (emoAniConfig.aniType.Contains("Loop"))
                {
                    PlayLoopAni(emoAniConfig, uiEmoteExpressionGo, false,loopNeedFinish);
                }
                else
                {
                    ResetEmoteForUICharacter();
                    gameObject.SetActive(false);
                    OnCompleteDoubleEmote?.Invoke();
                }

                return false;
            }
            else
            {
                return true;
            }
        }, isPlaySound);
    }

    internal void PlayPlayerADoubleEmoteListForHall(Transform petATransform, List<PlayerAniConfig> aniConfigList, PlayAniType[] playList, Action OnCompleteDoubleEmote = null, bool isPlaySound = true)
    {
        PlayAniType playAniType = playList[0];
        var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playAniType.ToString()));
        if (emoAniConfig == null)
        {
            emoAniConfig = GetInteractAniConfig(aniConfigList, playAniType);
            if (emoAniConfig == null) return;
        }

        var pos = transform.position + transform.TransformDirection(emoAniConfig.interactPos / 1.7f * transform.lossyScale.x);
        var rot = Quaternion.LookRotation(-transform.forward) * Quaternion.Euler(emoAniConfig.interactRot);
        petATransform.SetPositionAndRotation(pos, rot);
        gameObject.SetActive(true);

        PlayDoubleEmote(aniConfigList, playList, 0, (index) =>
        {
            if (playList.Length == index)
            {
                var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playList[playList.Length - 1].ToString()));
                if (emoAniConfig.aniType.Contains("Loop"))
                {
                    PlayLoopAni(emoAniConfig, uiEmoteExpressionGo, false);
                }
                else
                {
                    ResetEmoteForUICharacter();
                    gameObject.SetActive(false);
                    OnCompleteDoubleEmote?.Invoke();
                }

                return false;
            }
            else
            {
                return true;
            }
        }, isPlaySound);
    }

    protected void PlayDoubleEmote(List<PlayerAniConfig> aniConfigList, PlayAniType[] playList, int index, Func<int, bool> OnCompleteAni, bool isPlaySound)
    {
        PlayAniType playAniType = playList[index];
        var emoAniConfig = aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playAniType.ToString()));
        if (emoAniConfig == null)
        {
            emoAniConfig = GetInteractAniConfig(aniConfigList, playAniType);
            if (emoAniConfig == null) return;
        }

        var expression = CreateExpression(emoAniConfig);
        SetUIEmoteExpression(expression);

        PlayConfigAni(emoAniConfig, () =>
        {
            index++;
            if (OnCompleteAni.Invoke(index))
            {
                PlayDoubleEmote(aniConfigList, playList, index, OnCompleteAni, isPlaySound);
            }
        }, isPlaySound);
    }

    Dictionary<PlayAniType, PlayAniType> DefaultAniTypeConfig = new Dictionary<PlayAniType, PlayAniType>() { { PlayAniType.DoublePlayerB, PlayAniType.DoublePlayerA }, { PlayAniType.DoubleLoopPlayerBStart, PlayAniType.DoubleLoopPlayerAStart },
        { PlayAniType.DoubleLoopPlayerBLoop, PlayAniType.DoubleLoopPlayerALoop }, { PlayAniType.DoubleLoopPlayerBEnd, PlayAniType.DoubleLoopPlayerAEnd } };
    protected PlayerAniConfig GetInteractAniConfig(List<PlayerAniConfig> aniConfigList, PlayAniType playAniType)
    {
        if (DefaultAniTypeConfig.ContainsKey(playAniType))
        {
            playAniType = DefaultAniTypeConfig[playAniType];
            return aniConfigList.Find(aniConfig => aniConfig.aniType.Equals(playAniType.ToString()));
        }

        return null;
    }

    /// <summary>
    /// 根据配置播放动画
    /// </summary>
    /// <param name="animId"></param>
    public IAnimationEvent CrossFadeConfigAni(PlayerAniConfig aniConfigList, float normalizedTransitionDuration, Action onComplete = null, bool isPlaySound = true)
    {
        float bodyAniTime = CrossFadeBodyAni(aniConfigList, normalizedTransitionDuration, onComplete);
        if (isPlaySound)
            PlayEmoteSound(aniConfigList, bodyAniTime);

        if (!string.IsNullOrEmpty(aniConfigList.facePath))
        {
            PlayingFaceEmo = StartCoroutine(PlayFaceAni(aniConfigList, bodyAniTime));
        }
        else
        {
            // 没有面部动画还原正常眼部动画
            ResetFace();
        }
        return this;
    }

    public void PlayConfigAni(PlayerAniConfig aniConfigList, Action onComplete, bool isPlaySound = true)
    {
        if (aniConfigList == null)
        {
            onComplete?.Invoke();
            return;
        }

        float bodyAniTime = PlayBodyAni(aniConfigList, onComplete);
        if (isPlaySound)
            PlayEmoteSound(aniConfigList, bodyAniTime);

        // 不管有没有面部动画都要先还原正常面部动画
        ResetFace();
        if (!string.IsNullOrEmpty(aniConfigList.facePath))
        {
            PlayingFaceEmo = StartCoroutine(PlayFaceAni(aniConfigList, bodyAniTime));
        }
    }

    public PlayerAniConfig PlayConfigAni(List<PlayerAniConfig> aniConfigList, PlayAniType aniType, Action onComplete, bool isPlaySound = true)
    {
        var emoAniConfig = aniConfigList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(aniType.ToString()));

        PlayConfigAni(emoAniConfig, onComplete, isPlaySound);

        return emoAniConfig;
    }

    public PlayerAniConfig PlayConfigLoopAni(List<PlayerAniConfig> aniConfigList, PlayAniType startAniType, PlayAniType loopAniType, Func<PlayerAniConfig, List<GameObject>> onComplete, bool isPlaySound = true, Func<bool> loopNeedFinish = null)
    {
        return PlayConfigAni(aniConfigList, startAniType, () =>
        {
            var loopAniConfig = aniConfigList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(loopAniType.ToString()));

            PlayLoopAni(loopAniConfig, onComplete(loopAniConfig), isPlaySound, loopNeedFinish);
        }, isPlaySound);
    }

    protected void PlayLoopAni(PlayerAniConfig loopAniConfig, List<GameObject> expressionGameObject, bool isPlaySound = true, Func<bool> loopNeedFinish = null)
    {
        if (loopAniConfig == null) return;
        SetSpecialActivity(false);
        string loopAniPath = AniPath + loopAniConfig.bodyPath;
        float bodyAniTime = PlayBodyAni(loopAniConfig, () =>
        {
            if (loopNeedFinish != null)
            {
                if (loopNeedFinish())
                {
                    return;
                }
            }
            PlayLoopAni(loopAniConfig, expressionGameObject, false, loopNeedFinish);
        });
        if (isPlaySound)
            PlayEmoteSound(loopAniConfig, bodyAniTime);
        if (!string.IsNullOrEmpty(loopAniConfig.facePath))
        {
            StopEmoteCo();
            PlayingFaceEmo = StartCoroutine(PlayFaceAni(loopAniConfig, bodyAniTime));
        }

        if (expressionGameObject != null && expressionGameObject.Count > 0)
        {
            expressionGameObject.ForEach(x =>
            {
                PlayExpressionAnim(loopAniConfig, x);
            });
        }
    }

    public void ResetEmoteAnimation()
    {
        ResetAnimation();
        Play("idle");
        SetPlayerAniState(PlayerAniState.Idle);
    }

    protected void ResetAnimation()
    {
        StopEmoteSound();
        StopEmoteCo();
        ResetFace();
    }

    protected void ResetFace()
    {
        OnUGCPartsResetAnim?.Invoke();
        PlayCurEyeAni();
    }

    protected void OnDisable()
    {
        AkSoundManager.Inst.StopAll(gameObject);
    }

    public virtual void PlayCurEyeAni()
    {
        var eyePartData = Wrap.GetPartData(UniqueType.GetAvatar(AvatarSubType.Eyes));
        Wrap.RsetFaceMat();
        if (eyePartData == null || eyePartData.IsNull())
        {
            ClearClip(1);
            Wrap.RsetFaceMat();
            return;
        }
        var bData = DataTables.GetAvatarCommonData(eyePartData.Id);
        LoadPlay(bData?.aniPath, 1);

    }

    public void StopEmoteCo()
    {
        if (PlayingFaceEmo != null)
        {
            StopCoroutine(PlayingFaceEmo);
            PlayingFaceEmo = null;
        }
    }

    public EmoUIConfig GetEmoteUIConfig(string emoteId)
    {
        return Es.DataTables.GetEmoUIConfig(emoteId);
    }

    #region sound

    protected readonly string SwitchGrop = "Emote_Group";

    public void PlayEmoteSound(PlayerAniConfig aniConfig, float animTime = 1)
    {

        var soundName = aniConfig?.soundName;
        if (string.IsNullOrEmpty(soundName)) return;
        var soundVersion = aniConfig.soundVersion;
        string eventName = "Play_Emote_" + GetEventName();
        string switchGroup = SwitchGrop;
        if (!string.IsNullOrEmpty(soundVersion))
        {
            eventName = "Play_Emote_"+soundVersion + "_" + GetEventName();
            switchGroup = SwitchGrop + "_" + soundVersion;
        }
        AkSoundManager.Inst.StopAll(gameObject);
        AkSoundManager.Inst.PlaySound(switchGroup, soundName, eventName, gameObject);
        if (!GameAgentManager.Inst.IsInHallScene())
        {
            MessageHelper.Broadcast(MessageName.ReduceMusic, true);
        }

        if (animTime > 0)
        {
            TimerManager.Inst.RunOnce("ReduceMusic", animTime, () =>
            {
                MessageHelper.Broadcast(MessageName.RestoreMusic, true);
            });
        }

    }

    public void StopEmoteSound()
    {
        AkSoundManager.Inst.StopAll(gameObject);
        if (!GameAgentManager.Inst.IsInHallScene())
        {
            MessageHelper.Broadcast(MessageName.RestoreMusic, true);
        }
    }

    protected string GetEventName()
    {
        // TODO: 判断是否是自己
        if (playerId == AccountDataManager.Inst.Uid)
            return "1P";
        else
            return "3P";
    }
    #endregion

    #region body

    protected float PlayBodyAni(PlayerAniConfig aniConfig, Action onComplete)
    {
        if (aniConfig == null || string.IsNullOrEmpty(aniConfig.bodyPath))
        {
            onComplete?.Invoke();
            return 0;
        }
        SetSpecialActivity(false);
        string path = AniPath + aniConfig.bodyPath;
        var aniEvent = LoadPlay(path)?.OnCompleteEvent(() =>
        {
            onComplete?.Invoke();
        });
        if(aniEvent!=null)
            return aniEvent.AnimationTime;
        else
        {
            Debug.LogError("找不到IAnimationEvent ");
            return 3;
        }
    }

    protected float CrossFadeBodyAni(PlayerAniConfig aniConfig, float normalizedTransitionDuration, Action onComplete)
    {
        string path = AniPath + aniConfig.bodyPath;
        var aniEvent = LoadCrossFade(path, normalizedTransitionDuration).OnCompleteEvent(() =>
         {
             onComplete?.Invoke();
         });

        return aniEvent.AnimationTime;
    }

    #endregion

    #region face

    protected Coroutine PlayingFaceEmo;

    protected IEnumerator PlayFaceAni(PlayerAniConfig aniConfig, float bodyAniTime)
    {
        // 如果延迟时间为0的时候，还是跳过一帧导致触发一次默认眼睛动画
        if (aniConfig.faceDelayTime > 0)
        {
            yield return new WaitForSeconds(aniConfig.faceDelayTime);
        }

        float faceAniTime = PlayFaceAnim(aniConfig);

        if (aniConfig.faceEndTime != 0)
            faceAniTime = aniConfig.faceEndTime;

        if ((bodyAniTime - faceAniTime) > 0.01f)
        {
            yield return new WaitForSeconds(faceAniTime - aniConfig.faceDelayTime);
            PlayCurEyeAni();
            OnUGCPartsResetAnim?.Invoke();
            if (bodyAniTime - faceAniTime > 0)
            {
                yield return new WaitForSeconds(bodyAniTime - faceAniTime);
            }
        }
        else
        {
            yield return new WaitForSeconds(bodyAniTime - aniConfig.faceDelayTime);
        }

        PlayingFaceEmo = null;
    }

    protected float PlayFaceAnim(PlayerAniConfig aniConfig)
    {
        string facePath = AniPath + aniConfig.facePath;
        OnUGCPartsPlayAnim?.Invoke(aniConfig.effectAnimName);
        OnUGCPartsPlayAnim?.Invoke(aniConfig.facePath);
        var loadPlay = LoadPlay(facePath, 1);
        return loadPlay != null? loadPlay.AnimationTime : 0f;
    }

    #endregion

    #region effect

    public List<GameObject> CreateExpression(PlayerAniConfig aniConfig)
    {
        if (aniConfig == null || aniConfig.effectPath == null || aniConfig.effectPath.Count == 0) return null;

        // Wrap 在异步回调期间可能被置空（对象被销毁或重置），提前守卫避免 NullReferenceException
        if (Wrap == null)
        {
            LoggerUtils.LogError($"[PlayerAnimationCtrl] CreateExpression 调用时 Wrap 为空");
            return null;
        }

        List<GameObject> expressionGameObject = new List<GameObject>();

        for (int i = 0; i < aniConfig.effectPath.Count; i++)
        {
            string path = aniConfig.effectPath[i];
            GameObject movePrefab = LoadEffectPrefab(path);
            if (movePrefab != null && gameObject.activeSelf)
            {
                expressionGameObject.Add(movePrefab);
                movePrefab.SetActive(true);

                // 使用 movePrefab 而非 expressionGameObject[i]，避免因前序 prefab 加载失败导致索引错位
                if (IsBandBody(i, aniConfig))
                {
                    movePrefab.transform.parent = transform;
                    movePrefab.transform.localRotation = Quaternion.identity;
                    movePrefab.transform.localPosition = Vector3.zero;
                    movePrefab.transform.localScale = Vector3.one;
                }
                else
                {
                    movePrefab.transform.parent = Wrap.GetBandNode(aniConfig.bandNode[i]);
                    movePrefab.transform.localRotation = Quaternion.Euler(aniConfig.r[i].x, aniConfig.r[i].y, aniConfig.r[i].z);
                    movePrefab.transform.localPosition = aniConfig.p[i];
                    movePrefab.transform.localScale = aniConfig.s[i];
                }

                transform.GetComponent<CharacterLOD>()?.SetLodMesh(movePrefab);
            }
            if (i < expressionGameObject.Count)
            {
                PlayExpressionAnim(aniConfig, expressionGameObject[i]);
            }
        }

        return expressionGameObject;
    }

    public void PlayExpressionAnim(PlayerAniConfig aniConfig, GameObject expressionGO)
    {
        if (expressionGO == null) return;
        if (!string.IsNullOrEmpty(aniConfig.effectAnimName))
        {
            expressionGO.gameObject.SetActive(true);
            var animator = expressionGO.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play(aniConfig.effectAnimName, 0, 0);
                animator.Update(0);
            }
        }
    }

    public void CossFadeExpressionAnim(PlayerAniConfig aniConfig, GameObject expressionGO, float normalizedTransitionDuration)
    {
        if (!string.IsNullOrEmpty(aniConfig.effectAnimName))
        {
            expressionGO.gameObject.SetActive(true);
            var animator = expressionGO.GetComponent<Animator>();
            if (animator != null)
            {
                animator.CrossFade(aniConfig.effectAnimName, normalizedTransitionDuration, 0, 0, 0);
                animator.Update(0);
            }
        }
    }

    protected bool IsBandBody(int id, PlayerAniConfig aniConfig)
    {
        if (aniConfig.bandId != null)
        {
            for (int i = 0; i < aniConfig.bandId.Count; i++)
            {
                if (aniConfig.bandId[i] == id)
                {
                    return false;
                }
            }
        }
        return true;
    }

    public void SetRandomMove(List<GameObject> expressionGameObject, string name, int id)
    {
        if (expressionGameObject != null && expressionGameObject.Count > 0)
        {
            for (int i = 0; i < expressionGameObject.Count; i++)
            {
                RandomMoveGameObject[] ran = expressionGameObject[i].GetComponentsInChildren<RandomMoveGameObject>();
                if (ran != null && ran.Length > 0)
                {
                    for (int j = 0; j < ran.Length; j++)
                    {
                        if (ran[j].isChangeTexture)
                        {
                            ran[j].ChangeTexture(name, id);
                        }
                    }
                }
            }
        }
    }

    public void ClearExpression(List<GameObject> expressionGameObject)
    {
        if (expressionGameObject != null && expressionGameObject.Count > 0)
        {
            for (int i = 0; i < expressionGameObject.Count; i++)
            {
                var kv = uiEmoteExpressionGoDict.ToList().Find(kv => kv.Value == expressionGameObject[i]);
                if (kv.Key != null) uiEmoteExpressionGoDict.Remove(kv.Key);
                GameObject.Destroy(expressionGameObject[i]);
            }
            expressionGameObject.Clear();
        }
    }

    public void HideExpression(List<GameObject> expressionGameObject)
    {
        if (expressionGameObject != null && expressionGameObject.Count > 0)
        {
            for (int i = 0; i < expressionGameObject.Count; i++)
            {
                expressionGameObject[i].SetActive(false);
            }
            expressionGameObject.Clear();
        }
    }

    public void ShowExpressionActive(List<GameObject> expressionGameObject,bool isActive)
    {
        if (expressionGameObject != null && expressionGameObject.Count > 0)
        {
            for (int i = 0; i < expressionGameObject.Count; i++)
            {
                expressionGameObject[i].SetActive(isActive);
            }
        }
    }

    protected GameObject LoadEffectPrefab(string path)
    {
        if (uiEmoteExpressionGoDict.ContainsKey(path)) return uiEmoteExpressionGoDict[path];
        var effectPrefab = Loader.Load<GameObject>(EffectPath + path + ".prefab");
        var go = effectPrefab.Instantiate();
        uiEmoteExpressionGoDict.Add(path, go);
        return go;
    }

    #endregion

    #endregion

    #region AI动画播放

    public void SetFloat(int id, float value)
    {
        m_Animator.SetFloat(id, value);
    }

    public void SetInteger(int id, int value)
    {
        m_Animator.SetInteger(id, value);
    }

    #endregion

    /// <summary>
    /// 异步获取 PGC 动画 body 片段的时长（秒）。
    /// 若为循环动画（emoAniDataList.Count != 1）或获取失败，返回 0f。
    /// </summary>
    public void GetBodyAnimDurationAsync(string emoteId, Action<float> callback)
    {
        var configs = DataTables.GetEmoAniConfigList()
            .FindAll(c => c.emoId == emoteId);

        if (configs == null || configs.Count != 1)
        {
            callback(0f);
            return;
        }

        string path = AniPath + configs[0].bodyPath + ".anim";

        // 始终使用异步加载，避免 bundle 未缓存时 Loader.Load 内部调用 WaitForCompletion 阻塞主线程
        var asyncWrapper = Loader.LoadAsync<AnimationClip>(path);
        asyncWrapper.completed = success =>
        {
            if (!success)
            {
                callback(0f);
                return;
            }

            var loadedWrapper = Loader.Load<AnimationClip>(path);
            if (loadedWrapper != null)
            {
                var clip = loadedWrapper.RetainAsset(gameObject);
                callback(clip != null ? clip.length : 0f);
            }
            else
            {
                callback(0f);
            }
        };
    }
}

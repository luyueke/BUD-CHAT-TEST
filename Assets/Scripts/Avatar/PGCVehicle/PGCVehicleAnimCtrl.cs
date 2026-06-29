using System;
using Es;
using Game.Audio;
using GameData;
using UnityEngine;

public class PGCVehicleAnimCtrl : PlayAnimation
{
    protected IParameterAnimation<PGCVehicleAnimParameter> m_ParameterAnimation;
    public IParameterAnimation<PGCVehicleAnimParameter> ParameterAnimation
    {
        get
        {
            if (m_ParameterAnimation == null)
            {
                m_ParameterAnimation = new PGCVehicleParameterAnimation(m_Animator);
            }

            return m_ParameterAnimation;
        }
    }

    public PGCVehicleAniState CurState { get; private set; }
    public PGCVehicleAniState PreState { get; private set; }

    private PlayerAnimationCtrl playerAniCtrl;
    private PgcVehicleConfig config;

    public Action<PGCVehicleAniState> OnStateChange { get; set; }

    public virtual void Init(PlayerAnimationCtrl playerAniCtrl)
    {
        m_Animator = GetComponentInChildren<Animator>();
        m_ParameterAnimation = new PGCVehicleParameterAnimation(m_Animator);
        CurState = PGCVehicleAniState.Idle;
        PreState = PGCVehicleAniState.Idle;
        Init();
        this.playerAniCtrl = playerAniCtrl;
    }

    public void LoadVehicleAni(PgcVehicleConfig config){
        if(config == null) return;
        this.config = config;
        //替换载具动画
        if(!string.IsNullOrEmpty(config.vStartAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vStartAnim.anim, VehicleAnim.Start);
        }

        if(!string.IsNullOrEmpty(config.vIdleAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vIdleAnim.anim, VehicleAnim.Idle);
        }

        if(!string.IsNullOrEmpty(config.vRunAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vRunAnim.anim, VehicleAnim.Run);
        }

        if(!string.IsNullOrEmpty(config.vJumpAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vJumpAnim.anim, VehicleAnim.Jump);
        }

        if(!string.IsNullOrEmpty(config.vFallAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vFallAnim.anim, VehicleAnim.Fall);
        }

        if(!string.IsNullOrEmpty(config.vLandAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vLandAnim.anim, VehicleAnim.Land);
        }

        if(!string.IsNullOrEmpty(config.vSkill1StartAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vSkill1StartAnim.anim, VehicleAnim.Skill1Start);
        }

        if(!string.IsNullOrEmpty(config.vSkill1LoopAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vSkill1LoopAnim.anim, VehicleAnim.Skill1Loop);
        }

        if(!string.IsNullOrEmpty(config.vSkill1EndAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vSkill1EndAnim.anim, VehicleAnim.Skill1End);
        }   

        if(!string.IsNullOrEmpty(config.vSkill2StartAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vSkill2StartAnim.anim, VehicleAnim.Skill2Start);
        }

        if(!string.IsNullOrEmpty(config.vSkill2LoopAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vSkill2LoopAnim.anim, VehicleAnim.Skill2Loop);
        }

        if(!string.IsNullOrEmpty(config.vSkill2EndAnim.anim)){
            GetClipAndOverrideAnimationClip(config.vSkill2EndAnim.anim, VehicleAnim.Skill2End);
        }



    }

    public void SetPlayerAnimOverride(){
                //替换玩家动画
        if(!string.IsNullOrEmpty(config.idleAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(playerAniCtrl, config.idleAnim.anim, SpecialAnim.Idle);
            GetClipAndOverridePlayerAnimationClip(playerAniCtrl, config.idleAnim.anim, SpecialAnim.ExhibitIdle);
        }

        if(!string.IsNullOrEmpty(config.runAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(playerAniCtrl, config.runAnim.anim, SpecialAnim.Run);
        }

        if(!string.IsNullOrEmpty(config.runAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(playerAniCtrl, config.runAnim.anim, SpecialAnim.FastRun);
        }
        
        if(!string.IsNullOrEmpty(config.jumpAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(playerAniCtrl, config.jumpAnim.anim, SpecialAnim.Jump);
            GetClipAndOverridePlayerAnimationClip(playerAniCtrl, config.jumpAnim.anim, SpecialAnim.JumpRun);
        }

        if(!string.IsNullOrEmpty(config.landAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(playerAniCtrl, config.landAnim.anim, SpecialAnim.Landed);
        }

        var clip = Loader.Load<AnimationClip>(config.jumpAnim.anim, gameObject);
        playerAniCtrl.OverrideAnimationClip("runjump24", clip);
    }

    public void SetOtherPlayerAnimOverride(PlayerAnimationCtrl otherAniCtrl){
        if(!string.IsNullOrEmpty(config.idleAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(otherAniCtrl, config.idleAnim.anim, SpecialAnim.Idle);
            GetClipAndOverridePlayerAnimationClip(otherAniCtrl, config.idleAnim.anim, SpecialAnim.ExhibitIdle);
        }

        if (!string.IsNullOrEmpty(config.runAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(otherAniCtrl, config.runAnim.anim, SpecialAnim.Run);
        }

        if(!string.IsNullOrEmpty(config.runAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(otherAniCtrl, config.runAnim.anim, SpecialAnim.FastRun);
        }

        if(!string.IsNullOrEmpty(config.jumpAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(otherAniCtrl, config.jumpAnim.anim, SpecialAnim.Jump);
            GetClipAndOverridePlayerAnimationClip(otherAniCtrl, config.jumpAnim.anim, SpecialAnim.JumpRun);
        }

        if(!string.IsNullOrEmpty(config.landAnim.anim)){
            GetClipAndOverridePlayerAnimationClip(otherAniCtrl, config.landAnim.anim, SpecialAnim.Landed);
        }

        var clip = Loader.Load<AnimationClip>(config.jumpAnim.anim, gameObject);
        otherAniCtrl.OverrideAnimationClip("runjump24", clip);
    }

    private void GetClipAndOverrideAnimationClip(string anim, VehicleAnim vehicleAnim){
        var clip = Loader.Load<AnimationClip>(anim, gameObject);
        if(clip != null){
            OverrideAnimationClip(vehicleAnim.GetString(), clip);
        }
    }

    private void GetClipAndOverridePlayerAnimationClip(PlayerAnimationCtrl playerAniCtrl, string anim, SpecialAnim playerAnim){
        var clip = Loader.Load<AnimationClip>(anim, gameObject);
        if(clip != null){
            playerAniCtrl.OverrideAnimationClip(playerAnim.GetString(), clip);
        }
    }

    public void OverrideAnimationClip(string stateName,AnimationClip clip){
        m_Animator.OverrideAnimationClip(AniOverrideCtrl, clipOverrides, stateName, clip);
    }

    private bool _idleFloatOverridden;
    /// <summary>抓着人时把 idle 与 run 的 clip 都覆盖成 float（载具持人态恒悬停，移动与否都播 float），
    /// 松空(0人)后分别还原为 config 原始 idle / run。floating=true 用 floatClipPath；当前若在 idle/run 立即重播生效。</summary>
    public void SetIdleFloating(bool floating, string floatClipPath){
        if(m_Animator == null || _idleFloatOverridden == floating) return;
        AnimationClip idleClip, runClip;
        if(floating){
            idleClip = runClip = Loader.Load<AnimationClip>(floatClipPath, gameObject);
        }else{
            idleClip = (config != null && !string.IsNullOrEmpty(config.vIdleAnim.anim))
                ? Loader.Load<AnimationClip>(config.vIdleAnim.anim, gameObject) : null;
            runClip = (config != null && !string.IsNullOrEmpty(config.vRunAnim.anim))
                ? Loader.Load<AnimationClip>(config.vRunAnim.anim, gameObject) : null;
        }
        if(idleClip == null) return; // idle 必须有（还原时无原始 idle 配置则放弃）
        _idleFloatOverridden = floating;
        OverrideAnimationClip(VehicleAnim.Idle.GetString(), idleClip);
        if(runClip != null) OverrideAnimationClip(VehicleAnim.Run.GetString(), runClip);
        // 当前若正处于 idle 或 run，立即重播使新 clip 生效
        if(CurState == PGCVehicleAniState.Idle || CurState == PGCVehicleAniState.Run)
            SetAniState(CurState);
    }

    private bool _driverIdleFloatOverridden;
    /// <summary>抓着人时把驾驶员 avatar 的 idle 覆盖成 float，松空(0人)还原为 config 原始玩家 idle。
    /// floating=true 用 playerFloatPath；false 用 config.idleAnim.anim 还原。同时覆盖 Idle 与 ExhibitIdle 两个键。</summary>
    public void SetDriverIdleFloating(bool floating, string playerFloatPath){
        if(playerAniCtrl == null || _driverIdleFloatOverridden == floating) return;
        AnimationClip clip;
        if(floating)
            clip = Loader.Load<AnimationClip>(playerFloatPath, gameObject);
        else if(config != null && !string.IsNullOrEmpty(config.idleAnim.anim))
            clip = Loader.Load<AnimationClip>(config.idleAnim.anim, gameObject);
        else
            return; // 无原始玩家 idle 配置，无法还原
        if(clip == null) return;
        _driverIdleFloatOverridden = floating;
        playerAniCtrl.OverrideAnimationClip(SpecialAnim.Idle.GetString(), clip);
        playerAniCtrl.OverrideAnimationClip(SpecialAnim.ExhibitIdle.GetString(), clip);
    }

    public void SetAniState(PGCVehicleAniState state){
        PreState = CurState;
        CurState = state;
        ParameterAnimation.SetInteger(PGCVehicleAnimParameter.CurState, (int)CurState);
        ParameterAnimation.SetInteger(PGCVehicleAnimParameter.PreState, (int)PreState);
        ParameterAnimation.SetTrigger(PGCVehicleAnimParameter.StateOn);
        OnStateChange?.Invoke(CurState);
        // 抓着人(float)时，载具进入 Idle 的同一帧让驾驶员 avatar idle 也从头重播，
        // 保证两者 float 同相位（仅娃娃机持人态生效，_driverIdleFloatOverridden 由 SetDriverIdleFloating 控制）。
        // 驾驶员 avatar 一直停在 idle 状态，trigger 方式不产生真实 transition 无法重播；
        // Animator.Play 可在已处于 idle 时把该状态硬重置到第 0 帧，不依赖 transition、不改 Cur/PrePlayerState。
        if(state == PGCVehicleAniState.Idle && _driverIdleFloatOverridden && playerAniCtrl != null)
            playerAniCtrl.Play(SpecialAnim.Idle.GetString(), 0, 0f);
    }

    public void SetMoveSpeed(Vector3 velocity){
        float speed = new Vector2(velocity.x, velocity.z).magnitude;
        ParameterAnimation.SetFloat(PGCVehicleAnimParameter.MoveSpeed, speed);
        ParameterAnimation.SetFloat(PGCVehicleAnimParameter.YMoveSpeed, velocity.y);
    }

    public void SetSkilling(bool isSkilling){
        ParameterAnimation.SetBool(PGCVehicleAnimParameter.isSkilling, isSkilling);
    }

    public bool IsCurrentAnimFinished()
    {
        if (m_Animator == null) return true;
        var info = m_Animator.GetCurrentAnimatorStateInfo(0);
        if (m_Animator.IsInTransition(0)) return false;
        return info.normalizedTime >= 1.0f;
    }

    /// <summary>当前 layer0 是否正处于指定状态、且已播放到 normalizedTime 之后（未在过渡中）。
    /// 用于在某 state 接近播完时插入后续处理（如 skill1Start 快播完时翻 isSkilling 进 skill1End）。</summary>
    public bool IsStatePlayedTo(string stateName, float normalizedTime, int layer = 0)
    {
        if (m_Animator == null) return false;
        if (m_Animator.IsInTransition(layer)) return false;
        var info = m_Animator.GetCurrentAnimatorStateInfo(layer);
        return info.IsName(stateName) && info.normalizedTime >= normalizedTime;
    }

    /// <summary>当前 layer 若正处于 stateName（且不在过渡中），返回其已播放秒数(本周期内 frac×clipLength)；
    /// 否则返回 -1（过渡中/未进入/已离开）。用于把"动画真实播放进度"换成秒，驱动时序而非墙钟。</summary>
    public float GetStatePlaybackTime(string stateName, int layer = 0)
    {
        if (m_Animator == null) return -1f;
        if (m_Animator.IsInTransition(layer)) return -1f;
        var info = m_Animator.GetCurrentAnimatorStateInfo(layer);
        if (!info.IsName(stateName)) return -1f;
        float frac = info.normalizedTime - Mathf.Floor(info.normalizedTime); // 取本周期内 0..1
        return frac * info.length;
    }

    public void ResetAnimation(){
        ParameterAnimation.SetFloat(PGCVehicleAnimParameter.MoveSpeed, 0);
        ParameterAnimation.SetFloat(PGCVehicleAnimParameter.YMoveSpeed, 0);
        ParameterAnimation.SetInteger(PGCVehicleAnimParameter.CurState, (int)PGCVehicleAniState.Idle);
        ParameterAnimation.SetInteger(PGCVehicleAnimParameter.PreState, (int)PGCVehicleAniState.Idle);
        ParameterAnimation.SetTrigger(PGCVehicleAnimParameter.StateOn);
    }

    protected void OnDisable(){
        AkSoundManager.Inst.StopAll(gameObject);
    }

}
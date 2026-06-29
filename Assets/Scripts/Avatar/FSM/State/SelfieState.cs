using System;
using System.Collections.Generic;
using Es;
using FSM;
using Game.KinematicCharacter;
using Message;
using UnityEngine;

public class ExitSelfieEvent
{
    public Action exitSelfie;
}
public class SelfieState : PlayerStateTemplate<PlayerStateController>
{
    private const string LEFT_EFFECT_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 L Clavicle/Bip001 L UpperArm/Bip001 L Forearm/Bip001 L Hand/effect_l";
    private const string RIGHT_EFFECT_PATH = "Bip001/Bip001 Pelvis/Bip001 Spine/Bip001 Spine1/Bip001 Neck/Bip001 R Clavicle/Bip001 R UpperArm/Bip001 R Forearm/Bip001 R Hand/effect_r";
    private readonly string selfiePrefabPath = "Assets/Loadable/AnimationsExpress/Feat/selfiestick_effect/selfiestick_effect.prefab";
    
    private readonly string right_run_path = "Assets/Loadable/Animations/SelfiePose/selfiestick_move_r.anim";
    private readonly string right_jump_path = "Assets/Loadable/Animations/SelfiePose/selfiestick_jump_r.anim";
    private List<GameObject> expressionGameObject;
    private GameObject selfieNode;
    private Animator selfieAnimator;
    private bool isStartAni = false;
    private Vector3 effRot = new Vector3(84.282f, -123.077f, 29.161f);
    private Vector3 effRot_R = new Vector3(297.115753f,84.3963547f,69.7631683f);
    private Vector3 effPos = new Vector3(0,0,0.02f);
    private ExitSelfieEvent exitSelfieEvent;
    private CameraSelfiePose selfieConfig;
    private enum SelfieAniType
    {
        In = 1,
        Idle =2,
        Out,
    }

    bool isShowSelfieSticker = true;

    public SelfieState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        if (owner.IsSelf && args != null && args.Length > 0)
        {
            exitSelfieEvent = args[0] as ExitSelfieEvent;
        }
        if(args != null && args.Length > 1){
            string selfieId = args[1] as string;
            if(!string.IsNullOrEmpty(selfieId)){
                selfieConfig = DataTables.GetCameraSelfiePose(selfieId);
                OverrideAnima();
            }
        }else{
            selfieConfig = null;
        }
        //SetInputLock(true);
    }

    private void OverrideAnima(){
        if(selfieConfig == null){
            return;
        }

        SetInputLock(!selfieConfig.moveable);

        if(selfieConfig.stickHand == 1 && selfieConfig.moveable){
            var r_warpper = Loader.Load<AnimationClip>(right_run_path);
            if (r_warpper == null) return;
            AnimationClip r_clipRes = r_warpper.RetainAsset(owner.gameObject);
            if (r_clipRes == null) return;
            AnimationClip r_clip = AnimationClip.Instantiate(r_clipRes, owner.transform);
            owner.PlayerAnimCtrl.OverrideAnimationClip("prop_none_selfie_move", r_clip);

            var r_jump_warpper = Loader.Load<AnimationClip>(right_jump_path);
            if (r_jump_warpper == null) return;
            AnimationClip r_jump_clipRes = r_jump_warpper.RetainAsset(owner.gameObject);
            if (r_jump_clipRes == null) return;
            AnimationClip r_jump_clip = AnimationClip.Instantiate(r_jump_clipRes, owner.transform);
            owner.PlayerAnimCtrl.OverrideAnimationClip("prop_none_selfie_jump", r_jump_clip);
        }

        var warpper = Loader.Load<AnimationClip>(selfieConfig.resourcePath + ".anim");
        if (warpper == null) return;
        AnimationClip clipRes = warpper.RetainAsset(owner.gameObject);
        if (clipRes == null) return;
        AnimationClip clip = AnimationClip.Instantiate(clipRes, owner.transform);
        owner.PlayerAnimCtrl.OverrideAnimationClip("selfiestick_idle", clip);

        
    }

    public override void EnterMainState()
    {
        base.EnterMainState();
        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Selfie);
        // StateEventManager.Inst.RegisterStateEvent<Vector3>(owner.PlayerID, StateEvent.MoveJoystick, owner.MoveJoystick);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, owner.OnClickJumpBtn);
        MessageHelper.AddListener<string>(MessageName.SelfieChangePose, OnSelfieChangePose);
        MessageHelper.AddListener<bool>(MessageName.ShowSelfieSticker, OnShowSelfieSticker);
    }

    public override void ExitMainState()
    {
        base.ExitMainState();
        // 兼容“未播完 SelfieIn 就被快速退出”的场景，先终止自拍道具动画驱动。
        isStartAni = false;
        // StateEventManager.Inst.UnRegisterStateEvent<Vector3>(owner.PlayerID, StateEvent.MoveJoystick,
        //     owner.MoveJoystick);
        StateEventManager.Inst.UnRegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, owner.OnClickJumpBtn);
        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Default);
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
        ReleaseEffect();
        exitSelfieEvent?.exitSelfie?.Invoke();
        owner.PlayerAnimCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
        owner.PlayerAnimCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
        owner.PlayerAnimCtrl.OverrideAnimationClip("selfiestick_idle", null);
        SetInputLock(false);
        selfieConfig = null;
        MessageHelper.RemoveListener<string>(MessageName.SelfieChangePose, OnSelfieChangePose);
        MessageHelper.RemoveListener<bool>(MessageName.ShowSelfieSticker, OnShowSelfieSticker);
    }

    public override void CoexistState()
    {
        base.CoexistState();
        CreateSelfieEffect();
        PlaySelfieAni(SelfieAniType.In);
        var animId = AnimId.SelfieIn;
        if(selfieConfig != null && selfieConfig.stickHand == 1){
            animId = AnimId.SelfieIn_R;
        }
        if(selfieConfig != null){
            EnterRunAniState();
        }else{
            owner.PlayerAnimCtrl.CrossFadeFixedAnimFromConfig(animId, 0.1f).OnCompleteEvent(EnterRunAniState);
        }
    }

    private void EnterRunAniState()
    {
        // 若进入动画回调晚到（状态已退出），直接忽略，避免把角色卡回自拍流程。
        if (!owner.ContainsAllState(PlayerState.CameraMode))
        {
            return;
        }
        PlaySelfieAni(SelfieAniType.Idle);
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
    }

    private void PlaySelfieAni(SelfieAniType aniState)
    {
        if (!isStartAni) return;

        selfieAnimator.SetInteger("BoardState", (int) aniState);
    }

    private void CreateSelfieEffect()
    {
        var skateBoradRes = Loader.Load<GameObject>(selfiePrefabPath).RetainAsset(owner.gameObject);
        var par = owner.transform.Find(LEFT_EFFECT_PATH);
        if(selfieConfig != null && selfieConfig.stickHand == 1){
            par = owner.transform.Find(RIGHT_EFFECT_PATH);
        }

        if(par != null){
            selfieNode = GameObject.Instantiate(skateBoradRes, par);
            var selfieTransorm = selfieNode.transform;
            selfieTransorm.localPosition = effPos;
            selfieTransorm.localScale = Vector3.one;
            if(selfieConfig != null){
                selfieTransorm.localEulerAngles = selfieConfig.stickRot;
            }else{
                selfieTransorm.localEulerAngles = effRot;
            }
            selfieAnimator = selfieNode.GetComponent<Animator>();
            isStartAni = true;

            if(isShowSelfieSticker){
                selfieNode.gameObject.SetActive(true);
            }else{
                selfieNode.gameObject.SetActive(false);
            }
        }
    }


    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        PlaySelfieAni(SelfieAniType.Out);
        isStartAni = false;

        owner.PlayerAnimCtrl.CrossFadeAnimFromConfig(AnimId.SelfieOut, 0.1f).OnCompleteEvent(() =>
        {
            owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
            base.PlayExitAnimation(onComplete);
        });
    }

    private void ReleaseEffect()
    {
        if (selfieNode == null) return;
        GameObject.Destroy(selfieNode);
        selfieNode = null;
        selfieAnimator = null;
    }

    public void SetInputLock(bool isLock)
    {
        if (isLock)
        {
            owner.PlayerKCCtrl.SetFreezeCharacter(true);
            if (MobileJoystick.Inst != null)
            {
                MobileJoystick.Inst.OnResetJoystick();
            }
        }
        else
        {
            owner.PlayerKCCtrl.SetFreezeCharacter(false);
        }
    }

    private void OnSelfieChangePose(string selfieId)
    {
        if(string.IsNullOrEmpty(selfieId)){
            selfieConfig = null;
            owner.PlayerAnimCtrl.OverrideAnimationClip("prop_none_selfie_jump", null);
            owner.PlayerAnimCtrl.OverrideAnimationClip("prop_none_selfie_move", null);
            owner.PlayerAnimCtrl.OverrideAnimationClip("selfiestick_idle", null);
            SetInputLock(false);
            selfieNode.gameObject.SetActive(true);
            selfieNode.transform.localPosition = effPos;
            selfieNode.transform.localScale = Vector3.one;
            selfieNode.transform.localEulerAngles = effRot;
        }else{
            selfieConfig = DataTables.GetCameraSelfiePose(selfieId);
            var par = owner.transform.Find(LEFT_EFFECT_PATH);
            if(selfieConfig != null && selfieConfig.stickHand == 1){
                par = owner.transform.Find(RIGHT_EFFECT_PATH);
            }
            selfieNode.transform.parent = par;
            selfieNode.transform.localPosition = effPos;
            selfieNode.transform.localEulerAngles = selfieConfig.stickRot;
            if(isShowSelfieSticker){
                selfieNode.gameObject.SetActive(true);
            }else{
                selfieNode.gameObject.SetActive(false);
            }
            OverrideAnima();
        }

    }

    private void OnShowSelfieSticker(bool isShow)
    {
        isShowSelfieSticker = isShow;
        if(selfieNode == null){
            return;
        }
        if(isShowSelfieSticker){
            selfieNode.gameObject.SetActive(true);
        }else{
            selfieNode.gameObject.SetActive(false);
        }
    }
}
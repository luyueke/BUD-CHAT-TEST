using Basic.Extensions;
using Es;
using Game.Audio;
using Game.Avatar;
using Game.Config;
using Game.KinematicCharacter;
using GameData;
using Message;
using System.Collections;
using UnityEngine;

/// <summary>
/// 圣天使KCC
/// </summary>
public class HolyangeKCC : BaseKCC {
    public override KCCType KCCType => KCCType.Default;
    public virtual string RunName => "run";

    private bool isFirstEnter = true;
    private Animator pgcAnimator;
    private AnimationEventHandler animEventHandler;

    private Animator effectAnimator;

    public HolyangeKCC(string uid, bool isSelf) : base(uid, isSelf)
    {

    }

    public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl) {
        base.OnEnter(motor, animCtrl);
        // 因为动作中有两次脚步，因此需要添加两个事件监听
        // 仅第一次进入和自己 才关闭模拟
        StateEventManager.Inst.RegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
        if (isFirstEnter && IsSelf) {
            isFirstEnter = false;
            SetKCCSimulate(false);
        }

        var backRoot = animCtrl.Wrap.GetBandNode((int)BodyNode.SpecialBackDeckNode);

        // 有问题，进入该状态时，衣服未创建，获取动画可能为null，若为null


        Motor.SetFrameCallBack(1, () => {
            if (pgcAnimator != null) {
                return;
            }
            pgcAnimator = backRoot.GetComponentInChildren<Animator>(true);
            animEventHandler = backRoot.GetComponentInChildren<AnimationEventHandler>(true);

            if (animEventHandler != null) {
                animEventHandler.AddStateEvent("run","Holyange_playFootSound1", 0.4f, OnRunAnimationEvent);
                animEventHandler.AddStateEvent("run","Holyange_playFootSound2", 1.3f, OnRunAnimationEvent);
                animEventHandler.AddStateEvent("fast_run","Holyange_playFootSound1", 0.3f, OnRunAnimationEvent);
                animEventHandler.AddStateEvent("fast_run","Holyange_playFootSound2", 0.9f, OnRunAnimationEvent);
            }
            InstEffect(animCtrl);
        });
        _gravity = new Vector3(0, -25f, 0);
        animCtrl.SetSpecialPgcId(animCtrl.specialAnimPgcId);

    }

    private void InstEffect(PlayerAnimationCtrl animCtrl)
    {
        string curEffectId = animCtrl.specialAnimPgcId;       
        var bData = Es.DataTables.GetAvatarCommonData(curEffectId);
        var specialData = Es.DataTables.GetSpecialSkinConfig(curEffectId);
        if (bData == null || specialData == null)
        {
            LoggerUtils.LogError("特效创建失败，因为配置拿不到！");
            return;
        }

        if (specialData.Effect == 0) return;
        var id = curEffectId;
        string effectName =  bData.prefabName + "_effect(Clone)";
        Transform effect = animCtrl.transform.Find(effectName);
        effectAnimator = effect?.GetComponent<Animator>();
       // Debug.Log($"HolyangeKCC curEffectId={curEffectId},ctrl={animCtrl.gameObject.name},effectName={effectName},effect={effect == null}effectAnimator={effectAnimator==null} ");
        //Loader.LoadAsyncOrSync<GameObject>(bData.texDir + bData.prefabName + "_effect.prefab", (isSuc, warpper) => {
        //    if (isSuc && warpper != null && id == curEffectId )
        //    {
        //        var tempPrefab = warpper.RetainAsset(motor.gameObject);
        //        GameObject effect = GameObject.Instantiate(tempPrefab, motor.transform.parent);
        //        effectAnimator = effect.GetComponent<Animator>();
        //    }
        //});
    }

 
    public override void OnExit() {
        if (PlayerAnimCtrl != null) {
            PlayerAnimCtrl.SetSpecialPgcId("0");
        }
        effectAnimator?.Play("idle");
        base.OnExit();
        if (animEventHandler != null) {
            animEventHandler.RemoveStateEvent("run","Holyange_playFootSound1");
            animEventHandler.RemoveStateEvent("run","Holyange_playFootSound2");
            animEventHandler.RemoveStateEvent("fast_run","Holyange_playFootSound1");
            animEventHandler.RemoveStateEvent("fast_run","Holyange_playFootSound2");
        }
        StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
    }

    public override void SetInputs(ref PlayerCharacterInputs inputs, Vector3 moveInputVector, Quaternion cameraPlanarRotation,
        Vector3 cameraPlanarDirection) {
        base.SetInputs(ref inputs, moveInputVector, cameraPlanarRotation, cameraPlanarDirection);
        if (!Motor.IsOnSimulate && (moveInputVector != Vector3.zero || inputs.JumpDown)) {
            SetKCCSimulate(true);
        }
    }


    private void SetKCCSimulate(bool isOn) {
        Motor.SetIsOnSimulate(isOn);
    }

    protected override void AirMovement(ref Vector3 currentVelocity, float deltaTime) {
        Vector3 lastVelocity = currentVelocity;
        base.AirMovement(ref currentVelocity, deltaTime);
        if (lastVelocity.y > 0 && currentVelocity.y < 0) {
            //到达顶端
            _gravity = new Vector3(0, -3.7f, 0);
            var tmpPgcAnimator = GetPGCAnimator();
            if (tmpPgcAnimator != null) {
                tmpPgcAnimator.Play("jump_down");
            }
        }
    }

    /// <summary>
    /// 快跑，慢跑切换
    /// </summary>
    protected override void OnRunStateChange() {
        base.OnRunStateChange();
        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator == null) {
            return;
        }
        if (IsFastRun) {
            tmpPgcAnimator.Play("fast_run");
        } else if (PlayerAnimCtrl.CurAniState == PlayerAniState.Run) {
            tmpPgcAnimator.Play("run");
        }
    }

    public override void OnLanded() {
        _gravity = new Vector3(0, -25f, 0);
        base.OnLanded();
    }


    public override void OnLeaveStableGround() {
        base.OnLeaveStableGround();
        if (PlayerID.StartsWith("pet")) return;
        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator != null) {
            tmpPgcAnimator.Play("jump_up");
        }
        MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Jump, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }

    private IEnumerator PlayIdleExhibit()
    {
        var tmpPgcAnimator = GetPGCAnimator();

        AkSoundManager.Inst.StopAll(PlayerAnimCtrl.gameObject);

        string name = "idle_exhibit";
            // MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
        tmpPgcAnimator.Play(name);
        var exhibitAnim = "Holyange/idle_exhibit.anim";
        var exhibitIdleAnimClip = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + exhibitAnim, PlayerAnimCtrl.gameObject);
        PlayerAnimCtrl.OverrideAnimationClip(SpecialAnim.Idle.GetString(), exhibitIdleAnimClip);

        var specialSkinConfig = DataTables.GetSpecialSkinConfig(PlayerAnimCtrl.specialAnimPgcId);
        if (specialSkinConfig != null)
        {
            var switchEventName = specialSkinConfig.exhibitIdleAnimInfo.audio;
            // Debug.LogError($"HolyangeKCC switchEventName={switchEventName},specialSkinConfig.SoundVersion={specialSkinConfig.SoundVersion}");
            if (!string.IsNullOrEmpty(switchEventName))
            {//播放特殊待机动画音效
                AkSoundManager.Inst.PlaySound($"Locomotion_Group", switchEventName, $"Play_Locomotion_1P", PlayerAnimCtrl.gameObject);
            }
        }
        if (effectAnimator == null)
        {
            InstEffect(PlayerAnimCtrl);
        }
        effectAnimator?.Play(name); //播放特殊待机特效

        yield return new WaitForSeconds(exhibitIdleAnimClip.length);

        PlayIdle();

    }
    private void PlayIdle()
    {
        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator == null)
        {
            return;
        }
   
        AkSoundManager.Inst.StopAll(PlayerAnimCtrl.gameObject);
        float random = UnityEngine.Random.Range(0, 100);
        string name;
 
        name = "idle";
        //  MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
        tmpPgcAnimator.Play(name);
        var exhibitAnim = "Holyange/idle.anim";
        var exhibitIdleAnimClip = Loader.Load<AnimationClip>(GameConsts.SpecialAnimDir + exhibitAnim, PlayerAnimCtrl.gameObject);
        PlayerAnimCtrl.OverrideAnimationClip(SpecialAnim.Idle.GetString(), exhibitIdleAnimClip);

        if (effectAnimator == null)
        {
            InstEffect(PlayerAnimCtrl);
        }
        effectAnimator?.Play(name); //播放特殊待机特效
     }

    private Coroutine exhibitAnimCoroutine;
    /// <summary>
    /// 动作状态切换
    /// </summary>
    /// <param name="newState"></param>
    private void OnAnimationStateChange(PlayerAniState newState) {

        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator == null) {
            return;
        }
        if(exhibitAnimCoroutine != null)
        {
            PlayerAnimCtrl.StopCoroutine(exhibitAnimCoroutine);
            exhibitAnimCoroutine = null;
        }
        if (newState == PlayerAniState.Idle) {

            AkSoundManager.Inst.StopAll(PlayerAnimCtrl.gameObject);
            float random = UnityEngine.Random.Range(0, 100);
            if(random > 50)
            {
                exhibitAnimCoroutine = PlayerAnimCtrl.StartCoroutine(PlayIdleExhibit()); //播完特殊待机后播普通待机
            }else
            {
                PlayIdle();
            }
        }
        else if (newState == PlayerAniState.Run) {
            tmpPgcAnimator.Play("run");
        }
    }

    public void OnRunAnimationEvent() {
        // 只有在着地并且在地上才播放脚步音效
        MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }

    private Animator GetPGCAnimator() {

        if (pgcAnimator == null) {
            var backRoot = PlayerAnimCtrl.Wrap.GetBandNode((int)BodyNode.SpecialBackDeckNode);
            pgcAnimator = backRoot.GetComponentInChildren<Animator>();
            animEventHandler = backRoot.GetComponentInChildren<AnimationEventHandler>(true);
            if (animEventHandler != null) {
                animEventHandler.AddStateEvent("run","Holyange_playFootSound1", 0.4f, OnRunAnimationEvent);
                animEventHandler.AddStateEvent("run","Holyange_playFootSound2", 1.3f, OnRunAnimationEvent);
                animEventHandler.AddStateEvent("fast_run","Holyange_playFootSound1", 0.3f, OnRunAnimationEvent);
                animEventHandler.AddStateEvent("fast_run","Holyange_playFootSound2", 0.9f, OnRunAnimationEvent);
            }
        }

        return pgcAnimator;
    }


}

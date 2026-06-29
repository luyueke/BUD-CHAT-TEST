using Basic.Extensions;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

/// <summary>
/// 圣天使KCC
/// </summary>
public class LuciferKCC : BaseKCC {
    public override KCCType KCCType => KCCType.Default;
    public virtual string RunName => "run";

    private bool isFirstEnter = true;
    private Animator pgcAnimator;
    private AnimationEventHandler animEventHandler;


    public LuciferKCC(string uid, bool isSelf) : base(uid, isSelf)
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
        });
        _gravity = new Vector3(0, -25f, 0);
        animCtrl.SetSpecialPgcId(animCtrl.specialAnimPgcId);
    }

    public override void OnExit() {
        if (PlayerAnimCtrl != null) {
            PlayerAnimCtrl.SetSpecialPgcId("0");
        }

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


    /// <summary>
    /// 动作状态切换
    /// </summary>
    /// <param name="newState"></param>
    private void OnAnimationStateChange(PlayerAniState newState) {

        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator == null) {
            return;
        }
        if (newState == PlayerAniState.Idle) {
            tmpPgcAnimator.Play("idle");
        } else if (newState == PlayerAniState.Run) {
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

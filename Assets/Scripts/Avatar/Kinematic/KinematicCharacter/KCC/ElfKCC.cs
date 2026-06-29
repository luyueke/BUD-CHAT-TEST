using Basic.Extensions;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

/// <summary>
///  雪精灵
/// </summary>
public class ElfKCC: BaseKCC {
    public override KCCType KCCType => KCCType.Default;
    public virtual string RunName => "run";

    private bool isFirstEnter = true;
    private Animator pgcAnimator;


    public ElfKCC(string uid, bool isSelf) : base(uid, isSelf) {

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
        _gravity = new Vector3(0, -25f, 0);
        animCtrl.SetSpecialPgcId(animCtrl.specialAnimPgcId);

    }

    public override void OnExit() {
        if (PlayerAnimCtrl != null) {
            PlayerAnimCtrl.SetSpecialPgcId("0");
        }

        base.OnExit();
        StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
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

    public override void OnLanded() {
        _gravity = new Vector3(0, -25f, 0);
        base.OnLanded();
    }

    /// <summary>
    /// 快跑，慢跑切换
    /// </summary>
    protected override void OnRunStateChange() {
        base.OnRunStateChange();
        var tmpPgcAnimator = GetPGCAnimator();

        if (IsFastRun) {
            if (tmpPgcAnimator != null) {
                tmpPgcAnimator.Play("fast_run");
            }
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.FastRun, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
        } else if (PlayerAnimCtrl.CurAniState == PlayerAniState.Run) {
            if (tmpPgcAnimator != null) {
                tmpPgcAnimator.Play("run");
            }
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, PlayerID.StartsWith("pet")? 2 : IsSelf? 0 : 1);
        }
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
        if (newState == PlayerAniState.Idle) {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
            if (tmpPgcAnimator != null) {
                tmpPgcAnimator.Play("idle");
            }

        } else if (newState == PlayerAniState.Run) {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
            if (tmpPgcAnimator != null) {
                tmpPgcAnimator.Play("run");
            }
        }
    }

        private Animator GetPGCAnimator() {
            if (pgcAnimator == null) {
                var backRoot = PlayerAnimCtrl.Wrap.GetBandNode((int)BodyNode.SpecialHatNode);
                pgcAnimator = backRoot.GetComponentInChildren<Animator>();
            }

            return pgcAnimator;
        }

}

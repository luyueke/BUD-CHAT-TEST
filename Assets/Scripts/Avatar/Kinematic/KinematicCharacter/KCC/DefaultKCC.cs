using Game.Audio;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

public class DefaultKCC : BaseKCC
{
    public override KCCType KCCType => KCCType.Default;
    public virtual string RunName => "run";
    public virtual string RunFast => "run_fast";

    private bool isFirstEnter = true;

    public DefaultKCC(string uid, bool isSelf) : base(uid, isSelf)
    {

    }

    public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl) {
        base.OnEnter(motor, animCtrl);

        // 因为动作中有两次脚步，因此需要添加两个事件监听
        animCtrl.AddStateEvent(RunName, "playFootSound_1", 0.03f, OnRunAnimationEvent, true);
        animCtrl.AddStateEvent(RunName, "playFootSound_2", 0.36f, OnRunAnimationEvent,true);

        animCtrl.AddStateEvent(RunFast, "playFootSound_1", 0.03f, OnRunAnimationEvent,true);
        animCtrl.AddStateEvent(RunFast, "playFootSound_2", 0.2f, OnRunAnimationEvent,true);

        // 仅第一次进入和自己 才关闭模拟
        if (isFirstEnter && IsSelf) {
            isFirstEnter = false;
            SetKCCSimulate(false);
        }
    }

    public override void OnExit() {
        if (PlayerAnimCtrl != null) {
            PlayerAnimCtrl.RemoveStateEvent(RunName, "playFootSound_1");
            PlayerAnimCtrl.RemoveStateEvent(RunName, "playFootSound_2");

            PlayerAnimCtrl.RemoveStateEvent(RunFast, "playFootSound_1");
            PlayerAnimCtrl.RemoveStateEvent(RunFast, "playFootSound_2");
        }
        base.OnExit();
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
        base.OnLanded();
        var groundCollider = Motor.GroundingStatus.GroundCollider;
        if (PlayerID.StartsWith("pet")) return;
        if (PlayerAnimCtrl!= null && !PlayerAnimCtrl.IsFootSoundEnable) return;
        MessageHelper.Broadcast(MessageName.FootSound, CharacterFootType.Landed, groundCollider.gameObject, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }

    public override void OnLeaveStableGround() {
        base.OnLeaveStableGround();
        if (PlayerID.StartsWith("pet")) return;
        if (PlayerAnimCtrl!= null && !PlayerAnimCtrl.IsFootSoundEnable) return;
        MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Jump, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }

    public virtual void OnRunAnimationEvent() {
        // 只有在着地并且在地上才播放脚步音效
        if (Motor == null || !Motor.GroundingStatus.IsStableOnGround || Motor.GroundingStatus.GroundCollider == null) {
            return;
        }
        var groundCollider = Motor.GroundingStatus.GroundCollider;
        if (PlayerAnimCtrl!= null && !PlayerAnimCtrl.IsFootSoundEnable) return;
        MessageHelper.Broadcast(MessageName.FootSound, CharacterFootType.Run, groundCollider.gameObject, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }
}

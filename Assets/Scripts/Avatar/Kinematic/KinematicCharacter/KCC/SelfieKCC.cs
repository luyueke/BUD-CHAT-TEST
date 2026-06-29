using Game.KinematicCharacter;
using UnityEngine;

public class SelfieKCC: DefaultKCC
{
    public override KCCType KCCType => KCCType.Selfie;

    public override string RunName => "prop_none_selfie_move";

    public SelfieKCC(string uid, bool isSelf) : base(uid, isSelf)
    {
    }

    public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl)
    {
        base.OnEnter(motor, animCtrl);
        m_StableMovementData.OrientationMethod = OrientationMethod.NoToward;
    }

    public override void OnExit()
    {
        base.OnExit();
        m_StableMovementData.OrientationMethod = OrientationMethod.TowardsMovement;
    }

    public override void SetInputs(ref PlayerCharacterInputs inputs, Vector3 moveInputVector, Quaternion cameraPlanarRotation,
        Vector3 cameraPlanarDirection)
    {
        //不进入FastRun状态
        inputs.PressJoystickTime = 0;
        base.SetInputs(ref inputs, moveInputVector, cameraPlanarRotation, cameraPlanarDirection);
    }
}

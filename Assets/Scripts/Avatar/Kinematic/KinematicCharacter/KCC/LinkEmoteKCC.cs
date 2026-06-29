using Game.KinematicCharacter;
using UnityEngine;

public class LinkEmoteKCC: DefaultKCC
{
    public override KCCType KCCType => KCCType.LinkEmote;

    private const string runName = "link_run_a";
    private const string runFast = "link_run_fast_a";
    public override string RunName => runName;
    public override string RunFast => runFast;

    

    public LinkEmoteKCC(string uid, bool isSelf) : base(uid, isSelf)
    {
    }

    public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl)
    {
        base.OnEnter(motor, animCtrl);
    }

    public override void OnExit()
    {
        base.OnExit();
    }
    
}

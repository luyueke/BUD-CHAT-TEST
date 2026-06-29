using FSM;

public class SwimState : PlayerStateTemplate<PlayerStateController>
{
    public SwimState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }
    
    public override void EnterMainState()
    {
        base.EnterMainState();
        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Swim);
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Default);
    }
}

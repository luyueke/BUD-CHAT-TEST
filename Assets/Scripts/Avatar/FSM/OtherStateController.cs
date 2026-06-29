using Game.Audio;
using UnityEngine;

public class OtherStateController : PlayerStateController
{
    public override bool IsSelf => false;
    // public override bool IsSelfAIBuddy => false;

    public override void BindPlayerID(string playerId)
    {
        base.BindPlayerID(playerId);

        InitPlayerState();
    }
}

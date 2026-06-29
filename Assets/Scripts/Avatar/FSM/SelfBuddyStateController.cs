using Game.Audio;
using UnityEngine;

public class SelfBuddyStateController : PlayerStateController
{
    public override bool IsSelf => false;
    public override bool IsSelfAIBuddy => true;

    public override void BindPlayerID(string playerId)
    {
        base.BindPlayerID(playerId);

        InitPlayerState();
    }
}
using Game.Avatar;
using Game.Avatar.Kinematic.KinematicCharacter.KCC;
using UnityEngine;

public class EffectKCC : BaseSpecialKCC
{
    public EffectKCC(string uid, bool isSelf) : base(uid, isSelf)
    {
    }
    
    protected override Animator GetPGCAnimator() {
        if (pgcAnimator == null) {
            var backRoot = PlayerAnimCtrl.Wrap.GetBandNode((int)BodyNode.SpecialEffectNode);
            pgcAnimator = backRoot.GetComponentInChildren<Animator>();
            pgcAnimEventHandler = pgcAnimator?.GetComponent<AnimationEventHandler>();
            InitPGCAnimationEvent();
        }
        return pgcAnimator;
    }
}
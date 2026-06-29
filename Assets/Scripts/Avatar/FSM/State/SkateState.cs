using FSM;
using UnityEngine;

public class SkateState : PlayerStateTemplate<PlayerStateController>
{
    private readonly string SkateEffectPath = "Assets/Loadable/AnimationsExpress/Feat/SkateEffect/skate_effect.prefab";

    private GameObject skateEffect;

    public SkateState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void EnterMainState()
    {
        base.EnterMainState();

        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Skate);

        CreateSkateEffect();
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Default);

        ReleaseSkateEffect();
    }

    private void CreateSkateEffect()
    {
        var skiEffectRes = Loader.Load<GameObject>(SkateEffectPath).RetainAsset(owner.gameObject);
        skateEffect = GameObject.Instantiate(skiEffectRes, owner.transform);
    }

    private void ReleaseSkateEffect()
    {
        if (skateEffect == null) return;

        GameObject.Destroy(skateEffect);
    }
}

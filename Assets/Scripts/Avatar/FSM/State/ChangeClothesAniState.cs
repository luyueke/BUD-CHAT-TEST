using Es;
using FSM;
using System.Collections.Generic;
using UnityEngine;

public class ChangeClothesAniState : PlayerStateTemplate<PlayerStateController>
{
    private FeatAniConfig featAniConfig;
    private List<GameObject> expressionGameObject;

    public ChangeClothesAniState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);

        featAniConfig = DataTables.GetFeatAniConfigList().Find((aniConfig) => aniConfig.StateID == (int)stateID);
    }

    public override void EnterMainState()
    {
        base.EnterMainState();

        if (owner.IsSelf)
            owner.PlayerKCCtrl.SetFreezeCharacter(true);
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        if (owner.IsSelf)
            owner.PlayerKCCtrl.SetFreezeCharacter(false);
    }

    public override void CoexistState()
    {
        base.CoexistState();

        PlayChangeClothesAni();
    }

    public override void DirectIntoState()
    {
        base.DirectIntoState();

        ExitState();
    }

    public override void OnExit()
    {
        base.OnExit();

        owner.PlayerAnimCtrl.ResetEmoteAnimation();
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
    }

    public override void ReleaseData()
    {
        base.ReleaseData();

        featAniConfig = null;
    }

    protected void ExitState()
    {
        owner.ExitState(stateID);
    }

    protected virtual void PlayChangeClothesAni()
    {
        var aniConfig = featAniConfig.ConvertToAniConfig();
        owner.PlayerAnimCtrl.PlayConfigAni(aniConfig, ExitState);
        expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(aniConfig);
    }
}

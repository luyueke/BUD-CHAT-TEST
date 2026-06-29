using Es;
using FSM;
using System;
using System.Collections.Generic;
using UnityEngine;

public class ChangeClothesState : PlayerStateTemplate<PlayerStateController>
{
    private List<FeatAniConfig> aniConfgiList;
    private List<GameObject> expressionGameObject;

    public ChangeClothesState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);

        aniConfgiList = DataTables.GetFeatAniConfigList().FindAll((aniConfig) => aniConfig.StateID == (int)stateID);
    }

    public override void ReleaseData()
    {
        base.ReleaseData();

        aniConfgiList = null;
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

        var aniConfig = owner.PlayerAnimCtrl.PlayConfigLoopAni(aniConfgiList.ConvertToAniConfig(), PlayAniType.SingleLoopStart, PlayAniType.SingleLooping, CreateEffect);
        expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(aniConfig);
    }

    private List<GameObject> CreateEffect(PlayerAniConfig emoAniConfig)
    {
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
        expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);

        return expressionGameObject;
    }

    public override void OnExit()
    {
        base.OnExit();

        owner.PlayerAnimCtrl.ResetEmoteAnimation();
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
    }

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        //owner.animationController.StopLoop(emoteInfo);

        owner.PlayerAnimCtrl.StopEmoteCo();

        var emoAniConfig = owner.PlayerAnimCtrl.PlayConfigAni(aniConfgiList.ConvertToAniConfig(), PlayAniType.SingleLoopEnd, () =>
        {
            var featAniConfig = DataTables.GetFeatAniConfigList().Find((aniConfig) => aniConfig.StateID == (int)PlayerState.ChangeClothesAni);
            var aniConfig = featAniConfig.ConvertToAniConfig();
            owner.PlayerAnimCtrl.PlayConfigAni(aniConfig, () =>
            {
                onComplete?.Invoke(this);
            });
            expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(aniConfig);
        });

        CreateEffect(emoAniConfig);
    }
}

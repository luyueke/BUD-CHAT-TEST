using FSM;
using System;
using UnityEngine;

public class SkiState : PlayerStateTemplate<PlayerStateController>
{
    private readonly string SkateBoardPrefabPath = "Assets/Loadable/AnimationsExpress/Feat/SkateBoard/SkateBoard.prefab";
    private readonly string SkiEffectPath = "Assets/Loadable/AnimationsExpress/Feat/SkateBoard/ski_effect.prefab";

    private GameObject skateBoard;
    private Animator skateBoardAnimator;
    private GameObject skiEffect;

    private BoardAniType curBoardAniType = BoardAniType.Run;
    private enum BoardAniType
    {
        In = 1,
        Out,
        Run,
        Run_L,
        Run_R,
        Jump,
    }

    private PlayerChildAniState curChildAniState = PlayerChildAniState.None;
    private bool isStartBoardAni = false;

    public SkiState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void EnterMainState()
    {
        base.EnterMainState();

        StateEventManager.Inst.RegisterStateEvent<PlayerAniState>(owner.PlayerID, StateEvent.PlayerAniState, OnChangePlayerAniState);
        StateEventManager.Inst.RegisterStateEvent<PlayerChildAniState>(owner.PlayerID, StateEvent.SkiAniChange, PlaySkiAni);

        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Ski);
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(owner.PlayerID, StateEvent.PlayerAniState, OnChangePlayerAniState);
        StateEventManager.Inst.UnRegisterStateEvent<PlayerChildAniState>(owner.PlayerID, StateEvent.SkiAniChange, PlaySkiAni);

        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Default);

        RleaseSkateBorad();
        ReleaseSkiEffect();
    }

    public override void CoexistState()
    {
        base.CoexistState();

        CreateSkateBoard();

        PlayBoardAni(BoardAniType.In);
        owner.PlayerAnimCtrl.CrossFadeFixedAnimFromConfig(AnimId.SkiIn, 0.1f).OnCompleteEvent(EnterRunAniState);
    }

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        ReleaseSkiEffect();
        PlayBoardAni(BoardAniType.Out);
        isStartBoardAni = false;

        owner.PlayerAnimCtrl.CrossFadeAnimFromConfig(AnimId.SkiOut, 0.1f).OnCompleteEvent(() =>
        {
            owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
            base.PlayExitAnimation(onComplete);
        });
    }

    private void EnterRunAniState()
    {
        CreateSkiEffect();
        PlayBoardAni(BoardAniType.Run);

        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Run);
    }

    private void PlaySkiAni(PlayerChildAniState aniState)
    {
        if (curChildAniState == aniState) return;

        curChildAniState = aniState;
        owner.PlayerAnimCtrl.SetPlayerChildAniState(curChildAniState);

        switch (curChildAniState)
        {
            case PlayerChildAniState.None:
                curBoardAniType = BoardAniType.Run;
                break;
            case PlayerChildAniState.Left:
                curBoardAniType = BoardAniType.Run_L;
                break;
            case PlayerChildAniState.Right:
                curBoardAniType = BoardAniType.Run_R;
                break;
            default:
                break;
        }

        PlayBoardAni(curBoardAniType);
    }

    private void OnChangePlayerAniState(PlayerAniState aniState)
    {
        if (aniState == PlayerAniState.Jump)
        {
            PlayBoardAni(BoardAniType.Jump);
        }
        else if (aniState == PlayerAniState.Run)
        {
            PlayBoardAni(BoardAniType.Run);
        }
    }

    private void PlayBoardAni(BoardAniType aniType)
    {
        if (!isStartBoardAni) return;

        skateBoardAnimator.SetInteger("BoardState", (int)aniType);
    }

    private void CreateSkateBoard()
    {
        var skateBoradRes = Loader.Load<GameObject>(SkateBoardPrefabPath).RetainAsset(owner.gameObject);
        skateBoard = GameObject.Instantiate(skateBoradRes, owner.transform);
        skateBoardAnimator = skateBoard.GetComponent<Animator>();

        isStartBoardAni = true;
    }

    private void RleaseSkateBorad()
    {
        GameObject.Destroy(skateBoard);

        skateBoard = null;
        skateBoardAnimator = null;
    }

    private void CreateSkiEffect()
    {
        var skiEffectRes = Loader.Load<GameObject>(SkiEffectPath).RetainAsset(owner.gameObject);
        skiEffect = GameObject.Instantiate(skiEffectRes, owner.transform);
    }

    private void ReleaseSkiEffect()
    {
        if (skiEffect == null) return;

        GameObject.Destroy(skiEffect);
    }
}

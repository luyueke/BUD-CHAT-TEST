using FSM;
using GameData.PgcData;
using Message;
using Pb.Base;
using System;
using System.Collections.Generic;
using Es;
using UnityEngine;

public class LinkEmoteStartState : PlayerStateTemplate<PlayerStateController>
{
    protected List<EmoAniConfig> emoAniDataList;
    protected List<GameObject> expressionGameObject;
    protected List<GameObject> petExpressionGameObject;
    private bool withPet;
    private bool isBanAudio;
    public LinkEmoteStartState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        
        string emoteID = (string)args[0];
        if (args.Length>1)
        {
            isBanAudio = (int)args[1] == 1;
        }
        owner.emoteData = Es.DataTables.GetEmoUIConfig(emoteID);
        emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
        
        EmoteNetData netData = new EmoteNetData();
        netData.EmoteId = emoteID;
        netData.EmoteType = EmoteType.LinkEmote;
        netData.Interact = InteractType.Start;
        owner.linkEmoteData = netData;
        
        
        withPet = false;
    }

    public override void OnEnter()
    {
        base.OnEnter();

        if (owner.IsSelf)
        {
            RegisterCancelEmote();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        
        owner.PlayerAnimCtrl.ResetEmoteAnimation();
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);

        if (withPet)
        {
            owner.PetAnimCtrl?.ResetEmoteAnimation();
            owner.PetAnimCtrl?.ClearExpression(petExpressionGameObject);
            withPet = false;
        }

        if (owner.IsSelf)
        {
            UnRegisterCancelEmote();
        }
    }

    public override void ReleaseData()
    {
        base.ReleaseData();
        
        emoAniDataList?.Clear();
        emoAniDataList = null;
        if (owner != null)
        {
            owner.emoteData = null;
        }
        
        if (owner != null)
        {
            owner.linkEmoteData = null;
        }
    }

    private void RegisterCancelEmote()
    {
        StateEventManager.Inst.RegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick, OnMoveJoystick);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, OnJump);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.PetSingleEmote, OnPetSingleEmote);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.Teleport, OnTeleport);
        
    }

    private void UnRegisterCancelEmote()
    {
        StateEventManager.Inst.UnRegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick, OnMoveJoystick);
        StateEventManager.Inst.UnRegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, OnJump);
        StateEventManager.Inst.UnRegisterStateEvent(owner.PlayerID, StateEvent.PetSingleEmote, OnPetSingleEmote);
        StateEventManager.Inst.UnRegisterStateEvent(owner.PlayerID, StateEvent.Teleport, OnTeleport);
    }

    private void OnMoveJoystick(float axisForward, float axisRight)
    {
        if (axisForward != 0 || axisRight != 0)
        {
            SelfCancelEmote();
            ExitState();
        }
    }

    private void OnJump()
    {
        SelfCancelEmote();
        ExitState();
    }
    
	private void OnTeleport()
    {
        SelfCancelEmote();
        ExitState();
    }

    private void OnPetSingleEmote()
    {
        if (owner.IsSelf)
        {
            if (owner.emoteData != null && (owner.emoteData.emoType == (int)EmoteSubType.PetWithPlayer || owner.emoteData.emoType == (int)EmoteSubType.PetWithPlayerLoop))
            {
                // 正在进行人宠交互，要主动退出
                SelfCancelEmote();
                owner.ExitState(PlayerState.SingleEmote, false);
            }
        }
    }
	
    private void SelfCancelEmote()
    {
        if (owner?.emoteData == null) return;

        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = AccountDataManager.Inst.Uid;
        netData.EmoteId = owner?.emoteData?.pgcId;
        netData.EmoteType = (EmoteType)owner?.emoteData?.emoAniType;
        netData.Interact = InteractType.End;

        MessageHelper.Broadcast(MessageName.SelfCancelEmote, netData);
    }

    public override void EnterMainState()
    {
        base.EnterMainState();

        owner.PlayerKCCtrl.SetFreezeCharacter(true);
        owner.PlayerKCCtrl.Motor.SetIsOnSimulate(false);
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        owner.PlayerKCCtrl.SetFreezeCharacter(false);
        owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
        owner.PetKCCtrl.SetFreezeCharacter(false);
        owner.PetKCCtrl.Motor.SetIsOnSimulate(true);
        owner.PetKCCtrl.GetComponent<PetMoveControllerKcc>().enabled = true;
    }

    public override void CoexistState()
    {
        base.CoexistState();

        if (owner.emoteData == null)
        {
            LoggerUtils.Log("owner.emoteData is null");
            return;
        }
        var pgcId = owner.emoteData.pgcId;

        owner.PlayerAnimCtrl.DownloadAnimationAB(pgcId, (success) =>
        {
            if (owner.emoteData == null || pgcId != owner.emoteData.pgcId) return;

            if (!success)
            {
                ExitState();
                return;
            }

            if (emoAniDataList.Count == 1)
            {
                var emoAniConfig = emoAniDataList[0].ConvertToAniConfig();
                owner.PlayerAnimCtrl.PlayConfigAni(emoAniConfig, ExitState,isPlaySound:!isBanAudio);
                expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);
            }
            else
            {
                PlayerAniConfig emoAniConfig;
                if (owner.emoteData.emoType == 1 || owner.emoteData.emoType == 2)
                {
                    emoAniConfig = owner.PlayerAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.SingleLoopStart, PlayAniType.SingleLooping, CreateEffect,isPlaySound:!isBanAudio);
                    expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);
                }
                else if (owner.emoteData.emoType == 3 || owner.emoteData.emoType == 4 || owner.emoteData.emoType == (int)EmoteType.LinkEmote)
                {
                    emoAniConfig = owner.PlayerAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.DoubleStart, PlayAniType.DoubleLoop, CreateEffect,isPlaySound:!isBanAudio);
                    owner.emoteOPId = null;
                    expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);
                }
            }
        });
    }

    public override void InterruptState(PlayerState beState)
    {
        base.InterruptState(beState);
        SelfCancelEmote();
    }

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        if (emoAniDataList.Count == 1 || owner.emoteData.emoType == 7)
        {
            onComplete?.Invoke(this);
        }
        else
        {
            if (owner.emoteData.emoType == 1 || owner.emoteData.emoType == 2 || owner.emoteData.emoType == 3 || owner.emoteData.emoType == 4)
            {
                owner?.PlayerAnimCtrl?.StopEmoteCo();

                PlayAniType aniType = owner?.emoteData?.emoType == 2 ? PlayAniType.SingleLoopEnd : PlayAniType.DoubleEnd;

                var emoAniConfig = owner?.PlayerAnimCtrl?.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), aniType, () =>
                {
                    onComplete?.Invoke(this);
                },isPlaySound:!isBanAudio);

                CreateEffect(emoAniConfig);
            }
            else if (owner.emoteData.emoType == 8)
            {
                owner?.PlayerAnimCtrl?.StopEmoteCo();
                owner?.PetAnimCtrl?.StopEmoteCo();
                var emoAniConfig = owner?.PetAnimCtrl?.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.PetWithPlayerLoopPetAEnd, () =>
                {
                    owner?.PetAnimCtrl.ResetEmoteAnimation();
                    onComplete?.Invoke(this);
                },isPlaySound:!isBanAudio);
                CreateEffect(emoAniConfig);

                emoAniConfig = owner?.PlayerAnimCtrl?.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.PetWithPlayerLoopPlayerAEnd, null,isPlaySound:!isBanAudio);
                CreateEffect(emoAniConfig);
            }
            else
            {
                onComplete?.Invoke(this);
            }

            owner.emoteData = null;
        }
    }

    public override bool RepeatEntry(params object[] args)
    {
        return false;
    }

    private List<GameObject> CreateEffect(PlayerAniConfig emoAniConfig)
    {
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
        expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);

        return expressionGameObject;
    }
    
    protected void ExitState()
    {
        owner.ExitState(stateID);
    }
}


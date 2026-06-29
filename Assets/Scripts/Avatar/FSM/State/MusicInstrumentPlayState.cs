using Es;
using FSM;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Pb.Game;
using System;
using System.Linq;
using UIAgent;
using UnityEngine;

public class MusicalSyncParam
{
    public string moveId;
    public string resId;
    public InstrumentDetailInfo detailInfo;
    public bool isReconnect;
    public bool isHide;
}

public class MusicInstrumentPlayState : PlayerStateTemplate<PlayerStateController>
{
    private PlayerHoldBehaviour playerHoldBehaviour;
    private MusicalSyncParam _musicalSyncParam;
    private bool _isSelfPlayer;
    private string _petLoopAnimId = "40600001";

    public MusicInstrumentPlayState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }
    

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        if (args != null && args.Length > 0)
        {
            _musicalSyncParam = (MusicalSyncParam)args[0];
        }
        
        playerHoldBehaviour = owner.GetComponentInChildren<PlayerHoldBehaviour>(true);
        _isSelfPlayer = owner.GetComponentInChildren<SelfStateController>(true) != null;
    }
    
    public override void EnterMainState()
    {
        base.EnterMainState();
        owner.PlayerKCCtrl.SetFreezeCharacter(true);
        if (owner.PetAnimCtrl != null)
        {
            owner.PetEmoState.OnPetBeginFollow(true);
            owner.PetAnimCtrl.PlayInstrumentIdleAni(owner.PlayerID);
        }

        // owner.PlayerKCCtrl.Motor.SetIsOnSimulate(false);
        StateEventManager.Inst.RegisterStateEvent<MusicalNetData>(owner.PlayerID, StateEvent.MusicInstrumentPlay, OnRecvPlayBst);
    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        owner.PlayerKCCtrl.SetFreezeCharacter(false);
        // owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
   
        StateEventManager.Inst.UnRegisterStateEvent< MusicalNetData>(owner.PlayerID, StateEvent.MusicInstrumentPlay, OnRecvPlayBst);
        playerHoldBehaviour.HideMusicHold();
        owner.PlayerAnimCtrl.ResetEmoteAnimation();
        if (owner.PetAnimCtrl != null)
        {
            owner.PetAnimCtrl.ResetEmoteForUICharacter(); 
            owner.PetEmoState.OnPetBeginFollow(true);
        }
    }

    private void OnRecvPlayBst(MusicalNetData musicalNetData)
    {
        if (musicalNetData.IsBanAudio==1)
        {
            return;
        }
        playerHoldBehaviour.PlayMusicSyllable(musicalNetData.SyllList.ToList());
    }


    public override void CoexistState()
    {
        base.CoexistState();
       
    }

    public override void OnEnter()
    {
        base.OnEnter();
        var config = DataTables.GetInstrumentConfig(_musicalSyncParam.resId);
        if (config != null)
        {
            owner.Wrap.ChangePart(UniqueType.GetAvatar(AvatarSubType.MusicalInstrument), _musicalSyncParam.resId, () =>
            {
                playerHoldBehaviour.StartPlayInstrument(_musicalSyncParam);
            });
        }
        else
        {
            UIAgentManager.Inst.GetBatchInfo(_musicalSyncParam.resId, (t) =>
            {
                owner.Wrap.ChangeUGCPart(t.skinInfo, () =>
                {
                    playerHoldBehaviour.PreviewUGCInstrument(t.skinActionInfo.instrumentInfo);
                });
            });
        }
    }

    public override void OnExit()
    {
        if (_musicalSyncParam != null && _musicalSyncParam.isHide)
        {
            playerHoldBehaviour.SetActiveMusic(false);
        }
        base.OnExit();
    }


    public override void DirectIntoState()
    {
        base.DirectIntoState();
    }

    public override void InterruptState(PlayerState beState)
    {
        base.InterruptState(beState);
    }
    
    
    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        playerHoldBehaviour.ExitPlayInstrument(true,() =>
        {
            onComplete?.Invoke(this);

            if (_isSelfPlayer)
            {
                MessageHelper.Broadcast(MessageName.OnExitPlayInstrumentAnim);
            }
        });
    }


    public override void ReleaseData()
    {
        base.ReleaseData();
    }

    
    protected void ExitState()
    {
        owner.ExitState(stateID);
    }
}

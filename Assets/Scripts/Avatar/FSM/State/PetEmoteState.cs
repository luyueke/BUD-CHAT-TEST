using Es;
using FSM;
using GameData.PgcData;
using Message;
using Pb.Base;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PetEmoteState : MonoBehaviour
{
    public PlayerStateController owner;
    private int randomID;
    private EmoUIConfig petEmoteData;
    private List<EmoAniConfig> emoAniDataList;
    protected List<GameObject> expressionGameObject;
    private bool isBanAudio;
    public void PlayEmote(string emoteID, int random,int banAudio = 0)
    {
        if (petEmoteData != null)
        {
            if (owner.IsSelf) SelfInterruptEmote();
        }

        petEmoteData = Es.DataTables.GetEmoUIConfig(emoteID);
        emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
        randomID = random;
        isBanAudio = banAudio == 1;
        OnEnter();
    }

    public void OnEnter()
    {
        if (owner.IsSelf)
        {
            RegisterCancelEmote();
        }

        owner.PetKCCtrl.SetFreezeCharacter(true);
        owner.PetKCCtrl.Motor.SetIsOnSimulate(false);

        PlayEmote();
    }

    public void OnExit()
    {
        owner.PetAnimCtrl?.ResetEmoteAnimation();
        owner.PetAnimCtrl?.ClearExpression(expressionGameObject);

        if (owner.IsSelf)
        {
            UnRegisterCancelEmote();
        }

        owner.PetKCCtrl.SetFreezeCharacter(false);
        owner.PetKCCtrl.Motor.SetIsOnSimulate(true);

        petEmoteData = null;
        emoAniDataList = null;
        randomID = 0;
    }

    private void RegisterCancelEmote()
    {
        StateEventManager.Inst.RegisterStateEvent<bool>(owner.PlayerID, StateEvent.PetBeginFollow, OnPetBeginFollow);
    }

    private void UnRegisterCancelEmote()
    {
        StateEventManager.Inst.UnRegisterStateEvent<bool>(owner.PlayerID, StateEvent.PetBeginFollow, OnPetBeginFollow);
    }

    public void OnPetBeginFollow(bool begin)
    {
        if (begin)
        {
            SelfCancelEmote();
            OnExit();
        }
    }

    private void SelfCancelEmote()
    {
        UnRegisterCancelEmote();

        if (petEmoteData == null) return;

        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = AccountDataManager.Inst.Uid;
        netData.EmoteId = petEmoteData?.pgcId;
        netData.EmoteType = (EmoteType)petEmoteData?.emoAniType;
        netData.Interact = InteractType.End;

        MessageHelper.Broadcast(MessageName.SelfCancelEmote, netData);
    }

    private void SelfInterruptEmote()
    {
        if (petEmoteData == null) return;

        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = AccountDataManager.Inst.Uid;
        netData.EmoteId = petEmoteData?.pgcId;
        netData.EmoteType = (EmoteType)petEmoteData?.emoAniType;
        netData.Interact = InteractType.End;

        MessageHelper.Broadcast(MessageName.SelfCancelEmote, netData);

        owner.PetAnimCtrl?.ResetEmoteAnimation();
        owner.PetAnimCtrl?.ClearExpression(expressionGameObject);

        petEmoteData = null;
    }

    private void PlayEmote()
    {
        if (petEmoteData == null)
        {
            LoggerUtils.Log("owner.emoteData is null");
            return;
        }
        var pgcId = petEmoteData.pgcId;
        owner.PetAnimCtrl.DownloadAnimationAB(pgcId, (success) =>
        {
            if (petEmoteData == null || pgcId != petEmoteData.pgcId) return;

            if (!success)
            {
                ExitState();
                return;
            }

            if (emoAniDataList.Count == 1)
            {
                var emoAniConfig = emoAniDataList[0].ConvertToAniConfig();
                owner.PetAnimCtrl.PlayConfigAni(emoAniConfig, ExitState,isPlaySound:!isBanAudio);
                expressionGameObject = owner.PetAnimCtrl.CreateExpression(emoAniConfig);
                if (emoAniDataList[0].randomCount > 0)
                {
                    owner.PetAnimCtrl.SetRandomMove(expressionGameObject, emoAniConfig.randomTexPath, randomID);
                }
            }
            else
            {
                PlayerAniConfig emoAniConfig;
                emoAniConfig = owner.PetAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.PetSingleLoopStart, PlayAniType.PetSingleLooping, CreateEffect,isPlaySound:!isBanAudio);
                expressionGameObject = owner.PetAnimCtrl.CreateExpression(emoAniConfig);
            }
        });
    }

    public void ExitState()
    {
        if (emoAniDataList == null) return;

        if (emoAniDataList.Count == 1)
        {
            OnExit();
        }
        else
        {
            owner?.PetAnimCtrl?.StopEmoteCo();

            var emoAniConfig = owner?.PetAnimCtrl?.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.PetSingleLoopEnd, () =>
            {
                OnExit();
            },isPlaySound:!isBanAudio);

            CreateEffect(emoAniConfig);
        }
    }

    private List<GameObject> CreateEffect(PlayerAniConfig emoAniConfig)
    {
        owner.PetAnimCtrl.ClearExpression(expressionGameObject);
        expressionGameObject = owner.PetAnimCtrl.CreateExpression(emoAniConfig);

        return expressionGameObject;
    }
}


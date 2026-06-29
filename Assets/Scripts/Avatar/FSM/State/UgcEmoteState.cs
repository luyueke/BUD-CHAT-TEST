using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Es;
using FSM;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using Pb.Base;
using UnityEngine;
using xasset;

public class UgcEmoteState: PlayerStateTemplate<PlayerStateController>
{
    private EmoteNetData emoteData;
    private bool isBanAudio;
    public UgcEmoteState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        emoteData = (EmoteNetData)args[0];
        isBanAudio = emoteData.IsBanAudio == 1;
    }

    public override void OnEnter()
    {
        base.OnEnter();

        if (owner.IsSelf || owner.IsSelfAIBuddy)
        {
            RegisterCancelEmote();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        if (owner.IsSelf || owner.IsSelfAIBuddy)
        {
            UnRegisterCancelEmote();
        }

        if (emoteData == null) return;
        if (emoteData.EmoteType == EmoteType.DoubleLoop || emoteData.EmoteType == EmoteType.DoubleOnce)
        {
            owner.PlayerAnimCtrl.ResetEmoteAnimation();
        }

        if (owner.IsAIBuddy)
        {
            BuddyNormalOnExit();
        }
        else
        {
            NormalOnExit();
        }
    }

    private void NormalOnExit()
    {
        var playerController = AvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
        if (playerController != null && playerController.Wrap != null)
        {
            var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
            playerAnimIkController.StopAnim();
            playerAnimIkController.ChangeUgcToPgcAnim();
            playerController.isUgcDoubleAnim = false;
            playerController.ugcEmoteData = null;
            var oriParent = playerController.PetKCCtrl.transform.GetChild(0);
            var curPetParent = playerController.PetWrap.CustomAvatar.transform.parent;
            if (oriParent != curPetParent)
            {
                playerController.PetWrap.CustomAvatar.transform.SetParent(oriParent);
                playerController.PetWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                playerController.PetWrap.CustomAvatar.transform.localEulerAngles = Vector3.zero;
            }
            var petAnimIkController = playerController.PetWrap.Avatar.GetComponent<AnimIKController>();
            petAnimIkController.StopAnim();
            petAnimIkController.ChangeUgcToPgcAnim();
        }
    }
    
    private void BuddyNormalOnExit()
    {
        var playerController = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
        if (playerController != null && playerController.Wrap != null)
        {
            var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
            playerAnimIkController.StopAnim();
            playerAnimIkController.ChangeUgcToPgcAnim();
            playerController.isUgcDoubleAnim = false;
            playerController.ugcEmoteData = null;
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
            ExitState();
        }
    }

    private void OnJump()
    {
        ExitState();
    }
    
	private void OnTeleport()
    {
        ExitState();
    }
    
    

    private void OnPetSingleEmote()
    {
        if (owner.IsSelf)
        {
            if (emoteData.AnimResType == 0)
            {
                owner.emoteData = Es.DataTables.GetEmoUIConfig(emoteData.EmoteId);
                if (owner.emoteData != null && (owner.emoteData.emoType == (int)EmoteSubType.PetWithPlayer || owner.emoteData.emoType == (int)EmoteSubType.PetWithPlayerLoop))
                {
                    // 正在进行人宠交互，要主动退出
                    ExitState();
                }
            }
            else
            {
                if (owner.emoteData != null && (owner.emoteData.emoType == (int)EmoteSubType.PetWithPlayer))
                {
                    // 正在进行人宠交互，要主动退出
                    ExitState();
                }
            }
        }
    }
	
    private void SelfCancelEmote()
    {
        if (emoteData == null) return;

        emoteData.Interact = InteractType.End;
        MessageHelper.Broadcast(MessageName.SelfCancelEmote, emoteData);
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
        if (owner.PetKCCtrl != null)
        {
            owner.PetKCCtrl.SetFreezeCharacter(false);
            owner.PetKCCtrl.Motor.SetIsOnSimulate(true);
            owner.PetKCCtrl.GetComponent<PetMoveControllerKcc>().enabled = true;
        }
    }
    
    private void DownloadAnim(string url,Action<AnimFrameData> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            LoggerUtils.LogError("url is null "+url);
            return;
        }

        var assetRequest = Asset.LoadRemoteAssetAsync(url);
        if (assetRequest != null)
        {
            assetRequest.completed += (_) =>
            {
                if (assetRequest.result == Request.Result.Success)
                {
                    var content = System.Text.Encoding.UTF8.GetString(assetRequest.asset);
                    var frameData = JsonConvert.DeserializeObject<AnimFrameData>(content);
                    callback?.Invoke(frameData);
                }
                else
                {
                    callback?.Invoke(null);
                }
            };
        }
    }

    public override void CoexistState()
    {
        base.CoexistState();
        if (emoteData == null) return;

        if (owner.IsAIBuddy)
        {
            BuddyCoexistState();
        }
        else
        {
            NormalCoexistState();
        }
    }

    private void NormalCoexistState()
    {
        //发起方
        var playerController = AvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
        var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
        var playerAnimIk = playerController.Wrap.Avatar.GetComponent<AvatarAnimIK>();

        var petAnimIkController = playerController.PetWrap.Avatar.GetComponent<AnimIKController>();
        var petAnimIk = playerController.PetWrap.Avatar.GetComponent<PetAnimIK>();

        playerAnimIk.ResetDefaultPosition();
        petAnimIk.ResetDefaultPosition();

        List<AnimPropData> propList = new List<AnimPropData>();
        if (emoteData.PropList != null && emoteData.PropList.Count != 0)
        {
            foreach (var propData in emoteData.PropList)
            {
                AnimPropData animData = new AnimPropData()
                {
                    index = propData.Index,
                    bindIndex = propData.BindIndex,
                    metaDataUrl = propData.MetaDataUrl,
                    id = propData.Id
                };
                propList.Add(animData);
            }
        }

        switch (emoteData.EmoteType)
        {
            case EmoteType.SingleOnce:
            case EmoteType.SingleLoop:
                playerAnimIkController.RemoveOtherAnimIk();
                playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
                playerAnimIkController.CreatePropIKs(UgcPoseSubType.Single, propList,false);
                DownloadAnim(emoteData.UgcAnimUrl, (frameData) =>
                {
                    if (frameData != null)
                    {
                        playerAnimIkController.SetAnimFrameData(frameData);
                        if (emoteData.IsUgcLoop == 0)
                        {
                            playerAnimIkController.PlayOnceAnim(ExitState,isPlaySound:!isBanAudio);
                        }
                        else
                        {
                            playerAnimIkController.PlayLoopAnim(isPlaySound:!isBanAudio);
                        }
                    }
                });
                break;
            case EmoteType.DoubleOnce:
            case EmoteType.DoubleLoop:
                //发起者
                // playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
                if (emoteData.SenderId == AccountDataManager.Inst.Uid)
                {
                    string pgcId = "40300103";//使用双人动画起手式
                    owner.PlayerAnimCtrl.DownloadAnimationAB(pgcId, (success) =>
                    {
                        if (!success)
                        {
                            ExitState();
                            return;
                        }
                        owner.emoteData = Es.DataTables.GetEmoUIConfig(pgcId);
                        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == pgcId);
                        if (owner.emoteData.emoType == 3)
                        {
                            owner.PlayerAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.DoubleStart, PlayAniType.DoubleLoop, CreateEffect,isPlaySound:!isBanAudio);
                        }
                    });
                    return;
                }
                playerController.isUgcDoubleAnim = true;
                playerController.ugcEmoteData = emoteData;
                break;
            case EmoteType.PetSingle:
            case EmoteType.PetSingleLoop:
                petAnimIkController.ChangeAnimResType(AnimResType.UGC);
                petAnimIkController.CreatePropIKs(UgcPoseSubType.PetSingle, propList,false);
                DownloadAnim(emoteData.UgcAnimUrl, (frameData) =>
                {
                    if (frameData != null)
                    {
                        petAnimIkController.SetAnimFrameData(frameData);
                        if (emoteData.IsUgcLoop == 0)
                        {
                            petAnimIkController.PlayOnceAnim(ExitState,isPlaySound:!isBanAudio);
                        }
                        else
                        {
                            petAnimIkController.PlayLoopAnim(isPlaySound:!isBanAudio);
                        }
                    }
                });
                break;
            case EmoteType.PetWithPlayer:
            case EmoteType.PetWithPlayerLoop:
                var tempParent = playerController.Wrap.CustomAvatar.transform.parent;
                playerController.PetWrap.CustomAvatar.transform.SetParent(tempParent);
                playerController.PetWrap.CustomAvatar.transform.localPosition = Vector3.zero;
                playerController.PetWrap.CustomAvatar.transform.localEulerAngles = Vector3.zero;
                
                playerAnimIkController.RemoveOtherAnimIk();
                playerAnimIkController.AddAnimIK(petAnimIk);
                playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
                playerAnimIkController.CreatePropIKs(UgcPoseSubType.PetWithPlayer, propList,false);
                
                DownloadAnim(emoteData.UgcAnimUrl, (frameData) =>
                {
                    if (frameData != null)
                    {
                        playerAnimIkController.SetAnimFrameData(frameData);
                        if (emoteData.IsUgcLoop == 0)
                        {
                            playerAnimIkController.PlayOnceAnim(ExitState,isPlaySound:!isBanAudio);
                        }
                        else
                        {
                            playerAnimIkController.PlayLoopAnim(isPlaySound:!isBanAudio);
                        }
                    }
                });
                break;
        }
    }
    
    private void BuddyCoexistState()
    {
        //发起方
        var playerController = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
        var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
        var playerAnimIk = playerController.Wrap.Avatar.GetComponent<AvatarAnimIK>();
        playerAnimIk.ResetDefaultPosition();

        List<AnimPropData> propList = new List<AnimPropData>();
        if (emoteData.PropList != null && emoteData.PropList.Count != 0)
        {
            foreach (var propData in emoteData.PropList)
            {
                AnimPropData animData = new AnimPropData()
                {
                    index = propData.Index,
                    bindIndex = propData.BindIndex,
                    metaDataUrl = propData.MetaDataUrl,
                    id = propData.Id
                };
                propList.Add(animData);
            }
        }

        switch (emoteData.EmoteType)
        {
            case EmoteType.SingleOnce:
            case EmoteType.SingleLoop:
                playerAnimIkController.RemoveOtherAnimIk();
                playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
                playerAnimIkController.CreatePropIKs(UgcPoseSubType.Single, propList,false);
                DownloadAnim(emoteData.UgcAnimUrl, (frameData) =>
                {
                    if (frameData != null)
                    {
                        playerAnimIkController.SetAnimFrameData(frameData);
                        if (emoteData.IsUgcLoop == 0)
                        {
                            playerAnimIkController.PlayOnceAnim(ExitState,isPlaySound:!isBanAudio);
                        }
                        else
                        {
                            playerAnimIkController.PlayLoopAnim(isPlaySound:!isBanAudio);
                        }
                    }
                });
                break;
            case EmoteType.DoubleOnce:
            case EmoteType.DoubleLoop:
                //发起者
                if (emoteData.SenderId == AccountDataManager.Inst.Uid)
                {
                    string pgcId = "40300103";//使用双人动画起手式
                    owner.PlayerAnimCtrl.DownloadAnimationAB(pgcId, (success) =>
                    {
                        if (!success)
                        {
                            ExitState();
                            return;
                        }
                        owner.emoteData = Es.DataTables.GetEmoUIConfig(pgcId);
                        var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == pgcId);
                        if (owner.emoteData.emoType == 3)
                        {
                            owner.PlayerAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(), PlayAniType.DoubleStart, PlayAniType.DoubleLoop, CreateEffect,isPlaySound:!isBanAudio);
                        }
                    });
                    return;
                }
                playerController.isUgcDoubleAnim = true;
                playerController.ugcEmoteData = emoteData;
                break;
        }
    }
    
    private List<GameObject> CreateEffect(PlayerAniConfig emoAniConfig)
    {
        return null;
    }

    public override void InterruptState(PlayerState beState)
    {
        base.InterruptState(beState);
        SelfCancelEmote();
    }
    
    public override bool RepeatEntry(params object[] args)
    {
        return false;
    }

    protected void ExitState()
    {
        SelfCancelEmote();
        owner.ExitState(stateID);
    }
    
}
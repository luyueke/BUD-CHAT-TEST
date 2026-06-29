using System;
using System.Collections.Generic;
using BUD.AnimPose;
using FSM;
using Game.Avatar;
using GameData.BaseInfo;
using GameData.PgcData;
using Message;
using Newtonsoft.Json;
using Pb.Base;
using UnityEngine;
using xasset;

public class UgcDoubleEmoteState: PlayerStateTemplate<PlayerStateController>
{
    private EmoteNetData emoteData;
    private bool isBanAudio;
    public UgcDoubleEmoteState(PlayerState id, PlayerStateController owner) : base(id, owner)
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
        if (emoteData == null)
        {
            return;
        }
        
        if (owner.PlayerID == emoteData.SenderId)
        {
            owner.PlayerAnimCtrl.ResetEmoteAnimation();
            var playerController = AvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
            if (playerController != null && playerController.Wrap != null)
            {
                var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
                playerAnimIkController.StopAnim();
                playerAnimIkController.ChangeUgcToPgcAnim();
            }

            PlayerStateController receiverStateCtrl = null;
            if (owner.IsSelfAIBuddy)
            {
                receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
            }
            else
            {
                receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteData.ReceiverId);
            }
        
            if (receiverStateCtrl != null && receiverStateCtrl.Wrap != null)
            { 
                var oriParent = receiverStateCtrl.PlayerKCCtrl.transform.GetChild(0);
                var curPetParent = receiverStateCtrl.Wrap.CustomAvatar.transform.parent;
                if (oriParent != curPetParent)
                {
                    receiverStateCtrl.Wrap.CustomAvatar.transform.SetParent(oriParent);
                    receiverStateCtrl.Wrap.CustomAvatar.transform.localPosition = Vector3.zero;
                    receiverStateCtrl.Wrap.CustomAvatar.transform.localEulerAngles = Vector3.zero;
                }
                var receiverAnimIkController = receiverStateCtrl.Wrap.Avatar.GetComponent<AnimIKController>();
                receiverAnimIkController.StopAnim();
                receiverAnimIkController.ChangeUgcToPgcAnim();
            }
        }
        
        if (owner.IsSelf || owner.IsSelfAIBuddy)
        {
            UnRegisterCancelEmote();
        }
    }
    
    private void RegisterCancelEmote()
    {
        StateEventManager.Inst.RegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick, OnMoveJoystick);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, OnJump);
        StateEventManager.Inst.RegisterStateEvent(owner.PlayerID, StateEvent.Teleport, OnTeleport);
    }

    private void UnRegisterCancelEmote()
    {
        StateEventManager.Inst.UnRegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick, OnMoveJoystick);
        StateEventManager.Inst.UnRegisterStateEvent(owner.PlayerID, StateEvent.JumpBtn, OnJump);
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

    private void SelfCancelEmote()
    {
        if (emoteData == null) return;
        InteractType interactType = emoteData.EmoteType == EmoteType.DoubleOnce ? InteractType.End : InteractType.InteractEnd;
        emoteData.Interact = interactType;
        
        if (owner.IsSelfAIBuddy)
        {
            if (emoteData.EmoteType == EmoteType.DoubleOnce)
            {
                emoteData.EmoteType = EmoteType.BuddyWithPlayerOnce;
            }
            else
            {
                emoteData.EmoteType = EmoteType.BuddyWithPlayerLoop;
            }
            emoteData.Interact = InteractType.End;
            emoteData.BuddyId = AccountDataManager.Inst.Uid;
        }
        
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
        if (owner.PlayerKCCtrl != null)
        {
            owner.PlayerKCCtrl.SetFreezeCharacter(false);
            owner.PlayerKCCtrl.Motor.SetIsOnSimulate(true);
        }

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

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        onComplete?.Invoke(this);
    }

    public override void CoexistState()
    {
        base.CoexistState();
        if (emoteData == null)
        {
            LoggerUtils.LogError("ugcdoubleemotestate error");
            return;
        }
        List<AnimPropData> propList = new List<AnimPropData>();
        if (emoteData != null && emoteData.PropList != null && emoteData.PropList.Count != 0)
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
            case EmoteType.BuddyWithPlayerOnce:
            case EmoteType.BuddyWithPlayerLoop:
                if (owner.PlayerID == emoteData.SenderId)
                {

                    var playerController = AvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
                    playerController.isUgcDoubleAnim = false;
                    playerController.emoteData = null;
                    var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
                    var playerAnimIk = playerController.Wrap.Avatar.GetComponent<AvatarAnimIK>();
                    playerAnimIk.ResetDefaultPosition();


                    var receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);

                    var tempParent = playerController.Wrap.CustomAvatar.transform.parent;
                    receiverStateCtrl.Wrap.CustomAvatar.transform.SetParent(tempParent);
                    receiverStateCtrl.Wrap.CustomAvatar.transform.localPosition = Vector3.zero;
                    receiverStateCtrl.Wrap.CustomAvatar.transform.localEulerAngles = Vector3.zero;

                    var receiverAnimIk = receiverStateCtrl.Wrap.Avatar.GetComponent<AvatarAnimIK>();
                    receiverAnimIk.ResetDefaultPosition();
                    
                    playerAnimIkController.RemoveOtherAnimIk();
                    playerAnimIkController.AddAnimIK(receiverAnimIk);
                    playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
                    playerAnimIkController.CreatePropIKs(UgcPoseSubType.Double, propList,false);
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
                }
                break;
            case EmoteType.DoubleOnce:
            case EmoteType.DoubleLoop:
                if (owner.PlayerID == emoteData.SenderId)
                {

                    var playerController = AvatarController.Inst.GetPlayerStateCtrl(emoteData.SenderId);
                    playerController.isUgcDoubleAnim = false;
                    playerController.emoteData = null;
                    var playerAnimIkController = playerController.Wrap.Avatar.GetComponent<AnimIKController>();
                    var playerAnimIk = playerController.Wrap.Avatar.GetComponent<AvatarAnimIK>();
                    playerAnimIk.ResetDefaultPosition();


                    var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteData.ReceiverId);

                    var tempParent = playerController.Wrap.CustomAvatar.transform.parent;
                    receiverStateCtrl.Wrap.CustomAvatar.transform.SetParent(tempParent);
                    receiverStateCtrl.Wrap.CustomAvatar.transform.localPosition = Vector3.zero;
                    receiverStateCtrl.Wrap.CustomAvatar.transform.localEulerAngles = Vector3.zero;

                    var receiverAnimIk = receiverStateCtrl.Wrap.Avatar.GetComponent<AvatarAnimIK>();
                    receiverAnimIk.ResetDefaultPosition();
                    
                    playerAnimIkController.RemoveOtherAnimIk();
                    playerAnimIkController.AddAnimIK(receiverAnimIk);
                    playerAnimIkController.ChangeAnimResType(AnimResType.UGC);
                    playerAnimIkController.CreatePropIKs(UgcPoseSubType.Double, propList,false);
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
                }
                break;
        }
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
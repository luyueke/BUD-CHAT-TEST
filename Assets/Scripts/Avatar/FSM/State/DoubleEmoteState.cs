using FSM;
using Message;
using Pb.Base;
using System.Collections.Generic;
using System;
using UnityEngine;
using Es;
using Game.Avatar;
using Game.Config;
using Newtonsoft.Json;

public class DoubleEmoteState : BaseEmoteState
{
    // 双人Emote发送者
    private bool isEmoteSender = true;
    private bool isBanAudio;
    // 取消监听注册的目标 id：本人/自身 buddy 用 owner.PlayerID；地图 buddy 用配对玩家 emoteOPId（自身 id 收不到玩家移动事件）
    private string _cancelListenId;
    // 地图 buddy 进入双人emote前的放置位/朝向，退出时归位（静态 buddy 无 follow 自动归位）
    private bool _hasMapBuddyHome;
    private Vector3 _mapBuddyHomePos;
    private Quaternion _mapBuddyHomeRot;
    public DoubleEmoteState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);

        isEmoteSender = (bool)args[1];
        owner.emoteOPId = args[2].ToString();
        owner.emoteOPId = args[2].ToString();
        if (args.Length>3)
        {
            isBanAudio = (int)args[3] == 1;
        }
    }

    public override void OnEnter()
    {
        base.OnEnter();

        // 地图共享 buddy：记录进入前的放置位/朝向，退出时归位（重定位到表演位后无 follow 自动还原）
        if (AIBuddyAvatarController.IsMapBuddyKey(owner.PlayerID) && owner.PlayerKCCtrl != null)
        {
            _mapBuddyHomePos = owner.PlayerKCCtrl.transform.position;
            _mapBuddyHomeRot = owner.PlayerKCCtrl.transform.rotation;
            _hasMapBuddyHome = true;
        }

        if ((EmoteType)owner.emoteData.emoAniType == EmoteType.DoubleLoop)
        {
            if (owner.IsSelf || owner.IsSelfAIBuddy)
            {
                _cancelListenId = owner.PlayerID;
                RegisterCancelEmote(_cancelListenId);
            }
            else if (AIBuddyAvatarController.IsMapBuddyKey(owner.PlayerID))
            {
                // 地图 buddy 自身 PlayerID 收不到玩家移动事件，改监听配对玩家(emoteOPId)的移动以同步取消
                _cancelListenId = owner.emoteOPId;
                RegisterCancelEmote(_cancelListenId);
            }
        }
    }

    public override void OnExit()
    {
        base.OnExit();

        if ((EmoteType)owner.emoteData.emoAniType == EmoteType.DoubleLoop)
        {
            if (!string.IsNullOrEmpty(_cancelListenId))
            {
                UnRegisterCancelEmote(_cancelListenId);
                _cancelListenId = null;
            }
        }

        // 地图共享 buddy：退出双人emote后归位到放置点（静态 buddy 无 follow，否则停在表演位）
        if (_hasMapBuddyHome && owner.PlayerKCCtrl != null && owner.PlayerKCCtrl.Motor != null)
        {
            owner.PlayerKCCtrl.Motor.SetPositionAndRotation(_mapBuddyHomePos, _mapBuddyHomeRot);
            _hasMapBuddyHome = false;
        }
    }

    private void RegisterCancelEmote(string listenId)
    {
        StateEventManager.Inst.RegisterStateEvent<float, float>(listenId, StateEvent.MoveJoystick, OnMoveJoystick);
        StateEventManager.Inst.RegisterStateEvent(listenId, StateEvent.JumpBtn, OnJump);
        StateEventManager.Inst.RegisterStateEvent(listenId, StateEvent.Teleport, OnTeleport);
    }

    private void UnRegisterCancelEmote(string listenId)
    {
        StateEventManager.Inst.UnRegisterStateEvent<float, float>(listenId, StateEvent.MoveJoystick, OnMoveJoystick);
        StateEventManager.Inst.UnRegisterStateEvent(listenId, StateEvent.JumpBtn, OnJump);
        StateEventManager.Inst.UnRegisterStateEvent(listenId, StateEvent.Teleport, OnTeleport);
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

    private void SelfCancelEmote()
    {
        if (!string.IsNullOrEmpty(_cancelListenId))
        {
            UnRegisterCancelEmote(_cancelListenId);
            _cancelListenId = null;
        }

        var emoAniType = (EmoteType)owner.emoteData.emoAniType;
        InteractType interactType = emoAniType == EmoteType.DoubleOnce ? InteractType.End : InteractType.InteractEnd;

        EmoteNetData netData = new EmoteNetData();

        if (isEmoteSender)
        {
            netData.SenderId = AccountDataManager.Inst.Uid;
            netData.ReceiverId = owner.emoteOPId;
        }
        else
        {
            netData.SenderId = owner.emoteOPId;
            netData.ReceiverId = AccountDataManager.Inst.Uid;
        }

        netData.EmoteId = owner.emoteData.pgcId;
        netData.EmoteType = (EmoteType)owner.emoteData.emoAniType;
        netData.Interact = interactType;

        if (owner.IsSelfAIBuddy)
        {
            switch (emoAniType)
            {
                case EmoteType.DoubleOnce:
                    netData.EmoteType = EmoteType.BuddyWithPlayerOnce;
                    break;
                case EmoteType.DoubleLoop:
                    netData.EmoteType = EmoteType.BuddyWithPlayerLoop;
                    break;
                default:
                    LoggerUtils.LogError("SelfCancelEmote 未知的EmoteType", emoAniType);
                    break;
            }

            netData.Interact = InteractType.End;
            netData.BuddyId = AccountDataManager.Inst.Uid;
        }
        else if (AIBuddyAvatarController.IsMapBuddyKey(owner.PlayerID))
        {
            // 地图共享 buddy 取消：转 Buddy 类型 + End，SenderId=配对玩家、ReceiverId=map key，
            // 供其他端 EndBuddyDoubleLoopEmote 按 key 解除该共享 buddy（复用 ReceiverId，不新增 proto）
            switch (emoAniType)
            {
                case EmoteType.DoubleOnce:
                    netData.EmoteType = EmoteType.BuddyWithPlayerOnce;
                    break;
                case EmoteType.DoubleLoop:
                    netData.EmoteType = EmoteType.BuddyWithPlayerLoop;
                    break;
                default:
                    LoggerUtils.LogError("SelfCancelEmote 未知的EmoteType", emoAniType);
                    break;
            }
            netData.Interact = InteractType.End;
            netData.SenderId = owner.emoteOPId;    // 配对玩家 uid
            netData.ReceiverId = owner.PlayerID;    // 地图 buddy 的确定性 key
        }
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
    }

    public override void CoexistState()
    {
        base.CoexistState();
        xasset.Assets.FastVerifyMode = true;
        var pgcId = owner.emoteData.pgcId;
        owner.PlayerAnimCtrl.DownloadAnimationAB(pgcId, (success) =>
        {
            xasset.Assets.FastVerifyMode = false;
            if (owner.emoteData == null || pgcId != owner.emoteData.pgcId) return;

            if (!success)
            {
                ExitState();
                return;
            }

            PlayerAniConfig emoAniConfig = null;

            if ((EmoteType)owner.emoteData.emoAniType == EmoteType.DoubleOnce)
            {
                // 双人单次
                EmoAniConfig aniConfig = null;
                if (!isEmoteSender)
                {
                    aniConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoublePlayerB.ToString()));
                }

                if (aniConfig == null)
                {
                    aniConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoublePlayerA.ToString()));
                }

                emoAniConfig = aniConfig.ConvertToAniConfig();

                owner.PlayerAnimCtrl.PlayConfigAni(emoAniConfig, ExitState,isPlaySound:!isBanAudio);
                expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);
            }
            else
            {
                PlayAniType startType = PlayAniType.DoubleLoopPlayerAStart;
                PlayAniType loopType = PlayAniType.DoubleLoopPlayerALoop;

                if (!isEmoteSender)
                {
                    if (emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerBStart.ToString())) != null)
                    {
                        startType = PlayAniType.DoubleLoopPlayerBStart;
                    }

                    if (emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerBLoop.ToString())) != null)
                    {
                        loopType = PlayAniType.DoubleLoopPlayerBLoop;
                    }
                }

                emoAniConfig = owner.PlayerAnimCtrl.PlayConfigLoopAni(emoAniDataList.ConvertToAniConfig(), startType, loopType, CreateEffect,isPlaySound:!isBanAudio);

                expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);
            }

            // 调整到相应位置（地图共享 buddy 同样重定位到玩家面前的表演位；退出时由 OnExit 归位到放置点）
            if (!isEmoteSender)
            {
                //目标：找到发起者的 PlayerStateController
                PlayerStateController senderStateCtrl;
                if (owner.IsAIBuddy)
                {
                    senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(owner.emoteOPId);
                }
                else
                {
                    //两种情况 - 双人Emote 或者 Buddy 和 玩家自己
                    if (owner.emoteOPId.Contains(GameConsts.AIBuddyTag))
                    {
                        senderStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(owner.emoteOPId);
                    }
                    else
                    {
                        senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(owner.emoteOPId);
                    }
                }

                var pos = senderStateCtrl.transform.position + senderStateCtrl.transform.TransformDirection(emoAniConfig.interactPos / 1.7f * senderStateCtrl.transform.lossyScale.x);
                var rot = Quaternion.LookRotation(-senderStateCtrl.transform.forward) * Quaternion.Euler(emoAniConfig.interactRot);
                owner.PlayerKCCtrl.Motor.SetPositionAndRotation(pos, rot);
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
        if ((EmoteType)owner.emoteData.emoAniType == EmoteType.DoubleOnce)
        {
            onComplete?.Invoke(this);
        }
        else
        {
            owner.PlayerAnimCtrl.StopEmoteCo();

            // 发送者 AEnd, 接受者BEnd
            PlayAniType aniType = PlayAniType.DoubleLoopPlayerAEnd;
            if (!isEmoteSender)
            {
                if (emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerBEnd.ToString())) != null)
                {
                    aniType = PlayAniType.DoubleLoopPlayerBEnd;
                }
            }

            var emoAniConfig = owner.PlayerAnimCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), aniType, () =>
            {
                onComplete?.Invoke(this);
            },isPlaySound:!isBanAudio);

            CreateEffect(emoAniConfig);
        }
    }

    private List<GameObject> CreateEffect(PlayerAniConfig emoAniConfig)
    {
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
        expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);

        return expressionGameObject;
    }
}

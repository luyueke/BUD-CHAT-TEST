using FSM;
using Message;
using Pb.Base;
using System.Collections.Generic;
using System;
using UnityEngine;
using Es;
using Game.Audio;
using Game.Avatar;
using Pb.Map;
using UIAgent;

public class LinkEmoteState : PlayerStateTemplate<PlayerStateController>
{
    private int playerABType = 0;
    private string emoteId = "";
    private bool lastIsMove = false;//用来去重判断玩家是否动了摇杆
    private bool hasEnter = false;//判断是否第一次进入
    private bool isPlayingEnterAni = false;//PlayerB进入动画播放中，屏蔽OnAnimationStateChange打断
    
    protected List<EmoAniConfig> emoAniDataList;
    protected List<GameObject> expressionGameObject;
    private PlayerAniConfig runAniConfig;
    private PlayerAniConfig landAniConfig;

    private LinkEmoteFollower linkEmoteFollower;//PlayerB才有的跟随器
    private UserInfoHeadView _followerHeadView; // 竹篓类贴身表情需要隐藏的跟随者名牌
    protected readonly string SwitchGrop = "Emote_Group";
    
    public LinkEmoteState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        emoteId = args[0].ToString();
        playerABType = (int)args[1];
        owner.emoteOPId = args[2].ToString();
        
        EmoteNetData netData = new EmoteNetData();
        netData.EmoteId = emoteId;
        netData.EmoteType = EmoteType.LinkEmote;
        netData.Interact = InteractType.Interact;
        if (IsFollower())
        {
            netData.SenderId = owner.emoteOPId;
            netData.ReceiverId = owner.PlayerID;
        }
        else
        {
            netData.SenderId = owner.PlayerID;
            netData.ReceiverId = owner.emoteOPId;
        }

        owner.linkEmoteData = netData;
        
        emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
        linkEmoteFollower = owner.PlayerKCCtrl.GetComponentInChildren<LinkEmoteFollower>(true);
        linkEmoteFollower?.StopFollow();
        
        var runEmoConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.LinkRunA.ToString()));
        if (runEmoConfig != null)
        {
            runAniConfig = runEmoConfig.ConvertToAniConfig();
        }
        
        var landEmoConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.LinkLandA.ToString()));
        if (landEmoConfig != null)
        {
            landAniConfig = landEmoConfig.ConvertToAniConfig();
        }
    }

    private bool IsFollower()
    {
        return playerABType == (int)PlayerABType.PlayerB;
    }
    
    private void AdapterLinkUI(bool isLink)
    {
        if (isLink)
        {
          
            UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestChangeOcBtn,AbilityKey.LinkEmote);
            UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestInstrumentBtn,AbilityKey.LinkEmote);
            UIShowAbilityManager.Inst.AddBanBility(UIAbility.EnterSelfieBtn,AbilityKey.LinkEmote);
            if (LinkEmoteManager.Inst.IsPlayerB(AccountDataManager.Inst.Uid))
            {
                UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestEmoteBtn,AbilityKey.LinkEmote);
                UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestBackToSpawnBtn, AbilityKey.LinkEmote);
                UIShowAbilityManager.Inst.AddBanBility(UIAbility.GuestJumpBtn,AbilityKey.LinkEmote);
            }
        }
        else
        {
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestEmoteBtn,AbilityKey.LinkEmote);
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestJumpBtn,AbilityKey.LinkEmote);
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestBackToSpawnBtn,AbilityKey.LinkEmote);
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestChangeOcBtn,AbilityKey.LinkEmote);
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.GuestInstrumentBtn,AbilityKey.LinkEmote);
            UIShowAbilityManager.Inst.RemoveBanBility(UIAbility.EnterSelfieBtn,AbilityKey.LinkEmote);
        }
    }
    
    
    private void SetFollowerAniPos(Vector3 interactPos,Vector3 interactRot)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(owner.emoteOPId);
        var pos = senderStateCtrl.transform.position + senderStateCtrl.transform.TransformDirection(interactPos / 1.7f * senderStateCtrl.transform.lossyScale.x);
        var rot = Quaternion.LookRotation(-senderStateCtrl.transform.forward) * Quaternion.Euler(interactRot);
        owner.PlayerKCCtrl.Motor.SetPositionAndRotation(pos, rot);
    }

    private void SetFollowerExitPos(FrameData lastFrameData)
    {
        if (lastFrameData == null || (lastFrameData.Postion == default && lastFrameData.Rotation == default)) return;
        owner.PlayerKCCtrl.Motor.SetPositionAndRotation(lastFrameData.Postion.ToFixVector3(), lastFrameData.Rotation.ToFixQuaternion());
    }

    private void LoadAnimRes(string emoteId,Action<bool> callback)
    {
        owner.PlayerAnimCtrl.DownloadAnimationAB(emoteId, callback);
    }

    private void EnterLinkMode()
    {
         //如果不是第一次进入则重启启动跟随
         if (IsFollower())
         {
             SetIsOnSimulate(false);
         }
         else
         {
             SetIsOnSimulate(true);
         }
         
        if (hasEnter)
        {
            if (IsFollower())
            {
                linkEmoteFollower?.StartFollow();
                linkEmoteFollower?.SyncPlayerAniState();
            }
        
            if (expressionGameObject != null)
            {
                owner.PlayerAnimCtrl?.ShowExpressionActive(expressionGameObject, true);
            }
        }
        else
        {
            LoadAnimRes(emoteId, (success) =>
            {
                //TODO:@Jaywill 异常处理，需要解绑
                if (!success)
                {
                    ExitState();
                    return;
                }
            
                EmoAniConfig aniConfig = null;
                //跟随玩家
                if (IsFollower())
                {
                    aniConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerBStart.ToString()));
                }
                else
                {
                    aniConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerAStart.ToString()));
                }

                PlayerAniConfig emoAniConfig = aniConfig.ConvertToAniConfig();
                expressionGameObject = owner.PlayerAnimCtrl.CreateExpression(emoAniConfig);

                //调整到动画交互位置
                if (IsFollower())
                {
                    SetFollowerAniPos(emoAniConfig.interactPos,emoAniConfig.interactRot);
                }

                if (IsReconnect)
                {
                    if (IsFollower())
                    {
                        linkEmoteFollower?.StartFollow();
                    }
                }
                else
                {
                    isPlayingEnterAni = true;
                    owner.PlayerAnimCtrl.PlayConfigAni(emoAniConfig, () =>
                    {
                        isPlayingEnterAni = false;
                        if (IsFollower() && linkEmoteFollower != null)
                        {
                            linkEmoteFollower.SyncPlayerAniState();
                        }
                        else
                        {
                            owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
                        }
                        UpdateLinkExpressionForState(PlayerAniState.Idle);
                    });
                    if (IsFollower() && linkEmoteFollower != null)
                    {
                        linkEmoteFollower.StartFollow();
                    }
                }
            
            });
        }
        hasEnter = true;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        owner.PlayerAnimCtrl.SetPlayerABType(playerABType);
        
        //跟随玩家
        if (IsFollower())
        {
            SetIsOnSimulate(false);
            //监听发起者PlayerA
            StateEventManager.Inst.RegisterStateEvent<PlayerAniState>(owner.emoteOPId, StateEvent.PlayerAniState, OnAnimationStateChange);
            StateEventManager.Inst.RegisterStateEvent<GroundEvent>(owner.emoteOPId, StateEvent.GroundEvent, OnGroundEvent);
            StateEventManager.Inst.RegisterStateEvent<bool>(owner.emoteOPId, StateEvent.FastRun, OnFastRunChange);
        }
        else
        {
            StateEventManager.Inst.RegisterStateEvent<PlayerAniState>(owner.PlayerID, StateEvent.PlayerAniState, OnSenderAniStateChange);
        }
        
        if (owner.IsSelf && IsFollower())
        {
            StateEventManager.Inst.RegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick, OnMoveJoystick);
        }
        
        if (owner.IsSelf)
        {
            MessageHelper.Broadcast(MessageName.LinkEmoteStateChange, true);
            AdapterLinkUI(true);
        }
        
        if (owner.IsSelfAIBuddy)
        {
            MessageHelper.Broadcast(MessageName.BuddyLinkEmoteStateChange, true);
            AdapterLinkUI(true);
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        
        owner.PlayerAnimCtrl.SetPlayerABType((int)PlayerABType.PlayerA);//恢复默认值
        owner.PlayerAnimCtrl.ResetEmoteAnimation();
        owner.PlayerAnimCtrl.ClearExpression(expressionGameObject);
        
        //跟随玩家
        if (IsFollower())
        {
            linkEmoteFollower?.StopFollow();
            SetIsOnSimulate(true);
            StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(owner.emoteOPId, StateEvent.PlayerAniState, OnAnimationStateChange);
            StateEventManager.Inst.UnRegisterStateEvent<GroundEvent>(owner.emoteOPId, StateEvent.GroundEvent, OnGroundEvent);
            StateEventManager.Inst.UnRegisterStateEvent<bool>(owner.emoteOPId, StateEvent.FastRun, OnFastRunChange);
        }
        else
        {
            StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(owner.PlayerID, StateEvent.PlayerAniState, OnSenderAniStateChange);
        }
        
        if (owner.IsSelf && IsFollower())
        {
            StateEventManager.Inst.UnRegisterStateEvent<float, float>(owner.PlayerID, StateEvent.MoveJoystick, OnMoveJoystick);   
        }

        if (owner.IsSelf)
        {
            MessageHelper.Broadcast(MessageName.LinkEmoteStateChange, false);
            AdapterLinkUI(false);
        }
        
        if (owner.IsSelfAIBuddy)
        {
            MessageHelper.Broadcast(MessageName.BuddyLinkEmoteStateChange, false);
            AdapterLinkUI(false);
        }
    }

    
    public override void EnterMainState()
    {
        base.EnterMainState();
        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.LinkEmote);
        // 竹篓表情 Player B 贴合 Player A 位置，名牌重叠，隐藏 Player B 名牌
        if (IsFollower() && emoteId == "40900006")
        {
            _followerHeadView = owner.PlayerKCCtrl.GetComponentInChildren<UserInfoHeadView>(true);
            if (_followerHeadView != null) _followerHeadView.gameObject.SetActive(false);
        }
        var ctrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
        if (ctrl != null)
        {
            //todo 这里没有执行状态退出，导致最终PlayerStateMachine.m_CurrentStateList中的第一个状态未释放
            if (ctrl.ContainsCurrentState(PlayerState.SingleEmote))
            {
                ctrl.ExitState(PlayerState.SingleEmote, false);
            }
            //GameAIBuddyManager.Inst.SendBuddyLinkReq(emoteId, EmoteType.BuddyLinkEmote, InteractType.Start);
            ctrl.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.LinkEmote);
            linkEmoteFollower = ctrl.PlayerKCCtrl.GetComponentInChildren<LinkEmoteFollower>(true);
            linkEmoteFollower?.StartFollow();
            linkEmoteFollower?.SyncPlayerAniState();
        }
        EnterLinkMode();
        bool hasFootSound = LinkEmoteManager.Inst.HasFootSound(emoteId);
        if (owner.PlayerAnimCtrl != null)
        {
            owner.PlayerAnimCtrl.IsFootSoundEnable = hasFootSound;
        }
    }

    public override void ExitMainState()
    {
        base.ExitMainState();
        if (IsFollower())
        {
            linkEmoteFollower?.StopFollow();
            if (_followerHeadView != null)
            {
                _followerHeadView.gameObject.SetActive(true);
                _followerHeadView = null;
            }
        }

        if (expressionGameObject != null)
        {
            owner.PlayerAnimCtrl?.ShowExpressionActive(expressionGameObject, false);
        }
        owner.PlayerKCCtrl.TransitionToState(Game.KinematicCharacter.KCCType.Default);
        
        if (owner.PlayerAnimCtrl != null)
        {
            owner.PlayerAnimCtrl.IsFootSoundEnable = true;
        }
    }


    public override void CoexistState()
    {
        base.CoexistState();
    }

    public override void DirectIntoState()
    {
        base.DirectIntoState();
        // EnterLinkMode();
    }

    public override void InterruptState(PlayerState beState)
    {
        base.InterruptState(beState);
    }

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        owner.PlayerAnimCtrl?.StopEmoteCo();

        if (emoAniDataList == null)
        {
            onComplete?.Invoke(this);
            return;
        }

        // 发送者 AEnd, 接受者BEnd
        PlayAniType aniType = PlayAniType.DoubleLoopPlayerAEnd;
        if (IsFollower())
        {
            linkEmoteFollower?.StopFollow();
            if (emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerBEnd.ToString())) != null)
            {
                aniType = PlayAniType.DoubleLoopPlayerBEnd;
            }
        }
        
        var aniConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(aniType.ToString()));
        if (aniConfig == null || string.IsNullOrEmpty(aniConfig.bodyPath))
        {
            //调整到动画交互位置
            if (IsFollower())
            {
                SetFollowerAniPos(aniConfig.interactPos,aniConfig.interactRot);
            }
            onComplete?.Invoke(this);
        }
        else
        {
            var emoAniConfig = owner.PlayerAnimCtrl.PlayConfigAni(emoAniDataList.ConvertToAniConfig(), aniType, () =>
            {
                if (IsFollower())
                {
                    SetFollowerExitPos(owner.linkEmoteData?.LastFrameData);
                }

                onComplete?.Invoke(this);
            });
            //调整到动画交互位置
            if (IsFollower())
            {
                SetFollowerAniPos(emoAniConfig.interactPos,emoAniConfig.interactRot);
            }
        }
    }

    
    protected void ExitState()
    {
        owner.ExitState(stateID);
    }
    
    protected void SetIsOnSimulate(bool isEnable)
    {
        owner.PlayerKCCtrl.Motor.SetIsOnSimulate(isEnable);
        owner.PlayerKCCtrl.SetFreezeCharacter(!isEnable);
    }
    
    public override void ReleaseData()
    {
        base.ReleaseData();
        StopEmoteSoud();
        
        emoAniDataList?.Clear();
        emoAniDataList = null;
        expressionGameObject = null;
        
        if (owner != null)
        {
            owner.linkEmoteData = null;
        }

        hasEnter = false;
        isPlayingEnterAni = false;
    }


    private void OnMoveJoystick(float axisForward, float axisRight)
    {
        bool isMove = axisForward != 0 || axisRight != 0;
        if (!lastIsMove && isMove)
        {
            MessageHelper.Broadcast(MessageName.LinkEmoteShowUnlink);
        }

        lastIsMove = isMove;
    }


    private PlayerAniState lastState = PlayerAniState.Idle;
    private void OnAnimationStateChange(PlayerAniState newState) {
        if (isPlayingEnterAni) return;
        if(owner.IsMainState(stateID))
        {
            owner.PlayerAnimCtrl.SetPlayerAniState(newState);
            PlayFollowerFootSound(newState);
            UpdateLinkExpressionForState(newState);
            lastState = newState;
        }
    }

    private void OnSenderAniStateChange(PlayerAniState newState)
    {
        if (owner.IsMainState(stateID))
            UpdateLinkExpressionForState(newState);
    }

    private void UpdateLinkExpressionForState(PlayerAniState state)
    {
        if (expressionGameObject == null || expressionGameObject.Count == 0) return;
        string suffix = IsFollower() ? "B" : "A";
        string aniTypeName = null;
        if (state == PlayerAniState.Idle) aniTypeName = "LinkIdle" + suffix;
        else if (state == PlayerAniState.Run) aniTypeName = "LinkRun" + suffix;
        else if (state == PlayerAniState.Jump) aniTypeName = "LinkJump" + suffix;
        else if (state == PlayerAniState.Land) aniTypeName = "LinkLand" + suffix;
        else return;
        var aniConfig = emoAniDataList?.Find(c => c.aniType.Equals(aniTypeName));
        if (aniConfig == null || string.IsNullOrEmpty(aniConfig.effectAnimName)) return;
        var playerAniConfig = aniConfig.ConvertToAniConfig();
        expressionGameObject.ForEach(go => owner.PlayerAnimCtrl.PlayExpressionAnim(playerAniConfig, go));
    }

    private void OnGroundEvent(GroundEvent groundEvent)
    {
        // LoggerUtils.Log("###OnGroundEvent:"+groundEvent);
        if (groundEvent == GroundEvent.Landed)
        {
            PlayFollowerFootSound(PlayerAniState.Land);
        }
    }

    private void OnFastRunChange(bool isFastRun)
    {
        // LoggerUtils.Log("###OnFastRunChange:"+isFastRun);
    }

    private void PlayFollowerFootSound(PlayerAniState newState)
    {
        // LoggerUtils.Log("###PlayFollowerFootSound:"+newState);
        if (owner.PlayerAnimCtrl == null || owner.PlayerAnimCtrl.gameObject == null) return;
        if (lastState != PlayerAniState.Run && newState == PlayerAniState.Run)
        {
            if (runAniConfig != null)
            {
                StopEmoteSoud();
                PlayEmoteSound(runAniConfig);
            }
        } 
        else if (newState == PlayerAniState.Land)
        {
            if (landAniConfig != null)
            {
                PlayEmoteSound(landAniConfig);
            }
        }
        else if (newState == PlayerAniState.Jump)
        {
            if (!LinkEmoteManager.Inst.IsFlyingEmote(emoteId))
            {
                StopEmoteSoud();
            }
        }
        else
        {
            
            if (newState != PlayerAniState.Run)
            {
                StopEmoteSoud();
            }
        }
    }

    public void StopEmoteSoud()
    {
        if (owner.gameObject != null)
        {
            AkSoundManager.Inst.StopAll(owner.gameObject);
        }
    }
    

    public void PlayEmoteSound(PlayerAniConfig aniConfig, float animTime = 1)
    {
        var soundName = aniConfig?.soundName;
        if (string.IsNullOrEmpty(soundName)) return;
        var soundVersion = aniConfig.soundVersion;
        string eventName = "Play_Emote_" + GetEventName();
        string switchGroup = SwitchGrop;
        if (!string.IsNullOrEmpty(soundVersion))
        {
            eventName = "Play_Emote_"+soundVersion + "_" + GetEventName();
            switchGroup = SwitchGrop + "_" + soundVersion;
        }

        if (owner.gameObject != null)
        {
            AkSoundManager.Inst.PlaySound(switchGroup, soundName, eventName, owner.gameObject);
        }
    }
    
    protected string GetEventName()
    {
        if (owner.IsSelf)
            return "1P";
        else
            return "3P";
    }


    private void SelfCancelEmote()
    {
        EmoteNetData netData = new EmoteNetData();

        if (playerABType == (int)PlayerABType.PlayerA)
        {
            netData.SenderId = AccountDataManager.Inst.Uid;
            netData.ReceiverId = owner.emoteOPId;
        }
        else
        {
            netData.SenderId = owner.emoteOPId;
            netData.ReceiverId = AccountDataManager.Inst.Uid;
        }

        netData.EmoteId = emoteId;
        netData.EmoteType = EmoteType.LinkEmote;
        netData.Interact = InteractType.InteractEnd;

        MessageHelper.Broadcast(MessageName.SelfCancelEmote, netData);

    }

}

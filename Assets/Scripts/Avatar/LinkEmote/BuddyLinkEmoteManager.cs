using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.Config;
using Game.KinematicCharacter;
using GameData;
using Message;
using Pb.Base;
using Pb.Game;
using Pb.Map;
using UnityEngine;

public class BuddyLinkEmoteManager : GameInstance<BuddyLinkEmoteManager>
{
    //所有发起Buddy牵手的玩家
    public List<string> bindBuddyLinkPlayersList = new List<string>();
    private Dictionary<string, string> clipNameDict = new Dictionary<string, string>();
    private Vector3 SingleEmotePos = new Vector3(0.9f,0,0);
    private Vector3 SingleEmoteRot = new Vector3(0, 0, 0);

    public BuddyLinkEmoteManager()
    {
        clipNameDict[PlayAniType.LinkIdleA.ToString()] = "link_idle_a";
        clipNameDict[PlayAniType.LinkIdleB.ToString()] = "link_idle_b";
        clipNameDict[PlayAniType.LinkRunA.ToString()] = "link_run_a";
        clipNameDict[PlayAniType.LinkRunB.ToString()] = "link_run_b";
        clipNameDict[PlayAniType.LinkRunFastA.ToString()] = "link_run_fast_a";
        clipNameDict[PlayAniType.LinkRunFastB.ToString()] = "link_run_fast_b";
        clipNameDict[PlayAniType.LinkJumpA.ToString()] = "link_jump_a";
        clipNameDict[PlayAniType.LinkJumpB.ToString()] = "link_jump_b";
        clipNameDict[PlayAniType.LinkLandA.ToString()] = "link_land_a";
        clipNameDict[PlayAniType.LinkLandB.ToString()] = "link_land_b";
    }
    
    // buddyKey：空=本人 buddy（按 senderId 取，旧行为）；非空=地图共享 buddy 的确定性 key（AINpcInMap_{entityUid}）
    public void StartBind(string senderId, string emoteId, bool isReconnect = false, string buddyKey = "")
    {
        if (bindBuddyLinkPlayersList.Contains(senderId))
        {
            LoggerUtils.LogError(senderId, "仍然处于Buddy牵手状态");
            return;
        }

        var playerStateCtr = AvatarController.Inst.GetPlayerStateCtrl(senderId);
        var buddyStateCtr = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(string.IsNullOrEmpty(buddyKey) ? senderId : buddyKey);
        if (playerStateCtr == null || buddyStateCtr == null) return;

        bindBuddyLinkPlayersList.Add(senderId);

        //初始化跟随器
        var follower = InitLinkFollower(playerStateCtr.PlayerKCCtrl.transform, senderId, buddyStateCtr.PlayerKCCtrl.transform);

        //从动画配置获取PlayerB跟随偏移位置
        var aniConfigList = GetAniConfigList(emoteId);
        if (aniConfigList != null)
        {
            var idleConfig = GetAniConfig(aniConfigList, PlayAniType.LinkIdleB);
            if (idleConfig != null)
            {
                follower.UpdateIdleOffset(idleConfig.interactPos);
                follower.UpdateIdleRotation(idleConfig.interactRot);
            }

            var runConfig = GetAniConfig(aniConfigList, PlayAniType.LinkRunB);
            if (runConfig != null)
            {
                follower.UpdateRunOffset(runConfig.interactPos);
                follower.UpdateRunRotation(runConfig.interactRot);
            }
            

            var runFastConfig = GetAniConfig(aniConfigList, PlayAniType.LinkRunFastB);
            if (runFastConfig != null)
            {
                follower.UpdateRunFastOffset(runFastConfig.interactPos);
                follower.UpdateRunFastRotation(runFastConfig.interactRot);
            }
            
        }

        //替换动作片段
        ChangeAnimClip(senderId, emoteId, buddyKey);

        if (!isReconnect)
        {
            playerStateCtr.EnterState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerA,senderId);
            buddyStateCtr.EnterState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerB,senderId);
        }
        else
        {
            playerStateCtr.ReconnectIntoState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerA,senderId);
            buddyStateCtr.ReconnectIntoState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerB,senderId);
        }
        
        AddIgnoreCollidersEach(playerStateCtr.PlayerKCCtrl, buddyStateCtr.PlayerKCCtrl);
    }
        

    public void StopBind(EmoteNetData emoteNetData)
    {
        if (emoteNetData.LastFrameData != null)
        {
            var playerB = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);
            if (playerB != null)
            {
                playerB.linkEmoteData.LastFrameData = emoteNetData.LastFrameData;
            }
        }

        // ReceiverId 为地图共享 buddy 的 key 时透传；否则视为本人 buddy（StopBind 内回退 senderId）
        StopBind(emoteNetData.SenderId,
            AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : "");
    }

    public void StopBind(string senderId, string buddyKey = "")
    {
        if (!bindBuddyLinkPlayersList.Contains(senderId))
        {
            LoggerUtils.LogError(senderId, "没有Buddy牵手状态");
            return;
        }

        bindBuddyLinkPlayersList.Remove(senderId);
        var playerStateCtr = AvatarController.Inst.GetPlayerStateCtrl(senderId);
        var buddyStateCtr = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(string.IsNullOrEmpty(buddyKey) ? senderId : buddyKey);

        if (playerStateCtr != null)
        {
            //特殊处理，如果解绑牵手的时候，包含正在播放的的单人emote，则先停止单人emote
            if (playerStateCtr.ContainsCurrentState(PlayerState.SingleEmote))
            {
                playerStateCtr.ExitState(PlayerState.SingleEmote,false);
            }
            
            if (playerStateCtr.ContainsCurrentState(PlayerState.UgcEmote))
            {
                playerStateCtr.ExitState(PlayerState.UgcEmote,false);
            }

            playerStateCtr.ExitState(PlayerState.LinkEmote);
        }

        if (buddyStateCtr != null)
        {
            //特殊处理，如果解绑牵手的时候，包含正在播放的的单人emote，则先停止单人emote
            if (buddyStateCtr.ContainsCurrentState(PlayerState.SingleEmote))
            {
                buddyStateCtr.ExitState(PlayerState.SingleEmote,false);
            }
            
            if (buddyStateCtr.ContainsCurrentState(PlayerState.UgcEmote))
            {
                buddyStateCtr.ExitState(PlayerState.UgcEmote,false);
            }
            
            buddyStateCtr.ExitState(PlayerState.LinkEmote);
            ReleaseLinkFollower(buddyStateCtr.PlayerKCCtrl.transform);
           
        }

        RemoveIgnoreCollidersEach(playerStateCtr?.PlayerKCCtrl, buddyStateCtr?.PlayerKCCtrl);
    }

    public LinkEmoteFollower InitLinkFollower(Transform playerA, string playerIdA, Transform playerB)
    {
        LinkEmoteFollower follower = playerB.GetComponent<LinkEmoteFollower>();
        if (follower == null)
        {
            follower = playerB.gameObject.AddComponent<LinkEmoteFollower>();
        }

        follower.Initialize(playerA, playerIdA);
        return follower;
    }


    public void ReleaseLinkFollower(Transform playerB)
    {
        if (playerB == null) return;
        LinkEmoteFollower follower = playerB.GetComponent<LinkEmoteFollower>();
        if (follower != null)
        {
            follower.Release();
            GameObject.Destroy(follower);
        }
    }


    public void ClearIgnoreColliders(KinematicCharacterController kccCtr)
    {
        if (kccCtr != null && kccCtr.CurIKCController != null)
        {
            kccCtr.CurIKCController.MiscData.IgnoredColliders.Clear();
        }
    }

    public void AddIgnoreCollidersEach(KinematicCharacterController playerA, KinematicCharacterController playerB)
    {
        if (playerA == null || playerB == null) return;
        playerA.AddIgnoreColliders(playerB.transform);
        playerB.AddIgnoreColliders(playerA.transform);
    }

    public void RemoveIgnoreCollidersEach(KinematicCharacterController playerA, KinematicCharacterController playerB)
    {
        if (playerA != null && playerB != null)
        {
            playerA.RemoveIgnoreColliders(playerB.transform);
            playerB.RemoveIgnoreColliders(playerA.transform);
        }

        if (playerA != null && playerB == null)
        {
            playerA.ClearIgnoreColliders();
        }

        if (playerA == null && playerB != null)
        {
            playerB.ClearIgnoreColliders();
        }
    }

    public bool IsInBuddyLinkState(string playerId)
    {
        return bindBuddyLinkPlayersList.Contains(playerId);
    }

    public void SetPlayerOffsetPos(string senderId, Vector3 posOffset, Vector3 rotationOffset)
    {
        var playerStateCtr = AvatarController.Inst.GetPlayerStateCtrl(senderId);
        var buddyStateCtr = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(senderId);
        if (playerStateCtr == null || buddyStateCtr == null)
            return;
        Vector3 targetPosition = playerStateCtr.transform.position + playerStateCtr.transform.rotation * posOffset;
        Quaternion targetRotation = playerStateCtr.transform.rotation * Quaternion.Euler(rotationOffset);

        buddyStateCtr.PlayerKCCtrl.Motor.SetPositionAndRotation(targetPosition, targetRotation);
    }

    //特殊需求：如果玩家A和玩家B处于牵手状态，则玩家A播放单人表情时，玩家B需要同步播放
    public void CheckLinkEnterSingleEmote(string senderId, EmoteNetData emoteNetData,PlayerState playerState)
    {
        LoggerUtils.Log("###CheckLinkEnterSingleEmote:" + senderId);
        //如果发起者处于牵手状态
        
        if (IsInBuddyLinkState(senderId))
        {
            SetPlayerOffsetPos(senderId,SingleEmotePos,SingleEmoteRot);
            var buddyStateCtr = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(senderId);
            if (buddyStateCtr != null)
            {
                if (playerState == PlayerState.UgcEmote)
                {
                    var playerBEmoteData = emoteNetData.Clone();
                    playerBEmoteData.SenderId = senderId;
                    buddyStateCtr.EnterState(PlayerState.UgcEmote, playerBEmoteData);
                }
                else
                {
                    buddyStateCtr.EnterState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult, emoteNetData.IsBanAudio);
                }
            }
        }
    }
    
    //特殊需求：如果玩家A和玩家B处于牵手状态，则玩家A播放单人表情时，玩家B需要同步播放
    public void CheckLinkReconnectSingleEmote(string senderId, EmoteNetData emoteNetData,PlayerState playerState)
    {
        //TODO:待优化，目前延迟一帧执行，方式还没绑定就先跑到这

        TimerManager.Inst.RunOnce("CheckLinkReconnectSingleEmote", 0.1f, () =>
        {
            //如果发起者处于牵手状态
            if (IsInBuddyLinkState(senderId))
            {
                SetPlayerOffsetPos(senderId,SingleEmotePos,SingleEmoteRot);
                var buddyStateCtr = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(senderId);
                if (buddyStateCtr != null)
                {
                    LoggerUtils.Log("###CheckLinkReconnectSingleEmote EnterState:"+senderId);
                    if (playerState == PlayerState.UgcEmote)
                    {
                        var playerBEmoteData = emoteNetData.Clone();
                        playerBEmoteData.SenderId = senderId;
                        if (buddyStateCtr.ugcEmoteData == null || buddyStateCtr.ugcEmoteData.EmoteId != emoteNetData.EmoteId)
                        {
                            buddyStateCtr.EnterState(PlayerState.UgcEmote, playerBEmoteData);
                        }
                        else
                        {
                            buddyStateCtr.ReconnectIntoState(PlayerState.UgcEmote, playerBEmoteData);
                            LoggerUtils.Log("#####CheckLinkReconnectSingleEmote UgcEmote重复进入");
                        }
                    }
                    else
                    {
                        if (buddyStateCtr.emoteData == null || buddyStateCtr.emoteData.pgcId != emoteNetData.EmoteId)
                        {
                            buddyStateCtr.EnterState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult, emoteNetData.IsBanAudio);
                        }
                        else
                        {
                            buddyStateCtr.ReconnectIntoState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult, emoteNetData.IsBanAudio);
                            LoggerUtils.Log("#####CheckLinkReconnectSingleEmote SingleEmote重复进入");
                        }

                        
                    }
                }
                
            }
        });
    }

    
    //特殊需求：如果玩家A和玩家B处于牵手状态，则玩家A播放单人表情时，玩家B需要同步播放
    public void CheckLinkExitSingleEmote(string senderId,PlayerState playerState)
    {
        LoggerUtils.Log("###CheckLinkExitSingleEmote:"+senderId);
        //如果发起者处于牵手状态
        if (IsInBuddyLinkState(senderId))
        {
            if (!AccountDataManager.Inst.IsSelf(senderId))
            {
                var playerB = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(senderId);
                if (playerB != null)
                {
                    playerB.ExitState(playerState);
                    LoggerUtils.Log("###CheckLinkExitSingleEmote ExitState:"+senderId);
                }
            }
        }
    }
    #region 动画相关接口

    public List<EmoAniConfig> GetAniConfigList(string emoteId)
    {
        return DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
    }

    public EmoAniConfig GetAniConfig(string emoteId,PlayAniType aniType)
    {
        var aniDataList = GetAniConfigList(emoteId);
        if (aniDataList == null || aniDataList.Count <= 0)
        {
            return null;
        }

        return aniDataList.Find(x => x.aniType == aniType.ToString());
    }
    
    public EmoAniConfig GetAniConfig(List<EmoAniConfig> aniDataList,PlayAniType aniType)
    {
        if (aniDataList == null || aniDataList.Count <= 0)
        {
            return null;
        }

        return aniDataList.Find(x => x.aniType == aniType.ToString());
    }

    #endregion

    #region 走、跑、跳动画片段

    public bool IsLinkPoseAnim(string aniType)
    {
       return clipNameDict.ContainsKey(aniType);
    }

    public string GetClipName(string aniType)
    {
        if (clipNameDict.ContainsKey(aniType))
        {
            return clipNameDict[aniType];
        }

        return "";
    }
    
    public void ClearAnimClip(string playerId, string buddyKey = "")
    {
        //1.Clean Player 的 AnimClip
        var player = AvatarController.Inst.GetPlayerStateCtrl(playerId);
        if (player == null || player.PlayerAnimCtrl == null) return;
        foreach (var clipName in clipNameDict.Values)
        {
            player.PlayerAnimCtrl.OverrideAnimationClip(clipName,null);
        }

        //2.Clean Buddy 的 AnimClip（buddyKey 空=本人 buddy，非空=地图共享 buddy 的确定性 key）

        var buddyPlayer = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(string.IsNullOrEmpty(buddyKey) ? playerId : buddyKey);
        if (buddyPlayer == null || buddyPlayer.PlayerAnimCtrl == null) return;
        foreach (var clipName in clipNameDict.Values)
        {
            buddyPlayer.PlayerAnimCtrl.OverrideAnimationClip(clipName,null);
        }
    }

    public void ChangeAnimClip(string playerId,string emoteId, string buddyKey = "")
    {
        ClearAnimClip(playerId, buddyKey);

        var playerStateController = AvatarController.Inst.GetPlayerStateCtrl(playerId);
        ChangeControllerAnimClip(playerStateController, emoteId);

        // buddyKey 空=本人 buddy，非空=地图共享 buddy 的确定性 key
        var buddyPlayerStateController = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(string.IsNullOrEmpty(buddyKey) ? playerId : buddyKey);
        ChangeControllerAnimClip(buddyPlayerStateController, emoteId);
    }
    
    private void ChangeControllerAnimClip(PlayerStateController playerStateController, string emoteId)
    {
        string AniPath = "Assets/Loadable/Animations/";
        if (playerStateController == null || playerStateController.PlayerAnimCtrl == null) return;
        var playerAnimCtrl = playerStateController.PlayerAnimCtrl;
        Dictionary<string, AnimationClip> changeClipDict = new Dictionary<string, AnimationClip>();
        playerAnimCtrl.DownloadAnimationAB(emoteId, (success) =>
        {
            if (!success)
            {
                return;
            }
            var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteId);
            foreach (var aniConfig in emoAniDataList)
            {
                if (IsLinkPoseAnim(aniConfig.aniType))
                {
                    var clipName = GetClipName(aniConfig.aniType);
                    string path = AniPath + aniConfig.bodyPath + ".anim";
                    var clip = Loader.Load<AnimationClip>(path, playerStateController.PlayerAnimCtrl.gameObject);
                    if (clip != null) {
                        changeClipDict.Add(clipName, clip);
                    }
                }
                
                foreach (var item in changeClipDict) {
                    if (item.Value != null) {
                        playerAnimCtrl.OverrideAnimationClip(item.Key, item.Value);
                    }
                }
            }
        });
    }
    
    #endregion
    
    public void SendExitLinkReq(EmoteNetData curLinkEmoteData)
    {
        if (curLinkEmoteData == null) 
            return;
        
        if(!bindBuddyLinkPlayersList.Contains(AccountDataManager.Inst.Uid))
            return;
        
        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = curLinkEmoteData.SenderId;
        netData.ReceiverId = curLinkEmoteData.ReceiverId;
        netData.EmoteId = curLinkEmoteData.EmoteId;
        netData.EmoteType = EmoteType.BuddyLinkEmote;
        netData.Interact = InteractType.End;
        
        FrameData frameData = new FrameData();
        frameData.Postion = AvatarController.Inst.SelfController.Motor.TransientPosition.ToFixPB();
        frameData.Rotation = AvatarController.Inst.SelfController.Motor.TransientRotation.ToFixPB();
        
        netData.LastFrameData = frameData;
        MessageHelper.Broadcast(MessageName.SelfCancelEmote, netData);
    }
}

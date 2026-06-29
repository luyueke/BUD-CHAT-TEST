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

public class LinkEmoteManager : GameInstance<LinkEmoteManager>
{
    //所有绑定的玩家<playerA,playerB>
    public Dictionary<string, string> bindPlayersDict = new Dictionary<string, string>();

    private Dictionary<string, string> clipNameDict = new Dictionary<string, string>();

    private Vector3 SingleEmotePos = new Vector3(0.9f,0,0);
    private Vector3 SingleEmoteRot = new Vector3(0, 0, 0);

    private List<string> flyingLinkEmote = new List<string>();//飞行的牵手

    public LinkEmoteManager()
    {
        MessageHelper.AddListener<string>(MessageName.PlayerLeave, OnPlayerLeave);

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
        
        flyingLinkEmote.Add("40900003");
    }

    public override void Release()
    {
        base.Release();
        MessageHelper.RemoveListener<string>(MessageName.PlayerLeave, OnPlayerLeave);
    }

    public void StartBind(string playerIdA, string playerIdB, string emoteId, bool isReconnect = false)
    {
        bindPlayersDict[playerIdA] = playerIdB;
        var playerA = AvatarController.Inst.GetPlayerStateCtrl(playerIdA);
        var playerB = AvatarController.Inst.GetPlayerStateCtrl(playerIdB);
        
        //初始化跟随器
        var follower = InitLinkFollower(playerA.PlayerKCCtrl.transform, playerIdA, playerB.PlayerKCCtrl.transform);

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
        ChangeAnimClip(playerIdA, emoteId);
        ChangeAnimClip(playerIdB, emoteId);
        if (!isReconnect)
        {
            playerA.EnterState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerA, playerIdB);
            playerB.EnterState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerB, playerIdA);
        }
        else
        {
            playerA.ReconnectIntoState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerA, playerIdB);
            playerB.ReconnectIntoState(PlayerState.LinkEmote, emoteId, (int)PlayerABType.PlayerB, playerIdA);
        }
        AddIgnoreCollidersEach(playerA.PlayerKCCtrl, playerB.PlayerKCCtrl);
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

        StopBind(emoteNetData.SenderId, emoteNetData.ReceiverId);
    }

    public void StopBind(string playerIdA, string playerIdB)
    {
        bindPlayersDict.Remove(playerIdA);
        var playerA = AvatarController.Inst.GetPlayerStateCtrl(playerIdA);
        var playerB = AvatarController.Inst.GetPlayerStateCtrl(playerIdB);

        if (playerA != null)
        {
            //特殊处理，如果解绑牵手的时候，包含正在播放的的单人emote，则先停止单人emote
            if (playerA.ContainsCurrentState(PlayerState.SingleEmote))
            {
                playerA.ExitState(PlayerState.SingleEmote,false);
            }
            
            if (playerA.ContainsCurrentState(PlayerState.UgcEmote))
            {
                playerA.ExitState(PlayerState.UgcEmote,false);
            }

            playerA.ExitState(PlayerState.LinkEmote);
        }

        if (playerB != null)
        {
            //特殊处理，如果解绑牵手的时候，包含正在播放的的单人emote，则先停止单人emote
            if (playerB.ContainsCurrentState(PlayerState.SingleEmote))
            {
                playerB.ExitState(PlayerState.SingleEmote,false);
            }
            
            if (playerB.ContainsCurrentState(PlayerState.UgcEmote))
            {
                playerB.ExitState(PlayerState.UgcEmote,false);
            }
            
            playerB.ExitState(PlayerState.LinkEmote);
            ReleaseLinkFollower(playerB.PlayerKCCtrl.transform);
           
        }

        RemoveIgnoreCollidersEach(playerA?.PlayerKCCtrl, playerB?.PlayerKCCtrl);
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

    public bool IsPlayerA(string playerId)
    {
        return bindPlayersDict.ContainsKey(playerId);
    }

    public bool IsPlayerB(string playerId)
    {
        return bindPlayersDict.ContainsValue(playerId);
    }

    public bool IsPlayerLinking(string playerId)
    {
        return IsPlayerA(playerId) || IsPlayerB(playerId);
    }

    public string GetPlayerAId(string playerBId)
    {
        if (!IsPlayerB(playerBId)) return "";
        foreach (var playerAId in bindPlayersDict.Keys)
        {
            if (bindPlayersDict[playerAId] == playerBId)
            {
                return playerAId;
            }
        }

        return "";
    }

    public string GetPlayerBId(string playerAId)
    {
        if (!IsPlayerA(playerAId)) return "";
        return bindPlayersDict[playerAId];
    }

    public bool HasFootSound(string emoteId)
    {
        if (flyingLinkEmote.Contains(emoteId))
        {
            return false;
        }

        return true;
    }

    public bool IsFlyingEmote(string emoteId)
    {
        return flyingLinkEmote.Contains(emoteId);
    }

    public void SetPlayerOffsetPos(string playerAId,string playerBId,Vector3 posOffset,Vector3 rotationOffset)
    {
        var playerA = AvatarController.Inst.GetPlayerStateCtrl(playerAId);
        var playerB = AvatarController.Inst.GetPlayerStateCtrl(playerBId);
        if(playerA == null || playerB == null) return;
        Vector3 targetPosition = playerA.transform.position + playerA.transform.rotation * posOffset;
        Quaternion targetRotation = playerA.transform.rotation * Quaternion.Euler(rotationOffset);
        
        playerB.PlayerKCCtrl.Motor.SetPositionAndRotation(targetPosition, targetRotation);
    }

    //特殊需求：如果玩家A和玩家B处于牵手状态，则玩家A播放单人表情时，玩家B需要同步播放
    public void CheckLinkEnterSingleEmote(string reqPlayerId, EmoteNetData emoteNetData,PlayerState playerState)
    {
        LoggerUtils.Log("###CheckLinkEnterSingleEmote:"+reqPlayerId);
        //如果发起者处于牵手状态
        if (IsPlayerA(reqPlayerId))
        {
            string playerBId = GetPlayerBId(reqPlayerId);
            SetPlayerOffsetPos(reqPlayerId,playerBId,SingleEmotePos,SingleEmoteRot);
            var playerB = AvatarController.Inst.GetPlayerStateCtrl(playerBId);
            if (playerB != null)
            {
                if (playerState == PlayerState.UgcEmote)
                {
                    var playerBEmoteData = emoteNetData.Clone();
                    playerBEmoteData.SenderId = playerBId;
                    playerB.EnterState(PlayerState.UgcEmote, playerBEmoteData);
                }
                else
                {
                    playerB.EnterState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult, emoteNetData.IsBanAudio);
                }
            }
        }
    }
    
    //特殊需求：如果玩家A和玩家B处于牵手状态，则玩家A播放单人表情时，玩家B需要同步播放
    public void CheckLinkReconnectSingleEmote(string reqPlayerId, EmoteNetData emoteNetData,PlayerState playerState)
    {
        //TODO:待优化，目前延迟一帧执行，方式还没绑定就先跑到这

        TimerManager.Inst.RunOnce("CheckLinkReconnectSingleEmote", 0.1f, () =>
        {
            //如果发起者处于牵手状态
            if (IsPlayerA(reqPlayerId))
            {
                string playerBId = GetPlayerBId(reqPlayerId);
                SetPlayerOffsetPos(reqPlayerId,playerBId,SingleEmotePos,SingleEmoteRot);
                
                var playerB = AvatarController.Inst.GetPlayerStateCtrl(playerBId);
                if (playerB != null)
                {
                    LoggerUtils.Log("###CheckLinkReconnectSingleEmote EnterState:"+reqPlayerId);
                    if (playerState == PlayerState.UgcEmote)
                    {
                        var playerBEmoteData = emoteNetData.Clone();
                        playerBEmoteData.SenderId = playerBId;
                        if (playerB.ugcEmoteData == null || playerB.ugcEmoteData.EmoteId != emoteNetData.EmoteId)
                        {
                            playerB.EnterState(PlayerState.UgcEmote, playerBEmoteData);
                        }
                        else
                        {
                            playerB.ReconnectIntoState(PlayerState.UgcEmote, playerBEmoteData);
                            LoggerUtils.Log("#####CheckLinkReconnectSingleEmote UgcEmote重复进入");
                        }
                    }
                    else
                    {
                        if (playerB.emoteData == null || playerB.emoteData.pgcId != emoteNetData.EmoteId)
                        {
                            playerB.EnterState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult, emoteNetData.IsBanAudio);
                        }
                        else
                        {
                            playerB.ReconnectIntoState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult, emoteNetData.IsBanAudio);
                            LoggerUtils.Log("#####CheckLinkReconnectSingleEmote SingleEmote重复进入");
                        }

                        
                    }
                }
                
            }
        });
    }

    
    //特殊需求：如果玩家A和玩家B处于牵手状态，则玩家A播放单人表情时，玩家B需要同步播放
    public void CheckLinkExitSingleEmote(string reqPlayerId,PlayerState playerState)
    {
        LoggerUtils.Log("###CheckLinkExitSingleEmote:"+reqPlayerId);
        //如果发起者处于牵手状态
        if (IsPlayerA(reqPlayerId))
        {
            string playerBId = GetPlayerBId(reqPlayerId);
            if (!AccountDataManager.Inst.IsSelf(playerBId))
            {
                var playerB = AvatarController.Inst.GetPlayerStateCtrl(playerBId);
                if (playerB != null)
                {
                    playerB.ExitState(playerState);
                    LoggerUtils.Log("###CheckLinkExitSingleEmote ExitState:"+reqPlayerId);
                }
            }
        }
    }

    private void OnPlayerLeave(string playerId)
    {
        if (!IsPlayerLinking(playerId)) return;
        if (IsPlayerA(playerId))
        {
            string playerBId = bindPlayersDict[playerId];
            StopBind(playerId,playerBId);
        }
        else if(IsPlayerB(playerId))
        {
            string playerAId = GetPlayerAId(playerId);
            StopBind(playerAId,playerId);
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

    public void ClearAnimClip(string playerId)
    {
        var player = AvatarController.Inst.GetPlayerStateCtrl(playerId);
        if (player == null || player.PlayerAnimCtrl == null) return;
        foreach (var clipName in clipNameDict.Values)
        {
            player.PlayerAnimCtrl.OverrideAnimationClip(clipName,null);
        }
    }

    public void ChangeAnimClip(string playerId,string emoteId)
    {
        ClearAnimClip(playerId);
        string AniPath = "Assets/Loadable/Animations/";
        var player = AvatarController.Inst.GetPlayerStateCtrl(playerId);
        if (player == null || player.PlayerAnimCtrl == null) return;
        var playerAnimCtrl = player.PlayerAnimCtrl;
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
                    var clip = Loader.Load<AnimationClip>(path, player.PlayerAnimCtrl.gameObject);
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
        if (curLinkEmoteData == null) return;
        if (!IsPlayerLinking(AccountDataManager.Inst.Uid)) return;
        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = curLinkEmoteData.SenderId;
        netData.ReceiverId = curLinkEmoteData.ReceiverId;
        netData.EmoteId = curLinkEmoteData.EmoteId;
        netData.EmoteType = EmoteType.LinkEmote;
        netData.Interact = InteractType.InteractEnd;
        FrameData frameData = new FrameData();
        
        //广播B的位置，如果B已经退房，则广播A的位置
        if (IsPlayerB(AccountDataManager.Inst.Uid))
        {
            frameData.Postion = AvatarController.Inst.SelfController.Motor.TransientPosition.ToFixPB();
            frameData.Rotation = AvatarController.Inst.SelfController.Motor.TransientRotation.ToFixPB();
        }
        else
        {
            var playerB =AvatarController.Inst.GetPlayerStateCtrl(curLinkEmoteData.ReceiverId);
            if (playerB != null)
            {
                frameData.Postion = playerB.PlayerKCCtrl.Motor.TransientPosition.ToFixPB();
                frameData.Rotation = playerB.PlayerKCCtrl.Motor.TransientRotation.ToFixPB();
            }
            else
            {
                frameData.Postion = AvatarController.Inst.SelfController.Motor.TransientPosition.ToFixPB();
                frameData.Rotation = AvatarController.Inst.SelfController.Motor.TransientRotation.ToFixPB();
            }
        }
        
        netData.LastFrameData = frameData;
        MessageHelper.Broadcast(MessageName.SelfCancelEmote, netData);
    }
    


}

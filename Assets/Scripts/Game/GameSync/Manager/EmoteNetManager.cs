using Game.Avatar;
using GameData.GameSync;
using Message;
using NetEngine;
using Pb.Base;
using Pb.Game;
using System;
using System.Collections.Generic;
using Es;
using Game.Utils;
using GameData.BaseInfo;
using GameData.Manager;
using GameSync.Manager;
using Google.Protobuf.Collections;
using Pb.Map;

public class EmoteNetManager : GameInstance<EmoteNetManager>
{
    private Dictionary<string, Action<string, EmoteNetData>> stateCallbackDic;

    private readonly string Reconnect = "Reconnect";
    public void Init() { }
    private const string ugcEmote = "UgcEmote";
    private const string buddyTag = "AIBuddy";
    public EmoteNetManager()
    {
        stateCallbackDic = new Dictionary<string, Action<string, EmoteNetData>>();

        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.Emote, OnEmoteRecv);
        NetSyncManager.Inst.AddAllRoomBroadcastListener(SubCmdType.Emote, OnEmoteToChat);

        MessageHelper.AddListener<SubCmdType, CommonSyncClientData>(MessageName.LocalNetworkSync, OnLocalNetworkSync);

        MessageHelper.AddListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        MessageHelper.AddListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);
        MessageHelper.AddListener<string>(MessageName.AOEForceSelfEmote, OnAOEForceSelfEmote);

        AddStateListener(EmoteType.SingleOnce, InteractType.Start, EnterSingleEmote);
        AddStateListener(EmoteType.SingleOnce, InteractType.End, EndSingleEmote);

        AddStateListener(EmoteType.SingleLoop, InteractType.Start, EnterSingleEmote);
        AddStateListener(EmoteType.SingleLoop, InteractType.End, EndSingleEmote);

        AddStateListener(EmoteType.DoubleOnce, InteractType.Start, EnterSingleEmote);
        AddStateListener(EmoteType.DoubleOnce, InteractType.End, EndSingleEmote);
        AddStateListener(EmoteType.DoubleOnce, InteractType.Interact, EnterInteractEmote);
        AddStateListener(EmoteType.BuddyWithPlayerOnce, InteractType.Start, EnterBuddyInteractEmote);
        AddStateListener(EmoteType.BuddyWithPlayerOnce, InteractType.End, EndBuddyDoubleLoopEmote);

        AddStateListener(EmoteType.DoubleLoop, InteractType.Start, EnterSingleEmote);
        AddStateListener(EmoteType.DoubleLoop, InteractType.End, EndSingleEmote);
        AddStateListener(EmoteType.DoubleLoop, InteractType.Interact, EnterInteractEmote);
        AddStateListener(EmoteType.DoubleLoop, InteractType.InteractEnd, EndDoubleLoopEmote);
        AddStateListener(EmoteType.BuddyWithPlayerLoop, InteractType.Start, EnterBuddyInteractEmote);
        AddStateListener(EmoteType.BuddyWithPlayerLoop, InteractType.End, EndBuddyDoubleLoopEmote);

        AddStateListener(EmoteType.PetSingle, InteractType.Start, EnterPetEmote);
        AddStateListener(EmoteType.PetSingle, InteractType.End, EndPetEmote);
        AddStateListener(EmoteType.PetSingleLoop, InteractType.Start, EnterPetEmote);
        AddStateListener(EmoteType.PetSingleLoop, InteractType.End, EndPetEmote);
        AddStateListener(EmoteType.PetWithPlayer, InteractType.Start, EnterSingleEmote);
        AddStateListener(EmoteType.PetWithPlayer, InteractType.End, EndSingleEmote);
        AddStateListener(EmoteType.PetWithPlayerLoop, InteractType.Start, EnterSingleEmote);
        AddStateListener(EmoteType.PetWithPlayerLoop, InteractType.End, EndSingleEmote);
        
        AddStateListener(EmoteType.LinkEmote, InteractType.Start, EnterLinkStartEmote);
        AddStateListener(EmoteType.LinkEmote, InteractType.End, ExitLinkStartEmote);
        AddStateListener(EmoteType.LinkEmote, InteractType.Interact, EnterLinkEmote);
        AddStateListener(EmoteType.LinkEmote, InteractType.InteractEnd, ExitLinkEmote);
        
        AddStateListener(EmoteType.LinkEmote, InteractType.Start, ReconnectLinkStartEmote, true);
        AddStateListener(EmoteType.LinkEmote, InteractType.Interact, ReconnectLinkEmote, true);
        
        AddStateListener(EmoteType.BuddyLinkEmote, InteractType.Start, EnterBuddyLinkEmote);
        AddStateListener(EmoteType.BuddyLinkEmote, InteractType.End, EndBuddyLinkEmote);
        AddStateListener(EmoteType.BuddyLinkEmote, InteractType.Start, ReconnectBuddyLinkEmote, true);


        AddStateListener(EmoteType.SingleLoop, InteractType.Start, ReconnectSingleLoopEmote, true);
        AddStateListener(EmoteType.DoubleOnce, InteractType.Start, ReconnectSingleLoopEmote, true);
        AddStateListener(EmoteType.DoubleLoop, InteractType.Start, ReconnectSingleLoopEmote, true);
        AddStateListener(EmoteType.DoubleLoop, InteractType.Interact, ReconnectDoubleLoopEmote, true);
        AddStateListener(EmoteType.PetSingleLoop, InteractType.Start, ReconnectPetLoopEmote, true);
        AddStateListener(EmoteType.PetWithPlayerLoop, InteractType.Start, ReconnectSingleLoopEmote, true);
        AddStateListener(EmoteType.BuddyWithPlayerLoop, InteractType.Start, ReconnectBuddyDoubleLoopEmote, true);

        
        AddUgcEmoteStateListener(ugcEmote, InteractType.Start, EnterUgcEmote);
        AddUgcEmoteStateListener(ugcEmote, InteractType.End, EndUgcEmote);
        AddUgcEmoteStateListener(ugcEmote, InteractType.Interact, EnterUgcInteractEmote);
        AddUgcEmoteStateListener(ugcEmote, InteractType.InteractEnd, EndUgcDoubleLoopEmote);
        AddUgcEmoteStateListener(ugcEmote, InteractType.Start, ReconnectUgcEmoteLoop, true);
        AddUgcEmoteStateListener(ugcEmote, InteractType.Interact, ReconnectUgcDoubleLoopEmote, true);
        
        AddUgcEmoteStateListener(ugcEmote + buddyTag, InteractType.Start, EnterBuddyUgcInteractEmote);
        AddUgcEmoteStateListener(ugcEmote + buddyTag, InteractType.End, EndBuddyUgcDoubleLoopEmote);
        AddUgcEmoteStateListener(ugcEmote + buddyTag, InteractType.Start, ReconnectBuddyUgcEmoteLoop, true);
        AddUgcEmoteStateListener(ugcEmote + buddyTag, InteractType.End, ReconnectBuddyUgcDoubleLoopEmote, true);
    }

    public override void Release()
    {
        base.Release();

        stateCallbackDic.Clear();

        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.Emote, OnEmoteRecv);
        NetSyncManager.Inst.RemoveAllRoomBroadcastListener(SubCmdType.Emote, OnEmoteToChat);

        MessageHelper.RemoveListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        MessageHelper.RemoveListener<EmoteNetData>(MessageName.SelfCancelEmote, OnSelfCancelEmote);
        MessageHelper.RemoveListener<string>(MessageName.AOEForceSelfEmote, OnAOEForceSelfEmote);
        MessageHelper.RemoveListener<SubCmdType, CommonSyncClientData>(MessageName.LocalNetworkSync, OnLocalNetworkSync);
    }

    private void OnLocalNetworkSync(SubCmdType cmdType, CommonSyncClientData syncData)
    {
        // 只处理表情动作相关的消息
        if (cmdType != SubCmdType.Emote) return;

        var emoteNetData = syncData.Body as EmoteNetData;
        if (emoteNetData == null) return;

        // 处理本地同步消息
        // 1. 处理聊天消息
        //OnEmoteToChat(syncData);
        
        // 2. 处理状态同步
        InvokeState(emoteNetData.EmoteType, emoteNetData.Interact, emoteNetData, syncData.PalyerId);
    }

    private void OnSyncMapInfoComplete()
    {
        var players = Global.Map.ClientData.Players;

        for (int i = 0; i < players.Count; i++)
        {
            //因为我要确保在做动作之前，AIBuddy先创建出来。
            BuddyStatus buddyStatus = players[i].BuddyStatus;
            GameAIBuddyManager.Inst.OnSyncMapInfoComplete(players[i].PlayerInfo, buddyStatus);
            
            EmoteNetData emoteNetData = players[i].EmoteStatus;
            if (emoteNetData != null)
            {
                InvokeState(emoteNetData.EmoteType, emoteNetData.Interact, emoteNetData, players[i].PlayerInfo.Uid, true);
            }

            EmoteNetData petEmoteNetData = players[i].PetEmoteStatus;
            if (petEmoteNetData != null)
            {
                InvokeState(petEmoteNetData.EmoteType, petEmoteNetData.Interact, petEmoteNetData, players[i].PlayerInfo.Uid, true);
            }
            
            EmoteNetData linkNetData = players[i].LinkEmoteStatus;
            if (linkNetData != null)
            {
                InvokeState(linkNetData.EmoteType, linkNetData.Interact, linkNetData, players[i].PlayerInfo.Uid, true);
            }
        }
    }

    public void SendEmoteReq(string emoteId, EmoteType emoteType, InteractType interactType, int randomResult = 0, bool isUgc = false, bool isBanAudio = false)
    {
        // 发送动作表情请求
        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = AccountDataManager.Inst.Uid;
        netData.EmoteId = emoteId;
        netData.EmoteType = emoteType;
        netData.Interact = interactType;
        netData.RandomResult = randomResult;
        netData.IsBanAudio = isBanAudio ? 1 : 0;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.Emote, netData);
    }


    public void SendUgcEmoteReq(EmoteNetData netData)
    {
        // 发送Ugc动作表情请求
        NetSyncManager.Inst.SendAllRoom(SubCmdType.Emote, netData);
    }

    public void SendEmoteInteractReq(string emoteId, string senderId, EmoteType emoteType, InteractType interactType)
    {
        // 发送交互请求
        EmoteNetData netData = new EmoteNetData();
        // test 写死id测试
        netData.ReceiverId = AccountDataManager.Inst.Uid;
        netData.SenderId = senderId;
        netData.EmoteId = emoteId;
        netData.EmoteType = emoteType;
        netData.Interact = interactType;

        NetSyncManager.Inst.SendAllRoom(SubCmdType.Emote, netData);
    }
    
    
    private void OnEmoteRecv(CommonSyncClientData netData)
    {
        var emoteNetData = (EmoteNetData)netData.Body;
        InvokeState(emoteNetData.EmoteType, emoteNetData.Interact, emoteNetData, netData.PalyerId);
    }

    private void OnEmoteToChat(CommonSyncClientData netData)
    {
        var playerId = netData.PalyerId;

        var emoteNetData = (EmoteNetData)netData.Body;
        if (emoteNetData.Interact == InteractType.End || emoteNetData.Interact == InteractType.InteractEnd)
        {
            return;
        }

        if (emoteNetData.AnimResType == 0)
        {
            var emoteConfig = DataTables.GetEmoUIConfig(emoteNetData.EmoteId);
            if (emoteConfig != null)
            {
                var pgcData = DataTables.GetPgcNameData(emoteNetData.EmoteId);
            	ChatDispatcherUtils.Inst.Dispatch($"{LocalizationManager.Inst.GetLocalizedText(pgcData != null ? pgcData.Name : emoteConfig.name)}", ChatType.Emote, playerId);
            }
        }
        else
        {
            ChatDispatcherUtils.Inst.Dispatch(emoteNetData.Msg, ChatType.Emote, playerId);
        }

    }

    private void AddStateListener(EmoteType emoteType, InteractType interactType, Action<string, EmoteNetData> callback, bool isReconnect = false)
    {
        string key = emoteType.ToString() + interactType.ToString() + (isReconnect ? Reconnect : "");
        if (stateCallbackDic.ContainsKey(key))
        {
            stateCallbackDic[key] = callback;
        }
        else
        {
            stateCallbackDic.Add(key, callback);
        }
    }

    private void AddUgcEmoteStateListener(string key, InteractType interactType, Action<string, EmoteNetData> callback, bool isReconnect = false)
    {
        string newKey = key + interactType.ToString() + (isReconnect ? Reconnect : "");
        if (stateCallbackDic.ContainsKey(key))
        {
            stateCallbackDic[newKey] = callback;
        }
        else
        {
            stateCallbackDic.Add(newKey, callback);
        }
    }


    private void InvokeState(EmoteType emoteType, InteractType interactType, EmoteNetData emoteNetData, string reqPlayerId, bool isReconnect = false)
    {
        if (emoteNetData.AnimResType == 0)
        {
            //PGC Emote
            string key = emoteType.ToString() + interactType.ToString() + (isReconnect ? Reconnect : "");
            if (stateCallbackDic.ContainsKey(key))
            {
                stateCallbackDic[key]?.Invoke(reqPlayerId, emoteNetData);
            }
        }
        else
        {
            //UGC Emote
            string key;
            if (string.IsNullOrEmpty(emoteNetData.BuddyId))
            {
                 key = ugcEmote + interactType.ToString() + (isReconnect ? Reconnect : "");
            }
            else
            {
                key = ugcEmote + buddyTag + interactType.ToString() + (isReconnect ? Reconnect : "");

            }
            if (stateCallbackDic.ContainsKey(key))
            {
                stateCallbackDic[key]?.Invoke(reqPlayerId, emoteNetData);
            }
        }
    }

    private void EnterSingleEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        LoggerUtils.Log("######EnterSingleEmote:"+reqPlayerId);
        LinkEmoteManager.Inst.CheckLinkEnterSingleEmote(reqPlayerId,emoteNetData,PlayerState.SingleEmote);
        BuddyLinkEmoteManager.Inst.CheckLinkEnterSingleEmote(reqPlayerId,emoteNetData,PlayerState.SingleEmote);
        
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;

        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }

        playerStateCtrl.EnterState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult,emoteNetData.IsBanAudio);
    }

    private void EndSingleEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        LinkEmoteManager.Inst.CheckLinkExitSingleEmote(reqPlayerId,PlayerState.SingleEmote);
        BuddyLinkEmoteManager.Inst.CheckLinkExitSingleEmote(reqPlayerId,PlayerState.SingleEmote);
        
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }
        playerStateCtrl.ExitState(PlayerState.SingleEmote);
    }
    


    //牵手邀请动作
    private void EnterLinkStartEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;

        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }
        
        playerStateCtrl.EnterState(PlayerState.LinkEmoteStart, emoteNetData.EmoteId, emoteNetData.IsBanAudio);
    }


    private void ExitLinkStartEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }
        
        playerStateCtrl.ExitState(PlayerState.LinkEmoteStart);
    }
    
    private void EnterLinkEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);
    
        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            LinkEmoteManager.Inst.StartBind(emoteNetData.SenderId,emoteNetData.ReceiverId,emoteNetData.EmoteId);
        }
    }
    
    private void EnterBuddyLinkEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        if (AccountDataManager.Inst.IsSelf(reqPlayerId)) 
            return;
        
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        // ReceiverId 为地图共享 buddy 的 key 时用它定位 buddy；否则回退 SenderId（本人 buddy，行为不变）
        var buddyKey = AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : emoteNetData.SenderId;
        var receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(buddyKey);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            BuddyLinkEmoteManager.Inst.StartBind(emoteNetData.SenderId, emoteNetData.EmoteId, false,
                AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : "");
        }
    }
    
    private void ExitLinkEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        LinkEmoteManager.Inst.StopBind(emoteNetData);
    }

    private void EndBuddyLinkEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        BuddyLinkEmoteManager.Inst.StopBind(emoteNetData);
    }
    
    private void ReconnectLinkStartEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }

        playerStateCtrl.ReconnectIntoState(PlayerState.LinkEmoteStart, emoteNetData.EmoteId, emoteNetData.IsBanAudio);
    }
    
    private void ReconnectLinkEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            LinkEmoteManager.Inst.StartBind(emoteNetData.SenderId,emoteNetData.ReceiverId,emoteNetData.EmoteId,true);
        }
    }
    
    private void ReconnectBuddyLinkEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        // ReceiverId 为地图共享 buddy 的 key 时用它定位 buddy；否则回退 SenderId（本人 buddy，行为不变）
        var buddyKey = AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : emoteNetData.SenderId;
        var receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(buddyKey);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            BuddyLinkEmoteManager.Inst.StartBind(emoteNetData.SenderId, emoteNetData.EmoteId, true,
                AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : "");
        }
    }

    private void EnterPetEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;

        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }

        playerStateCtrl.PetEmoState.PlayEmote(emoteNetData.EmoteId, emoteNetData.RandomResult,emoteNetData.IsBanAudio);
    }

    private void EndPetEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }

        playerStateCtrl.PetEmoState.ExitState();
    }

    private void EnterInteractEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            senderStateCtrl.EnterState(PlayerState.DoubleEmote, emoteNetData.EmoteId, true, emoteNetData.ReceiverId,emoteNetData.IsBanAudio);
            receiverStateCtrl.EnterState(PlayerState.DoubleEmote, emoteNetData.EmoteId, false, emoteNetData.SenderId,emoteNetData.IsBanAudio);
        }
    }
    private void EnterUgcInteractEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            senderStateCtrl.EnterState(PlayerState.UgcDoubleEmote, emoteNetData);
            receiverStateCtrl.EnterState(PlayerState.UgcDoubleEmote, emoteNetData);
        }
    }
    
    private void EnterBuddyUgcInteractEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        //AIBuddy的双人动作 本地先预表现了
        if(AccountDataManager.Inst.IsSelf(reqPlayerId))
            return;
        
        var ownerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var buddyStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);

        if (ownerStateCtrl != null && buddyStateCtrl != null)
        {
            ownerStateCtrl.EnterState(PlayerState.UgcDoubleEmote, emoteNetData);
            buddyStateCtrl.EnterState(PlayerState.UgcDoubleEmote, emoteNetData);
        }
    }
    
    private void EnterBuddyInteractEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        //AIBuddy的双人动作 本地先预表现了
        if(AccountDataManager.Inst.IsSelf(reqPlayerId))
            return;
        
        var ownerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        // ReceiverId 为地图共享 buddy 的 key 时用它定位 buddy；否则回退 SenderId（本人 buddy，行为不变）
        var buddyKey = AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : emoteNetData.SenderId;
        var buddyStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(buddyKey);

        if (ownerStateCtrl != null && buddyStateCtrl != null)
        {
            ownerStateCtrl.EnterState(PlayerState.DoubleEmote, emoteNetData.EmoteId, true, emoteNetData.SenderId,emoteNetData.IsBanAudio);
            buddyStateCtrl.EnterState(PlayerState.DoubleEmote, emoteNetData.EmoteId, false, emoteNetData.SenderId,emoteNetData.IsBanAudio);
        }
    }

    private void EndUgcDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);

        if (senderStateCtrl != null)
        {
            senderStateCtrl.ExitState(PlayerState.UgcDoubleEmote);
        }
        if (receiverStateCtrl != null)
        {
            receiverStateCtrl.ExitState(PlayerState.UgcDoubleEmote);
        }
    }
    
    private void EndBuddyUgcDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);

        if (senderStateCtrl != null)
        {
            senderStateCtrl.ExitState(PlayerState.UgcDoubleEmote);
        }
        if (receiverStateCtrl != null)
        {
            receiverStateCtrl.ExitState(PlayerState.UgcDoubleEmote);
        }
    }

    private void EndDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            senderStateCtrl.ExitState(PlayerState.DoubleEmote);
        }

        if (receiverStateCtrl != null)
        {
            receiverStateCtrl.ExitState(PlayerState.DoubleEmote);
        }
    }
    
    private void EndBuddyDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        //AIBuddy的双人动作 本地先预表现了
        // if(AccountDataManager.Inst.IsSelf(reqPlayerId))
        //     return;
        
        var ownerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        // ReceiverId 为地图共享 buddy 的 key 时按它解除；否则回退 SenderId（本人 buddy，行为不变）
        var buddyKey = AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : emoteNetData.SenderId;
        var buddyStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(buddyKey);

        if (ownerStateCtrl != null)
        {
            ownerStateCtrl.ExitState(PlayerState.DoubleEmote);
        }

        if (buddyStateCtrl != null)
        {
            buddyStateCtrl.ExitState(PlayerState.DoubleEmote);
        }
    }

    private void OnSelfCancelEmote(EmoteNetData netData)
    {
        NetSyncManager.Inst.SendAllRoom(SubCmdType.Emote, netData);
    }

    private void OnAOEForceSelfEmote(string emoteId)
    {
        var selfCtrl = AvatarController.Inst.SelfStateController;
        if (selfCtrl == null) return;
        selfCtrl.EnterState(PlayerState.SingleEmote, emoteId, 0, 1);
        SendEmoteReq(emoteId, EmoteType.SingleOnce, InteractType.Start, isBanAudio: true);
    }

    private void EnterUgcEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        LinkEmoteManager.Inst.CheckLinkEnterSingleEmote(reqPlayerId,emoteNetData,PlayerState.UgcEmote);
        BuddyLinkEmoteManager.Inst.CheckLinkEnterSingleEmote(reqPlayerId,emoteNetData,PlayerState.UgcEmote);
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AccountDataManager.Inst.Uid)) return;

        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }
        playerStateCtrl.EnterState(PlayerState.UgcEmote, emoteNetData);
    }

    private void EndUgcEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        LinkEmoteManager.Inst.CheckLinkExitSingleEmote(reqPlayerId,PlayerState.UgcEmote);
        BuddyLinkEmoteManager.Inst.CheckLinkExitSingleEmote(reqPlayerId,PlayerState.UgcEmote);
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;
        
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }
        playerStateCtrl.ExitState(PlayerState.UgcEmote);
    }

    private void ReconnectUgcEmoteLoop(string reqPlayerId, EmoteNetData emoteNetData)
    {
        LinkEmoteManager.Inst.CheckLinkReconnectSingleEmote(reqPlayerId,emoteNetData,PlayerState.UgcEmote);
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }
        playerStateCtrl.ReconnectIntoState(PlayerState.UgcEmote, emoteNetData);
    }
    
    private void ReconnectBuddyUgcEmoteLoop(string reqPlayerId, EmoteNetData emoteNetData)
    {
        BuddyLinkEmoteManager.Inst.CheckLinkReconnectSingleEmote(reqPlayerId,emoteNetData,PlayerState.UgcEmote);
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }
        playerStateCtrl.ReconnectIntoState(PlayerState.UgcEmote, emoteNetData);
    }
    
    private void ReconnectUgcDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            senderStateCtrl.ReconnectIntoState(PlayerState.UgcDoubleEmote, emoteNetData);
            receiverStateCtrl.ReconnectIntoState(PlayerState.UgcDoubleEmote, emoteNetData);
        }
        
    }
    
    private void ReconnectBuddyUgcDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            senderStateCtrl.ReconnectIntoState(PlayerState.UgcDoubleEmote, emoteNetData);
            receiverStateCtrl.ReconnectIntoState(PlayerState.UgcDoubleEmote, emoteNetData);
        }
        
    }
    

    private void ReconnectSingleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        LinkEmoteManager.Inst.CheckLinkReconnectSingleEmote(reqPlayerId,emoteNetData,PlayerState.SingleEmote);
        
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }

        playerStateCtrl.ReconnectIntoState(PlayerState.SingleEmote, emoteNetData.EmoteId, emoteNetData.RandomResult, emoteNetData.IsBanAudio);
    }

    private void ReconnectPetLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.LogError($"收到Emote请求，但无法找到 playerId : {reqPlayerId}");
            return;
        }

        playerStateCtrl.PetEmoState.PlayEmote(emoteNetData.EmoteId, emoteNetData.RandomResult,emoteNetData.IsBanAudio);
    }

    private void ReconnectDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        var receiverStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.ReceiverId);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            senderStateCtrl.ReconnectIntoState(PlayerState.DoubleEmote, emoteNetData.EmoteId, true, emoteNetData.ReceiverId, emoteNetData.IsBanAudio);
            receiverStateCtrl.ReconnectIntoState(PlayerState.DoubleEmote, emoteNetData.EmoteId, false, emoteNetData.SenderId, emoteNetData.IsBanAudio);

        }
    }
    
    private void ReconnectBuddyDoubleLoopEmote(string reqPlayerId, EmoteNetData emoteNetData)
    {
        var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(emoteNetData.SenderId);
        // ReceiverId 为地图共享 buddy 的 key 时用它定位 buddy；否则回退 SenderId（本人 buddy，行为不变）
        var buddyKey = AIBuddyAvatarController.IsMapBuddyKey(emoteNetData.ReceiverId) ? emoteNetData.ReceiverId : emoteNetData.SenderId;
        var receiverStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(buddyKey);

        if (senderStateCtrl != null && receiverStateCtrl != null)
        {
            LoggerUtils.LogError("ReconnectBuddyDoubleLoopEmote");
            senderStateCtrl.ReconnectIntoState(PlayerState.DoubleEmote, emoteNetData.EmoteId, true, emoteNetData.SenderId, emoteNetData.IsBanAudio);
            receiverStateCtrl.ReconnectIntoState(PlayerState.DoubleEmote, emoteNetData.EmoteId, false, emoteNetData.SenderId, emoteNetData.IsBanAudio);
        }
    }
}

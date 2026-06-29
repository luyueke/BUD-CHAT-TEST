using System;
using System.Collections;
using System.Collections.Generic;
using Es;
using Game.Avatar;
using GameData.Account;
using GameData.BaseInfo;
using GameData.GameSync;
using Message;
using NetEngine;
using Game.Vehicle.PGCVehicle;
using Newtonsoft.Json;
using Pb.Base;
using Pb.Game;
using Pb.Map;
using UnityEngine;
using static CustomBodyTypeController;
using PB_Quaternion = Pb.Game.PB_Quaternion;
using PB_Vector3 = Pb.Game.PB_Vector3;

public class GameAIBuddyManager : GameInstance<GameAIBuddyManager>
{
    // 当前 buddy 正坐在载具上的 driverUid 集合（用于待机暂停、按钮隐藏、下车清理）
    private readonly HashSet<string> _buddyOnVehicle = new HashSet<string>();
    // 其中走 PGC 载具的 driverUid（PGC 下车走 PGCVehicleManager，UGC 走 CharacterWrap，需区分）
    private readonly HashSet<string> _buddyOnPgcVehicle = new HashSet<string>();

    public void Init() { }
    public GameAIBuddyManager()
    {
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.CallBuddy, OnRecvAIBuddyNetData);
        MessageHelper.AddListener(MessageName.BatchCreatePlayers, OnBatchCreatePlayers);
        MessageHelper.AddListener<string>(MessageName.OnPlayerGetOutVehicle, OnPlayerGetOutVehicle);
        MessageHelper.AddListener<string, bool>(MessageName.OnBuddyVehicleStateChange, OnBuddyVehicleStateChangeCleanup);
    }

    public override void Release()
    {
        base.Release();
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.CallBuddy, OnRecvAIBuddyNetData);
        MessageHelper.RemoveListener(MessageName.BatchCreatePlayers, OnBatchCreatePlayers);
        MessageHelper.RemoveListener<string>(MessageName.OnPlayerGetOutVehicle, OnPlayerGetOutVehicle);
        MessageHelper.RemoveListener<string, bool>(MessageName.OnBuddyVehicleStateChange, OnBuddyVehicleStateChangeCleanup);
        _buddyOnVehicle.Clear();
        _buddyOnPgcVehicle.Clear();
    }

    /// <summary>查询某玩家的 buddy 是否正坐在载具上。</summary>
    public bool IsBuddyOnVehicle(string driverUid)
    {
        return !string.IsNullOrEmpty(driverUid) && _buddyOnVehicle.Contains(driverUid);
    }

    /// <summary>
    /// 召唤双人载具并让 AI 伙伴坐上乘客位（玩家驾驶）。
    /// 载具走标准 SendCreateVehicle 全端同步；buddy 上车走 op=RideVehicle 同步。
    /// </summary>
    // buddyStateKey：空=本人 buddy（旧行为）；非空=地图共享 buddy 的确定性 key（坐上玩家载具乘客位）
    public void RideVehicleWithBuddy(VehicleInfo vehicleInfo, Action<bool> onCall = null, string buddyStateKey = "")
    {
        var rideBuddyStateCtrl = string.IsNullOrEmpty(buddyStateKey)
            ? AIBuddyAvatarController.Inst.SelfStateController
            : AIBuddyAvatarController.Inst.GetPlayerStateCtrl(buddyStateKey);
        if (vehicleInfo == null || rideBuddyStateCtrl == null)
        {
            onCall?.Invoke(false);
            return;
        }

        var selfWrap = AvatarController.Inst.SelfWrap;
        if (selfWrap == null)
        {
            onCall?.Invoke(false);
            return;
        }

        var driverUid = AccountDataManager.Inst.Uid;

        // 试玩态（本地无房间）：载具实体不会经 CallVehicle 网络回包创建，需本地直造（司机自动就位），
        // buddy 再坐乘客位；不发任何同步（纯本地预览效果）。
        if (Global.Room == null)
        {
            bool created = GameVehicleManager.Inst.CreateVehicleLocalOnly(
                driverUid, vehicleInfo.id, JsonConvert.SerializeObject(vehicleInfo));
            onCall?.Invoke(created);
            if (created)
            {
                EnterBuddyToVehicleWithRetry(driverUid, buddyStateKey);
            }
            return;
        }

        var pos = selfWrap.Avatar.transform.position;
        var rot = selfWrap.Avatar.transform.rotation;

        GameVehicleManager.Inst.SendCreateVehicle(driverUid, vehicleInfo.id,
            JsonConvert.SerializeObject(vehicleInfo), pos, rot, (isSuccess) =>
            {
                onCall?.Invoke(isSuccess);
                if (!isSuccess) return;

                EnterBuddyToVehicleWithRetry(driverUid, buddyStateKey);
                // 复用 CallBuddyNetData.BuddyId 携带地图 buddy 的 key（本人 buddy 留空 → 远端回退 driverUid）
                NetSyncManager.Inst.SendAllRoom(SubCmdType.CallBuddy,
                    new CallBuddyNetData { Op = (int)AIBuddyOPType.RideVehicle, BuddyId = buddyStateKey ?? string.Empty });
            });
    }

    /// <summary>
    /// 让 driverUid 的 buddy 坐上 driverUid 的载具乘客位（本地+其他端通用）。
    /// 载具可能异步创建，未就绪时延迟重试。
    /// </summary>
    // buddyKey：地图共享 buddy 的确定性 key（坐 driverUid 的载具）；空=本人 buddy（按 driverUid 取，旧行为）
    private void EnterBuddyToVehicleWithRetry(string driverUid, string buddyKey = "", int retry = 6)
    {
        var buddyStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(
            AIBuddyAvatarController.IsMapBuddyKey(buddyKey) ? buddyKey : driverUid);

        // PGC 载具：座位/动画由 PGCVehicleManager 模板驱动（与 UGC 的姿势数据路径不同）
        var pgcCtrl = PGCVehicleManager.Inst.GetPGCVehicleController(driverUid);
        if (pgcCtrl != null && buddyStateCtrl != null)
        {
            if (PGCVehicleManager.Inst.EnterPassengerForBuddy(driverUid, buddyStateCtrl))
            {
                buddyStateCtrl.IsRidingVehicle = true; // 暂停待机，让位载具坐姿（地图 buddy 也生效，不依赖消息 key 匹配）
                _buddyOnVehicle.Add(driverUid);
                _buddyOnPgcVehicle.Add(driverUid);
                MessageHelper.Broadcast(MessageName.OnBuddyVehicleStateChange, driverUid, true);
                return;
            }
        }
        else
        {
            // UGC 载具：用 doublePoseData/doubleUserDetail 姿势数据就座
            var vehicleInfo = GameVehicleManager.Inst.GetPlayerCurVehicle(driverUid);
            var driverWrap = AvatarController.Inst.GetPlayerStateCtrl(driverUid)?.Wrap;
            var buddyWrap = buddyStateCtrl?.Wrap;
            if (vehicleInfo != null && driverWrap != null && buddyWrap != null)
            {
                driverWrap.GetInVehicleForBuddy(buddyWrap, vehicleInfo);
                buddyStateCtrl.IsRidingVehicle = true; // 暂停待机，让位载具坐姿
                _buddyOnVehicle.Add(driverUid);
                MessageHelper.Broadcast(MessageName.OnBuddyVehicleStateChange, driverUid, true);
                return;
            }
        }

        if (retry > 0)
            TimerManager.Inst.RunOnce("buddy_ride_retry_" + driverUid, 0.3f,
                () => EnterBuddyToVehicleWithRetry(driverUid, buddyKey, retry - 1));
    }

    /// <summary>
    /// 伙伴下车通知（PGC：由 PGCVehicleController 在载具销毁前主动广播；UGC：由本管理器自身广播）。
    /// 统一在此清理本管理器的车上状态，确保所有 PGC 载具销毁入口（DiscardVehicle / TrapBox / ClientManager）都被覆盖。
    /// </summary>
    private void OnBuddyVehicleStateChangeCleanup(string driverUid, bool onVehicle)
    {
        if (onVehicle) return;
        _buddyOnVehicle.Remove(driverUid);
        _buddyOnPgcVehicle.Remove(driverUid);
    }

    // 载具收纳/玩家下车（全端同步事件）：若该玩家 buddy 在车上，则就地下车
    private void OnPlayerGetOutVehicle(string driverUid)
    {
        if (!_buddyOnVehicle.Contains(driverUid)) return;

        if (_buddyOnPgcVehicle.Contains(driverUid))
        {
            // PGC：正常已由 PGCVehicleController 在载具销毁前广播下车并清理；此处兜底（载具未销毁时）
            var buddyStateCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(driverUid);
            PGCVehicleManager.Inst.CancelPassengerForBuddy(driverUid, buddyStateCtrl);
            _buddyOnPgcVehicle.Remove(driverUid);
        }
        else
        {
            var driverWrap = AvatarController.Inst.GetPlayerStateCtrl(driverUid)?.Wrap;
            var buddyCtrl = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(driverUid);
            if (buddyCtrl != null) buddyCtrl.IsRidingVehicle = false; // 下车恢复待机
            var buddyWrap = buddyCtrl?.Wrap;
            if (driverWrap != null && buddyWrap != null)
                driverWrap.GetOutVehicleForBuddy(buddyWrap);
        }

        _buddyOnVehicle.Remove(driverUid);
        MessageHelper.Broadcast(MessageName.OnBuddyVehicleStateChange, driverUid, false);
    }
    
    /// <summary>
    /// 用 Cabin AI 伙伴数据召唤。
    /// 调用方（UI 层）负责从 CabinCharacterUgcInfo 拆解出基础类型后传入，
    /// 避免 Game 程序集依赖 GameUI 程序集。
    /// </summary>
    /// <param name="buddyId">CabinCharacterUgcInfo.id</param>
    /// <param name="buddyName">CabinCharacterUgcInfo.name</param>
    /// <param name="avatarJson">默认皮肤的 avatarJson</param>
    /// <param name="usingEmoteJson">usingEmote 的 JSON，其他端据此本地跑待机循环</param>
    /// <param name="awakeInteract">本次随机选中的唤醒动作（可空），同步给其他端播放</param>
    public void CallSelfAIBuddyByCabin(string buddyId, string buddyName, string avatarJson,
        string usingEmoteJson, InteractSyncData awakeInteract)
    {
        var avatarData = CharacterData.DeserializeObject(avatarJson);
        if (avatarData == null) return;

        var kcc = AIBuddyAvatarController.Inst.CreateSelfAIBuddyByCabin(
            AccountDataManager.Inst.Uid, avatarData, buddyName);
        if (kcc == null) return;

        kcc.Motor.SetCapsuleHeightData((BodyType)avatarData.bodyType);

        var buddyPos = AIBuddyAvatarController.Inst.GetSelfAvatarPosition();
        var buddyRot = AIBuddyAvatarController.Inst.GetSelfAvatarRotation();
        kcc.Motor.SetPositionAndRotation(buddyPos, buddyRot);

        if (AIBuddyAvatarController.Inst.SelfController == null) return;

        var netData = new CallBuddyNetData
        {
            Op             = (int)AIBuddyOPType.CallBuddy,
            BuddyId        = buddyId,
            Postion        = new PB_Vector3 { X = buddyPos.x, Y = buddyPos.y, Z = buddyPos.z },
            Rotation       = new PB_Quaternion { X = buddyRot.x, Y = buddyRot.y, Z = buddyRot.z, W = buddyRot.w },
            BuddyImageJson = avatarJson,
            BuddyName      = buddyName ?? string.Empty,
            UsingEmoteJson = usingEmoteJson ?? string.Empty,
        };
        FillInteractFields(netData, awakeInteract);
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallBuddy, netData);
    }

    /// <summary>触发互动（口令）：本地已播放，这里只广播给房间内其他玩家。</summary>
    public void SendBuddyInteract(InteractSyncData interact)
    {
        if (interact == null || AIBuddyAvatarController.Inst.SelfController == null) return;

        var netData = new CallBuddyNetData { Op = (int)AIBuddyOPType.Interact };
        FillInteractFields(netData, interact);
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallBuddy, netData);
    }

    private static void FillInteractFields(CallBuddyNetData netData, InteractSyncData interact)
    {
        if (interact == null) return;
        netData.InteractEmoteId      = interact.EmoteId ?? string.Empty;
        netData.InteractIsPgc        = interact.IsPgc;
        netData.InteractUgcAnimId    = interact.UgcAnimId ?? string.Empty;
        netData.InteractAudioUrl     = interact.AudioUrl ?? string.Empty;
        netData.InteractDelaySecond  = interact.DelaySecond;
        netData.InteractIsMute       = interact.IsMute;
        netData.InteractRandomResult = interact.RandomResult;
        netData.InteractCommand      = interact.Command ?? string.Empty;
        netData.InteractText         = interact.Text ?? string.Empty;
        netData.InteractBuddyName    = interact.BuddyName ?? string.Empty;
    }

    private static InteractSyncData ParseInteractFields(CallBuddyNetData netData)
    {
        // 动作/语音/口令全为空才视为无内容，返回 null
        if (string.IsNullOrEmpty(netData.InteractEmoteId)
            && string.IsNullOrEmpty(netData.InteractUgcAnimId)
            && string.IsNullOrEmpty(netData.InteractCommand)
            && string.IsNullOrEmpty(netData.InteractAudioUrl))
            return null;
        return new InteractSyncData
        {
            EmoteId      = netData.InteractEmoteId,
            IsPgc        = netData.InteractIsPgc,
            UgcAnimId    = netData.InteractUgcAnimId,
            AudioUrl     = netData.InteractAudioUrl,
            DelaySecond  = netData.InteractDelaySecond,
            IsMute       = netData.InteractIsMute,
            RandomResult = netData.InteractRandomResult,
            Command      = netData.InteractCommand,
            Text         = netData.InteractText,
            BuddyName    = netData.InteractBuddyName,
        };
    }

    /// <summary>
    /// 召唤自己的AIBuddy
    /// </summary>
    public void CallSelfAIBuddy(AIBuddyInfo buddyInfo)
    {
        var aibuddyAvatarCtrl = AIBuddyAvatarController.Inst.CreateSelfAIBuddy(AccountDataManager.Inst.Uid, buddyInfo);
        var buddyAvatarJson = CharacterData.DeserializeObject(buddyInfo.npc.npcAvatarJson);
        if (buddyAvatarJson != null)
        {
            aibuddyAvatarCtrl.Motor.SetCapsuleHeightData((BodyType)buddyAvatarJson.bodyType);
        }
        var buddyPos = AIBuddyAvatarController.Inst.GetSelfAvatarPosition();
        var buddyRot = AIBuddyAvatarController.Inst.GetSelfAvatarRotation();

        if (AIBuddyAvatarController.Inst.SelfAIBuddyInfo != null)
        {
            CallBuddyNetData netData = new CallBuddyNetData();
            netData.Op = (int)AIBuddyOPType.CallBuddy;
            netData.BuddyId = buddyInfo.id;
        
            netData.Postion = new PB_Vector3();
            netData.Postion.X = buddyPos.x;
            netData.Postion.Y = buddyPos.y;
            netData.Postion.Z = buddyPos.z;
        
            netData.Rotation = new PB_Quaternion();
            netData.Rotation.X = buddyRot.x;
            netData.Rotation.Y = buddyRot.y;
            netData.Rotation.Z = buddyRot.z;
            netData.Rotation.W = buddyRot.w;

            if (!string.IsNullOrEmpty(AIBuddyAvatarController.Inst.SelfAIBuddyInfo.npc?.npcAvatarJson))
            {
                netData.BuddyImageJson = AIBuddyAvatarController.Inst.SelfAIBuddyInfo.npc?.npcAvatarJson;
            }

            netData.BuddyName = "";
            if (!string.IsNullOrEmpty(AIBuddyAvatarController.Inst.SelfAIBuddyInfo.npc?.npcName))
            {
                netData.BuddyName = AIBuddyAvatarController.Inst.SelfAIBuddyInfo.npc?.npcName;
            }
        
            NetSyncManager.Inst.SendAllRoom(SubCmdType.CallBuddy, netData);
        }
    }

    /// <summary>
    /// 放回自己的AIBuddy
    /// </summary>
    public void ExitSelfAIBuddy()
    {
        AIBuddyAvatarController.Inst.DestroyAIBuddy(AccountDataManager.Inst.Uid);
        
        CallBuddyNetData netData = new CallBuddyNetData();
        netData.Op = (int)AIBuddyOPType.Cancel;
        
        NetSyncManager.Inst.SendAllRoom(SubCmdType.CallBuddy, netData);
    }

    // buddyStateKey：空=本人 buddy（SelfStateController，旧行为）；非空=地图共享 buddy 的确定性 key
    // （AINpcInMap_{entityUid}，跨端一致）。远端据 netData.ReceiverId 定位同一 buddy。
    public void SendBuddyEmoteReq(string emoteId, EmoteType emoteType, InteractType interactType, int randomResult = 0, string buddyStateKey = "")
    {
        var selfStateCtr = AvatarController.Inst.SelfStateController;
        var selfBuddyStateCtr = string.IsNullOrEmpty(buddyStateKey)
            ? AIBuddyAvatarController.Inst.SelfStateController
            : AIBuddyAvatarController.Inst.GetPlayerStateCtrl(buddyStateKey);
        if (selfBuddyStateCtr == null) return;
        selfStateCtr.EnterState(PlayerState.DoubleEmote, emoteId, true, AccountDataManager.Inst.Uid);
        selfBuddyStateCtr.EnterState(PlayerState.DoubleEmote, emoteId, false, AccountDataManager.Inst.Uid);

        switch (emoteType)
        {
            case EmoteType.DoubleOnce:
                emoteType = EmoteType.BuddyWithPlayerOnce;
                break;
            case EmoteType.DoubleLoop:
                emoteType = EmoteType.BuddyWithPlayerLoop;
                break;
            
            default:
                LoggerUtils.LogError("SendBuddyEmoteReq 未知的EmoteType", emoteType);
                break;
        }
        
        // 发送动作表情请求
        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = AccountDataManager.Inst.Uid;
        netData.EmoteId = emoteId;
        netData.EmoteType = emoteType;
        netData.Interact = interactType;
        netData.RandomResult = randomResult;
        netData.BuddyId = AIBuddyAvatarController.Inst.SelfAIBuddyInfo?.id ?? string.Empty;
        // 复用 ReceiverId 携带地图 buddy 的 key（本人 buddy 留空 → 远端回退 SenderId，行为不变）
        netData.ReceiverId = buddyStateKey ?? string.Empty;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.Emote, netData);
    }

    // buddyStateKey：空=本人 buddy（旧行为）；非空=地图共享 buddy 的确定性 key（AINpcInMap_{entityUid}）
    public void SendBuddyLinkReq(string emoteId, EmoteType emoteType, InteractType interactType, int randomResult = 0,bool isUgc = false, string buddyStateKey = "")
    {
        EmoteNetData netData = new EmoteNetData();
        netData.SenderId = AccountDataManager.Inst.Uid;
        netData.EmoteId = emoteId;
        netData.EmoteType = emoteType;
        netData.Interact = interactType;
        netData.RandomResult = randomResult;
        netData.BuddyId = AIBuddyAvatarController.Inst.SelfAIBuddyInfo?.id ?? string.Empty;
        // 复用 ReceiverId 携带地图 buddy 的 key（本人 buddy 留空 → 远端回退 SenderId，行为不变）
        netData.ReceiverId = buddyStateKey ?? string.Empty;

        BuddyLinkEmoteManager.Inst.StartBind(netData.SenderId, netData.EmoteId, false, buddyStateKey);

        NetSyncManager.Inst.SendAllRoom(SubCmdType.Emote, netData);
    }

    private void OnRecvAIBuddyNetData(CommonSyncClientData netData)
    {
        //排除自己
        if(netData.PalyerId == AccountDataManager.Inst.Uid)
            return;
        

        var playerId = netData.PalyerId;

        var callBuddyNetData = (CallBuddyNetData)netData.Body;
        var buddyName = callBuddyNetData.BuddyName;

        if (callBuddyNetData.Op == (int)AIBuddyOPType.CallBuddy)
        {
            var buddyAvatarJson = callBuddyNetData.BuddyImageJson;
            var createPos_PB = callBuddyNetData.Postion;
            var createPos = new Vector3();
            createPos.x = createPos_PB.X;
            createPos.y = createPos_PB.Y;
            createPos.z = createPos_PB.Z;
        
            var createRot_PB = callBuddyNetData.Rotation;
            var createRot = Quaternion.identity;
            createRot.x = createRot_PB.X;
            createRot.y = createRot_PB.Y;
            createRot.z = createRot_PB.Z;
            createRot.w = createRot_PB.W;

            CreateOtherBuddy(playerId, buddyName, buddyAvatarJson, createPos, createRot);

            // 广播待机数据 + 本次唤醒动作，由 GameUI 层驱动其他端 buddy 待机循环 / 播唤醒动作
            var awakeInteract = ParseInteractFields(callBuddyNetData);
            MessageHelper.Broadcast(MessageName.OnRecvBuddySummonSync,
                playerId, callBuddyNetData.UsingEmoteJson, awakeInteract);
        }
        else if (callBuddyNetData.Op == (int)AIBuddyOPType.Interact)
        {
            // 口令互动：由 GameUI 层驱动其他端 buddy 播动作 + 语音 + 聊天/气泡显示
            var interact = ParseInteractFields(callBuddyNetData);
            if (interact != null)
            {
                MessageHelper.Broadcast(MessageName.OnRecvBuddyInteractSync, playerId, interact);
                MessageHelper.Broadcast(MessageName.OnBuddyCommandChat, playerId, interact);
            }
        }
        else if (callBuddyNetData.Op == (int)AIBuddyOPType.RideVehicle)
        {
            // 其他玩家让其 buddy 坐上其载具乘客位；BuddyId 为地图共享 buddy 的 key 时坐该 buddy
            EnterBuddyToVehicleWithRetry(playerId, callBuddyNetData.BuddyId);
        }
        else
        {
            AIBuddyAvatarController.Inst.DestroyAIBuddy(playerId);
        }
    }
    
    public void OnSyncMapInfoComplete(PlayerInfo playerInfo, BuddyStatus buddyStatus)
    {
        if(playerInfo == null || buddyStatus == null)
            return;
        
        if(string.IsNullOrEmpty(buddyStatus.BuddyImageJson))
            return;

        var playerId = playerInfo.Uid;
        
        //1.先创建Buddy
        var buddyAvatarJson = buddyStatus.BuddyImageJson;
        var buddyName = buddyStatus.BuddyName;
 
        //2.设置坐标
        var createPos = buddyStatus.Postion.ToVector3();
        var createRot = buddyStatus.Rotation.ToGameQuaternion();

        CreateOtherBuddy(playerId, buddyName, buddyAvatarJson, createPos, createRot);

        // 3.启动待机循环：新人进房也能看到别人 buddy 的待机动画。
        // 无唤醒动作（awake=null）→ 不播唤醒、initialDelay=0 直接进待机循环。
        if (!string.IsNullOrEmpty(buddyStatus.UsingEmoteJson))
            MessageHelper.Broadcast(MessageName.OnRecvBuddySummonSync,
                playerId, buddyStatus.UsingEmoteJson, (InteractSyncData)null);
    }

    private void CreateOtherBuddy(string playerId, string buddyName, string buddyAvatarJson, Vector3 createPos, Quaternion createRot)
    {
        if(AIBuddyAvatarController.Inst.GetPlayerStateCtrl(playerId) != null)
            return;
        
        //1.先创建Buddy
        var buddyAvatarData = CharacterData.DeserializeObject(buddyAvatarJson);
        var buddyKccCtr = AIBuddyAvatarController.Inst.CreateOtherGameAIBuddy(buddyName, buddyAvatarData, playerId);
            
        //2.设置坐标
        buddyKccCtr.Motor.SetPositionAndRotation(createPos, createRot);
    }

    public void ChatToAIBuddyAndBroadcast(string chatContent)
    {
        if(string.IsNullOrEmpty(chatContent))
            return;

        var buddyInfo = AIBuddyAvatarController.Inst.SelfAIBuddyInfo;
        var selfChatBubble = AccountDataManager.Inst.UserInfo.chatBubbles;
        ChatNetData netData = new ChatNetData();
        netData.Content = chatContent;
        if (!string.IsNullOrEmpty(buddyInfo.npc.npcName))
        {
            netData.Content = "@" + buddyInfo.npc.npcName + " " + chatContent;
        }
        netData.ChatBubbles = selfChatBubble;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.Chat, netData);
    }

    public void RcvAIBuddyChatAndBroadcast(AIBuddyInfo buddyInfo, string chatContent)
    {
        ChatNetData netData = new ChatNetData();
        netData.Content = "@" + AccountDataManager.Inst.UserInfo.nickname + " " + chatContent;
        netData.BuddyId = buddyInfo.id;
        if (!string.IsNullOrEmpty(buddyInfo?.npc?.npcName))
        {
            netData.BuddyName = buddyInfo?.npc?.npcName;
        }
        NetSyncManager.Inst.SendAllRoom(SubCmdType.Chat, netData);
    }

    private void OnBatchCreatePlayers()
    {
        foreach (var player in  Global.Map.ClientData.Players)
        {
            var senderStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(player.PlayerInfo.Uid);
            if (senderStateCtrl.IsInDoubleEmote())
            {
                var otherPlayerBuddy = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(player.PlayerInfo.Uid);
                if(otherPlayerBuddy == null)
                    continue;
                
                PlayAniType startType = PlayAniType.DoubleLoopPlayerAStart;
                PlayAniType loopType = PlayAniType.DoubleLoopPlayerALoop;
                PlayerAniConfig emoAniConfig = null;
                EmoAniConfig aniConfig = null;
                var emoteID = senderStateCtrl.emoteData.pgcId;
                var emoAniDataList = DataTables.GetEmoAniConfigList().FindAll((emoAniData) => emoAniData.emoId == emoteID);
                if(emoAniDataList == null)
                    continue;
                
                aniConfig = emoAniDataList.Find((emoteAniConfig) => emoteAniConfig.aniType.Equals(PlayAniType.DoubleLoopPlayerBLoop.ToString()));
                if(aniConfig == null)
                    continue;
                
                emoAniConfig = aniConfig.ConvertToAniConfig();
                if(emoAniConfig == null)
                    continue;
                
                var pos = senderStateCtrl.transform.position + senderStateCtrl.transform.TransformDirection(emoAniConfig.interactPos / 1.7f * senderStateCtrl.transform.lossyScale.x);
                var rot = Quaternion.LookRotation(-senderStateCtrl.transform.forward) * Quaternion.Euler(emoAniConfig.interactRot);
                otherPlayerBuddy.PlayerKCCtrl.Motor.SetPositionAndRotation(pos, rot);
            }
        }
    }
}

public enum AIBuddyOPType
{
    Cancel = 0,
    CallBuddy = 1,
    Interact = 2,     // 触发互动（口令）
    RideVehicle = 3,  // 伙伴坐上玩家的双人载具乘客位
}

/// <summary>
/// 互动动作同步载体（唤醒动作 / 口令动作通用）。
/// 跨程序集只传基础类型，避免 Game 层依赖 GameUI 层的 characterInteraction。
/// </summary>
public class InteractSyncData
{
    public string EmoteId;
    public int IsPgc;
    public string UgcAnimId;
    public string AudioUrl;
    public int DelaySecond;
    public int IsMute;
    public int RandomResult;
    public string Command;   // 口令文案（仅口令互动有，唤醒动作为空），用于聊天 Chat 分类
    public string Text;      // 台词文本，用于头顶气泡
    public string BuddyName; // 伙伴名，用于聊天显示
}

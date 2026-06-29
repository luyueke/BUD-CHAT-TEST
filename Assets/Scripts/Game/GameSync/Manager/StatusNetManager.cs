using Message;
using Pb.Base;
using Pb.Game;
using System.Collections.Generic;
using Game.Avatar;
using GameData.GameSync;
using Google.Protobuf;

public class StatusNetManager : GameInstance<StatusNetManager>
{
    public void Init() { }

    public StatusNetManager()
    {
        //MessageHelper.AddListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        MessageHelper.AddListener<List<int>, List<CacheStateData>>(MessageName.SyncPlayerStatus, SendPlayerStatus);
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.ConflictStatus, OnConfilctRecv);
    }

    public override void Release()
    {
        base.Release();

        //MessageHelper.RemoveListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.ConflictStatus, OnConfilctRecv);
        MessageHelper.RemoveListener<List<int>, List<CacheStateData>>(MessageName.SyncPlayerStatus, SendPlayerStatus);
    }

    private void SendPlayerStatus(List<int> curStateList, List<CacheStateData> cacheStateList)
    {
        var playerConfictStatus = new PlayerConflictStatus();
        playerConfictStatus.CurStateList.Clear();
        playerConfictStatus.CurStateList.AddRange(curStateList);
        playerConfictStatus.CacheStateList.Clear();
        playerConfictStatus.CacheStateList.AddRange(cacheStateList);

        NetSyncManager.Inst.Send(SubCmdType.ConflictStatus, playerConfictStatus);
    }

    private void OnConfilctRecv(CommonSyncClientData netData)
    {
        var conflictNetData = (PlayerConflictStatus)netData.Body;
         
        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(netData.PalyerId);
        
        LoggerUtils.Log("###StatusNetManager OnConfilctRecv:"+netData.PalyerId + " "+ conflictNetData.CurStateList);
        DealPlayerState(playerStateCtrl,conflictNetData);
        
        //如果为双人牵手则让玩家B同步玩家A状态
        if (LinkEmoteManager.Inst.IsPlayerA(netData.PalyerId) && !AccountDataManager.Inst.IsSelf(netData.PalyerId))
        {
            string playerBId = LinkEmoteManager.Inst.GetPlayerBId(netData.PalyerId);
            var playerB = AvatarController.Inst.GetPlayerStateCtrl(playerBId);
            if (playerB != null)
            {
                DealPlayerState(playerB,conflictNetData);
            }
        }

    }

    private void DealPlayerState(PlayerStateController playerStateCtrl,PlayerConflictStatus conflictNetData)
    {
        //TODO:临时写法，只解析第一个主状态，后续可扩展
        if (conflictNetData.CurStateList != null && conflictNetData.CurStateList.Count > 0)
        {
            var netMainState = (PlayerState)conflictNetData.CurStateList[0];
            if (playerStateCtrl.stateMachine.IsMainState(netMainState))
            {
                return;
            }

            if (netMainState == PlayerState.Default || (playerStateCtrl.stateMachine.GetCrrrentStateCount() > 1 && netMainState == PlayerState.LinkEmote))
            {
                var curMainState = playerStateCtrl.stateMachine.GetMainState(); 
                playerStateCtrl.ExitState(curMainState);
                return;
            }

            // 这个地方只处理正常状态，emote和后面游戏道具都不能用这个，没有具体参数，网络同步在其他地方处理
            // 娃娃机 Bound/Captured/Falling 同样是带参状态（captorUid/slot），不能无参重建——
            // 它们的晚加入补偿由 GameVehicleManager 借 BannerJson 持久化+重建处理。
            if (netMainState == PlayerState.SingleEmote || netMainState == PlayerState.DoubleEmote
                || netMainState == PlayerState.UgcEmote || netMainState == PlayerState.UgcDoubleEmote
                || netMainState == PlayerState.MusicInstrumentPlay || netMainState == PlayerState.LinkEmoteStart
                || netMainState == PlayerState.LinkEmote || netMainState == PlayerState.PGCVehicle
                || netMainState == PlayerState.Passenger
                || netMainState == PlayerState.Bound || netMainState == PlayerState.Captured
                || netMainState == PlayerState.Falling)
            {
                return;
            }

            if (playerStateCtrl.CanEnterState(netMainState))
            {
                playerStateCtrl.EnterState(netMainState);
            }
        }
    }


    // 因执行顺序，挪到外部
    //private void OnSyncMapInfoComplete()
    //{
    //    var players = Global.Map.ClientData.Players;

    //    for (int i = 0; i < players.Count; i++)
    //    {
    //        List<int> curStateList = players[i].ConflictStatus.CurStateList.ToList();
    //        List<CacheStateData> cacheStateList = players[i].ConflictStatus.CacheStateList.ToList();
    //        StateEventManager.Inst.TriggerStateEvent(players[i].PlayerInfo.Uid, StateEvent.SyncStatus, curStateList, cacheStateList);
    //    }
    //}
}

using System.Collections;
using System.Collections.Generic;
using Game.Avatar;
using Game.Base;
using GameData.BaseInfo;
using GameData.GameSync;
using Message;
using NetEngine;
using Pb.Base;
using Pb.Game;
using Pb.Map;


public class MusicalInstrumentNetManager : GameInstance<MusicalInstrumentNetManager>,IAutoInit
{
    public MusicalInstrumentNetManager()
    {
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.MusicalBst, OnRecvPlayBst);
        NetSyncManager.Inst.AddBroadcastListener(SubCmdType.MusicalOp,OnRecvOpChange);
        MessageHelper.AddListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
    }

    public void Init()
    {
        
    }

    public MusicalStatus ConvertToMusicalStauts(MusicalSyncParam musicalParam)
    {
        MusicalStatus musicalStatus = new MusicalStatus();
        musicalStatus.MoveId = musicalParam.moveId;
        musicalStatus.ResId = musicalParam.resId;
        var detailInfo = musicalParam.detailInfo;
        if (detailInfo != null)
        {
            musicalStatus.DetailInfo = new PInstrumentDetailInfo
            {
                PDef = detailInfo.pDef.ToPB_Vector3(),
                RDef = detailInfo.rDef.ToPB_Vector3(),
                SDef = detailInfo.sDef.ToPB_Vector3()
            };
        }

        return musicalStatus;
    }

    public MusicalSyncParam ConvertToMuscialParam(MusicalStatus musicalStatus)
    {
        MusicalSyncParam param = new MusicalSyncParam();
        param.moveId = musicalStatus.MoveId;
        param.resId = musicalStatus.ResId;
        var detailInfo = musicalStatus.DetailInfo;
        if (detailInfo != null)
        {
            param.detailInfo = new InstrumentDetailInfo
            {
                pDef = detailInfo.PDef.ToVector3(),
                rDef = detailInfo.RDef.ToVector3(),
                sDef = detailInfo.SDef.ToVector3()
            };
        }
        return param;
    }

    //联机数据获取完毕
    private void OnSyncMapInfoComplete()
    {
        var players = Global.Map.ClientData.Players;

        for (int i = 0; i < players.Count; i++)
        {
            MusicalStatus musicalStatus = players[i].MusicStatus;
            if (musicalStatus == null) continue;

            var playerId = players[i].PlayerInfo.Uid;
            SyncPlayerMusicalState(playerId, musicalStatus, true);
        }
    }

    private void SyncPlayerMusicalState(string reqPlayerId, MusicalStatus musicalStatus,bool isReconnect = false)
    {
        // 忽略自己发送的请求
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;

        var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(reqPlayerId);

        if (playerStateCtrl == null)
        {
            LoggerUtils.Log($"SyncPlayerMusicalState 无法找到 playerId : {reqPlayerId}");
            return;
        }

        if (musicalStatus.Op == 1)
        {
            var param = ConvertToMuscialParam(musicalStatus);
            param.isReconnect = isReconnect;
            playerStateCtrl.EnterState(PlayerState.MusicInstrumentPlay, param);
            
        }
        else
        {
            if (playerStateCtrl.ContainsCurrentState(PlayerState.MusicInstrumentPlay))
            {
                playerStateCtrl.ExitState(PlayerState.MusicInstrumentPlay);
            }
        }
    }

    //发送：拿出乐器或者收起乐器
    public void SendChangeOp(MusicalHoldState holdState,MusicalSyncParam param)
    {
        MusicalStatus musicalStatus = ConvertToMusicalStauts(param);
        musicalStatus.Op = (int)holdState;
        NetSyncManager.Inst.SendAllRoom(SubCmdType.MusicalOp, musicalStatus);
    }
    
    //发送游玩数据
    public void SendMusicPlay(List<SyllablePlayData> syllList)
    {
        if (syllList == null || syllList.Count <= 0) return;
        MusicalNetData netData = new MusicalNetData();
        netData.SyllList.AddRange(syllList);
        NetSyncManager.Inst.SendAllRoom(SubCmdType.MusicalBst, netData);
    }

    public void SendMusicPlay(SyllablePlayData syllData)
    {
        MusicalNetData netData = new MusicalNetData();
        netData.SyllList.Add(syllData);
        NetSyncManager.Inst.SendAllRoom(SubCmdType.MusicalBst, netData);
    }


    private void OnRecvPlayBst(CommonSyncClientData netData)
    {
        var musicalNetData = (MusicalNetData)netData.Body;
        string reqPlayerId = netData.PalyerId;
        
        if (reqPlayerId.Equals(AvatarController.Inst.SelfStateController.PlayerID)) return;
        StateEventManager.Inst.TriggerStateEvent(reqPlayerId, StateEvent.MusicInstrumentPlay, musicalNetData);
        
    }

    private void OnRecvOpChange(CommonSyncClientData netData)
    {
        var musicalStatus = (MusicalStatus)netData.Body;
        SyncPlayerMusicalState(netData.PalyerId,musicalStatus);
    }


    public override void Release()
    {
        base.Release();
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.MusicalBst, OnRecvPlayBst);
        NetSyncManager.Inst.RemoveBroadcastListener(SubCmdType.MusicalOp,OnRecvOpChange);
        MessageHelper.RemoveListener(MessageName.SyncMapInfoComplete, OnSyncMapInfoComplete);
    }
}

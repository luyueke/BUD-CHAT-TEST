using System;
using Pb.Base;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData.GameSync;
using GameData.Manager;
using NetEngine;
using NetEngine.src;
using NetEngine.src.Util;
using Network;
using Network.Http;
using Newtonsoft.Json;
using UnityEngine;
using Game.Props.PropsManagers;
using Game.Base;
using Game.Utils;
using UIAgent;
using System.Linq;
using System.Collections.Generic;
using Basic.Utils;
using GameData.BaseInfo;
using HLOD;
using Message;
using Pb.Map;
using Game.Vehicle.PGCVehicle;

namespace GameSync.Manager
{
    public class ClientManager : GameInstance<ClientManager>,IGameMono
    {
        private const string TAG = "ClientManager";
        private static float lastFrameTime;
        private bool _isEnterRoom = false;
        private bool _offlineFlag = false;
        private object roomUpdateLock = new object();//双心跳修改后，RoomUpdate锁防止多次调用断线重连
        public bool IsForceSync = false;
        public GamePlayerInfoManager PlayerInfosManager = new GamePlayerInfoManager();

        public void Init()
        {
            // MainThreadDispatcher.Init();
            PlayerInfosManager.Clear();
        }

        public void ConnectServer()
        {
            var onlineData = GameDataManager.Inst.gameOnlineData;
            LoggerUtils.Log($"{TAG} onlineData:{onlineData}");
            GetGameServer(onlineData.roomCode,onlineData.roomType,onlineData.roomSubType);
        }


        public void LeaveRoom()
        {
            if (Global.Room == null) return;
            if(Core.Room != null)
            {
                Core.Room?.LeaveRoom(new LeaveRoomReq(){
                    PlayerId = Player.Id
                }, (rep)=>
                {

                });
            }
        }

        public void BackToSpawn()
        {
            if (AvatarController.Inst.SelfStateController.IsLinkPlayerB())
            {
                UIAgentManager.Inst.ShowToast("被牵手状态下不可返回出生点哦");
                return;
            }

            if((AvatarController.Inst.SelfStateController.ContainsCurrentState(PlayerState.PGCVehicle)
            && AvatarController.Inst.SelfStateController.PGCVehicleKinematicCtrl != null)
            || GameVehicleManager.Inst.IsDriver(AccountDataManager.Inst.UserInfo.uid)
            ){
                PGCVehicleManager.Inst.RemovePGCVehicle(AccountDataManager.Inst.UserInfo.uid);
                MessageHelper.Broadcast(MessageName.OnPlayerTryGetOutVehicle, AccountDataManager.Inst.UserInfo.uid);
            }

            if(AvatarController.Inst.SelfStateController.ContainsCurrentState(PlayerState.Passenger)
            && GameVehicleManager.Inst.IsPassenger(AccountDataManager.Inst.UserInfo.uid)
            ){
                MessageHelper.Broadcast(MessageName.OnPlayerTryGetOutVehicle, AccountDataManager.Inst.UserInfo.uid);
            }

            var spManager = GlobalNodeManager.Inst.Get<SpawnPointManager>();
            Vector3 pos = Vector3.zero;
            if(AvatarController.Inst.SelfController.Motor.IsDriveVehicle)
            {
                var defaultPos = spManager.GetDefaultSpawnPoint();
                var vehicleInfo = GameVehicleManager.Inst.GetPlayerCurVehicle(AccountDataManager.Inst.UserInfo.uid);
                if(vehicleInfo != null)
                {
                    pos = new Vector3(defaultPos.x, vehicleInfo.vehicleHeight, defaultPos.z);
                }
                else{
                    pos = defaultPos;
                    LoggerUtils.LogError("没有载具信息，返回默认出生点");
                }
            }
            else{
                pos = spManager.GetDefaultSpawnPoint();
            }
            var rotation = spManager.GetDefaultSpawnRotation();
            var kinematicCharacter = AvatarController.Inst.SelfController;
            kinematicCharacter.Motor.SetPositionAndRotation(pos, rotation);
            MobileJoystick.Inst.OnResetJoystick();
            if (HLODManager.HasInstance && HLODManager.Inst.isStarted) {
                HLODManager.Inst.UpdateLODGrid();
            }
            kinematicCharacter.PlayerAnimCtrl.ResetEmoteAnimation();
            kinematicCharacter.SetFreezeCharacter(false);
            kinematicCharacter.OnTeleport();
        }

        /// <summary>
        /// 获取房间Session
        /// </summary>
        private void GetGameServer(string roomCode,RoomType type,RoomSubType subType)
        {
            int roomType = (int)type;
            int roomSubType = (int)subType;

            string mapId = GameDataManager.Inst.mapGlobalData.curUgcBaseInfo.id;
            GetGameServerReq gameServerReq = new GetGameServerReq()
            {
                mapId = mapId,
                roomCode = roomCode,
                roomType = roomType,
                roomSubType = roomSubType
            };

            NetworkManager.Inst.SendHttpRequest("/engine/getGameServer",HttpMethod.POST,JsonConvert.SerializeObject(gameServerReq),
                onReceive: content=>
                {
                    LoggerUtils.Log($"{TAG} GetGameServer Success:{content}");
                    GameServerInfo gameServerInfo = JsonConvert.DeserializeObject<GameServerInfo>(content);

                    // Ribin LocalServer
                    //gameServerInfo.ip = "10.10.1.172";
                    //gameServerInfo.roomPort = 8088;
                    //gameServerInfo.framePort = 8089;
                    //gameServerInfo.roomCode = "BUD-TEST";

                    UnInitSDK();
                     // 初始化一些房间的信息
                    Global.Room = new Room(gameServerInfo.roomCode, gameServerReq);
                    Global.Map = new Map(gameServerReq.mapId);
                    InitSDK(gameServerInfo);
                },onFail: content =>
                {
                    LoggerUtils.Log($"{TAG} GetGameServer Fail:{content}");
                    //进入离线模式
                },null,5f,3);
        }

        public void GetServerPlayerInfos(Action callback = null)
        {
            string roomCode = "";
            if(Global.Room == null || Global.Room.ClientData == null)
            {
                LoggerUtils.LogError("还没连接上当前房间");
                return;
            }
            roomCode = Global.Room.ClientData.RoomCode;
            GameDataManager.Inst.gameOnlineData.roomCode = Global.Room.ClientData.RoomCode;
            GetServerPlayersReq serverReq = new GetServerPlayersReq()
            {
                roomCode = roomCode,
            };

            NetworkManager.Inst.SendHttpRequest("/engine/getServerPlayers",HttpMethod.POST,JsonConvert.SerializeObject(serverReq),
                onReceive: content=>
                {
                    LoggerUtils.Log($"{TAG} GetServerPlayerInfos Success:{content}");
                    GetServerPlayersRsp serverRsp = JsonConvert.DeserializeObject<GetServerPlayersRsp>(content);
                    if(serverRsp.players != null)
                    {
                        PlayerInfosManager.InitPlayerInfos(serverRsp.players);
                    }

                    callback?.Invoke();

                },onFail: content =>
                {
                    LoggerUtils.Log($"{TAG} GetGameServer Fail:{content}");
                    //进入离线模式
                },null,5f,3);
        }

        private string GetRoomType()
        {
            string roomType = "";
            return roomType;
        }

        public string GetRoomCode()
        {
            string roomCode = "";
            if(Global.Room != null && Global.Room.ClientData != null)
            {
              roomCode = Global.Room.ClientData.RoomCode;
            }
            return roomCode;
        }

        public int GetMaxPlayersCount()
        {
            return GlobalNodeManager.Inst.Get<SpawnPointManager>().GetLastIndex();
        }

        public void InitSDK(GameServerInfo gameServerInfo)
        {
            #if UNITY_EDITOR
            Debugger.Enable = true;
            #endif

            _isEnterRoom = false;

            GameInfoPara gameInfo = new GameInfoPara()
            {
                OpenId = Player.OpenId
            };

            ConfigPara configPara = new ConfigPara()
            {
                Url = gameServerInfo.ip,
                Port = gameServerInfo.roomPort,
                FramePort = gameServerInfo.framePort
            };

            Listener.Init(gameInfo,configPara, (eve) =>
            {
                RunOnMainThread(() =>
                {
                    if (eve.Code == 0)
                    {
                        LoggerUtils.Log($"{TAG} InitSDK Success");
                        DotweenNetManager.Inst.Init();
                        StartEnterRoom();
                    }
                    else
                    {
                        LoggerUtils.Log($"{TAG} InitSDK fail:{eve.Code}");
                    }
                });
            });
        }

        // 开始进房
        void StartEnterRoom()
        {
            string roomCode = Global.Room.ClientData.RoomCode;
            string mapId = Global.Map.ClientData.MapId;
            LoggerUtils.Log($"{TAG} StartEnterRoom RoomCode:{roomCode}。");
            var onlineData = GameDataManager.Inst.gameOnlineData;
            var ugcInfo = GameDataManager.Inst.mapGlobalData.GetOriginInfo<MapInfo>();

            Core.Room.EnterRoom(new EnterRoomReq(){
                RoomCode = roomCode,
                MapId = mapId,
                PlayerId = Player.Id,
                PlayerInfo = new PlayerInfo(){
                    Uid = Player.Id,
                    AvatarJson = Player.AvatarJson,
                    Name = Player.Name,
                    HeadUrl = Player.HeadUrl,
                    PetAvatarJson = Player.PetAvatarJson,
                    HiddenPet = Player.HiddenPet,
                    PetName = Player.PetName,
                },
                MaxPlayers = ugcInfo.gameSetting.maxPlayer,
                RoomType =onlineData.roomType,
                RoomSubType = onlineData.roomSubType,
                Version = DeviceInfoManager.Inst.DeviceBaseData.version
            }, (eve)=>
            {
                RunOnMainThread(() =>
                {
                    if (eve.Code == 0)
                    {
                        // 进房成功
                        _isEnterRoom = true;
                        InitRoomBst();
                        SyncMapInfo();
                        LoggerUtils.Log($"{TAG} EnterRoom Success.{eve}");
                        Sdk.Instance.ConnectFrameSocket((eve)=>{});
                        string welcomeStr = LocalizationManager.Inst.GetLocalizedText("欢迎加入{0}，开始聊天吧！",GameDataManager.Inst.mapGlobalData.curUgcBaseInfo.name);
                        ChatDispatcherUtils.Inst.Dispatch(welcomeStr, ChatType.Chat);
                        GetServerPlayerInfos();
                    } else {
                        LoggerUtils.Log($"{TAG} EnterRoom fail.{eve}");
                    }
                });
            });
        }

        /// <summary>
        /// 同步下当前地图的信息
        /// </summary>
        void SyncMapInfo()
        {
            string roomCode = Global.Room.ClientData.RoomCode;
            string mapId = Global.Map.ClientData.MapId;
            LoggerUtils.Log($"{TAG} Start SyncMapInfo RoomCode:{roomCode}。");
            Core.Room.SyncMapInfo(mapId, roomCode,Player.Id,(eve)=>
            {
                RunOnMainThread(()=>{
                    if (eve.Code == 0)
                    {
                        MapInfoRsp mapInfoData = (MapInfoRsp)eve.Data;
                        Global.Map.InitMap(mapInfoData);
                        InitMapBst();
                        LoggerUtils.Log($"{TAG} SyncMapInfo Success.{eve}");

                        //分化地图数据
                        NetSyncManager.Inst.OnMapInfoSync(mapInfoData);

                        // 创建其他玩家
                        var players = Global.Map.ClientData.Players;
                        AvatarController.Inst.CreateDiffBatchOtherGameAvatar(players.ToList(), () =>
                        {
                            // 初始化其他玩家的位置
                            foreach (var player in players)
                            {
                                var otherPlayer = AvatarController.Inst.GetPlayerStateCtrl(player.PlayerInfo.Uid);
                                if (player.LastFrameData != null)
                                {
                                    otherPlayer.PlayerKCCtrl.Motor.SetPositionAndRotation(player.LastFrameData.Postion.ToFixVector3(), player.LastFrameData.Rotation.ToFixQuaternion());
                                }
                            }
                            MessageHelper.Broadcast(MessageName.BatchCreatePlayers);
                        });

                        MessageHelper.Broadcast(MessageName.SyncMapInfoComplete);

                        for (int i = 0; i < players.Count; i++)
                        {
                            PlayerInfosManager.AddPlayerInfo(players[i].PlayerInfo);
                            List<int> curStateList = players[i].ConflictStatus.CurStateList.ToList();
                            List<CacheStateData> cacheStateList = players[i].ConflictStatus.CacheStateList.ToList();
                            StateEventManager.Inst.TriggerStateEvent(players[i].PlayerInfo.Uid, StateEvent.SyncStatus, curStateList, cacheStateList);
                        }
                        MessageHelper.Broadcast(MessageName.PlayUserTitle, AccountDataManager.Inst.UserInfo.uid);
                    }
                    else
                    {
                        OnReconnectError(eve.Code);
                    }
                });
            });
        }

        void InitRoomBst()
        {
            Listener.Add(Global.Room);
            NetSyncManager.Inst.InitRoomBst();
            Global.Room.OnPlayerEnter += OnRoomPlayerEnter;
            Global.Room.OnPlayerLeave += OnRoomPlayerLeave;
            Global.Room.OnBstFrameData += OnBstFrameData;
            Global.Room.OnUpdate += OnRoomUpdate;
        }

        void InitMapBst()
        {
            Global.Map.OnPlayerEnter += OnPlayerEnter;
            Global.Map.OnPlayerLeave += OnPlayerLeave;
        }

        void OnRoomPlayerEnter(PlayerEnterBst enterBst)
        {
            RunOnMainThread(()=>
            {
                PlayerInfosManager.AddPlayerInfo(enterBst.PlayerStatus.PlayerInfo);
            });
        }

        void OnRoomPlayerLeave(PlayerLeaveBst leaveBst)
        {
            RunOnMainThread(()=>
            {
                PlayerInfosManager.RemovePlayerInfo(leaveBst.PlayerId);
            });
        }

        void OnPlayerEnter(PlayerEnterBst enterBst)
        {
            RunOnMainThread(()=>
            {
                var avatarData = CharacterData.DeserializeObject(enterBst.PlayerStatus.PlayerInfo.AvatarJson);
                var petData = PetData.DeserializeObject(enterBst.PlayerStatus.PlayerInfo.PetAvatarJson);
                var otherPlayer = AvatarController.Inst.GetOrCreateOtherGameAvatar(avatarData, petData, enterBst.PlayerStatus.PlayerInfo);
                UserInfoHeadView.Load(otherPlayer.gameObject, enterBst.PlayerStatus.PlayerInfo);
                FrameDataManager.Inst.AddOtherPlayer(enterBst.PlayerStatus.PlayerInfo.Uid);
                StateEventManager.Inst.TriggerStateEvent(enterBst.PlayerStatus.PlayerInfo.Uid, StateEvent.SyncStatus, enterBst.PlayerStatus.ConflictStatus.CurStateList, enterBst.PlayerStatus.ConflictStatus.CacheStateList.ToList());
                MessageHelper.Broadcast(MessageName.PlayerEnter,enterBst.PlayerStatus.PlayerInfo.Uid);
                MessageHelper.Broadcast(MessageName.PlayUserTitle, enterBst.PlayerStatus.PlayerInfo.Uid);
                //todo 进房消息
                ChatDispatcherUtils.Inst.Dispatch(LocalizationManager.Inst.GetLocalizedText("加入房间"), ChatType.Broadcast, enterBst.PlayerStatus.PlayerInfo.Uid);
            });
        }

        void OnPlayerLeave(PlayerLeaveBst leaveBst)
        {
            RunOnMainThread(()=>
            {
                MessageHelper.Broadcast(MessageName.PlayerLeave, leaveBst.PlayerId);
                FrameDataManager.Inst.RemoveOtherPlayer(leaveBst.PlayerId);
                AvatarController.Inst.RemoveGamePlayer(leaveBst.PlayerId);
                AIBuddyAvatarController.Inst.DestroyAIBuddy(leaveBst.PlayerId);
                //todo:退房消息
                ChatDispatcherUtils.Inst.Dispatch(LocalizationManager.Inst.GetLocalizedText("退出房间"), ChatType.Broadcast, leaveBst.PlayerId);
            });
        }

        private void OnReconnect()
        {
            SyncMapInfo();
        }

        private void OnReconnectError(ErrorCode errorCode)
        {
            LoggerUtils.LogError($"{TAG} OnReconnectError:" , errorCode);
            UIAgentManager.Inst.ClosePanel(WindowId.AlbumWindow, PanelId.AlbumPanel);
            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.CameraModePanel);
            //TODO:显示强弹弹窗
            UIAgentManager.Inst.OpenPanel(PanelId.ForceExitPanel);
        }

        public void UnInitSDK()
        {
            if (Global.Room != null)
            {
                Global.Room = null;
            }
            Global.UnInit();
        }

        public void RunOnMainThread(Action cb)
        {
            MainThreadDispatcher.Enqueue(cb);
        }

        public void Update()
        {

        }

        public void FixedUpdate()
        {
            if (Sdk.Instance == null) return;
            if (Sdk.Instance!=null)
            {
                Sdk.Instance.OnUpdate(Time.fixedDeltaTime);
            }

            if(!_isEnterRoom)
            {
                return;
            }

            int frameStep = 60;
            lastFrameTime += Time.fixedDeltaTime * 1000;
            if(lastFrameTime >= frameStep)
            {
                SendFrame();
                lastFrameTime %= frameStep;
            }
        }

        private void OnRoomUpdate(Room room,string tag,ResponseEvent eve)
        {
            if(!_isEnterRoom)
            {
                return;
            }

            lock (roomUpdateLock)
            {
                if(room.GetNetworkState(ConnectionType.Common) == false)
                {
                    _offlineFlag = true;
                    LoggerUtils.Log($"{TAG} OnRoomUpdate *************  房间链接断开");
                }
                else if(room.GetNetworkState(ConnectionType.Common) == true)
                {
                    if(_offlineFlag)
                    {
                        LoggerUtils.Log($"{TAG} OnRoomUpdate *************  房间链接恢复");
                        OnReconnect();
                    }
                    _offlineFlag = false;
                }
            }
        }


        //暂时写在这里
        private void SendFrame()
        {
            if(AvatarController.Inst == null || AvatarController.Inst.SelfController == null || MobileJoystick.Inst == null)
            {
                return;
            }

            if(Global.Room == null || Global.Room.GetNetworkState(ConnectionType.Frame) == false)
            {
                // LoggerUtils.Log($"帧连接断开");
                return;
            }

            FrameItem frameItem = new FrameItem();
            frameItem.PlayerId = Player.Id;
            frameItem.SyncType = IsForceSync?(int)SyncType.Force:(int)SyncType.Normal;
            FrameDataManager.Inst.SetFrameData(frameItem);

            if (frameItem.Frames.Count == 0) return;
            //LoggerUtils.Log("发送帧同步" + frameItem.Frames.ToString());
            Core.FrameSender?.SendFrame(frameItem,(eve)=>{
                if (eve.Code == 0)
                {
                    //LoggerUtils.Log("发送帧同步成功\r\n");
                }
                else {
                    LoggerUtils.Log($"发送帧同步失败:{eve.Code}");
                }
            });
        }

        public void OnBstFrameData(RecvFrameBst bst)
        {
            RunOnMainThread(() =>
            {
                if (!FrameDataManager.HasInstance) {
                    return;
                }
                var frameData = bst.Frame;
                var frameItems = frameData.Items;
                for (int i = 0; i < frameItems.Count; i++)
                {
                    var item = frameItems[i];
                    // LoggerUtils.Log($"收到帧数据PlayerId:{item.PlayerId} ,postion:{item.Postion} rotation:{item.Rotation}");
                    if (item.PlayerId == Player.Id) continue;

                    FrameDataManager.Inst.OtherFrameDataHandler(item);
                }
            });
        }

        public void TestReconnect()
        {
            Core.Socket1.ConnectNewSocketTask(Core.Socket1.Url);
        }

        public override void Release()
        {
            base.Release();
            LoggerUtils.Log($"{TAG} Release");
            // LeaveRoom();
            //关闭Socket相关
            Global.UnInit();
            Global.Room = null;
        }
    }
}

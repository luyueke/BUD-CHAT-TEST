using AIGame.Base;
using Cinemachine;
using Es;
using Game.Avatar;
using Game.Base;
using Game.ECS;
using Game.KinematicCharacter;
using Game.MapSetting;
using Game.MusicalInstrument;
using Game.Props.PropsManagers;
using Game.Props.PropsManagers.AIGames.AIPark.FSM;
using Game.Utils;
using GameData.BaseInfo;
using GameData.Manager;
using Message;
using Pb.Map;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class AIPark_StageBehaviour : AIPark_BasePropBehaviour
    {
        private List<Transform> MusicParent = new();

        private List<Transform> NpcParent = new();

        private Transform StageParent;

        private Transform PeopleParent;

        private bool isPlaying;

        private float duration;

        [HideInInspector] public Vector3 v3 = new Vector3(-11, 0, 0);

        [HideInInspector] public AICommonGameStageMusic bgm;

        [HideInInspector] public List<string> npc = new List<string>();

        [HideInInspector] public List<AICommonGameConfig_Musical> musical = new();

        [HideInInspector] public List<AIPark_InstrumentBehaviour> behaviours = new();

        [HideInInspector] public Dictionary<AIPark_InstrumentBehaviour, string> beUse = new();
        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            _maxUsedCount = 4;
            for (int i = 1; i < 5; i++)
            {
                MusicParent.Add(transform.Find("MusicPos/" + i));
            }
            for (int i = 1; i < 5; i++)
            {
                NpcParent.Add(transform.Find("NpcPos/" + i));
            }
            StageParent = transform.Find("StagePos");
            PeopleParent = transform.Find("PeoplePos");

            var ls = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.AICommonGameConfig.stage.musicalInstruments;
            var mgr = GlobalNodeManager.Inst.Get<AIPark_InstrumentManager>();
            var gamePropConfig = GamePropDataHelper.GetPropDataByID("20500006");
            for (int i = 0; i < ls.Count; i++)
            {
                // 创建
                if (ls[i] == null)
                {
                    continue;
                }
                var idx = i;
                PNodeData data = new PNodeData();
                data.PropId = gamePropConfig.Id;
                data.Pos = Vector3.up.ToPB();
                data.Scale = Vector3.one.ToPB();
                data.Rotation = Vector3.zero.ToPB();
                var entity = GamePropNodeManager.Inst.GetNodeFlowSystem().Create(data, MusicParent[i]).GetBehaviour<AIPark_InstrumentBehaviour>();
                entity.SetData(ls[i], this, idx);
                behaviours.Add(entity);
            }
        }

        protected override void Play()
        {
            //UIAgentManager.Inst.OpenPanel(PanelId.AIParkGameStagePanel, WindowId.GuestWindow, this,false);
            foreach (var item in behaviours)
            {
                item.IsCanClick = false;
            }
            var ls = _usePropCharacterMsg.ToList();
            for (int i = 0; i < ls.Count; i++)
            {
                var usePropCharacterMsg = ls[i];

                if (usePropCharacterMsg.Value.originParentTrans == null)
                {
                    usePropCharacterMsg.Value.originParentTrans = usePropCharacterMsg.Value.kccMotor.transform.parent;
                }
                if (usePropCharacterMsg.Value.useParentTrans == null)
                {
                    var item = GetNpcParent(usePropCharacterMsg.Value.playerStateController.PlayerID);
                    usePropCharacterMsg.Value.useParentTrans = item.Item2;
                    usePropCharacterMsg.Value.usePosIndex = item.Item1;

                    behaviours[item.Item1].gameObject.SetActive(false);

                    MoveToNode(usePropCharacterMsg.Value.kccMotor, usePropCharacterMsg.Value.useParentTrans, false);

                    usePropCharacterMsg.Value.kccMotor.transform.localPosition = Vector3.up;
                    usePropCharacterMsg.Value.kccMotor.transform.localRotation = Quaternion.identity;

                    GetMusicalSync(usePropCharacterMsg.Value.usePosIndex, (param) =>
                    {
                        if (usePropCharacterMsg.Value.playerStateController.CanEnterState(PlayerState.MusicInstrumentPlay))
                        {
                            usePropCharacterMsg.Value.playerStateController.EnterState(PlayerState.MusicInstrumentPlay, param);
                        }
                    });
                }
            }
        }

        (int, Transform) GetNpcParent(string id)
        {
            if (beUse.Count > 0)
            {
                //找一找场景中已经占位的
                foreach (var item in beUse)
                {
                    if (item.Value == id)
                    {
                        var idx = behaviours.IndexOf(item.Key);
                        return (idx, MusicParent[idx]);
                    }
                }
            }
            for (int i = 0; i < behaviours.Count; i++)
            {
                //随便找一个没被占位的
                if (!beUse.ContainsKey(behaviours[i]))
                {
                    var idx = i;
                    return (idx, MusicParent[idx]);
                }
            }

            return (0, null);
        }

        public void SetNpc(AIPark_InstrumentBehaviour instrument, string player)
        {
            if (!beUse.ContainsKey(instrument))
            {
                beUse.Add(instrument, player);
            }
            beUse[instrument] = player;
        }
        public void SetNpc2(int idx, string player)
        {
            if (!npc.Contains(player))
            {
                npc.Add(player);
            }
            var instrument = behaviours[idx];
            if (!beUse.ContainsKey(instrument))
            {
                beUse.Add(instrument, player);
            }
            beUse[instrument] = player;
        }

        public override int GetUsePosIndex(string playerID, KinematicCharacterController kcc)
        {
            var item = GetNpcParent(playerID);
            if (item.Item2 == null)
            {
                Debug.LogError("舞台没有空位");
                return -1;
            }
            return item.Item1;
        }

        public override void OnPropPosFree(KinematicCharacterController kcc)
        {
            if (kcc.PlayerID == AccountDataManager.Inst.Uid)
            {
                //自己退出
                CameraReset();
            }
            var posIdx = GetPropPosIdxByKcc(kcc);
            if (posIdx != -1)
            {
                var usePropCharacterMsg = _usePropCharacterMsg[posIdx];
                MoveToNode(usePropCharacterMsg.kccMotor, usePropCharacterMsg.originParentTrans, true);
                _usePropCharacterMsg.Remove(posIdx);

                usePropCharacterMsg.playerStateController.ExitState(PlayerState.MusicInstrumentPlay);

                //usePropCharacterMsg.kccMotor.transform.localPosition = Vector3.zero;
                //usePropCharacterMsg.kccMotor.transform.localRotation = Quaternion.identity;

                var item = GetNpcParent(usePropCharacterMsg.playerStateController.PlayerID);
                beUse.Remove(behaviours[item.Item1]);

                behaviours[item.Item1].gameObject.SetActive(true);
            }
            if (_currentUsedCount == 0)
            {
                OnReset();
                OnPropReset();
            }
        }

        public override void OnPropReset()
        {
            if (isPlaying == false && IsCanClick)
                return;

            isPlaying = false;
            IsCanClick = true;

            CameraReset();

            beUse.Clear();
            //for (int i = 0; i < npc.Count; i++)
            //{
            //    var id = npc[i];
            //    var music = musical[i];
            //
            //    PlayerStateController player;
            //    if (id == AccountDataManager.Inst.UserInfo.uid)
            //    {
            //        player = AvatarController.Inst.SelfStateController;
            //    }
            //    else
            //    {
            //        player = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(id);
            //    }
            //
            //    player.ExitState(PlayerState.MusicInstrumentPlay);
            //
            //    player.EnterState(PlayerState.Default);
            //
            //    if (id == AccountDataManager.Inst.UserInfo.uid)
            //    {
            //        MoveToNode(player.PlayerKCCtrl.Motor, AvatarController.Inst.transform, true);
            //    }
            //    else
            //    {
            //        MoveToNode(player.PlayerKCCtrl.Motor, AIBuddyAvatarController.Inst.transform, true);
            //    }
            //
            //    player.PlayerKCCtrl.transform.localPosition = Vector3.zero;
            //
            //    //var holdBehaviour = player.GetComponent<PlayerHoldBehaviour>();
            //    //holdBehaviour.StopPreviewInstrument(true);
            //}


        }

        private void Update()
        {
            if (duration > 0)
            {
                duration -= Time.deltaTime;
                if (duration <= 0 && isPlaying)
                {
                    UIAgentManager.Inst.OpenPanel(PanelId.AIParkConfirmPanel, WindowId.GuestWindow);
                    MessageHelper.Broadcast(MessageName.OnConfirmPanel,
                        new AIParkConfirmParam
                        {
                            Title = "现场演出",
                            Desc = "演奏已结束，是否退出舞台",
                            LText = "再来一次",
                            RText = "退出舞台",
                            LAction = Again,
                            RAction = Stop
                        });
                }
            }
        }

        public void FillNpc()
        {
            npc.Clear();
            npc.Add(AccountDataManager.Inst.UserInfo.uid);
            var count = musical.Count - 1;
            var player = AIBuddyAvatarController.Inst.GetDic().ToList();
            for (var i = 0; i < count; i++)
            {
                if (i < player.Count)
                {
                    npc.Add(player[i].Value.PlayerID);
                }
            }
        }

        public void Stop()
        {
            var ls = new List<UsePropCharacterMsg>();
            ls.AddRange(_usePropCharacterMsg.Values);
            foreach (var id in ls)
            {
                var tem = id.playerStateController.PlayerID;
                if (tem == AccountDataManager.Inst.Uid)
                {
                    AIParkPropsManager.Inst.SelfEnterIdle();
                    continue;
                    // tem = ((int)ParkNpcRoleType.self).ToString();
                }
                var npc = AIPark_CharacterManager.Inst.GetNpc(tem);

                npc.ForceEnterLastHistory(true);
                // var npcBev = npc.GetComponent<AIPark_CharacterBehaviour>();
                // npcBev.GetChapterHandler().ForceChangeState(ActionType.Idle);
            }
        }

        public void CameraReset()
        {
            foreach (var item in behaviours)
            {
                item.IsCanClick = true;
            }
            if (!bgm.isLocal)
            {
                BgMusicManager.Inst.OnEdit();
            }
            else
            {
                AIGameSoundUtils.Inst.StopBgm(bgm.name);
            }

            var ugc = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.bgMusicUrl;
            if (!string.IsNullOrEmpty(ugc))
            {
                BgMusicManager.Inst.SetUGCMusic(ugc);
                BgMusicManager.Inst.PlayUGCMusic();
            }
            else
            {
                AIGameSoundUtils.Inst.PlayBgm(AIParkConfig.Bgm_S11Para_MainScene);
            }

            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.AIParkGameStagePanel);
            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.GuestInstrumentPlayPanel);
            UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.AIParkConfirmPanel);

            GameCameraUtils.Inst.GetCustomVirtualCamera().enabled = false;
            GameCameraUtils.Inst.SetCinemachineTouchController(false, new CinemachineTouchParam());
            GameCameraUtils.Inst.GetPlayVirtualCamera().enabled = true;
        }

        public void CameraAnim(CinemachineTouchParam param)
        {
            var playerCamera = GameCameraUtils.Inst.GetPlayVirtualCamera();
            var camera = GameCameraUtils.Inst.GetCustomVirtualCamera();
            GameCameraUtils.Inst.SetCinemachineTouchController(true, param);
            camera.Follow = param.target;
            camera.LookAt = param.target;
            camera.m_Lens.FieldOfView = param.fov;
            var com = camera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if(com == null){
                com = camera.AddCinemachineComponent<CinemachineFramingTransposer>();
            }
            com.m_TrackedObjectOffset = param.followOff;

            playerCamera.enabled = false;
            if (!camera.enabled)
            {
                camera.enabled = true;
            }
        }

        public void Show()
        {
            BgMusicManager.Inst.StopGameMusicNode();
            isPlaying = true;
            Again();
            foreach (var id in npc)
            {
                var tem = id;
                if (tem == AccountDataManager.Inst.Uid)
                {
                    tem = ((int)ParkNpcRoleType.self).ToString();
                }
                var _npc = AIPark_CharacterManager.Inst.GetNpc(tem);

                // AIParkPropsManager.Inst.ExitAction(tem);
                var npcBev = _npc.GetComponent<AIPark_CharacterBehaviour>();
                AIParkPropsManager.Inst.EnterAction(npcBev, ActionType.PerformOnStage, this, true);
            }

            //for (int i = 0; i < npc.Count; i++)
            //{
            //    var id = npc[i];
            //    var music = musical[i];
            //
            //    PlayerStateController player;
            //    if (id == AccountDataManager.Inst.UserInfo.uid)
            //    {
            //        player = AvatarController.Inst.SelfStateController;
            //    }
            //    else
            //    {
            //        player = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(id);
            //    }
            //
            //    MoveToNode(player.PlayerKCCtrl.Motor, NpcParent[i], false);
            //    player.PlayerKCCtrl.Motor.transform.localPosition = Vector3.zero;
            //    player.PlayerKCCtrl.Motor.transform.localRotation = Quaternion.identity;
            //
            //    //var holdBehaviour = player.GetComponent<PlayerHoldBehaviour>();
            //
            //    MusicalSyncParam param = new MusicalSyncParam();
            //
            //
            //    param.resId = music.id;
            //    if (music.isUgc)
            //    {
            //        UIAgentManager.Inst.AssetGetInfo(4, music.id, (t) =>
            //        {
            //            //holdBehaviour.PreviewUGCInstrument(t.skinActionInfo.instrumentInfo);
            //            param.detailInfo = t.skinActionInfo.instrumentInfo.animDetailInfo;
            //            param.moveId = t.skinActionInfo.instrumentInfo.moveId;
            //            player.DirectIntoState(PlayerState.MusicInstrumentPlay, param);
            //        });
            //    }
            //    else
            //    {
            //        //var config = DataTables.GetInstrumentConfig(music.id);
            //        //InstrumentInfo info = new InstrumentInfo();
            //        //info.moveId = config.moveId;
            //        //info.toneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(config.toneId);
            //        //info.animDetailInfo = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
            //        //holdBehaviour.PreviewPGCInstrument(music.id);
            //        param.detailInfo = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
            //        param.moveId = DataTables.GetInstrumentConfig(music.id).moveId;
            //        player.DirectIntoState(PlayerState.MusicInstrumentPlay, param);
            //    }
            //}
        }

        public void Again()
        {
            if (!bgm.isLocal)
            {
                BgMusicManager.Inst.SetUGCMusic(bgm.url);
                BgMusicManager.Inst.PlayUGCMusic((source) =>
                {
                    if (source != null)
                    {
                        Debug.Log($"Ugc音效时长: {source.clip.length} 秒");
                        duration = source.clip.length;
                    }
                });
            }
            else
            {
                AIGameSoundUtils.Inst.PlayBgm(bgm.name, null);
                // AIGameSoundUtils.Inst.PlayBgm(bgm.name, null, (in_cookie, in_type, in_info) =>
                // {
                //     if (in_type == AkCallbackType.AK_Duration)
                //     {
                //         var durationInfo = in_info as AkDurationCallbackInfo;
                //         if (durationInfo != null)
                //         {
                //             float durationMs = durationInfo.fDuration; // 单位：毫秒
                //             Debug.Log($"官方音效时长: {durationMs / 1000f} 秒");
                //             duration = durationMs / 1000f;
                //         }
                //     }
                // });
            }
        }

        public void ToStageView(bool isShow = true)
        {

            CinemachineTouchParam param;
            param.target = StageParent.Find("target");
            param.followOff = new Vector3(0, 2, 4);
            param.fov = 100;
            param.rotateSpeed = 5;
            param.zoomSpeed = 5;
            param.mouseRotateSpeed = 5;
            param.mouseZoomSpeed = 5;
            param.minZoom = 10;
            param.maxZoom = 20;
            CameraAnim(param);
            if (isShow)
            {
                IsCanClick = false;
                beUse.Clear();
                var idx = 0;
                foreach (var item in npc)
                {
                    SetNpc(behaviours[idx], item);
                    idx++;
                }
                Show();
            }
        }

        public void ToPeopleView()
        {
            CinemachineTouchParam param;
            param.target = StageParent.Find("target");
            param.followOff = new Vector3(0, 0, 4);
            param.fov = 100;
            param.rotateSpeed = 5;
            param.zoomSpeed = 5;
            param.mouseRotateSpeed = 5;
            param.mouseZoomSpeed = 5;
            param.minZoom = 10;
            param.maxZoom = 20;
            CameraAnim(param);
        }

        public void ToNpcView(int idx)
        {
            var music = behaviours[idx];
            var id = beUse[music];
            PlayerStateController player;
            if (id == AccountDataManager.Inst.UserInfo.uid)
            {
                player = AvatarController.Inst.SelfStateController;
                if (UIAgentManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.GuestInstrumentPlayPanel) == false)
                {
                    UIAgentManager.Inst.OpenPanel(PanelId.GuestInstrumentPlayPanel, WindowId.GuestWindow, music.musical.id, music.instrumentInfo, true);
                }
            }
            else
            {
                player = AIBuddyAvatarController.Inst.GetPlayerStateCtrl(id);
                if (UIAgentManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.GuestInstrumentPlayPanel) == true)
                {
                    UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow, PanelId.GuestInstrumentPlayPanel);
                }
            }

            CinemachineTouchParam param;
            param.target = MusicParent[idx].Find("target");
            param.followOff = new Vector3(0, 1, 2);
            param.fov = 30;
            param.rotateSpeed = 1;
            param.zoomSpeed = 1;
            param.mouseRotateSpeed = 2;
            param.mouseZoomSpeed = 2;
            param.minZoom = 5;
            param.maxZoom = 15;
            CameraAnim(param);
        }

        public void SinglePlay(string id, AIPark_InstrumentBehaviour music)
        {
            var config = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.AICommonGameConfig;
            if (config.stage.musicUrls.Count <=0)
            {
                config.stage.DefaulBgm();
            }
            bgm = config.stage.musicUrls[0];
            npc.Clear();
            npc.Add(id);
            musical.Clear();
            musical.Add(music.musical);
            beUse.Clear();
            var idx = music.index;
            SetNpc(music, id);
            ToNpcView(idx);
            Show();

            IsCanClick = false;
            UIAgentManager.Inst.OpenPanel(PanelId.AIParkGameStagePanel, WindowId.GuestWindow, this, true);
        }

        public void GetMusicalSync(int idx, Action<MusicalSyncParam> action)
        {
            var behaviour = behaviours[idx];
            var id = beUse[behaviour];
            var index = npc.IndexOf(id);
            if (musical == null || musical.Count <= 0)
            {
                musical = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.AICommonGameConfig.stage.musicalInstruments;
                //var v = new AICommonGameConfig_Stage();
                //v.DefaultInstrments();
                //musical = v.musicalInstruments;
            }
            var music = musical[index];
            MusicalSyncParam param = new MusicalSyncParam();
            param.isHide = true;
            param.resId = music.id;
            if (music.isUgc)
            {
                UIAgentManager.Inst.AssetGetInfo(4, music.id, (t) =>
                {
                    //holdBehaviour.PreviewUGCInstrument(t.skinActionInfo.instrumentInfo);
                    param.detailInfo = t.skinActionInfo.instrumentInfo.animDetailInfo;
                    param.moveId = t.skinActionInfo.instrumentInfo.moveId;
                    action?.Invoke(param);
                });
            }
            else
            {
                //var config = DataTables.GetInstrumentConfig(music.id);
                //InstrumentInfo info = new InstrumentInfo();
                //info.moveId = config.moveId;
                //info.toneInfo = MusicalInstrumentUtils.GetPgcToneInfoByPgcToneId(config.toneId);
                //info.animDetailInfo = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
                //holdBehaviour.PreviewPGCInstrument(music.id);
                param.detailInfo = MusicalInstrumentUtils.GetDefaultInstrumentDetailInfo();
                param.moveId = DataTables.GetInstrumentConfig(music.id).moveId;
                param.detailInfo.pDef = Vector3.zero;
                param.detailInfo.rDef = Vector3.zero;
                param.detailInfo.sDef = Vector3.one;
                action?.Invoke(param);
            }

        }

        protected override void PlayWithBuddy()
        {
        }
    }
}


using System;
using System.Collections;
using System.Collections.Generic;
using AIGame.Base;
using Cinemachine;
using Game.Avatar;
using Game.Base;
using Game.MapSetting;
using Game.Props.PropsBehaviours;
using Game.Props.PropsComponents;
using Game.Utils;
using GameData.Manager;
using Message;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    public class TrojanHorseState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private ParkNpcTransData _targetPointData;
        private bool _isMoving = false;
        bool bUseCustomCamera = false;
        private AIPark_TrojanhorseBehaviour _trojanhorse;
        bool _hadPlay = false; //是否已经玩过

        Transform _horseTarget;
        public TrojanHorseState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _hadPlay = false;
            LoggerUtils.Log($"[TrojanHorseState] {_character.GetNpcName()} 进入旋转木马状态");

            if (_chapterData.nodeBaseBehaviour != null){
                _trojanhorse = _chapterData.nodeBaseBehaviour as AIPark_TrojanhorseBehaviour;
            }
            else
            {
                _trojanhorse = GlobalNodeManager.Inst.Get<AIPark_TrojanhorseMgr>().GetAvailableBehaviour((x) =>
                {
                    return x.IsCanClick;
                });
            }

            if (_trojanhorse != null){
                var (emptyNode, index) = _trojanhorse.GetEmptyNode();
                if (emptyNode != null)
                {
                    //这里正常应该是寻找离旋转木马最近的空位 然后执行到达目标，上座的逻辑
                    // _targetPointData = _trojanhorse.GetEmptySeesawChair(index);
                    // _targetPointData = AIPark_CharacterUtils.Inst.GetNearTransData(_trojanhorse.transform.position, 8.5f, 10f);
                    _targetPointData = AIPark_CharacterUtils.Inst.GetNearTransBetweenTwoPoints(_trojanhorse.transform.position, _character.transform.position, 8.5f, 9.2f);
                    // if (tempTargetPointData != null)
                    // {
                    //     _targetPointData = tempTargetPointData;
                    // }
                    MoveToTargetPoint();
                    return;
                }
            }
            AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.TrojanHorse);
        }

        public override void OnExit()
        {
            base.OnExit();
            Reset();
            LoggerUtils.Log($"[TrojanHorseState] {_character.GetNpcName()} 退出旋转木马状态");
            CleanData();
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            CleanData();
        }

        private void Reset()
        {
            if (_trojanhorse != null)
            {
                _trojanhorse.OnPropPosFree(_character._npcKccCtr);
                CheckResetCamera();
            }
            var curPos = _character.transform.position;
            _character.GetComponent<NavMeshAgent>().enabled = true;
            _character.StopMoving();
            if (_hasReachedTarget && _hadPlay)
            {
                if (_character.GetNpcID() == ((int)ParkNpcRoleType.self).ToString())
                {
                    var ugc = GameDataManager.Inst.mapGlobalData?.curUgcBaseInfo?.gameSetting?.bgMusicUrl;
                    if(string.IsNullOrEmpty(ugc)){
                        AIGameSoundUtils.Inst.PlayBgmWithNoCb(AIParkConfig.Bgm_S11Para_MainScene);
                    }else{
                        BgMusicManager.Inst.SetUGCMusic(ugc);
                        BgMusicManager.Inst.PlayUGCMusic();
                    }
                }
                if(_horseTarget != null)
                {
                    GameObject.Destroy(_horseTarget.gameObject);
                    _horseTarget = null;
                }
                // var pos = _trojanhorse.gameObject.transform.position;
                // pos.y = 0;
                var _npcTransData = AIPark_CharacterUtils.Inst.GetNearTransData(curPos, 3f, 5f);
                _character.SetPositionAndRotation(_npcTransData.pos, Quaternion.Euler(_npcTransData.rot));
            }

            _character.playerStateControllerState.ExitState(PlayerState.SingleEmote, false);    
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            _isMoving = false;
            _hasReachedTarget = true;
            _character.SetPositionAndRotation(_targetPointData.pos, Quaternion.Euler(_targetPointData.rot));
            Debug.Log("乐园" + _character.GetNpcName() + " 到达旋转木马位置");
            //todo 具体化动作
            _character.SetHandNodeActive(false); //手部动画是否隐藏
            _character.StopMoving();
            if (_trojanhorse != null && _trojanhorse.IsCanClick)  //当到达目标位置时，确保该道具仍然可以使用
            {
                _hadPlay = true;
                _character.GetComponent<NavMeshAgent>().enabled = false;
                var usePosIdx = _trojanhorse.AddUsePropCharacterMsg(_character._npcKccCtr, _character._npcKccCtr.Motor, _character.playerStateControllerState);
                // if(usePosIdx != -1)
                // {
                //     _trojanhorse.UsePosIdx(usePosIdx);
                // }
                _trojanhorse.OnTouchClick();
                CheckCamera();
            }
            else
            {
                AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.TrojanHorse);
                //走到目标位置发现该道具已经被占用(暂时不做这个扩展)
                AIParkPropsManager.Inst.CheckDoCustomActionWhenPropBePlaced(_character.GetNpcID(), ActionType.TrojanHorse);
            }
        }

        private void CheckCamera()
        {
            if (_character.GetNpcID() == ((int)ParkNpcRoleType.self).ToString())
            {
                BgMusicManager.Inst.StopGameMusicNode();
                AIGameSoundUtils.Inst.PlayBgmWithNoCb(AIParkConfig.Bgm_Para_Carousel);

                bUseCustomCamera = true;
                var selfTransform = AvatarController.Inst.SelfController.transform;
                var go = new GameObject("horseTarget");
                go.transform.SetParent(selfTransform);
                go.transform.localScale = Vector3.one;
                go.transform.localPosition = new Vector3(0,1,0);
                go.transform.localRotation = Quaternion.identity;
                _horseTarget = go.transform;

                var transform = go.transform;
                CinemachineTouchParam param;
                param.target = transform;
                param.followOff = new Vector3(0, 2.5f,-11);
                param.fov = 40;
                param.rotateSpeed = 1;
                param.zoomSpeed = 1;
                param.mouseRotateSpeed = 2;
                param.mouseZoomSpeed = 2;
                param.minZoom = 5;
                param.maxZoom = 15;
                var playerCamera = GameCameraUtils.Inst.GetPlayVirtualCamera();
                var camera = GameCameraUtils.Inst.GetCustomVirtualCamera();
                GameCameraUtils.Inst.SetCinemachineTouchController(true, param);
                camera.Follow = param.target;
                camera.LookAt = param.target;
                camera.m_Lens.FieldOfView = param.fov;
                camera.AddCinemachineComponent<CinemachineTransposer>().m_FollowOffset = param.followOff;
                camera.GetCinemachineComponent<CinemachineTransposer>().m_BindingMode = CinemachineTransposer.BindingMode.WorldSpace;
                camera.GetCinemachineComponent<CinemachineComposer>().m_TrackedObjectOffset = new Vector3(0,0,0);
                // camera.GetCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset = param.followOff;
                playerCamera.enabled = false;
                if (!camera.enabled)
                {
                    camera.enabled = true;
                }
            }
        }

        private void CheckResetCamera()
        {
            if (bUseCustomCamera)
            {
                GameCameraUtils.Inst.GetCustomVirtualCamera().enabled = false;
                GameCameraUtils.Inst.SetCinemachineTouchController(false, new CinemachineTouchParam());
                GameCameraUtils.Inst.GetPlayVirtualCamera().enabled = true;
            }
        }
        private void CleanData()
        {
            _isMoving = false;
            _hasReachedTarget = false;

            _character.SetHandNodeActive(true);
        }

        private void MoveToTargetPoint()
        {
            _isMoving = true;
            _character.SetHandNodeActive(true);

            LoggerUtils.Log($"[TrojanHorseState] {_character.GetNpcName()} 移动到旋转木马位置: {_targetPointData.pos}");
            if(_character.GetNpcID() == "0" || _chapterData.bQuickEnter){
                OnReachedTarget();
                return;
            }
            if(Vector3.Distance(_character.transform.position,_trojanhorse.transform.position) < 8.5f)
            {
                Debug.Log("乐园直接到达旋转木马" + _character.GetNpcName());
                OnReachedTarget();
                return;
            }
            _character.MoveToPositionNear(_targetPointData.pos, (canMove) =>
            {
                Debug.Log("乐园旋转木马" + _character.GetNpcName() + "到达情况:" + canMove);
                if (canMove)
                {
                    OnReachedTarget();
                }
                else
                {
                    AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.TrojanHorse);
                }
            }, 0,0.5f);
        }
    }
}
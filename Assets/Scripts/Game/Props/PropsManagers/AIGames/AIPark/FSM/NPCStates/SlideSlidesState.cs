using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Game.Avatar;
using Game.Base;
using Game.Props.PropsBehaviours;
using Game.Utils;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    public class SlideSlidesState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private ParkNpcTransData _targetPointData;
        private bool _isMoving = false;
        private bool bUseCustomCamera = false;
        AIPark_SlideBehaviour _slide;
        bool _hadPlay = false; //是否已经玩过
        public SlideSlidesState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _hadPlay = false;
            LoggerUtils.Log($"[SlideSlidesState] {_character.GetNpcName()} 进入滑梯状态");

            if (_chapterData.nodeBaseBehaviour != null)
            {
                _slide = _chapterData.nodeBaseBehaviour as AIPark_SlideBehaviour;
            }
            else
            {
                _slide = GlobalNodeManager.Inst.Get<AIPark_SlideMgr>().GetAvailableBehaviour((x) =>
                {
                    return x.IsCanClick;
                });
            }
            if (_slide != null)
            {
                var tempPos = _slide.transform.TransformPoint(new Vector3(-0.013f, 0, 0));
                tempPos.y = 0;
                var tempTargetPointData = AIPark_CharacterUtils.Inst.GetNearTransDataAtSameSide(_character.gameObject.transform.position, tempPos, 2f, 4f);
                if (tempTargetPointData != null)
                {
                    // tempPos = tempTargetPointData.pos;
                }
                _targetPointData = new();
                _targetPointData.pos = tempPos;
                _targetPointData.rot = new Vector3(0, 0, 0);
                MoveToTargetPoint();
                return;
            }
            AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SlideSlides);
        }

        public override void OnExit()
        {
            base.OnExit();
            Reset();
            LoggerUtils.Log($"[SlideSlidesState] {_character.GetNpcName()} 退出滑梯状态");
            CleanData();
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            CleanData();
        }

        private void Reset()
        {
            if (_slide != null)
            {
                _slide.OnPropPosFree(_character._npcKccCtr);
                CheckResetCamera();
            }
            var curPos = _character.transform.position;
            _character.GetComponent<NavMeshAgent>().enabled = true;
            _character.StopMoving();
            if (_hasReachedTarget && _hadPlay)
            {
                var _npcTransData = AIPark_CharacterUtils.Inst.GetNearTransData(curPos, 0.5f, 2.5f);
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
            //todo 具体化动作
            _character.SetHandNodeActive(false); //手部动画是否隐藏

            if (_slide != null && _slide.IsCanClick)
            {
                _hadPlay = true;
                _character.GetComponent<NavMeshAgent>().enabled = false;
                int usePosIndex = _slide.AddUsePropCharacterMsg(_character._npcKccCtr, _character._npcKccCtr.Motor, _character.playerStateControllerState);
                if (usePosIndex != -1)
                {
                    _slide._usePropCharacterMsg[usePosIndex].waitFrame = 2;
                    _slide.Set2BeginState(_slide._usePropCharacterMsg[usePosIndex]);
                    _slide.OnTouchClick();
                }
                CheckCamera();
            }
            else
            {
                AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SlideSlides);
                //走到目标位置发现该道具已经被占用(暂时不做这个扩展)
                AIParkPropsManager.Inst.CheckDoCustomActionWhenPropBePlaced(_character.GetNpcID(), ActionType.SlideSlides);
            }
        }

        private void CheckCamera()
        {
            if (_character.GetNpcID() == ((int)ParkNpcRoleType.self).ToString())
            {
                bUseCustomCamera = true;
                var transform = AvatarController.Inst.SelfController.transform;

                CinemachineTouchParam param;
                param.target = transform;
                param.followOff = new Vector3(0, 1, 5);
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
                camera.AddCinemachineComponent<CinemachineFramingTransposer>().m_TrackedObjectOffset = param.followOff;

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


        public bool IsInsideTarget()
        {
            if(_character.GetNpcID() == "0" || _chapterData.bQuickEnter){
                return true;
            }
            float distance = 1.5f;
            var pos1 = _character.transform.position;
            var pos2 = _slide.transform.position;
            pos2.y = 0;
            pos1.y = 0;
            return Vector3.Distance(pos1,pos2) < distance;
        }
        private void MoveToTargetPoint()
        {
            _isMoving = true;
            _character.SetHandNodeActive(true);

            LoggerUtils.Log($"[SlideSlidesState] {_character.GetNpcName()} 移动到滑梯位置: {_targetPointData.pos}");

            // _character.MoveToPositionNear(_targetPointData.pos, (canMove) =>
            // {
            //     if (canMove)
            //     {
            //         OnReachedTarget();
            //     }
            //     else
            //     {
            //         AIParkPropsManager.Inst.ExitAction(_character.GetNpcRoleType(), ActionType.SlideSlides);
            //     }
            // }, 0, 1f);
            if (IsInsideTarget())
            {
                OnReachedTarget();
                return;
            }

            _character.MoveToPosition(_targetPointData.pos, () =>
           {
               {
                   OnReachedTarget();
               }

           }, 0);
        }
    }
}
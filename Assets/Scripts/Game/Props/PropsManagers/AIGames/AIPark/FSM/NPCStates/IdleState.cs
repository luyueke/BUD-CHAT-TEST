using System;
using System.Collections;
using System.Collections.Generic;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    /// <summary>
    /// 默认的空闲状态，用于未知的ActionType类型
    /// </summary>
    public class IdleState : AIChapterState
    {
        private ParkNpcTransData _targetPointData;
        bool _isMoving = false;
        public IdleState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _character.playerStateControllerState.ExitState(PlayerState.SingleEmote);
            _targetPointData = GetPathSeekTarget();
            if (_targetPointData == null || _character.GetNpcID() == ((int)ParkNpcRoleType.self).ToString())
            {
                SetEndState();
                return;
            }else{
                MoveToTargetPoint();
            }

        }

        void OnReachedTarget(){
            _isMoving = false;
            _character.SetPositionAndRotation(_targetPointData.pos, Quaternion.Euler(_targetPointData.rot));
            SetEndState();
        }

        public void MoveToTargetPoint()
        {
            _isMoving = true;
            _targetPointData = GetPathSeekTarget();
            if (_targetPointData == null)
            {
                SetEndState();
                return;
            }

            _character.MoveToPositionNear(_targetPointData.pos, (canMove) =>
            {
                if (canMove)
                {
                    OnReachedTarget();
                }else{
                    SetEndState();
                }
            }, 0, 0.3f);
        }

        public void SetEndState()
        {
            if (_character._npcAnimController != null)
            {
                LoggerUtils.Log($"[IdleState] {_character.GetNpcName()} 进入空闲状态");
                var aniState = PlayerAniState.Idle;
                _character._npcAnimController.SetPlayerAniState(aniState, false);
            }
        }

        public ParkNpcTransData GetPathSeekTarget()
        {
            // if(_chapterData.location == (int)LocationType.None){
            //     return null;
            // }
            var npcBev = AIPark_CharacterManager.Inst.GetNpc(_character.GetNpcID());
            if (npcBev != null)
            {
                if (npcBev.GetChapterHandler().IsJustIdle())
                {
                    return null;
                }
            }
            return AIPark_CharacterUtils.Inst.GetLocationTargetNear((LocationType)_chapterData.location);
        }

        public bool DoPathSeek()
        {
            return _chapterData.location != (int)LocationType.None;
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[IdleState] {_character.GetNpcName()} 退出空闲状态");
        }

        public override void OnUpdate(float deltaTime)
        {
        }
    }
}
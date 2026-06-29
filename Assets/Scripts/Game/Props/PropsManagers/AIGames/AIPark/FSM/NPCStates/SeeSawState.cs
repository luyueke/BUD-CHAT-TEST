using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Message;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    public class SeeSawState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private ParkNpcTransData _targetPointData;
        private bool _isMoving = false;
        private AIPark_SeesawBehaviour _seesaw;
        bool _hadPlay = false; //是否已经玩过

        public SeeSawState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _hadPlay = false;
            LoggerUtils.Log($"[SeeSawState] {_character.GetNpcName()} 进入跷跷板状态");
            if (_chapterData.nodeBaseBehaviour != null)
            {
                _seesaw = _chapterData.nodeBaseBehaviour as AIPark_SeesawBehaviour;
            }
            else
            {
                _seesaw = GlobalNodeManager.Inst.Get<AIPark_SeesawMgr>().GetAvailableBehaviour((x) =>
                {
                    return x.IsCanClick;
                });
            }


            if (_seesaw != null)
            {
                var (emptyNode, index) = _seesaw.GetEmptyNode();
                if (emptyNode != null)
                {
                    _targetPointData = _seesaw.GetEmptySeesawChair(index);
                    // _targetPointData.pos = _seesaw.transform.position; //直接使用整个跷跷板的位置
                    var tempTargetPointData = AIPark_CharacterUtils.Inst.GetNearTransDataAtSameSide(_character.gameObject.transform.position, _targetPointData.pos, 0.5f, 1.2f);
                    if (tempTargetPointData != null)
                    {
                        _targetPointData = tempTargetPointData;
                    }
                    MoveToTargetPoint();
                    return;
                }
            }
            AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SeeSaw);
        }

        public bool IsInsideTarget(){
            if(_character.GetNpcID() == "0" || _chapterData.bQuickEnter){
                return true;
            }
            var pos1 = _character.transform.position;
            var pos2 = _seesaw.transform.position;
            pos2.y = 0;
            pos1.y = 0;
            return Vector3.Distance(pos1,pos2) < 1.5f;
        }

        public override void OnExit()
        {
            //这里应该采样周边可站立的点 重置motor到这个点
            base.OnExit();
            Reset();
            LoggerUtils.Log($"[SeeSawState] {_character.GetNpcName()} 退出跷跷板状态");
            CleanData();
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            CleanData();
        }

        private void Reset()
        {
            if (_seesaw != null)
            {
                _seesaw.OnPropPosFree(_character._npcKccCtr);
            }
            var curPos = _character.transform.position;
            _character.GetComponent<NavMeshAgent>().enabled = true;
            _character.StopMoving();
            if (_hasReachedTarget && _hadPlay)
            {
                var _npcTransData = AIPark_CharacterUtils.Inst.GetNearTransData(curPos, 0.5f, 1.2f);
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
            // _seesaw = GlobalNodeManager.Inst.Get<AIPark_SeesawMgr>().GetAvailableBehaviour((x) =>
            // {
            //     return x.IsCanClick;
            // });
            if (_seesaw != null && _seesaw.IsCanClick)  //当到达目标位置时，确保该道具仍然可以使用
            {
                _hadPlay = true;
                _character.GetComponent<NavMeshAgent>().enabled = false;
                _seesaw.AddUsePropCharacterMsg(_character._npcKccCtr, _character._npcKccCtr.Motor, _character.playerStateControllerState);
                _seesaw.OnTouchClick();
            }
            else
            {
                AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SeeSaw);
                //走到目标位置发现该道具已经被占用
                AIParkPropsManager.Inst.CheckDoCustomActionWhenPropBePlaced(_character.GetNpcID(), ActionType.SeeSaw);
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

            LoggerUtils.Log($"[SeeSawState] {_character.GetNpcName()} 移动到跷跷板位置: {_targetPointData.pos}");
            if(IsInsideTarget()){
                OnReachedTarget();
                return;
            }
            _character.MoveToPositionNear(_targetPointData.pos, (canMove) =>
            {
                if (canMove)
                {
                    OnReachedTarget();
                }
                else
                {
                    AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SeeSaw);
                }
            }, 0,0.1f);
        }
    }
}
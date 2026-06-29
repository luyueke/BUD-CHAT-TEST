using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    public class SwingingState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private ParkNpcTransData _targetPointData;
        private bool _isMoving = false;
        private AIPark_SwingBehaviour _swinging;
        bool _hadPlay = false; //是否已经玩过
        public SwingingState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _hadPlay = false;

            LoggerUtils.Log($"[SwingingState] {_character.GetNpcName()} 进入荡秋千状态");

            if (_chapterData.nodeBaseBehaviour != null)
            {
                _swinging = _chapterData.nodeBaseBehaviour as AIPark_SwingBehaviour;
            }
            else
            {
                _swinging = GlobalNodeManager.Inst.Get<AIPark_SwingMgr>().GetAvailableBehaviour((x) =>
                {
                    return x.IsCanClick;
                });
            }

            if (_swinging != null)
            {
                var (emptyNode, index) = _swinging.GetEmptyNode();
                if (emptyNode != null)
                {
                    _targetPointData = _swinging.GetEmptySwingChair(index); //暂时不用椅子位置，直接到秋千位置
                    _targetPointData.pos = _swinging.transform.position;
                    var tempTargetPointData = AIPark_CharacterUtils.Inst.GetNearTransDataAtSameSide(_character.gameObject.transform.position, _swinging.transform.position, 0.5f, 1.2f);
                    if (tempTargetPointData != null)
                    {
                        _targetPointData = tempTargetPointData;
                    }
                    MoveToTargetPoint();
                    return;
                }
            }
            AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.Swinging);

        }
        /// <summary>
        /// 判断是否在秋千范围内
        /// </summary>
        /// <returns></returns>
        public bool IsInsideTarget(){
             if(_character.GetNpcID() == "0" || _chapterData.bQuickEnter){
                return true;
            }
            var pos1 = _character.transform.position;
            var pos2 = _swinging.transform.position;
            pos2.y = 0;
            pos1.y = 0;
            return Vector3.Distance(pos1,pos2) < 2f;
        }

        public override void OnExit()
        {
            base.OnExit();

            Reset();
            LoggerUtils.Log($"[SwingingState] {_character.GetNpcName()} 退出荡秋千状态");
            CleanData();
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            CleanData();
        }

        private void Reset()
        {

            if (_swinging != null)
            {
                _swinging.OnPropPosFree(_character._npcKccCtr);
            }
            var curPos = _swinging.transform.position;
            _character.GetComponent<NavMeshAgent>().enabled = true;
            _character.StopMoving();
            if (_hasReachedTarget && _hadPlay)
            {
                var _npcTransData = AIPark_CharacterUtils.Inst.GetNearTransData(curPos, 0.5f, 2.8f);
                if(_npcTransData != null){
                    _character.SetPositionAndRotation(_npcTransData.pos, Quaternion.Euler(_npcTransData.rot));
                    Debug.Log("乐园荡秋千" + _character.GetNpcName() + " 重置位置: " + _npcTransData.pos);
                }

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
            Debug.Log("乐园" + _character.GetNpcName() + " 到达荡秋千位置");

            //todo 具体化动作
            _character.SetHandNodeActive(false); //手部动画是否隐藏
            if (_swinging != null && _swinging.IsCanClick)  //当到达目标位置时，确保该道具仍然可以使用
            {
                _hadPlay = true;
                _character.GetComponent<NavMeshAgent>().enabled = false;
                _swinging.AddUsePropCharacterMsg(_character._npcKccCtr, _character._npcKccCtr.Motor, _character.playerStateControllerState);
                _swinging.OnTouchClick();
            }
            else
            {
                AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.Swinging);
                //走到目标位置发现该道具已经被占用
                AIParkPropsManager.Inst.CheckDoCustomActionWhenPropBePlaced(_character.GetNpcID(), ActionType.Swinging);
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

            LoggerUtils.Log($"[SwingingState] {_character.GetNpcName()} 移动到荡秋千位置: {_targetPointData.pos}");
            if(IsInsideTarget()){
                Debug.Log("乐园直接上去荡秋千" + _character.GetNpcName());
                OnReachedTarget();
                return;
            }
            _character.MoveToPositionNear(_targetPointData.pos, (canMove) =>
            {
                Debug.Log("乐园荡秋千" + _character.GetNpcName() + "到达情况:" + canMove);
                if (canMove)
                {
                    OnReachedTarget();
                }
                else
                {
                    AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.Swinging);
                }
            }, 0, 1.5f);
        }
    }
}
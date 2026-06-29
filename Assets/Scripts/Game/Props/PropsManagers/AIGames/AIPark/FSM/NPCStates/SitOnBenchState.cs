using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    public class SitOnBenchState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private ParkNpcTransData _targetPointData;
        private bool _isMoving = false;
        private AIPark_BenchBehaviour _bench;
        bool _hadPlay = false; //是否已经玩过

        public SitOnBenchState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            _hadPlay = false;
            LoggerUtils.Log($"[SitOnBenchState] {_character.GetNpcName()} 进入长椅状态");
            if (_chapterData.nodeBaseBehaviour != null)
            {
                _bench = _chapterData.nodeBaseBehaviour as AIPark_BenchBehaviour;
            }
            else
            {
                _bench = GlobalNodeManager.Inst.Get<AIPark_BenchMgr>().GetAvailableBehaviour((LocationType)_chapterData.location);
            }
            if (_bench != null)
            {
                var (emptyNode, index) = _bench.GetEmptyNode();
                _targetPointData = AIPark_CharacterUtils.Inst.GetNearTransDataAtSameSide(_character.gameObject.transform.position, _bench.transform.position, 0.5f, 1.2f);

                // if (emptyNode != null)
                // {
                //     _targetPointData = _bench.GetEmptySeesawChair(index);
                // }else{
                //     _targetPointData = _bench.GetEmptySeesawChair(0);
                // }
                MoveToTargetPoint();
                return;
            }
            else
            { 
                //寻找其他椅子
            }
            
            AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SitOnBench);
        }

        public override void OnExit()
        {
            base.OnExit();
            Reset();
            LoggerUtils.Log($"[SitOnBenchState] {_character.GetNpcName()} 退出长椅状态");
            CleanData();
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            CleanData();
        }

        private void Reset()
        {
            if (_bench != null)
            {
                _bench.OnPropPosFree(_character._npcKccCtr);
            }
            _character.GetComponent<NavMeshAgent>().enabled = true;
            _character.StopMoving();
            if (_hasReachedTarget && _hadPlay)
            {
                var _npcTransData = AIPark_CharacterUtils.Inst.GetNearTransData(_character.gameObject.transform.position, 0.5f, 1.2f);
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
            if (_bench != null && _bench.IsCanClick)  //当到达目标位置时，确保该道具仍然可以使用
            {
                _hadPlay = true;
                _character.GetComponent<NavMeshAgent>().enabled = false;
                var useIdx = _bench.AddUsePropCharacterMsg(_character._npcKccCtr, _character._npcKccCtr.Motor, _character.playerStateControllerState);
                LoggerUtils.Log("长椅AddUsePropCharacterMsg useIdx:"+useIdx+ "kcc="+_character._npcKccCtr.gameObject.name);
                _bench.OnTouchClick();
            }
            else
            {
                //椅子被占用
                _bench = GlobalNodeManager.Inst.Get<AIPark_BenchMgr>().GetAvailableBehaviour((bench)=>{
                    return bench.IsCanClick;
                });
                if(_bench != null){
                    var (emptyNode, index) = _bench.GetEmptyNode();
                    if (emptyNode != null)
                    {
                        // _targetPointData = _bench.GetEmptySeesawChair(index);
                        _targetPointData = AIPark_CharacterUtils.Inst.GetNearTransDataAtSameSide(_character.gameObject.transform.position, _bench.transform.position, 0.5f, 1.2f);
                        MoveToTargetPoint();
                    }else{
                        AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SitOnBench);
                    }
                }
                else
                {
                    AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SitOnBench);
                }
                // AIParkPropsManager.Inst.ExitAction(_character.GetNpcID(), ActionType.SitOnBench);
                //走到目标位置发现该道具已经被占用
                // AIParkPropsManager.Inst.CheckDoCustomActionWhenPropBePlaced(_character.GetNpcID(), ActionType.SitOnBench);
            }
        }

        private void CleanData()
        {
            _isMoving = false;
            _hasReachedTarget = false;

            _character.SetHandNodeActive(true);
        }

        public bool IsInsideTarget(){
            if(_character.GetNpcID() == "0" || _chapterData.bQuickEnter){
                return true;
            }
            return false;
            // var pos1 = _character.transform.position;
            // var pos2 = _seesaw.transform.position;
            // pos2.y = 0;
            // pos1.y = 0;
            // return Vector3.Distance(pos1,pos2) < 1.5f;
        }
        private void MoveToTargetPoint()
        {
            _isMoving = true;
            _character.SetHandNodeActive(true);

            LoggerUtils.Log($"[SitOnBenchState] {_character.GetNpcName()} 移动到    长椅位置: {_targetPointData.pos}");

            if (IsInsideTarget())
            {
                OnReachedTarget();
                return;
            }
            _character.MoveToPositionNear(_targetPointData.pos, (canMove) =>
            {
                if (canMove)
                {
                    OnReachedTarget();
                }
            }, 0, 1f);
        }
    }
}
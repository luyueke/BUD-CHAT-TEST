using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using Message;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    //ActionType.InjectionsToPatients
    public class InjectionsToPatientsState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        public InjectionsToPatientsState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        private ParkNpcTransData DoInjectionTransData = new ParkNpcTransData()
        {
            pos = new Vector3(-17.48f, 1.41f, -24.23f),
            rot = new Vector3(0, -180, 0),
        };
        
        
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[InjectionsToPatientsState] {_character.GetNpcName()} 进入给病人打针状态");
            
            //1.获取当前行为的目标地点
            _hasReachedTarget = false;
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[InjectionsToPatientsState] {_character.GetNpcName()} 退出给病人打针状态");
            
            _hasReachedTarget = false;
        }

        public override void OnUpdate(float deltaTime)
        {
            
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("InjectionsToPatientsState OnReachedTarget");
            _hasReachedTarget = true;
            
            //1.进入位置
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            _character.SetPositionAndRotation(DoInjectionTransData.pos, Quaternion.Euler(DoInjectionTransData.rot));

            var sittingStates = _characterManager.FindAllNpcsWithState<SittingState>();
            foreach (var sittingState in sittingStates.Values)
            {
                if (sittingState.GetChairType() == AIPark_ChairBehaviour.Park_ChairType.Injections)
                {
                    DoInjections();
                    return;
                }
            }
        }

        public void DoInjections()
        {
            //到达位置 并且椅子已经获取了
            if (_hasReachedTarget)
            {
                _character.PlayAnim("40100462");
            }
        }
    }
}
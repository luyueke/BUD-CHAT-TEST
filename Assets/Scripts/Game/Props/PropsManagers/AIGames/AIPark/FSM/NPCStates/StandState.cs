using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    //ActionType.Stand
    public class StandState : AIChapterState
    {
        protected bool _hasReachedTarget = false;

        public StandState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[StandState] {_character.GetNpcName()} 进入站立状态");
            
            //1.获取当前行为的目标地点
            _hasReachedTarget = false;
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[StandState] {_character.GetNpcName()} 退出站立状态");
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            _hasReachedTarget = true;
            
            // var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            // _character.SetPositionAndRotation(transData.pos, Quaternion.Euler(transData.rot));
            // // 站立状态可能不需要特别的动画，或者使用一个简单的站立动画
            // _character.PlayAnim("40200401"); // 站立动画ID，可能需要调整
        }
    }
} 
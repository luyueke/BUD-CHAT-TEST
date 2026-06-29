using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    //ActionType.Sleep
    public class SleepState : AIChapterState
    {
        private AIPark_BedBehaviour _curBedBev;
        
        public SleepState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[SleepState] {_character.GetNpcName()} 进入睡觉状态");
            
            //1.获取当前行为的目标地点
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[SleepState] {_character.GetNpcName()} 退出睡觉状态");
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("SleepState OnReachedTarget");
            
            _character.PlayAnim("40100029");
        }
    }
} 
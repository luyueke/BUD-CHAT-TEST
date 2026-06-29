using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.Toilet
    public class ToiletState : AIChapterState
    {
        private AIHospital_ToiletBehaviour _curToiletBev;
        public ToiletState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[ToiletState] {_character.GetNpcName()} 进入去上厕所的状态");
            
            //1.获取当前行为的目标地点
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[ToiletState] {_character.GetNpcName()} 退出上厕所的状态");
            if (_curToiletBev != null)
            {
                _curToiletBev.IsCanClick = true;
            }
            
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            _character.SetPositionAndRotation(transData.pos, Quaternion.Euler(transData.rot));
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("ToiletState OnReachedTarget");
            
            var toiletMgr = GlobalNodeManager.Inst.Get<AIHospital_ToiletManager>();
            _curToiletBev = toiletMgr.GetEmptyToilet();
            if (_curToiletBev != null)
            {
                _curToiletBev.IsCanClick = false;
                _character.SetPositionAndRotation(_curToiletBev.pos, Quaternion.Euler(_curToiletBev.rot));
                _character.PlayAnim("40200402");
            }
        }
    }
}

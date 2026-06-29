using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.LyingDown
    public class LyingDownState : AIChapterState
    {
        private AIHospital_BedBehaviour _curBedBev;
        
        public LyingDownState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }
        
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[LyingDownState] {_character.GetNpcName()} 进入躺下状态");
            
            //1.获取当前行为的目标地点
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[LyingDownState] {_character.GetNpcName()} 退出躺下状态");
            
            if (_curBedBev != null)
            {
                _curBedBev.IsCanClick = true;
            }
            
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            _character.SetPositionAndRotation(transData.pos, Quaternion.Euler(transData.rot));
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("LyingDownState OnReachedTarget");
            
            // 获取床铺管理器
            var bedManager = GlobalNodeManager.Inst.Get<AIHospital_BedManager>();
            // 获取可用的床铺
            _curBedBev = bedManager.GetEmptyBed();
            
            if (_curBedBev != null)
            {
                _curBedBev.IsCanClick = false;
                _character.SetPositionAndRotation(_curBedBev.pos, Quaternion.Euler(_curBedBev.rot));
                _character.PlayAnim("40200406"); // 躺下动画ID，可能需要调整
            }
        }
    }
} 
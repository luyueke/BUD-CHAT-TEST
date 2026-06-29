using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.TakingMedication
    public class TakingMedicationState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private AIHospital_MedicineBehaviour _curMedicineBev;

        public TakingMedicationState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[TakingMedicationState] {_character.GetNpcName()} 进入吃药状态");
            
            //1.获取当前行为的目标地点
            _hasReachedTarget = false;
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[TakingMedicationState] {_character.GetNpcName()} 退出吃药状态");
            
            if (_curMedicineBev != null)
            {
                _curMedicineBev.IsCanClick = true;
            }
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("TakingMedicationState OnReachedTarget");
            _hasReachedTarget = true;
            
            // 获取药物管理器
            var medicineManager = GlobalNodeManager.Inst.Get<AIHospital_MedicineManager>();
            // 获取可用的药物
            _curMedicineBev = medicineManager.GetEmptyMedicine();
            
            if (_curMedicineBev != null)
            {
                _curMedicineBev.IsCanClick = false;
                var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
                _character.SetPositionAndRotation(transData.pos, Quaternion.Euler(transData.rot));
                _character.PlayAnim("40100463"); // 吃药动画ID，可能需要调整
            }
        }
    }
} 
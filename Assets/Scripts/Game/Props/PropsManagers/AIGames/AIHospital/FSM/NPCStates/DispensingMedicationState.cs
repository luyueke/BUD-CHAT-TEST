using System;
using System.Collections;
using System.Collections.Generic;
using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.DispensingMedication
    public class DispensingMedicationState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private AIHospital_MedicineCabinetBehaviour _medicineCabinetBev;

        public DispensingMedicationState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[DispensingMedicationState] {_character.GetNpcName()} 进入配药状态");
            
            //1.获取当前行为的目标地点
            _hasReachedTarget = false;
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[DispensingMedicationState] {_character.GetNpcName()} 退出配药状态");
            if (_medicineCabinetBev != null)
            {
                _medicineCabinetBev.IsCanClick = true;
                _medicineCabinetBev = null;
            }
            _character._npcStateController.ExitState(PlayerState.SingleEmote);
            _character.SetHandNodeActive(true);
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("DispensingMedicationState OnReachedTarget");
            _hasReachedTarget = true;
            
            var medicineManager = GlobalNodeManager.Inst.Get<AIHospital_MedicineCabinetManager>();
            var medicineCabinetBev = medicineManager.GetCabinetBev();
            if (medicineCabinetBev != null)
            {
                medicineCabinetBev.IsCanClick = false;
                medicineCabinetBev.HandOpenDoor();
                _character.SetPositionAndRotation(medicineCabinetBev.pos, Quaternion.Euler(medicineCabinetBev.rot));
                _character.SetHandNodeActive(false);
                _character.PlayAnim("40200466");
            }
        }
    }
} 
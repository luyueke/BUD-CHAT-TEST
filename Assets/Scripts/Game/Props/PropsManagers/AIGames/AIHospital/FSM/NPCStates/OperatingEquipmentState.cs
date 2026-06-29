using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.OperatingEquipment
    public class OperatingEquipmentState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private AIHospital_EquipmentBehaviour curEquipmentBev;

        public OperatingEquipmentState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[OperateMedicalEquipmentState] {_character.GetNpcName()} 进入操作医疗设备状态");
            
            //1.获取当前行为的目标地点
            _hasReachedTarget = false;
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            
            //2.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[OperateMedicalEquipmentState] {_character.GetNpcName()} 退出操作医疗设备状态");
            
            if(curEquipmentBev != null)
            {
                curEquipmentBev.IsCanClick = true;
            }
            _character.SetHandNodeActive(true);
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("OperatingEquipmentState OnReachedTarget");
            _hasReachedTarget = true;
            var mgr = GlobalNodeManager.Inst.Get<AIHospital_EquipmentManager>();
            curEquipmentBev = mgr.GetBevs()[0];
            curEquipmentBev.IsCanClick = false;
            _character.SetPositionAndRotation(curEquipmentBev.pos, Quaternion.Euler(curEquipmentBev.rot));
            _character.SetHandNodeActive(false);
            _character.PlayAnim("40200460");
        }
    }
}

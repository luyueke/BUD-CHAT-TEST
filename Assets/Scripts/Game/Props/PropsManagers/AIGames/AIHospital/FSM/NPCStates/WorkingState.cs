using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.Working
    public class WorkingState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private AIHospital_ChairBehaviour _curChairBev;
        private AIHospital_MedicineCabinetBehaviour _medicineCabinetBev;

        public WorkingState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[WorkingState] {_character.GetNpcName()} 进入办公的状态 {_curLocationType.ToString()}");
            
            //1.获取当前行为的目标地点
            _hasReachedTarget = false;
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            var workingTransData = GetCurWorkingTransData(_curLocationType);
            
            //2.检查是否已经足够接近工作位置，是则直接触发到达目标
            float distanceThreshold = 0.5f; // 设置距离阈值，根据实际需求调整
            if (Vector3.Distance(_character.transform.position, workingTransData.pos) < distanceThreshold)
            {
                LoggerUtils.Log($"[WorkingState] {_character.GetNpcName()} 已经足够靠近工作位置，直接触发到达目标");
                OnReachedTarget();
                return;
            }
            
            //3.开始移动
            _character.MoveToPosition(transData.pos, OnReachedTarget);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[WorkingState] {_character.GetNpcName()} 退出办公的状态");

            if (_curChairBev != null)
            {
                _curChairBev.IsCanClick = true;
                _curChairBev = null;
            }

            if (_medicineCabinetBev != null)
            {
                _medicineCabinetBev.IsCanClick = true;
                _medicineCabinetBev = null;
            }
            
            var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
            _character.SetPositionAndRotation(transData.pos, Quaternion.Euler(transData.rot));
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("WorkingState OnReachedTarget");
            _hasReachedTarget = true;
            
            switch (_curLocationType)
            {
                //院长室
                case LocationType.DirectorsOffice:
                    var chairManager = GlobalNodeManager.Inst.Get<AIHospital_ChairManager>();
                    _curChairBev = chairManager.GetChairByType(AIHospital_ChairBehaviour.Hospital_ChairType.Dean);
                    if (_curChairBev != null)
                    {
                        _curChairBev.IsCanClick = false;
                        _character.SetPositionAndRotation(_curChairBev.pos, Quaternion.Euler(_curChairBev.rot));
                        _character.PlayAnim("40200464");
                    }
                    break;
                
                //药房
                case LocationType.Pharmacy:
                    var medicineManager = GlobalNodeManager.Inst.Get<AIHospital_MedicineCabinetManager>();
                    _medicineCabinetBev = medicineManager.GetCabinetBev();
                    if (_medicineCabinetBev != null)
                    {
                        _medicineCabinetBev.IsCanClick = false;
                        _character.SetPositionAndRotation(_medicineCabinetBev.pos, Quaternion.Euler(_medicineCabinetBev.rot));
                        _character.PlayAnim("40200466");
                    }
                    break;
            }
        }

        private HospitalNpcTransData GetCurWorkingTransData(LocationType locationType)
        {
            HospitalNpcTransData workingData = new HospitalNpcTransData();
            switch (_curLocationType)
            {
                //院长室
                case LocationType.DirectorsOffice:
                    var chairManager = GlobalNodeManager.Inst.Get<AIHospital_ChairManager>();
                    _curChairBev = chairManager.GetChairByType(AIHospital_ChairBehaviour.Hospital_ChairType.Dean);
                    if (_curChairBev != null)
                    {
                        workingData.pos = _curChairBev.pos;
                        workingData.rot = _curChairBev.rot;
                    }
                    break;
                
                //药房
                case LocationType.Pharmacy:
                    var medicineManager = GlobalNodeManager.Inst.Get<AIHospital_MedicineCabinetManager>();
                    _medicineCabinetBev = medicineManager.GetCabinetBev();
                    if (_medicineCabinetBev != null)
                    {
                        workingData.pos = _medicineCabinetBev.pos;
                        workingData.rot = _medicineCabinetBev.rot;
                    }
                    break;
            }

            return workingData;
        }
    }
} 
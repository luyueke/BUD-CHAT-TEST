using Game.Base;
using Game.Props.PropsBehaviours;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.Sitting
    public class SittingState : AIChapterState
    {
        private AIHospital_ChairBehaviour _curChairBev;

        private AIHospital_ChairBehaviour.Hospital_ChairType _chairType = AIHospital_ChairBehaviour.Hospital_ChairType.Default;
        
        public SittingState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        public AIHospital_ChairBehaviour.Hospital_ChairType GetChairType()
        {
            return _chairType;
        }
        
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[SittingState] {_character.GetNpcName()} 进入坐下状态");

            if (_curLocationType == LocationType.ConsultingRoom)
            {
                //1.获取当前行为的目标地点
                var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
                //2.开始移动
                _character.MoveToPosition(transData.pos, OnReachedTarget);
            }
            else
            {
                var chairManager = GlobalNodeManager.Inst.Get<AIHospital_ChairManager>();
                _curChairBev = chairManager.GetEmptyCorridorChair();
                if (_curChairBev != null)
                {
                    _curChairBev.IsCanClick = false;
                    var targetPos = new Vector3(-9.8f, 1.41f, _curChairBev.pos.z);
                    _character.MoveToPosition(targetPos, OnReachedTarget);
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[SittingState] {_character.GetNpcName()} 退出坐下状态");
            
            if (_curLocationType == LocationType.ConsultingRoom)
            {
                var transData = this._characterUtils.GetBehaviourLocation(_curLocationType, _curActionType);
                _character.SetPositionAndRotation(transData.pos, Quaternion.Euler(transData.rot));
            }
            else
            {
                if (_curChairBev != null)
                {
                    var targetPos = new Vector3(-9.76f, 1.41f, _curChairBev.pos.z);
                    _character.SetPositionAndRotation(targetPos, Quaternion.Euler(new Vector3(0,90,0)));
                }
            }

                        
            if (_curChairBev != null)
            {
                _curChairBev.IsCanClick = true;
            }
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("SittingState OnReachedTarget");
            
            // 获取椅子管理器
            var chairManager = GlobalNodeManager.Inst.Get<AIHospital_ChairManager>();
            // 根据位置类型获取对应的椅子

            _chairType = AIHospital_ChairBehaviour.Hospital_ChairType.Injections;
            switch (_curLocationType)
            {
                case LocationType.ConsultingRoom:
                    _chairType = AIHospital_ChairBehaviour.Hospital_ChairType.Injections;
                    _curChairBev = chairManager.GetChairByType(_chairType);
                    if (_curChairBev != null && _curChairBev.IsCanClick)
                    {
                        _curChairBev.IsCanClick = false;
                        _character.SetPositionAndRotation(_curChairBev.pos, Quaternion.Euler(_curChairBev.rot));
                        _character.PlayAnim("40200469"); // 坐下动画ID，可能需要调整
                        
                        var InjectNpcState = AIHospital_CharacterManager.Inst.FindNpcWithState<InjectionsToPatientsState>();
                        if (InjectNpcState != null)
                        {
                            InjectNpcState.DoInjections();
                        }
                    }
                    break;
                
                case LocationType.Corridor:
                case LocationType.WaitingRoom:
                    if (_curChairBev != null)
                    {
                        _curChairBev.IsCanClick = false;
                        _character.SetPositionAndRotation(_curChairBev.pos, Quaternion.Euler(_curChairBev.rot));
                        _character.PlayAnim("40200402"); // 坐下动画ID，可能需要调整
                    }
                    break;
            }
            
        }
    }
} 
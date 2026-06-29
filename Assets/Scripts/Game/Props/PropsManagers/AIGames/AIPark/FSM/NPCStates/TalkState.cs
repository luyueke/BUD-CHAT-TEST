using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    //ActionType.Talk
    public class TalkState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private AIPark_CharacterBehaviour _targetCharacter;
        private ParkNpcTransData _curTalkTransData;
        private string _talkTargetId;
        public TalkState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[TalkState] {_character.GetNpcName()} 进入交谈状态");
            _talkTargetId = _chapterData.talkTo;
            //1.获取当前行为的目标地点
            _curTalkTransData = TalkStateUtils.Inst.OnPlayerEnterTalkState(_curLocationType, _npcId);
            
            //2.开始移动
            _character.MoveToPosition(_curTalkTransData.pos, OnReachedTarget);
            
            _targetCharacter = AIPark_CharacterManager.Inst.GetNpc(_talkTargetId);
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[TalkState] {_character.GetNpcName()} 退出交谈状态");
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            // var idleState = new IdleState(_character, null);
            _targetCharacter = AIPark_CharacterManager.Inst.GetNpc(_talkTargetId);
            _targetCharacter?.GetCurrentState().OnExit();
        }

        public override void OnUpdate(float deltaTime)
        {
        }

        //获取npc是否到达目标点
        public bool CheckNpcIsReachedTarget()
        {
            return _hasReachedTarget;
        }

        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("TalkState OnReachedTarget");
            _hasReachedTarget = true;
            
            _character.SetPositionAndRotation(_curTalkTransData.pos, Quaternion.Euler(_curTalkTransData.rot));
            var npcState = AIPark_CharacterManager.Inst.GetNpcCurrentState(_chapterData.talkTo);
            if (npcState is TalkState)
            {
                var talkToNpcState = npcState as TalkState;
                if (talkToNpcState.CheckNpcIsReachedTarget())
                {
                    StartTaking();
                    talkToNpcState.StartTaking();
                }
            }
        }

        //开始交谈
        public void StartTaking()
        {
            LookAtTarget();
        }

        private void LookAtTarget()
        {
            if(_targetCharacter == null)
                return;
            
            // 获取玩家位置
            Vector3 playerPosition = _targetCharacter._npcKccCtr.transform.position;
            
            // 计算朝向玩家的方向（只考虑水平方向）
            Vector3 targetDirection = playerPosition - _character._npcKccCtr.transform.position;
            targetDirection.y = 0;
            targetDirection.Normalize();
            
            // 创建目标旋转（基于方向向量）
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            
            // 使用KCC设置旋转
            _character._npcKccCtr.Motor.SetRotation(targetRotation);
        }
    }
} 
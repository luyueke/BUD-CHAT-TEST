using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    //ActionType.ConductingRounds
    public class ConductingRoundsState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private List<HospitalNpcTransData> _currentCleaningPoints;
        private int _currentPointIndex = 0;
        private bool _isMovingBetweenPoints = false;
        private Coroutine _waitAndMoveCoroutine = null; // 用于追踪协程

        public ConductingRoundsState(AIHospital_CharacterBehaviour character, AIHospital_ChapterData data) : base(character, data) { }

        #region 查房巡逻点
        private List<HospitalNpcTransData> ConductingRoundPoints = new List<HospitalNpcTransData>()
        {
            new HospitalNpcTransData()
            {
                pos = new Vector3(5.185f,1.41f,-21.91f),
                rot = new Vector3(0,188,0),
            },
            new HospitalNpcTransData()
            {
                pos = new Vector3(1.423f,1.41f,-20.47f),
                rot =  new Vector3(0,-90,0),
            },
            new HospitalNpcTransData()
            {
                pos = new Vector3(3.529f,1.41f,-19.56f),
                rot =  new Vector3(0,90,0),
            },
            new HospitalNpcTransData()
            {
                pos = new Vector3(1.419f,1.41f,-17.659f),
                rot =  new Vector3(0,-90,0),
            },
        };

        #endregion
        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[ConductingRoundsState] {_character.GetNpcName()} 进入查房状态");
            
            // 开始查房循环
            _currentCleaningPoints = ConductingRoundPoints;
            _currentPointIndex = 0;
            _isMovingBetweenPoints = true;
            MoveToNextCleaningPoint();
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[ConductingRoundsState] {_character.GetNpcName()} 退出查房状态");
            CleanData();
        }

        public override void OnInterrupt()
        {
            base.OnInterrupt();
            CleanData();
        }

        public override void OnUpdate(float deltaTime)
        {
        }
        
        private void OnReachedTarget()
        {
            // LoggerUtils.LogError("ConductingRoundsState OnReachedTarget");
            _hasReachedTarget = true;
            
            // 已经到达某个查房点，播放动画并等待
            HospitalNpcTransData currentPoint = _currentCleaningPoints[_currentPointIndex];
            _character.SetPositionAndRotation(currentPoint.pos, Quaternion.Euler(currentPoint.rot));
            
            // 使用协程变量追踪协程
            if (_waitAndMoveCoroutine != null)
            {
                _character.StopCoroutine(_waitAndMoveCoroutine);
            }
            _waitAndMoveCoroutine = _character.StartCoroutine(WaitAndMoveToNextPoint());
        }

        private void CleanData()
        {
            // 安全停止协程
            if (_waitAndMoveCoroutine != null)
            {
                _character.StopCoroutine(_waitAndMoveCoroutine);
                _waitAndMoveCoroutine = null;
                LoggerUtils.Log($"[CleaningState] {_character.GetNpcName()} 停止清洁点移动协程");
            }
            
            // 重置变量
            _currentCleaningPoints = null;
            _currentPointIndex = 0;
            _isMovingBetweenPoints = false;
            _hasReachedTarget = false;
            
            _character._npcStateController.ExitState(PlayerState.SingleEmote);
        }
        
        private IEnumerator WaitAndMoveToNextPoint()
        {
            // 等待5秒
            yield return new WaitForSeconds(3.0f);
            
            // 移动到下一个查房点
            _currentPointIndex = (_currentPointIndex + 1) % _currentCleaningPoints.Count;
            MoveToNextCleaningPoint();
        }
        
        private void MoveToNextCleaningPoint()
        {
            if (_currentCleaningPoints != null && _currentCleaningPoints.Count > 0)
            {
                // 获取下一个查房点
                HospitalNpcTransData nextPoint = _currentCleaningPoints[_currentPointIndex];
                LoggerUtils.Log($"[ConductingRoundsState] {_character.GetNpcName()} 移动到第{_currentPointIndex + 1}个查房点: {nextPoint.pos}");
                
                // 移动到下一个查房点
                _character.MoveToPosition(nextPoint.pos, OnReachedTarget);
            }
        }
    }
} 
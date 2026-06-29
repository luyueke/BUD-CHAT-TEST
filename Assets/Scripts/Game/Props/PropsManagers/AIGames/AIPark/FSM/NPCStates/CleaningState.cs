using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Props.PropsManagers.AIGames.AIPark.FSM
{
    //ActionType.Cleaning
    public class CleaningState : AIChapterState
    {
        protected bool _hasReachedTarget = false;
        private List<ParkNpcTransData> _currentCleaningPoints;
        private int _currentPointIndex = 0;
        private bool _isMovingBetweenPoints = false;
        private Coroutine _waitAndMoveCoroutine = null; // 用于追踪协程

        public CleaningState(AIPark_CharacterBehaviour character, AIPark_ChapterData data) : base(character, data) { }

        #region 清洁坐标点
        private List<ParkNpcTransData> ToiletCleaningPoints = new List<ParkNpcTransData>()
        {
            new ParkNpcTransData()
            {
                pos = new Vector3(-12.73f, 1.41f, 13),
                rot =  new Vector3(0,90,0),
            },
            new ParkNpcTransData()
            {
                pos = new Vector3(-8.81f, 1.41f, 9.56f),
                rot =  new Vector3(0,0,0),
            },
            new ParkNpcTransData()
            {
                pos = new Vector3(-5.21f, 1.41f, 18.03f),
                rot =  new Vector3(0,0,0),
            },
        };
        private List<ParkNpcTransData> CorridorCleaningPoints = new List<ParkNpcTransData>()
        {
            new ParkNpcTransData()
            {
                pos = new Vector3(-4.78f,1.41f,-16.42f),
                rot =  new Vector3(0,-90,0),
            },
            new ParkNpcTransData()
            {
                pos = new Vector3(-5.631f,1.41f,-29.024f),
                rot =  new Vector3(0,0,0),
            },
            new ParkNpcTransData()
            {
                pos = new Vector3(-9.590f,1.41f,-22.645f),
                rot =  new Vector3(0,90,0),
            },
        };
        #endregion

        public override void OnEnter()
        {
            base.OnEnter();
            LoggerUtils.Log($"[CleaningState] {_character.GetNpcName()} 进入清洁状态");
            
            // 首次到达目标点，选择对应的清洁点列表
            switch (_curLocationType)
            {
                case LocationType.Corridor:
                    _currentCleaningPoints = CorridorCleaningPoints;
                    LoggerUtils.Log($"[CleaningState] {_character.GetNpcName()} 使用走廊清洁点，共{_currentCleaningPoints.Count}个点");
                    break;
                    
                case LocationType.Toilet:
                    _currentCleaningPoints = ToiletCleaningPoints;
                    LoggerUtils.Log($"[CleaningState] {_character.GetNpcName()} 使用厕所清洁点，共{_currentCleaningPoints.Count}个点");
                    break;
                    
                default:
                    // 默认使用走廊清洁点
                    _currentCleaningPoints = CorridorCleaningPoints;
                    LoggerUtils.Log($"[CleaningState] {_character.GetNpcName()} 使用默认清洁点，共{_currentCleaningPoints.Count}个点");
                    break;
            }
                
            // 开始清洁点循环
            _currentPointIndex = 0;
            _isMovingBetweenPoints = true;
            MoveToNextCleaningPoint();
        }

        public override void OnExit()
        {
            base.OnExit();
            LoggerUtils.Log($"[CleaningState] {_character.GetNpcName()} 退出清洁状态");
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
            // LoggerUtils.LogError("CleaningState OnReachedTarget");
            _hasReachedTarget = true;
            
            // 已经到达某个清洁点，播放动画并等待
            ParkNpcTransData currentPoint = _currentCleaningPoints[_currentPointIndex];
            _character.SetPositionAndRotation(currentPoint.pos, Quaternion.Euler(currentPoint.rot));
                
            // 播放清洁动画
            _character.SetHandNodeActive(false);
            _character.PlayAnim("40200467"); // 清洁动画ID
                
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

            _character.SetHandNodeActive(true);
        }
        
        private IEnumerator WaitAndMoveToNextPoint()
        {
            // 等待5秒
            yield return new WaitForSeconds(5.0f);
            // 退出状态
            _character._npcStateController.ExitState(PlayerState.SingleEmote);
            yield return new WaitForSeconds(3.0f);
            // 移动到下一个清洁点
            _currentPointIndex = (_currentPointIndex + 1) % _currentCleaningPoints.Count;
            MoveToNextCleaningPoint();
        }
        
        private void MoveToNextCleaningPoint()
        {
            _character.SetHandNodeActive(true);
            if (_currentCleaningPoints != null && _currentCleaningPoints.Count > 0)
            {
                // 获取下一个清洁点
                ParkNpcTransData nextPoint = _currentCleaningPoints[_currentPointIndex];
                LoggerUtils.Log($"[CleaningState] {_character.GetNpcName()} 移动到第{_currentPointIndex + 1}个清洁点: {nextPoint.pos}");
                
                // 移动到下一个清洁点
                _character.MoveToPosition(nextPoint.pos, OnReachedTarget);
            }
        }
    }
} 
using UnityEngine;
using System;

namespace Game.Props.PropsManagers.AIGames.AIHospital.FSM
{
    /// <summary>
    /// AI章节状态机，管理章节状态的切换、缓存和恢复
    /// </summary>
    public class AIChapterStateMachine
    {
        private AIChapterState currentState;

        /// <summary>
        /// 切换到新状态
        /// </summary>
        /// <param name="newState">新状态</param>
        public void ChangeState(AIChapterState newState)
        {
            currentState?.OnExit();
            currentState = newState;
            LoggerUtils.Log($"[AIChapterStateMachine] 切换到状态: {newState.GetType().Name}");
            currentState?.OnEnter();
        }
        
        /// <summary>
        /// 切换到新状态
        /// </summary>
        /// <param name="newState">新状态</param>
        public void InterruptState(AIChapterState newState)
        {
            currentState?.OnInterrupt();
            LoggerUtils.Log($"[AIChapterStateMachine] 打断状态: {currentState?.GetType().Name}");
            currentState = newState;
            LoggerUtils.Log($"[AIChapterStateMachine] 并且进入到新状态: {newState.GetType().Name}");
            currentState?.OnEnter();
        }

        /// <summary>
        /// 更新当前状态
        /// </summary>
        /// <param name="deltaTime">帧间隔时间</param>
        public void Update(float deltaTime)
        {
            currentState?.OnUpdate(deltaTime);
        }

        /// <summary>
        /// 检查当前状态是否为指定类型
        /// </summary>
        /// <typeparam name="T">状态类型</typeparam>
        /// <returns>是否为指定类型</returns>
        public bool CurrentStateIs<T>() where T : AIChapterState
        {
            return currentState != null && currentState.GetType() == typeof(T);
        }

        /// <summary>
        /// 获取当前状态
        /// </summary>
        public AIChapterState GetCurrentState()
        {
            return currentState;
        }
    }
} 
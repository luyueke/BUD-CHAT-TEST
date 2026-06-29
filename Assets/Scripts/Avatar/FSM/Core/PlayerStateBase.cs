using System;
using System.Collections.Generic;
using UnityEngine;

namespace FSM
{
    public abstract class PlayerStateBase : StateBase<PlayerState>
    {
        // 因某状态进入缓存池
        protected PlayerState forCacheState;

        protected PlayerStateMachine m_PlayerMachine;

        public override StateMachine<PlayerState> machine { set { m_PlayerMachine = value as PlayerStateMachine; } }

        protected PlayerStateBase(PlayerState id) : base(id)
        {
        }

        #region State LifeCycle
        /// <summary>
        /// 外部传入参数到状态
        /// </summary>
        /// <param name="args"></param>
        public virtual void InitData(params object[] args) { LoggerUtils.Log($"PlayerState InitData {stateID}"); }
        /// <summary>
        /// 进入状态
        /// </summary>
        public override void OnEnter() { LoggerUtils.Log($"PlayerState OnEnter {stateID}"); }
        /// <summary>
        /// 从缓存池中返回到状态中
        /// </summary>
        public virtual void FromCacheState() { LoggerUtils.Log($"PlayerState FromCacheState {stateID}"); }
        /// <summary>
        /// 进入主状态
        /// </summary>
        public virtual void EnterMainState() { LoggerUtils.Log($"PlayerState EnterMainState {stateID}"); }
        /// <summary>
        /// 进入状态，但不播放动画
        /// </summary>
        public virtual void DirectIntoState() { LoggerUtils.Log($"PlayerState DirectIntoState {stateID}");}
        /// <summary>
        /// 正常进入状态 - 如有进入动画播放进入动画
        /// </summary>
        public virtual void CoexistState() { LoggerUtils.Log($"PlayerState CoexistState {stateID}");}
        /// <summary>
        /// 缓存状态
        /// </summary>
        /// <param name="beState">因beState状态，此状态被缓存</param>
        public virtual void CacheState(PlayerState beState) { LoggerUtils.Log($"PlayerState CacheState {stateID}");}
        /// <summary>
        /// 中断状态
        /// </summary>
        /// <param name="beState">因beState状态，此状态被中断</param>
        public virtual void InterruptState(PlayerState beState) { LoggerUtils.Log($"PlayerState InterrupState {stateID}");}
        /// <summary>
        /// 忽略状态，弹窗提示
        /// </summary>
        /// <param name="beState">因beState状态此状态被忽略</param>
        public virtual void IgnoreState(PlayerState beState) {
            // string content = StatePromptManager.Inst.GetPromptContent(stateID, beState);
            //
            // if (string.IsNullOrEmpty(content))
            // {
            //     return;
            // }

            //TipPanel.ShowToast(content);
        }
        public virtual void PlayExitAnimation(Action<PlayerStateBase> onComplete) { onComplete?.Invoke(this); }
        /// <summary>
        /// 退出主状态
        /// </summary>
        public virtual void ExitMainState() { LoggerUtils.Log($"PlayerState ExitMainState {stateID}"); }
        /// <summary>
        /// 退出状态
        /// </summary>
        public override void OnExit() { LoggerUtils.Log($"PlayerState OnExit {stateID}"); }
        /// <summary>
        /// 释放外部传入的参数
        /// </summary>
        public virtual void ReleaseData() { LoggerUtils.Log($"PlayerState ReleaseData {stateID}"); }
        #endregion

        #region State Update
        public virtual void MainStateUpdate() { }

        public virtual void SubStateUpdate() { }

        public virtual void MainStateFixedUpdate() { }
        public virtual void MainStateLateUpdate() { }
        public virtual void SubStateLateUpdate() { }

        public virtual void SubStateFixedUpdate() { }
        #endregion

        /// <summary>
        /// 忽略重复进入当前状态
        /// </summary>
        public virtual bool RepeatEntry(params object[] args) { return true; /*重复进入弹窗 - 可配置到状态中*/ }

        public virtual bool IsAutoUpdateAnimClips => true;

        public PlayerState ForCacheState => forCacheState;

        public bool IsReconnect = false;

        /// <summary>
        /// 进入缓存时绑定状态
        /// </summary>
        /// <param name="state"></param>
        public void BindForCacheState(PlayerState stateID)
        {
            LoggerUtils.Log($"PlayerState BindForCacheState {this.stateID} : {stateID}");
            forCacheState = stateID;
            m_PlayerMachine.AddOnStateChangeListener(CheckForCacheStateExist);
        }

        /// <summary>
        /// 从缓存池中回到当前状态时去掉绑定关系
        /// </summary>
        public void UnBindForCacheState()
        {
            LoggerUtils.Log($"PlayerState UnBindForCacheState {stateID}");
            forCacheState = PlayerState.None;
            m_PlayerMachine.RemoveOnStateChangeListener(CheckForCacheStateExist);
        }

        /// <summary>
        /// 检查绑定的状态是否存在
        /// </summary>
        private void CheckForCacheStateExist(List<ExcludeStateAction> excludeActionList)
        {
            if (!m_PlayerMachine.ContainsCurrentState(forCacheState))
            {
                UnBindForCacheState();

                m_PlayerMachine.CacheTryEnterCurState(this, excludeActionList);
            }
        }
    }
}
using Message;
using Newtonsoft.Json;
using Pb.Base;
using Pb.Game;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FSM
{
    public class PlayerStateMachine : StateMachine<PlayerState>
    {
        #region params
        // 存储所有状态
        private Dictionary<PlayerState, PlayerStateBase> m_AllState;

        // 处理当前持有状态互斥
        private StateExcludeHandler.ExcludeTypeHandler excludeTypeHanlder;
        // 处理缓存池状态互斥
        private StateExcludeHandler.ExcludeTypeHandler fromCacheExcludeTypeHanlder;

        // 当前持有状态有变更时调用
        Action<List<ExcludeStateAction>> OnStateListChange;

        public Action<PlayerState> EnterMainState { private get; set; }
        //Action<AnimationClipList> UpdateAnimClipsAction;

        // 当前处于的状态
        private StateLinkList m_CurrentStateList;
        // 当前缓存中的状态
        private StateLinkList m_CacheStateList;

        private Dictionary<PlayerState, object[]> reconnectIntoStateDic;
        private Dictionary<StateExcludeType, Action<ExcludeStateAction>> excludeTypeActionDic;

        private bool isSelfMachine;
        private PlayerStateBase playingExitAniState;
        #endregion

        #region StateMachine
        protected override void Init()
        {
            m_AllState = new Dictionary<PlayerState, PlayerStateBase>();
            m_CurrentStateList = new PlayerStateLinkList();
            m_CacheStateList = new PlayerStateCacheLinkList();
            reconnectIntoStateDic = new Dictionary<PlayerState, object[]>();

            RegisterExcludeHandler();
            InitExcludeTypeAction();
        }

        /// <summary>
        /// 状态机初始化
        /// </summary>
        /// <param name="beginState"></param>
        public PlayerStateMachine(PlayerStateBase beginState, bool isSelfMachine) : base(beginState)
        {
            this.isSelfMachine = isSelfMachine;

            // 把状态添加到集合中
            AddState(beginState);

            m_CurrentStateList.AddFirst(beginState);

            beginState.OnEnter();
            beginState.EnterMainState();
        }

        /// <summary>
        /// 添加状态
        /// </summary>
        /// <param name="state"></param>
        public override void AddState(StateBase<PlayerState> state)
        {
            if (!m_AllState.ContainsKey(state.stateID))
            {
                m_AllState.Add(state.stateID, state as PlayerStateBase);
                state.machine = this;
            }
        }

        private bool IsCanSendToServer(PlayerState stateID)
        {
            return stateID == PlayerState.CameraMode
                   || stateID == PlayerState.SingleEmote
                   || stateID == PlayerState.DoubleEmote
                   || stateID == PlayerState.UgcEmote
                   || stateID == PlayerState.UgcDoubleEmote
                   || stateID == PlayerState.MusicInstrumentPlay
                   || stateID == PlayerState.LinkEmoteStart
                   || stateID == PlayerState.LinkEmote
            ;
        }

        /// <summary>
        /// 进入状态
        /// </summary>
        /// <param name="stateID"></param>
        /// <param name="args"></param>
        public override void EnterState(PlayerState stateID, bool isDirectIntoState, params object[] args)
        {
            PlayerStateBase state;

            if (!m_AllState.TryGetValue(stateID, out state)) return;

            if (playingExitAniState != null)
            {
                PlayExitAimationComplete(playingExitAniState);
            }

            if (ContainsAllState(stateID) && state.RepeatEntry(args)) return;

            PlayerState beState;
            if (!StateExcludeManager.Inst.CanEnterState(stateID, CurStateIDs, out beState))
            {
                state.IgnoreState(beState);
                return;
            }

            var mainState = m_CurrentStateList.First;

            var excludeTypeDic = StateExcludeManager.Inst.GetEnterStateExcludeType(state.stateID, CurStateIDs);
            List<ExcludeStateAction> excludeActionList = new List<ExcludeStateAction>();
            excludeTypeHanlder.HandlerExcludeType(state, excludeTypeDic, excludeActionList);

            List<ExcludeStateAction> fromCacheActionList = new List<ExcludeStateAction>();
            OnStateListChange?.Invoke(fromCacheActionList);
            excludeActionList.AddRange(fromCacheActionList);

            if (state.IsAutoUpdateAnimClips)
                UpdateAnimClips();

            if (IsCanSendToServer(stateID))
            {
                SendStateToServer();
            }

            bool isChangeMainState = !IsMainState(mainState.Value.stateID);

            var exitActionList = excludeActionList.FindAll(excludeState => excludeState.actionType == StateExcludeType.CacheState || excludeState.actionType == StateExcludeType.Interrupt);

            if (exitActionList != null)
            {
                foreach (var excludeStateAction in exitActionList)
                {
                    excludeTypeActionDic[excludeStateAction.actionType]?.Invoke(excludeStateAction);
                }
            }

            if (isChangeMainState)
            {
                mainState.Value.ExitMainState();
            }

            if (exitActionList != null)
            {
                foreach (var excludeStateAction in exitActionList)
                {
                    m_AllState[excludeStateAction.stateID].OnExit();
                    if (excludeStateAction.actionType == StateExcludeType.Interrupt)
                    {
                        m_AllState[excludeStateAction.stateID].ReleaseData();
                    }
                }
            }

            var enterActionList = excludeActionList.FindAll(excludeState => excludeState.actionType == StateExcludeType.DirectIntoState || excludeState.actionType == StateExcludeType.Coexist);

            state.InitData(args);

            if (enterActionList != null)
            {
                foreach (var excludeStateAction in enterActionList)
                {
                    m_AllState[excludeStateAction.stateID].OnEnter();
                }
            }

            foreach (var excludeStateAction in fromCacheActionList)
            {
                FromCacheState(excludeStateAction);
            }

            if (isChangeMainState)
            {
                EnterMainState?.Invoke(m_CurrentStateList.First.Value.stateID);
                m_CurrentStateList.First.Value.EnterMainState();
            }

            if (enterActionList != null)
            {
                foreach (var excludeStateAction in enterActionList)
                {
                    if (isDirectIntoState && stateID == excludeStateAction.stateID)
                    {
                        excludeStateAction.actionType = StateExcludeType.DirectIntoState;
                    }

                    excludeTypeActionDic[excludeStateAction.actionType]?.Invoke(excludeStateAction);
                }
            }

            fromCacheActionList.Clear();
            fromCacheActionList = null;
            excludeActionList.Clear();
            excludeActionList = null;
        }

        /// <summary>
        /// 断线重连直接进入状态
        /// </summary>
        /// <param name="stateID"></param>
        /// <param name="args"></param>
        public override void ReconnectIntoState(PlayerState stateID, params object[] args)
        {
            if (!reconnectIntoStateDic.ContainsKey(stateID))
            {
                reconnectIntoStateDic.Add(stateID, args);
            }
            else
            {
                reconnectIntoStateDic[stateID] = args;
            }
        }

        /// <summary>
        /// 结束状态
        /// </summary>
        /// <param name="stateID"></param>
        public override void ExitState(PlayerState stateID, bool isPlayExitAni)
        {
            PlayerStateBase state;

            if (!m_CurrentStateList.TryGetState(stateID, out state))
            {
                ExitCacheState(stateID);
                return;
            }

            if (playingExitAniState != null)
            {
                if (playingExitAniState.stateID == stateID)
                {
                    return;
                }

                PlayExitAimationComplete(playingExitAniState);
                // return;//注释掉该return,不然在播放退出动画期间收到下一个退出，会被忽略掉，导致状态错误
            }

            if (isPlayExitAni)
            {
                playingExitAniState = state;
                state.PlayExitAnimation(PlayExitAimationComplete);
            }
            else
            {
                PlayExitAimationComplete(state);
            }
        }

        /// <summary>
        /// 退出状态动画结束，人物真正退出状态
        /// </summary>
        /// <param name="state"></param>
        private void PlayExitAimationComplete(PlayerStateBase state)
        {
            playingExitAniState = null;

            if (!ContainsCurrentState(state.stateID)) return;

            var mainState = m_CurrentStateList.First;

            m_CurrentStateList.Remove(state);

            List<ExcludeStateAction> fromCacheActionList = new List<ExcludeStateAction>();
            OnStateListChange?.Invoke(fromCacheActionList);

            if (IsCanSendToServer(state.stateID))
            {
                SendStateToServer();
            }

            bool isChangeMainState = !IsMainState(mainState.Value.stateID);

            try
            {
                if (isChangeMainState)
                {
                    mainState.Value.ExitMainState();
                }

                state.OnExit();
                state.ReleaseData();
            }
            catch (Exception ex)
            {
                Debug.LogError("PlayerState PlayExitAimationComplete Exit State - msg : " + ex.StackTrace + "," + ex.Message);
            }

            if (fromCacheActionList.Count > 0)
            {
                foreach (var excludeStateAction in fromCacheActionList)
                {
                    try
                    {
                        m_AllState[excludeStateAction.stateID].OnEnter();
                        FromCacheState(excludeStateAction);

                        if (m_CurrentStateList.First.Value.stateID == excludeStateAction.stateID)
                        {
                            isChangeMainState = false;
                            EnterMainState?.Invoke(m_CurrentStateList.First.Value.stateID);
                            m_CurrentStateList.First.Value.EnterMainState();
                        }

                        excludeTypeActionDic[excludeStateAction.actionType]?.Invoke(excludeStateAction);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("PlayerState PlayExitAimationComplete FromCache - msg : " + ex.Message);
                        PlayExitAimationComplete(m_AllState[excludeStateAction.stateID]);
                    }
                }
            }

            if (isChangeMainState)
            {
                EnterMainState?.Invoke(m_CurrentStateList.First.Value.stateID);
                m_CurrentStateList.First.Value.EnterMainState();
            }

            if (m_CurrentStateList.First.Value.IsAutoUpdateAnimClips)
            {
                UpdateAnimClips();
            }

            fromCacheActionList.Clear();
            fromCacheActionList = null;
        }

        /// <summary>
        /// 从缓存池中退出状态
        /// </summary>
        /// <param name="stateID"></param>
        private void ExitCacheState(PlayerState stateID)
        {
            PlayerStateBase state;

            if (!m_CacheStateList.TryGetState(stateID, out state)) return;

            state.UnBindForCacheState();
            state.ReleaseData();
            m_CacheStateList.Remove(state);
        }
        #endregion

        #region Update
        public override void Update()
        {
            var curState = m_CurrentStateList.First;
            if (curState == null)
            {
                return;
            }

            if (curState.Value != null)
            {
                curState.Value.OnUpdate();
                curState.Value.MainStateUpdate();
            }

            curState = curState.Next;
            while (curState != null)
            {
                curState.Value.OnUpdate();
                curState.Value.SubStateUpdate();
                curState = curState.Next;
            }
        }

        public override void FixedUpdate()
        {
            var curState = m_CurrentStateList.First;
            if (curState == null)
            {
                return;
            }
            if (curState.Value != null)
            {
                curState.Value.OnFixedUpdate();
                curState.Value.MainStateFixedUpdate();
            }
            curState = curState.Next;
            while (curState != null && curState.Value != null)
            {
                curState.Value.OnFixedUpdate();
                curState.Value.SubStateFixedUpdate();
                curState = curState.Next;
            }
        }

        public override void LateUpdate()
        {
            var curState = m_CurrentStateList.First;
            if (curState == null)
            {
                return;
            }

            if (curState.Value != null)
            {
                curState.Value.OnLateUpdate();
                curState.Value.MainStateLateUpdate();
            }

            curState = curState.Next;
            while (curState != null)
            {
                curState.Value.OnLateUpdate();
                curState.Value.SubStateLateUpdate();
                curState = curState.Next;
            }
        }
        #endregion

        #region private 
        /// <summary>
        /// 注册互斥关系处理器
        /// </summary>
        private void RegisterExcludeHandler()
        {
            excludeTypeHanlder = new StateExcludeHandler.IgnoreHandler(StateExcludeType.Ignore, m_CurrentStateList, m_CacheStateList);
            var interruptHanlder = new StateExcludeHandler.InterruptHandler(StateExcludeType.Interrupt, m_CurrentStateList, m_CacheStateList);
            var cacheStateHanlder = new StateExcludeHandler.CacheStateHandler(StateExcludeType.CacheState, m_CurrentStateList, m_CacheStateList);
            var coexistHanlder = new StateExcludeHandler.CoexistHandler(StateExcludeType.Coexist, m_CurrentStateList, m_CacheStateList);
            var directIntoStateHanlder = new StateExcludeHandler.DirectIntoStateHandler(StateExcludeType.DirectIntoState, m_CurrentStateList, m_CacheStateList);

            excludeTypeHanlder.SetNextExcludeType(interruptHanlder);
            interruptHanlder.SetNextExcludeType(cacheStateHanlder);
            cacheStateHanlder.SetNextExcludeType(coexistHanlder);
            coexistHanlder.SetNextExcludeType(directIntoStateHanlder);

            fromCacheExcludeTypeHanlder = new StateExcludeHandler.FromCacheToCacheStateHandler(StateExcludeType.CacheState, m_CurrentStateList, m_CacheStateList);
            fromCacheExcludeTypeHanlder.SetNextExcludeType(coexistHanlder);
        }

        private void InitExcludeTypeAction()
        {
            excludeTypeActionDic = new Dictionary<StateExcludeType, Action<ExcludeStateAction>>();
            excludeTypeActionDic.Add(StateExcludeType.DirectIntoState, DirectIntoState);
            excludeTypeActionDic.Add(StateExcludeType.Coexist, CoexistState);
            excludeTypeActionDic.Add(StateExcludeType.CacheState, CacheState);
            excludeTypeActionDic.Add(StateExcludeType.Interrupt, InterruptState);
        }

        private void DirectIntoState(ExcludeStateAction args)
        {
            PlayerStateBase state;

            if (!m_AllState.TryGetValue(args.stateID, out state)) return;

            state.DirectIntoState();
        }

        private void CoexistState(ExcludeStateAction args)
        {
            PlayerStateBase state;

            if (!m_AllState.TryGetValue(args.stateID, out state)) return;

            state.CoexistState();
        }

        private void CacheState(ExcludeStateAction args)
        {
            PlayerStateBase state;

            if (!m_AllState.TryGetValue(args.stateID, out state)) return;

            state.CacheState(args.beState);
        }

        private void InterruptState(ExcludeStateAction args)
        {
            PlayerStateBase state;

            if (!m_AllState.TryGetValue(args.stateID, out state)) return;

            state.InterruptState(args.beState);
        }

        private void FromCacheState(ExcludeStateAction args)
        {
            PlayerStateBase state;

            if (!m_AllState.TryGetValue(args.stateID, out state)) return;

            state.FromCacheState();
        }
        #endregion

        #region pulbic
        public PlayerState[] CurStateIDs => m_CurrentStateList.StateIDs;

        /// <summary>
        /// 是否能进入当前状态
        /// </summary>
        /// <param name="stateID"></param>
        /// <returns></returns>
        public bool CanEnterState(PlayerState stateID)
        {
            PlayerStateBase state;

            if (!m_AllState.TryGetValue(stateID, out state)) return false;

            if (ContainsAllState(stateID) && state.RepeatEntry())
            {
                return false;
            }

            PlayerState beState;
            bool isEnter = StateExcludeManager.Inst.CanEnterState(stateID, CurStateIDs, out beState);

            if (isEnter == false)
            {
                state.IgnoreState(beState);
            }

            foreach (var curState in CurStateIDs)
            {
                // 不被中断或缓存
                if (StateExcludeManager.Inst.GetStateType(curState).isNotInterrupt)
                {
                    return false;
                }
            }

            return isEnter;
        }

        public void ResetToDefaultState()
        {
            foreach (var state in m_CurrentStateList)
            {
                state.InterruptState(PlayerState.None);
            }

            ClearAllState();

            PlayerStateBase defaultState = m_AllState[PlayerState.Default];

            m_CurrentStateList.AddFirst(defaultState);

            defaultState.OnEnter();
            defaultState.EnterMainState();

            UpdateAnimClips();
        }

        /// <summary>
        /// 当前生效状态中是否包含
        /// </summary>
        /// <param name="stateID"></param>
        /// <returns></returns>
        public bool ContainsCurrentState(PlayerState stateID)
        {
            return m_CurrentStateList.ContainsState(stateID);
        }

        /// <summary>
        /// 当前生效和缓存池中是否包含
        /// </summary>
        /// <param name="stateID"></param>
        /// <returns></returns>
        public bool ContainsAllState(PlayerState stateID)
        {
            return m_CurrentStateList.ContainsState(stateID) || m_CacheStateList.ContainsState(stateID);
        }

        /// <summary>
        /// 是否主状态
        /// </summary>
        /// <param name="stateID"></param>
        /// <returns></returns>
        public bool IsMainState(PlayerState stateID)
        {
            return m_CurrentStateList.First.Value.stateID.Equals(stateID);
        }


        public PlayerState GetMainState()
        {
            return m_CurrentStateList.First.Value.stateID;
        }

        public int GetCrrrentStateCount()
        {
            return m_CurrentStateList.Count;
        }

        /// <summary>
        /// 清空所有状态
        /// </summary>
        public void ClearAllState()
        {
            foreach (var state in m_CacheStateList)
            {
                RemoveCacheState(state);
            }
            m_CacheStateList.Clear();

            m_CurrentStateList.First.Value.ExitMainState();
            foreach (var state in m_CurrentStateList)
            {
                RemoveCurState(state);
            }
            m_CurrentStateList.Clear();
        }

        public void AddOnStateChangeListener(Action<List<ExcludeStateAction>> onChage)
        {
            OnStateListChange += onChage;
        }

        public void RemoveOnStateChangeListener(Action<List<ExcludeStateAction>> onChage)
        {
            OnStateListChange -= onChage;
        }

        /// <summary>
        /// 从缓存池尝试进入当前状态
        /// </summary>
        /// <param name="state"></param>
        public void CacheTryEnterCurState(PlayerStateBase state, List<ExcludeStateAction> excludeActionList)
        {
            var excludeTypeDic = StateExcludeManager.Inst.GetEnterStateExcludeType(state.stateID, CurStateIDs);
            fromCacheExcludeTypeHanlder.HandlerExcludeType(state, excludeTypeDic, excludeActionList);
        }

        /// <summary>
        /// 替换动画片段
        /// </summary>
        public void UpdateAnimClips()
        {
            //if (m_CurrentStateList.Count == 0)
            //{
            //    UpdateAnimClipsAction?.Invoke(null);
            //    return;
            //}

            //AnimationClipList animationClipList = StateExcludeManager.Inst.GetOverrideClipList(m_CurrentStateList.First.Value.stateID, m_CurrentStateList.StateIDs);

            //UpdateAnimClipsAction?.Invoke(animationClipList);
        }

        //public void SetUpdateAnimClipsAction(Action<AnimationClipList> UpdateAnimClipsAction)
        //{
        //    this.UpdateAnimClipsAction = UpdateAnimClipsAction;
        //}

        /// <summary>
        /// 断线重连
        /// </summary>
        /// <param name="playerID"></param>
        public void RegisterSyncStateCallback(string playerID)
        {
            StateEventManager.Inst.RegisterStateEvent<List<int>, List<CacheStateData>>(playerID, StateEvent.SyncStatus, ReceiveSyncState);
        }

        /// <summary>
        /// 释放状态机
        /// </summary>
        public void ReleaseStateMachine()
        {
            ClearAllState();

            m_CacheStateList = null;
            m_CurrentStateList = null;
            m_AllState.Clear();
            m_AllState = null;
        }

        #endregion

        #region SyncState

        private void SendStateToServer()
        {
            if (!isSelfMachine)
            {
                return;
            }

            if (IsChangeState())
            {
                SendState();
            }
        }

        private bool IsChangeState()
        {
            return true;
        }

        private void SendState()
        {
            List<CacheStateData> cacheStateList = new List<CacheStateData>();
            foreach (var state in m_CacheStateList)
            {
                cacheStateList.Add(new CacheStateData() { CacheStateId = (int)state.stateID, BindStateId = (int)state.ForCacheState });
            }

            MessageHelper.Broadcast(MessageName.SyncPlayerStatus, m_CurrentStateList.StateList, cacheStateList);
        }

        private void ReceiveSyncState(List<int> curStateList, List<CacheStateData> cacheStateList)
        {
            if (curStateList == null || curStateList.Count == 0)
            {
                curStateList = new List<int>();
                curStateList.Add(1);
            }

            if (IsNeedSync(curStateList, cacheStateList))
            {
                ClearAllState();

                if (cacheStateList != null)
                {
                    foreach (var cacheStateData in cacheStateList)
                    {
                        PlayerStateBase state;

                        if (!m_AllState.TryGetValue((PlayerState)cacheStateData.CacheStateId, out state)) continue;

                        AddCacheState(state, (PlayerState)cacheStateData.BindStateId);
                    }
                }

                foreach (var stateID in curStateList)
                {
                    PlayerStateBase state;

                    if (!m_AllState.TryGetValue((PlayerState)stateID, out state)) continue;

                    try
                    {
                        AddToCurState(state);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"PlayerState ReceiveSyncState AddToCurState - msg : {ex.Message} --" + ex.StackTrace);
                        PlayExitAimationComplete(state);
                    }
                }
            }

            reconnectIntoStateDic.Clear();
            //UpdateAnimClips();
        }

        private void AddToCurState(PlayerStateBase state)
        {
            m_CurrentStateList.AddLast(state);

            object[] args = null;
            reconnectIntoStateDic.TryGetValue(state.stateID, out args);
            state.IsReconnect = true;
            if (args != null)
            {
                state.InitData(args);
            }
            
            state.OnEnter();
            if (m_CurrentStateList.Count == 1)
            {
                EnterMainState?.Invoke(state.stateID);
                state.EnterMainState();
                state.CoexistState();
            }
            else
            {
                state.DirectIntoState();
            }
        }

        private void RemoveCurState(PlayerStateBase state)
        {
            state.OnExit();
            state.ReleaseData();
        }

        private void AddCacheState(PlayerStateBase state, PlayerState bindStateID)
        {
            m_CacheStateList.AddLast(state);

            state.BindForCacheState(bindStateID);

            object[] args = null;
            reconnectIntoStateDic.TryGetValue(state.stateID, out args);
            if (args != null)
            {
                state.InitData(args);
            }
        }

        private void RemoveCacheState(PlayerStateBase state)
        {
            state.UnBindForCacheState();
            state.ReleaseData();
        }

        private bool IsNeedSync(List<int> curStateList, List<CacheStateData> cacheStateList)
        {
            if (curStateList.Count != m_CurrentStateList.Count) return true;

            var curState = m_CurrentStateList.First;
            for (int i = 0; i < curStateList.Count; i++)
            {
                if ((int)curState.Value.stateID != curStateList[i])
                {
                    return true;
                }

                curState = curState.Next;
            }

            if (cacheStateList == null) return false;

            if (cacheStateList.Count != m_CacheStateList.Count) return true;

            var curCacheState = m_CacheStateList.First;
            for (int i = 0; i < cacheStateList.Count; i++)
            {
                if ((int)curCacheState.Value.stateID != cacheStateList[i].CacheStateId)
                {
                    return true;
                }

                curCacheState = curCacheState.Next;
            }

            return false;
        }

        #endregion

    }
}
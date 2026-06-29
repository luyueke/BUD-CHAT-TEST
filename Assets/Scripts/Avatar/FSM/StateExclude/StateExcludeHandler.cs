using FSM;
using System.Collections.Generic;
using System.Linq;

public class StateExcludeHandler
{
    public interface IExcludeTypeHandler
    {
        /// <summary>
        /// 设置下一个互斥类型
        /// </summary>
        /// <param name="nextHandler"></param>
        void SetNextExcludeType(IExcludeTypeHandler nextHandler);
        /// <summary>
        /// 处理互斥关系
        /// </summary>
        /// <param name="enterState"></param>
        /// <param name="excludeTypeDic"></param>
        void HandlerExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList);
    }

    public abstract class ExcludeTypeHandler : IExcludeTypeHandler
    {
        // 下一个互斥处理器
        protected IExcludeTypeHandler nextHandler;
        // 互斥类型
        protected StateExcludeType stateExcludeType;

        // 当前处于的状态
        protected StateLinkList m_CurrentStateList;
        // 当前缓存中的状态
        protected StateLinkList m_CacheStateList;

        public ExcludeTypeHandler(StateExcludeType stateExcludeType, StateLinkList m_CurrentStateList, StateLinkList m_CacheStateList)
        {
            this.stateExcludeType = stateExcludeType;
            this.m_CurrentStateList = m_CurrentStateList;
            this.m_CacheStateList = m_CacheStateList;
        }

        public virtual void SetNextExcludeType(IExcludeTypeHandler nextHandler)
        {
            this.nextHandler = nextHandler;
        }

        public virtual void HandlerExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList)
        {
            if (ExecuteExcludeType(enterState, excludeTypeDic, excludeActionList))
            {
                if (nextHandler != null)
                {
                    nextHandler.HandlerExcludeType(enterState, excludeTypeDic, excludeActionList);
                }
            }
        }

        /// <summary>
        /// 执行互斥
        /// </summary>
        /// <returns>是否进入下一个类型处理器</returns>
        protected abstract bool ExecuteExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList);
    }

    /// <summary>
    /// 不打断状态 - 忽略新状态 🚫
    /// </summary>
    public class IgnoreHandler : ExcludeTypeHandler
    {
        public IgnoreHandler(StateExcludeType stateExcludeType, StateLinkList m_CurrentStateList, StateLinkList m_CacheStateList) : base(stateExcludeType, m_CurrentStateList, m_CacheStateList)
        {
        }

        protected override bool ExecuteExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList)
        {
            if (excludeTypeDic == null || excludeTypeDic.Count == 0 || m_CurrentStateList.Count == 0)
            {
                m_CurrentStateList.AddFirst(enterState);
                excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.Coexist, stateID = enterState.stateID });

                return false;
            }

            foreach (var state in m_CurrentStateList)
            {
                if (excludeTypeDic.ContainsKey(state.stateID) && excludeTypeDic[state.stateID] == stateExcludeType)
                {
                    enterState.IgnoreState(state.stateID);

                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 中断当前状态, 进入新状态 ❌
    /// </summary>
    public class InterruptHandler : ExcludeTypeHandler
    {
        public InterruptHandler(StateExcludeType stateExcludeType, StateLinkList m_CurrentStateList, StateLinkList m_CacheStateList) : base(stateExcludeType, m_CurrentStateList, m_CacheStateList)
        {
        }

        protected override bool ExecuteExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList)
        {
            if (excludeTypeDic.Count == 0 || m_CurrentStateList.Count == 0)
            {
                m_CurrentStateList.AddFirst(enterState);
                excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.Coexist, stateID = enterState.stateID });

                return false;
            }

            //var state = m_CurrentStateList.First;
            //PlayerState[] playerStateArray = excludeTypeDic.Keys.ToArray();
            //for (int i = 0; i < playerStateArray.Length; i++)
            //{
            //    PlayerState stateID = playerStateArray[i];

            //    if (excludeTypeDic[stateID] == stateExcludeType)
            //    {
            //        var interruptState = state;
            //        excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.Interrupt, stateID = interruptState.Value.stateID, beState = enterState.stateID });

            //        state = interruptState.Next;
            //        m_CurrentStateList.Remove(interruptState.Value);

            //        excludeTypeDic.Remove(stateID);
            //    }
            //    else
            //    {
            //        state = state.Next;
            //    }
            //}

            PlayerState[] playerStateArray = excludeTypeDic.Keys.ToArray();
            for (int i = 0; i < playerStateArray.Length; i++)
            {
                PlayerState stateID = playerStateArray[i];

                if (excludeTypeDic[stateID] == stateExcludeType)
                {
                    m_CurrentStateList.TryGetState(stateID, out var interruptState);
                    excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.Interrupt, stateID = interruptState.stateID, beState = enterState.stateID });

                    m_CurrentStateList.Remove(interruptState);

                    excludeTypeDic.Remove(stateID);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 缓存当前状态，进入新状态 ❌*
    /// </summary>
    public class CacheStateHandler : ExcludeTypeHandler
    {
        public CacheStateHandler(StateExcludeType stateExcludeType, StateLinkList m_CurrentStateList, StateLinkList m_CacheStateList) : base(stateExcludeType, m_CurrentStateList, m_CacheStateList)
        {
        }

        protected override bool ExecuteExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList)
        {
            if (excludeTypeDic.Count == 0 || m_CurrentStateList.Count == 0)
            {
                m_CurrentStateList.AddFirst(enterState);
                excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.Coexist, stateID = enterState.stateID });

                return false;
            }

            //var state = m_CurrentStateList.First;
            //PlayerState[] playerStateArray = excludeTypeDic.Keys.ToArray();
            //for (int i = 0; i < playerStateArray.Length; i++)
            //{
            //    PlayerState stateID = playerStateArray[i];

            //    if (excludeTypeDic[stateID] == stateExcludeType)
            //    {
            //        var cacheState = state;

            //        // 存入缓存池
            //        PlayerState[] curCacheStateIDs = m_CacheStateList.StateIDs;
            //        var cacheExcludeTypeDic = StateExcludeManager.Inst.GetEnterStateExcludeType(cacheState.Value.stateID, curCacheStateIDs);
            //        m_CacheStateList.InsertState(cacheState.Value, cacheExcludeTypeDic != null ? cacheExcludeTypeDic.Values.ToArray() : null);

            //        excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.CacheState, stateID = cacheState.Value.stateID, beState = enterState.stateID });

            //        state = cacheState.Next;
            //        m_CurrentStateList.Remove(cacheState.Value);

            //        cacheState.Value.BindForCacheState(enterState.stateID);

            //        excludeTypeDic.Remove(stateID);
            //    }
            //    else
            //    {
            //        state = state.Next;
            //    }
            //}

            PlayerState[] playerStateArray = excludeTypeDic.Keys.ToArray();
            for (int i = 0; i < playerStateArray.Length; i++)
            {
                PlayerState stateID = playerStateArray[i];

                if (excludeTypeDic[stateID] == stateExcludeType)
                {
                    m_CurrentStateList.TryGetState(stateID, out var cacheState);

                    // 存入缓存池
                    PlayerState[] curCacheStateIDs = m_CacheStateList.StateIDs;
                    var cacheExcludeTypeDic = StateExcludeManager.Inst.GetEnterStateExcludeType(cacheState.stateID, curCacheStateIDs);
                    m_CacheStateList.InsertState(cacheState, cacheExcludeTypeDic != null ? cacheExcludeTypeDic.Values.ToArray() : null);

                    excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.CacheState, stateID = cacheState.stateID, beState = enterState.stateID });

                    m_CurrentStateList.Remove(cacheState);

                    cacheState.BindForCacheState(enterState.stateID);

                    excludeTypeDic.Remove(stateID);
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 不打断状态 - 但打断动画，播放新状态的动画 ✅2
    /// </summary>
    public class CoexistHandler : ExcludeTypeHandler
    {
        public CoexistHandler(StateExcludeType stateExcludeType, StateLinkList m_CurrentStateList, StateLinkList m_CacheStateList) : base(stateExcludeType, m_CurrentStateList, m_CacheStateList)
        {
        }

        protected override bool ExecuteExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList)
        {
            if (excludeTypeDic.Count == 0 || m_CurrentStateList.Count == 0)
            {
                m_CurrentStateList.AddFirst(enterState);
                excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.Coexist, stateID = enterState.stateID });

                return false;
            }

            m_CurrentStateList.InsertState(enterState, excludeTypeDic.Values.ToArray());

            if (m_CurrentStateList.First.Value.Equals(enterState))
            {
                excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.Coexist, stateID = enterState.stateID });

                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// 不打断状态 - 不打断动画 - 如有道具，直接加载道具 ✅1
    /// </summary>
    public class DirectIntoStateHandler : ExcludeTypeHandler
    {
        public DirectIntoStateHandler(StateExcludeType stateExcludeType, StateLinkList m_CurrentStateList, StateLinkList m_CacheStateList) : base(stateExcludeType, m_CurrentStateList, m_CacheStateList)
        {
        }

        protected override bool ExecuteExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList)
        {
            excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.DirectIntoState, stateID = enterState.stateID });

            return false;
        }
    }

    /// <summary>
    /// 从缓存池回到当前状态 - 🚫❌❌* 这三种状态回不了当前状态中
    /// </summary>
    public class FromCacheToCacheStateHandler : ExcludeTypeHandler
    {
        public FromCacheToCacheStateHandler(StateExcludeType stateExcludeType, StateLinkList m_CurrentStateList, StateLinkList m_CacheStateList) : base(stateExcludeType, m_CurrentStateList, m_CacheStateList)
        {
        }

        protected override bool ExecuteExcludeType(PlayerStateBase enterState, Dictionary<PlayerState, StateExcludeType> excludeTypeDic, List<ExcludeStateAction> excludeActionList)
        {
            if (excludeTypeDic == null || excludeTypeDic.Count == 0 || m_CurrentStateList.Count == 0)
            {
                m_CacheStateList.Remove(enterState);
                m_CurrentStateList.AddFirst(enterState);
                excludeActionList.Add(new ExcludeStateAction() { actionType = StateExcludeType.DirectIntoState, stateID = enterState.stateID });

                return false;
            }

            foreach (var excludeType in excludeTypeDic)
            {
                if (excludeType.Value > StateExcludeType.Coexist)
                {
                    enterState.BindForCacheState(excludeType.Key);

                    return false;
                }
            }

            m_CacheStateList.Remove(enterState);

            return true;
        }
    }
}

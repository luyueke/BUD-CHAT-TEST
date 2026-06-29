using System.Collections.Generic;

namespace FSM
{
    public abstract class StateLinkList : LinkedList<PlayerStateBase>
    {
        public PlayerStateBase this[PlayerState stateID]
        {
            get
            {
                foreach (var state in this)
                {
                    if (state.stateID.Equals(stateID))
                    {
                        return state;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// 获取当前ID列表
        /// </summary>
        public PlayerState[] StateIDs
        {
            get
            {
                if (this.Count == 0) return null;

                PlayerState[] stateIDs = new PlayerState[this.Count];

                int index = 0;
                foreach (var state in this)
                {
                    stateIDs[index] = state.stateID;
                    index++;
                }

                return stateIDs;
            }
        }

        public List<int> StateList
        {
            get
            {
                if (this.Count == 0) return null;

                List<int> stateList = new List<int>();
                foreach (var state in this)
                {
                    stateList.Add((int)state.stateID);
                }

                return stateList;
            }
        }

        /// <summary>
        /// 是否包含此状态
        /// </summary>
        /// <param name="stateID"></param>
        /// <returns></returns>
        public bool ContainsState(PlayerState stateID)
        {
            foreach (var state in this)
            {
                if (state.stateID == stateID)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 尝试获取State
        /// </summary>
        /// <param name="stateID"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        public bool TryGetState(PlayerState stateID, out PlayerStateBase state)
        {
            state = null;

            foreach (var playerState in this)
            {
                if (playerState.stateID == stateID)
                {
                    state = playerState;

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 插入状态
        /// </summary>
        /// <param name="state"></param>
        /// <param name="excludeTypes"></param>
        public abstract void InsertState(PlayerStateBase state, StateExcludeType[] excludeTypes);
    }

    public class PlayerStateLinkList : StateLinkList
    {
        public override void InsertState(PlayerStateBase state, StateExcludeType[] excludeTypes)
        {
            if (this.Count == 0 || excludeTypes == null)
            {
                this.AddFirst(state);
                return;
            }

            var curState = this.First;

            for (int i = 0; i < excludeTypes.Length; i++)
            {
                if (excludeTypes[i] == StateExcludeType.Coexist)
                {
                    this.AddBefore(curState, state);
                    return;
                }

                curState = curState.Next;
            }

            this.AddLast(state);
        }
    }

    public class PlayerStateCacheLinkList : StateLinkList
    {
        public override void InsertState(PlayerStateBase state, StateExcludeType[] excludeTypes)
        {
            if (this.Count == 0 || excludeTypes == null)
            {
                this.AddFirst(state);
                return;
            }

            var curState = this.First;

            for (int i = 0; i < excludeTypes.Length; i++)
            {
                if (excludeTypes[i] != StateExcludeType.DirectIntoState)
                {
                    this.AddBefore(curState, state);
                    return;
                }

                curState = curState.Next;
            }

            this.AddLast(state);
        }
    }

}
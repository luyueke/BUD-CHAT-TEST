namespace FSM
{
    /// <summary>
    /// 状态的基础类
    /// </summary>
    public abstract class StateBase<T>
    {
        // 给每个状态设置一个ID
        public T stateID { get; }

        protected StateMachine<T> m_Machine;
        // 被当前机器所控制
        public virtual StateMachine<T> machine { set { m_Machine = value; } }

        public StateBase(T id)
        {
            this.stateID = id;
        }

        // 状态机的生命周期
        public abstract void OnEnter();
        public virtual void OnUpdate() { }
        public virtual void OnFixedUpdate() { }

        public virtual void OnLateUpdate() { }

        public abstract void OnExit();
    }
}
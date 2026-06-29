namespace FSM
{
    public abstract class StateMachine<T>
    {
        public StateMachine(StateBase<T> beginState) { Init(); }

        protected abstract void Init();

        public abstract void AddState(StateBase<T> state);

        public abstract void EnterState(T stateID, bool isDirectIntoState, params object[] args);

        public abstract void ReconnectIntoState(T stateID, params object[] args);

        public abstract void ExitState(T stateID, bool isPlayExitAni);

        public abstract void Update();

        public abstract void FixedUpdate();
        public abstract void LateUpdate();
    }
}
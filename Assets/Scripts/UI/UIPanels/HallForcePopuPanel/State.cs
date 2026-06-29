
namespace FSM
{
    public class State
    {
        protected StateMachine Machine { get; private set; }

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        public virtual void OnExit() { }

        public void SetMachine(StateMachine machine)
        {
            Machine = machine;
        }

        public static implicit operator bool(State state)
        {
            return state != null;
        }
    }
}
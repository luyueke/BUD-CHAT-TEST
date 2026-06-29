
namespace FSM
{
    public class StateMachine
    {
        public State Current { get; private set; }
        public StateMachine()
        {
            Current = null;
        }

        public void ChangeState(State nextState)
        {
            Current?.OnExit();
            Current = nextState;
            if (nextState)
            {
                nextState.SetMachine(this);
                nextState.OnEnter();
            }            
        }

        public void DoUpdate()
        {
            Current?.OnUpdate();
        }
    }
}
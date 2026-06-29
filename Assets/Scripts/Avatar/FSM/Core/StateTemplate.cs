namespace FSM
{
    public abstract class PlayerStateTemplate<K> : PlayerStateBase
    {
        protected K owner;

        public PlayerStateTemplate(PlayerState id, K owner) : base(id)
        {
            this.owner = owner;
        }
    }

    public abstract class StateTemplate<T, K> : StateBase<T>
    {
        protected K owner;

        public StateTemplate(T id, K owner) : base(id)
        {
            this.owner = owner;
        }
    }
}
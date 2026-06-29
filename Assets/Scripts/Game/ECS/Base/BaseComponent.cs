namespace Game.ECS
{
    public abstract class BaseComponent
    {
        public uint CmpId{ get; private set; }
        public abstract BaseComponent Clone();

        public void SetId(uint id)
        {
            CmpId = id;
        }
    }
}


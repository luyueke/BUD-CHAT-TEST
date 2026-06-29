using System.Collections.Generic;

namespace Game.ECS
{
    public abstract class BaseSystem
    {
        protected List<Entity> entities = new List<Entity>();

        public abstract void Update();
    }
}

using Es;

namespace UI.EditOperation.Rules
{
    public abstract class BasicPropRule
    {
        public abstract void OnTriggerStart(GamePropEditOperation config);
        public abstract void OnTriggerEnd(GamePropEditOperation config);
    }
}
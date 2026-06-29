namespace View.UI.PopupPanelSystem.Base.Core
{
    public interface IPopupSystem
    {
        public void Release();
    }

    public abstract class BasePopupSystem : IPopupSystem
    {
        protected readonly PopupPanelManager Context;

        protected BasePopupSystem(PopupPanelManager context)
        {
            this.Context = context;
        }

        public abstract void Release();
    }
}
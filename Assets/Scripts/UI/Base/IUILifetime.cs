namespace UI.Base
{
    public interface IUILifetime
    {
        public void OnCreate();

        public void OnShow(params object[] args);

        public void OnHidden();

        /// <summary>
        /// window 被遮挡
        /// </summary>
        public void OnWindowBeCovered(bool isCover);

        /// <summary>
        /// window 重新显示在前台
        /// </summary>
        public void OnWindowBeFocused();

        public void OnWindowPop();
    }
}
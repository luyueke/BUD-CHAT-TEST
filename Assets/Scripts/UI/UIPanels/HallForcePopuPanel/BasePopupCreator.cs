using View.UI.PopupPanelSystem.Data;

namespace View.UI.PopupPanelSystem.Base.Core
{
    public abstract class BasePopupCreator
    {
        protected readonly PopupPanelManager Context;

        protected BasePopupCreator(PopupPanelManager context)
        {
            this.Context = context;
        }

        public abstract void Release();

        /// <summary>
        /// 解析数据，并创建Popup对象
        /// </summary>
        /// <param name="data"></param>
        public abstract void ParseDataAndCreatePopup(PopupRspData data);
    }
}
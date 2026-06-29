using Basic.Utils;
using View.UI.PopupPanelSystem.Base.Core;
using View.UI.PopupPanelSystem.Base.Utils;
using View.UI.PopupPanelSystem.Data;

namespace View.UI.PopupPanelSystem.ExtendsPopups
{
    /// <summary>
    /// WebTool配置的弹窗
    /// </summary>
    public class WebToolPopup : BasePopup
    {
        public WebtoolNewsData NewsData;

        public WebToolPopup(PopupPanelManager context, WebtoolNewsData newsData)
        {
            NewsData = newsData;
            Context = context;
            CurState = GetState();
        }

        public override void OnEnter()
        {
            if (IsCanPop())
            {
                OnPlayPopup();
            }
            else
            {
                PlayNext();
            }
        }

        protected override void OnPlayPopup()
        {
            base.OnPlayPopup();
            WebtoolForcePopuPanel webtoolForcePopuPanel =
                UIManager.Inst.OpenPanelTakeAni<WebtoolForcePopuPanel>(PanelId.WebtoolForcePopuPanel);
            webtoolForcePopuPanel.SetPopupData(NewsData);
            webtoolForcePopuPanel.OnClose = OnClose;
            webtoolForcePopuPanel.OnFinish = OnFinish;
        }

        public override bool IsCanPop()
        {
            return NewsData != null && base.IsCanPop();
        }

        protected override string CreateKey()
        {
            return $"WebToolPopup_{PopupDataHelper.GetUid()}_{NewsData.popupId}_{GameUtils.GetTimeDay()}";
        }
    }
}
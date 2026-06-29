
namespace View.UI.PopupPanelSystem.Base.Utils
{
    public static class PopupDataHelper
    {
        public static bool IsCanRunPopup()
        {
            UIManager.Inst.TryFindPanel<GameHallPanel>(PanelId.GameHallPanel, out var panel);
            return panel != null && panel.IsInGameHall();
        }

        public static string GetUid()
        {
            return AccountDataManager.Inst.Uid;
        }
    }
}
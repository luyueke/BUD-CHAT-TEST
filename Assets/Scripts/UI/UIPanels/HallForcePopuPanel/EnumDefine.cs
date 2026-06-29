namespace View.UI.PopupPanelSystem.Data
{
    public enum WarmStartType
    {
        ColdStart = 0,
        WarmStart = 1
    }

    public enum RefreshLoginGiftType
    {
        None = 0,
        Open = 1,
    }

    public enum PopupState
    {
        Show = 0,
        Closed = 1, //选择了关闭
        Finish = 2 //已完成
    }

    /// <summary>
    /// 弹窗级别,产品进行定义,值越小越先弹出
    /// </summary>
    public enum PopupControlLevel
    {
        Level0 = 0,
        Level1,
        Level2,
    }
}
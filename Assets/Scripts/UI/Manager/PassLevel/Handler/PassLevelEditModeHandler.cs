/// <summary>
/// 编辑模式
/// </summary>
public class PassLevelEditModeHandler : PassLevelHandlerBase
{
    public override void HandlePassed()
    {
        BackToEdit();
    }

    public override void HandleFailed()
    {
        BackToEdit();
    }

    private void BackToEdit()
    {
        //试玩下无论成功还是失败均回编辑
        if(UIManager.Inst.TryFindPanel<GamePlayPanel>(WindowId.GuestWindow, PanelId.GamePlayPanel, out var panel))
        {
            panel.OnSwitchToEdit();
        }
    }
}
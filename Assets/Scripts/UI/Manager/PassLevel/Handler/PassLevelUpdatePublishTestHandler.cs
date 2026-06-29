using Game.MapSetting;

/// <summary>
/// 覆盖发布自测发布模式--GameMode为Play
/// </summary>
public class PassLevelUpdatePublishTestHandler : PassLevelHandlerBase
{
    public override void HandlePassed()
    {
        var panel = UIManager.Inst.OpenPanel<CommonSingleConfirmPanel>(PanelId.CommonSingleConfirmPanel, new CommonSingleConfirmPanelData()
        {
            ContextString = "恭喜通关！现在可以发布地图啦！",
            ConfirmString = "发布",
            ConfirmClickAction = () =>
            {
                var curCostTime = PassLevelManager.Inst.GetCurPassLevelTime();
                PassLevelDataManager.Inst.SetPlayTestDuration(curCostTime);
                LoggerUtils.Log("PassLevelUpdatePublishTestHandler 覆盖发布==" + curCostTime);
                PassLevelManager.Inst.ExitCurrentMap(() =>
                {
                    if (UIManager.Inst.TryFindPanel<UpdateMapPanel>(PanelId.UpdateMapPanel, out var panel))
                    {
                        panel.SendUpdateHttpRequest();
                    }
                });
            }
        });
        panel.HideCloseBtn();
    }

    public override void HandleFailed()
    {
        CommonConfirmWithTitlePanel commonConfirmPanel =
            UIManager.Inst.OpenPanel<CommonConfirmWithTitlePanel>(PanelId.CommonConfirmWithTitlePanel);
        commonConfirmPanel.SetLocalText("", "通关失败！你需要通过才可以发布地图哦！", "再试一次", "退出");
        commonConfirmPanel.SetOnClickAction(() =>
        {
            PassLevelManager.Inst.PlayAgainOffline();
        }, () =>
        {
            PassLevelManager.Inst.ExitCurrentMap();
        });
        commonConfirmPanel.HideCloseBtn();
    }
}
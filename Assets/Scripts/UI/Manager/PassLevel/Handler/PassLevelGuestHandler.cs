/// <summary>
/// 联机游玩模式结算
/// </summary>
public class PassLevelGuestHandler : PassLevelHandlerBase
{
    public override void HandlePassed()
    {
        OpenPassLevelPanel(true);
        RequestRewardData();
    }

    public override void HandleFailed()
    {
        OpenPassLevelPanel(false);
        RequestRewardData();
    }

    public void RequestRewardData()
    {
        // todo:fsc 接入服务器奖励数据
        // PassLevelManager.Inst.SendPassLevelDataReq((rewardInfo) =>
        //     {
        //         if (PassLevelPanel.Instance) PassLevelPanel.Instance.SetRewards(rewardInfo);
        //     },
        //     (error) => { LoggerUtils.LogError($"PassLevelGuestHandler SendPassLevelDataReq faild :{error}"); });
    }

    private void OpenPassLevelPanel(bool isWin)
    {
        var panel = UIManager.Inst.OpenPanel<PassLevelPanel>(PanelId.PassLevelPanel, isWin);
        panel.SetButtons(new PassLevelPanel.ButtonSetting()
        {
            BtnText = "返回大厅",
            ClickAction = () =>
            {
                panel.CloseSelf();
                PassLevelManager.Inst.ExitCurrentMap();
            }
        }, new PassLevelPanel.ButtonSetting()
        {
            BtnText = "再来一次",
            ClickAction = () =>
            {
                panel.CloseSelf();
                PassLevelManager.Inst.PlayAgainOnline();
            }
        });
    }
}
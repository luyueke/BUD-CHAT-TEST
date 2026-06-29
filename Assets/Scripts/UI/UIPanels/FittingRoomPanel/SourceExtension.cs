using Game.Event;
using Game.Store;
using UI.UIPanels.GashaponPanel;
using UI.UIPanels.RechargePanel;
using View.UI.PopupPanelSystem.Data;

namespace UI.UIPanels.FittingRoom {
    public static class SourceExtension {

        public static void HandSkip(this Source source, string id) {

            switch (source) {
                case Source.Gashapon:
                    GashaponDataManager.Inst.JumpToGashapon(id);
                    break;
                case Source.SeasonPass:
                    UIManager.Inst.OpenPanel(PanelId.NewSeasonPassPanel);
                    break;
                case Source.VIP:
                    UIManager.Inst.OpenPanel(PanelId.RechargePanel, (int)RechargeId.VipMonthPack);
                    break;
                case Source.Activity:
                    if (int.TryParse(id, out var activityId)) {
                        id = ((ActivityId) activityId).ToString();
                    }
                    UIManager.Inst.OpenPanel(PanelId.ActivityCenterPanel, id);
                    break;
                case Source.HotSales:
                    if (int.TryParse(id, out var rechargeId)) {
                        UIManager.Inst.OpenPanel(PanelId.RechargePanel,rechargeId);

                    }
                    break;
                case Source.Gift:
                    var paidPackSkipData = new PaidPackSkipData();
                    paidPackSkipData.taskId = id;
                    paidPackSkipData.HandSkip();
                    break;
                case Source.BeginnerTask:
                    UIManager.Inst.OpenPanel(PanelId.BudNewbieTaskV2Panel);
                    break;
                case Source.CreatorReward:
                    UIManager.Inst.OpenPanel(PanelId.CreatorCenterPanel, id);
                    break;
                case Source.Task:
                    if (int.TryParse(id, out var taskId)) {
                        id = ((TASK_ID) taskId).ToString();
                    }
                    // 累充福利
                    if (id == TASK_ID.RechargeBenefits.ToString()) {
                        UIManager.Inst.OpenPanel(PanelId.CumulativeRechargePanel);
                    } else {
                        LoggerUtils.LogError("暂未处理跳转");
                    }

                    break;
            }


        }

        public static string GetName(this Source source, string id = null) {
            string sourceName = "";
            switch (source) {
                case Source.Gashapon:
                    sourceName = "前往扭蛋";
                    break;
                case Source.SeasonPass:
                    sourceName = "赛季通行证";
                    break;
                case Source.VIP:
                    sourceName = "VIP专属";
                    break;
                case Source.Activity:
                    sourceName = "限时活动";
                    break;
                case Source.HotSales:
                    sourceName = "热销";
                    break;
                case Source.Gift:
                    sourceName = "限时礼包";
                    break;
                case Source.BeginnerTask:
                    sourceName = "新手任务";
                    break;
                case Source.CreatorReward:
                    sourceName = "创作者奖品";
                    break;
                case Source.Task:
                    if (int.TryParse(id, out var taskId)) {
                        id = ((TASK_ID) taskId).ToString();
                    }
                    // 累充福利
                    if (id == TASK_ID.RechargeBenefits.ToString()) {
                        sourceName = "累充福利";
                    } else {
                        sourceName = "任务";
                    }
                    break;
            }
            return sourceName;
        }

    }

}

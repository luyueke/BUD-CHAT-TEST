using System.Collections.Generic;

public class FireworkWelcomeSpringDailyView : CommonDailyView {
    protected override Dictionary<CommonTaskType, List<int>> _taskTypeIds =>
        new() {
            { CommonTaskType.DailyTask, new List<int>() { 1, 2, 3, 4, 5 } },
            { CommonTaskType.OtherTask, new List<int>() { 6, 7, 8, 9, 10, 11 } }
        };
}

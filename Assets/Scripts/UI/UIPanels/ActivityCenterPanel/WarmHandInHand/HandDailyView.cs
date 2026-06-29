using System.Collections.Generic;

public class HandDailyView : CommonDailyView {
    protected override Dictionary<CommonTaskType, List<int>> _taskTypeIds =>
        new() {
            { CommonTaskType.DailyTask, new List<int>() { 1, 2, 3 } },
            { CommonTaskType.OtherTask, new List<int>() { 4, 5, 6 } }
        };
}

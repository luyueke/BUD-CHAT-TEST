using Game.Event;

public class TreasureHuntTaskItem : CommonDailyViewItem
{
    protected override void GoButtonClick()
    {
        EventCenterDataManager.Inst.SkipToTask((EventCenterSkipType)_info.eventSkipType);
    }
}

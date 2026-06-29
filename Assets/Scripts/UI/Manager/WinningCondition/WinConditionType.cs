using Es;

public enum WinConditionType
{
    ReachTheEndFlag = 1, //碰到终点旗子
    CollectAllStars = 2, //收集星星
}

public class WinConditionFactory
{
    public static WinConditionBase CreateNew(WinCondition config)
    {
        return (WinConditionType)config.Id switch
        {
            WinConditionType.ReachTheEndFlag => new ReachEndFlagCondition(config),
            WinConditionType.CollectAllStars => new CollectStarCondition(config),
            _ => null
        };
    }
}
using System;

public abstract class GameDurationBehaviorBase
{
    public long RingSecondsPassed = 0;
    public bool isRinged;

    public long leftTime = 0;
    public GameDurationData DurationData;

    protected GameDurationBehaviorBase(GameDurationData durationData, long ringSecondsPassed = 0)
    {
        RingSecondsPassed = ringSecondsPassed;
        DurationData = durationData;
    }

    public abstract void OnStart(Action act);
    public abstract void OnStop();

    //暂未使用
    public abstract void OnPause();
    public abstract void OnContinue();

    /// <summary>
    /// 每秒tick, 禁止继承类写高gc操作
    /// </summary>
    /// <param name="passedSecs">距离上一次tick经过的秒数</param>>
    public abstract void OnTick(long passedSecs);

    /// <summary>
    /// 度过指定时间(s)后,触发回调
    /// </summary>
    /// <param name="allPassedSecs">从计时开始到ring经过的总秒数</param>>
    public abstract void OnRing(long allPassedSecs);

    public abstract void OnTimeout(long timeCost);

}
using System;
using UnityEngine;

public interface IAnimationEvent
{
    /// <summary>
    /// 动画时长
    /// </summary>
    public float AnimationTime { get; }

    public AnimationClip GetClip(string stateName);
    /// <summary>
    /// 动画播放完成事件
    /// </summary>
    /// <returns></returns>
    public IAnimationEvent OnCompleteEvent(Action onCompleteEvent);
    /// <summary>
    /// 动画中触发事件
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public IAnimationEvent OnTimeEvent(float time, Action onTimeEvent);

    /// <summary>
    /// 添加人物动画状态机里事件
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="eventName"></param>
    /// <param name="time"></param>
    /// <param name="animationEvent"></param>
    /// <param name="isOverride"></param>
    void AddStateEvent(string stateName, string eventName, float time, Action animationEvent, bool isOverride = false);

    /// <summary>
    /// 删除人物动画状态机里事件
    /// </summary>
    /// <param name="stateName"></param>
    /// <param name="eventName"></param>
    void RemoveStateEvent(string stateName, string eventName);
}

public struct SingleAnimationEvent : IComparable<SingleAnimationEvent>
{
    private float time;
    private Action animationEvent;

    public SingleAnimationEvent(float time, Action animationEvent)
    {
        this.time = time;
        this.animationEvent = animationEvent;
    }

    public int CompareTo(SingleAnimationEvent other)
    {
        return this.time > other.time ? 1 : -1;
    }

    public void Invoke()
    {
        animationEvent?.Invoke();
    }
}

public struct SingleInterruptEvent
{
    private int stateHashCode;
    private Action interruptEvent;

    public int HashCode => stateHashCode;

    public SingleInterruptEvent(int stateHashCode, Action interruptEvent)
    {
        this.stateHashCode = stateHashCode;
        this.interruptEvent = interruptEvent;
    }

    public void Invoke()
    {
        interruptEvent?.Invoke();
        interruptEvent = null;
    }

    public void Clear()
    {
        stateHashCode = 0;
        interruptEvent = null;
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimationEventEx : MonoBehaviour, IAnimationEvent, IUpdateState
{
    protected Animator m_Animator;

    private List<List<SingleAnimationEvent>> AnimationEventList;
    private Dictionary<string, Action> stateEventDic;

    protected AnimationInfo m_CurAniInfo;
    protected AnimationInfo[] m_AniInfoArray;

    public float AnimationTime => m_CurAniInfo.AnimationTime;

    #region Mono
    //protected virtual void Awake()
    //{
    //    Init();
    //}

    public virtual void Init()
    {
        m_AniInfoArray = new AnimationInfo[m_Animator.layerCount];

        stateEventDic = new Dictionary<string, Action>();
        AnimationEventList = new List<List<SingleAnimationEvent>>();
        for (int i = 0; i < m_Animator.layerCount; i++)
        {
            AnimationEventList.Add(new List<SingleAnimationEvent>());
            m_AniInfoArray[i] = new AnimationInfo() { layer = i };
        }
    }
    #endregion

    #region IAnimationEvent
    public IAnimationEvent OnCompleteEvent(Action onCompleteEvent)
    {
        if (!m_Animator.gameObject.activeInHierarchy)
        {
            LoggerUtils.Log("人物隐藏中不能播放动画，并无法添加动画事件");
            return this;
        }

        AddAnimationClipEvent(m_CurAniInfo.animationClip, onCompleteEvent, m_CurAniInfo.AnimationTime, m_CurAniInfo.layer);

        return this;
    }

    public IAnimationEvent OnCompleteEvent(string stateName, Action<string> onCompleteEvent) {
        if (!m_Animator.gameObject.activeInHierarchy)
        {
            LoggerUtils.Log("人物隐藏中不能播放动画，并无法添加动画事件");
            return this;
        }
        var animClip = GetClip(stateName);
        if (animClip == null) {
            LoggerUtils.Log("找不到动画:" + stateName);
            return this;
        }
        AddAnimationClipEvent(animClip, () => {
            onCompleteEvent?.Invoke(stateName);
        }, animClip.length, m_CurAniInfo.layer);
        return this;
    }

    public IAnimationEvent OnTimeEvent(float time, Action onTimeEvent)
    {
        if (!m_Animator.gameObject.activeInHierarchy)
        {
            LoggerUtils.Log("人物隐藏中不能播放动画，并无法添加动画事件");
            return this;
        }


        AddAnimationClipEvent(m_CurAniInfo.animationClip, onTimeEvent, time, m_CurAniInfo.layer);

        return this;
    }

    public void AddStateEvent(string stateName, string eventName, float time, Action<string, string> animationEvent, bool isOverride = false) {
        var clip = GetClip(stateName);

        if (clip == null) return;

        void OnAnimationEventHandler() {
            animationEvent(stateName, eventName);
        }

        eventName = stateName + eventName;
        if (stateEventDic.ContainsKey(eventName))
        {
            if (isOverride) {
                stateEventDic[eventName] = OnAnimationEventHandler;
            }
            LoggerUtils.Log("事件名称重复");
            return;
        }

        stateEventDic.Add(eventName, OnAnimationEventHandler);

        AnimationEventHandler.UpsertClipEvent(clip, eventName, time);
    }

    public void AddStateEvent(string stateName, string eventName, float time, Action animationEvent, bool isOverride = false)
    {
        var clip = GetClip(stateName);

        if (clip == null) return;

        eventName = stateName + eventName;
        if (stateEventDic.ContainsKey(eventName))
        {
            if (isOverride) {
                stateEventDic[eventName] = animationEvent;
            }
            LoggerUtils.Log("事件名称重复");
            return;
        }

        stateEventDic.Add(eventName, animationEvent);

        AnimationEventHandler.UpsertClipEvent(clip, eventName, time);
    }

    public void RemoveStateEvent(string stateName, string eventName)
    {
        var clip = GetClip(stateName);

        if (clip == null || clip.events.Length == 0) return;

        eventName = stateName + eventName;

        for (int i = 0; i < clip.events.Length; i++)
        {
            if (eventName.Equals(clip.events[i].stringParameter))
            {
                var eventList = clip.events.ToList();
                eventList.RemoveAt(i);
                stateEventDic.Remove(eventName);

                if (eventList.Count == 0)
                    clip.events = null;
                else
                    clip.events = eventList.ToArray();

                break;
            }
        }
    }

    public virtual AnimationClip GetClip(string stateName)
    {
        return null;
    }
    #endregion

    #region private
    private void AddAnimationClipEvent(AnimationClip clip, Action animationEvent, float time, int layer)
    {
        if (animationEvent == null || clip == null) return;

        if (time < 0)
        {
            time = clip.length + time;
        }

        time = Mathf.Clamp(time, 0, clip.length);

        SingleAnimationEvent singleAnimationEvent = new SingleAnimationEvent(time, animationEvent);
        AnimationEventList[layer].Add(singleAnimationEvent);
        AnimationEventList[layer].Sort();

        AnimationEvent evt = new AnimationEvent();
        evt.functionName = "OnAnimationEvent";
        evt.intParameter = layer;
        evt.time = time;
        clip.AddEvent(evt);
    }

    private void OnAnimationEvent(int layer)
    {
        if (AnimationEventList[layer].Count > 0)
        {
            SingleAnimationEvent evt = AnimationEventList[layer][0];
            AnimationEventList[layer].RemoveAt(0);

            if (AnimationEventList[layer].Count == 0)
            {
                ClearAnimationEvent(layer);
            }

            evt.Invoke();
        }
    }

    protected void ClearAnimationEvent(int layer)
    {
        AnimationEventList[layer].Clear();
        AnimationInfo animationEventInfo = m_AniInfoArray[layer];
        if (animationEventInfo.animationClip != null)
        {
            animationEventInfo.animationClip.events = null;
            animationEventInfo.animationClip = null;
        }
    }

    private void OnStateEvent(string eventName)
    {
        if (stateEventDic.ContainsKey(eventName))
        {
            stateEventDic[eventName]?.Invoke();
        }
    }
    #endregion

    #region IUpdateState
    public void StateEnter(AnimatorStateInfo stateInfo, int layerIndex)
    {
    }

    public void StateExit(AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_AniInfoArray[layerIndex].stateHashCode == stateInfo.shortNameHash)
        {
            ClearAnimationEvent(layerIndex);
        }
    }
    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AnimationEventHandler : MonoBehaviour {

    protected Animator m_Animator;
    private Dictionary<string, Action> stateEventDic = new Dictionary<string, Action>();

    private void Awake() {
        m_Animator = GetComponent<Animator>();
    }

    public void AddStateEvent(string stateName, string eventName, float time, Action<string, string> animationEvent, bool isOverride = false) {
        var clip = m_Animator.runtimeAnimatorController.animationClips.FirstOrDefault(tmp => tmp.name == stateName);


        if (clip == null) return;

        void OnAnimationEventHandler() {
            animationEvent(stateName, eventName);
        }

        eventName = stateName + eventName;
        if (stateEventDic.ContainsKey(eventName)) {
            LoggerUtils.Log("事件名称重复");
            if (isOverride) {
                stateEventDic[eventName] = OnAnimationEventHandler;
            }
            return;
        }

        stateEventDic.Add(eventName, OnAnimationEventHandler);

        UpsertClipEvent(clip, eventName, time);
    }

    public void AddStateEvent(string stateName, string eventName, float time, Action animationEvent, bool isOverride = false) {
        var clip = m_Animator.runtimeAnimatorController.animationClips.FirstOrDefault(tmp => tmp.name == stateName);


        if (clip == null) return;

        eventName = stateName + eventName;
        if (stateEventDic.ContainsKey(eventName)) {
            LoggerUtils.Log("事件名称重复");
            if (isOverride) {
                stateEventDic[eventName] = animationEvent;
            }
            return;
        }

        stateEventDic.Add(eventName, animationEvent);

        UpsertClipEvent(clip, eventName, time);
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

    public AnimationClip GetClip(string stateName) {
        return m_Animator.runtimeAnimatorController.animationClips.FirstOrDefault(tmp => tmp.name == stateName);
    }

    // 已存在同名事件则更新时间，否则新增。clip.events 是 shared 资源数据，运行时改后会留在内存里，
    // 仅靠 isExists check + skip 会让代码里改过的 time 不生效，必须显式 upsert。
    internal static void UpsertClipEvent(AnimationClip clip, string eventName, float time) {
        var events = clip.events;
        var idx = Array.FindIndex(events, tmp => tmp.stringParameter == eventName);
        if (idx < 0) {
            AnimationEvent evt = new AnimationEvent {
                functionName = "OnStateEvent",
                stringParameter = eventName,
                time = time,
            };
            clip.AddEvent(evt);
        } else if (!Mathf.Approximately(events[idx].time, time)) {
            events[idx].time = time;
            clip.events = events;
        }
    }

    public void OnStateEvent(string eventName)
    {
        if (stateEventDic.ContainsKey(eventName))
        {
            stateEventDic[eventName]?.Invoke();
        }
    }
}

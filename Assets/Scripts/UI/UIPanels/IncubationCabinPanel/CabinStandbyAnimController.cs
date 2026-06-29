using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 养成舱待机动画自动循环控制器，供 IncubationCabinPanel 和 IncubationCabinRolesPanel 共用。
/// 逻辑：随机主动画(循环) 15-20s → 随机表演动画(非循环,播完) → 下一个主动画 ...
/// 主动画 > 3 个时，连续 3 次内不重复同一个动画。
/// </summary>
public class CabinStandbyAnimController
{
    private CabinPgcUgcPlayController _ctrl;
    private PendingEmoteData _pendingEmote;
    private BudTimer _timer;
    private bool _isRunning;

    // 主动画去重：记录最近 2 次下标（主动画 > 3 时生效，保证连续 3 次不重复）
    private readonly List<int> _recentLoopIndices = new List<int>();
    private const int MaxHistory = 2;

    // ── 外部接口 ──────────────────────────────────────────────────────────────

    /// <summary>角色加载完成后调用，启动自动待机循环</summary>
    public void Start(CabinPgcUgcPlayController controller, PendingEmoteData pendingEmote)
    {
        _ctrl = controller;
        _pendingEmote = pendingEmote;
        _isRunning = true;
        _recentLoopIndices.Clear();
        PlayAutoLoopAnim();
    }

    /// <summary>面板关闭 / OnHidden 时调用，停止所有待机逻辑并取消当前动画</summary>
    public void Stop()
    {
        _isRunning = false;
        StopCurrentTimer();
        _ctrl?.CancelAnim();
    }

    /// <summary>预览结束后恢复待机循环，复用已有的 controller 和 pendingEmote，不重置历史记录</summary>
    public void Resume()
    {
        _isRunning = true;
        PlayAutoLoopAnim();
    }

    /// <summary>用户点击循环动画：插播 5s 后恢复自动循环</summary>
    public void PlayUserLoopAnim(pEmoteData data)
    {
        if (!_isRunning)
            return;

        StopCurrentTimer();
        _ctrl.InitData(data);
        _ctrl.SetPlayEndCallback(null);
        // 手动点击待机动画也不播放音效
        _ctrl.PlayAnim(false);
        _timer = TimerManager.Inst.RunOnce("cabin_standby_manual_loop", 5f, ResumeAutoLoop);
    }

    /// <summary>用户点击非循环动画：插播完毕后恢复自动循环</summary>
    public void PlayUserPerformAnim(pEmoteData data)
    {
        if (!_isRunning)
            return;

        StopCurrentTimer();
        _ctrl.InitData(data);
        _ctrl.SetPlayEndCallback(() =>
        {
            StopCurrentTimer();
            ResumeAutoLoop();
        });
        // 先设置超时兜底定时器，再调用 PlayAnim，防止同步回调产生游离定时器
        _timer = TimerManager.Inst.RunOnce("cabin_standby_perform_timeout", 30f, () =>
        {
            _ctrl.SetPlayEndCallback(null);
            ResumeAutoLoop();
        });
        // 手动点击待机动画也不播放音效
        _ctrl.PlayAnim(false);
    }

    // ── 内部逻辑 ──────────────────────────────────────────────────────────────

    private void PlayAutoLoopAnim()
    {
        if (!_isRunning)
            return;

        // 防御：无论从何处调用，先取消当前待执行的定时器，避免游离定时器残留
        StopCurrentTimer();

        var loopList = _pendingEmote?.loopEmoteList;

        if (loopList == null || loopList.Count == 0)
            return;

        int idx = PickLoopIndex(loopList.Count);
        _ctrl.InitData(loopList[idx]);
        _ctrl.SetPlayEndCallback(null);
        // 待机自动循环不播放音效
        _ctrl.PlayAnim(false);

        float delay = UnityEngine.Random.Range(15f, 20f);
        _timer = TimerManager.Inst.RunOnce("cabin_standby_auto_switch", delay, OnAutoSwitchToPerform);
    }

    private void OnAutoSwitchToPerform()
    {
        if (!_isRunning)
            return;

        var performList = _pendingEmote?.emoteList;

        if (performList == null || performList.Count == 0)
        {
            // 无表演动画，直接切换到下一个主动画
            PlayAutoLoopAnim();
            return;
        }

        int idx = UnityEngine.Random.Range(0, performList.Count);
        _ctrl.InitData(performList[idx]);
        _ctrl.SetPlayEndCallback(() =>
        {
            StopCurrentTimer();
            PlayAutoLoopAnim();
        });
        // 先设置超时兜底定时器，再调用 PlayAnim
        // 防止 PlayAnim 同步触发回调时 _timer 尚未赋值，导致产生游离定时器打断后续表演动画
        _timer = TimerManager.Inst.RunOnce("cabin_standby_perform_timeout", 30f, () =>
        {
            _ctrl.SetPlayEndCallback(null);
            PlayAutoLoopAnim();
        });
        // 待机自动循环不播放音效
        _ctrl.PlayAnim(false);
    }

    private void ResumeAutoLoop()
    {
        if (!_isRunning)
            return;

        PlayAutoLoopAnim();
    }

    private void StopCurrentTimer()
    {
        TimerManager.Inst.Stop(_timer);
        _timer = null;
    }

    /// <summary>
    /// 随机选一个主动画下标。主动画 > 3 个时，排除最近 2 次的下标，保证连续 3 次不重复。
    /// </summary>
    private int PickLoopIndex(int count)
    {
        if (count <= 3)
            return UnityEngine.Random.Range(0, count);

        var candidates = new List<int>();

        for (int i = 0; i < count; i++)
        {
            if (!_recentLoopIndices.Contains(i))
                candidates.Add(i);
        }

        int picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _recentLoopIndices.Add(picked);

        if (_recentLoopIndices.Count > MaxHistory)
            _recentLoopIndices.RemoveAt(0);

        return picked;
    }
}

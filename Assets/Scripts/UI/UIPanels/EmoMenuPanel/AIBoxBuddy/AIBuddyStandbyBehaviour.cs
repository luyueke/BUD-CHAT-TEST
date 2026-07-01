using System;
using System.Collections.Generic;
using BUD.AnimPose;
using Game.Avatar;
using GameData.PgcData;
using Message;
using UnityEngine;

/// <summary>
/// 局内召唤 AI 伙伴的待机循环（挂在 buddy.Avatar 上，随 buddy 销毁自动停止）。
/// 流程：随机主动画(循环) 7-10s → 随机表演动画(非循环)播完 → 下一个主动画 ...
/// 主动画 > 3 个时连续 3 次不重复；待机动画无音效（走 idle 层，不被玩家移动打断）。
/// PGC 动作走 PlayerAnimationCtrl，UGC 动作复用 UgcIdleBehaviour。
/// </summary>
public class AIBuddyStandbyBehaviour : MonoBehaviour
{
    private PlayerAnimationCtrl _animCtrl;
    private AnimIKController _ikController;
    private PlayerStateController _stateCtrl; // 用于判断 buddy 是否正在牵手/emote 等忙碌状态
    private UgcIdleBehaviour _ugcBehaviour; // 复用现成的局内 UGC idle 播放
    private string _playerId; // buddy 所属玩家 uid（自己端=自己uid，其他端=对方uid）
    private bool _onVehicle;  // buddy 正坐在载具上时暂停待机（载具驱动乘客动画）
    private bool _vehicleStopped; // 上车时是否已停掉当前待机动画（只停一次，下车复位）
    private bool _busyStopped;   // 进入 LinkEmote 等忙碌状态时是否已停掉待机动画（只停一次，空闲后复位）

    private readonly List<pEmoteData> _loopList = new List<pEmoteData>();    // 主动画(循环)
    private readonly List<pEmoteData> _performList = new List<pEmoteData>(); // 表演动画(非循环)

    private enum Phase { Waiting, Main, Perform }
    private Phase _phase = Phase.Waiting;
    private float _timer;
    private float _phaseDuration;
    private bool _lastWasUgc; // 上一个播放的是否 UGC，用于切换时清理另一播放层

    // 主动画去重：记录最近 2 次下标（主动画 > 3 时生效，保证连续 3 次不重复）
    private readonly List<int> _recentLoop = new List<int>();
    private const int MaxHistory = 2;
    private const float PerformTimeout = 30f; // 表演动画异常未回调时的兜底超时

    /// <param name="initialDelay">召唤后留给唤醒动作播放的时间，之后再进入待机循环</param>
    public void StartStandby(PlayerAnimationCtrl animCtrl, AnimIKController ikController, PlayerStateController stateCtrl, PendingEmoteData data, float initialDelay, string playerId)
    {
        _animCtrl = animCtrl;
        _ikController = ikController;
        _stateCtrl = stateCtrl;
        _playerId = playerId;

        _loopList.Clear();
        _performList.Clear();
        CollectValid(data?.loopEmoteList, _loopList);
        CollectValid(data?.emoteList, _performList);
        _recentLoop.Clear();

        if (_loopList.Count == 0)
        {
            // 无可用主动画，不启动待机
            _phase = Phase.Waiting;
            _phaseDuration = float.MaxValue;
            return;
        }

        // UGC 待机播放复用 UgcIdleBehaviour：喂入所有 UGC 动画数据，mainIdle 留空以禁用其自循环
        var ugcIdleList = new List<UgcIdleData>();
        CollectUgc(_loopList, ugcIdleList);
        CollectUgc(_performList, ugcIdleList);
        if (ugcIdleList.Count > 0 && ikController != null)
        {
            _ugcBehaviour = gameObject.GetComponent<UgcIdleBehaviour>();
            if (_ugcBehaviour == null)
                _ugcBehaviour = gameObject.AddComponent<UgcIdleBehaviour>();
            _ugcBehaviour.Init(ikController, false);
            _ugcBehaviour.SetData(UgcAnimSubType.Single,
                new IdleData { mainIdle = string.Empty, subIdle = null, ugcIdleList = ugcIdleList });
        }

        _phase = Phase.Waiting;
        _timer = 0f;
        _phaseDuration = Mathf.Max(0f, initialDelay);

        MessageHelper.RemoveListener<string, bool>(MessageName.OnBuddyVehicleStateChange, OnVehicleStateChange);
        MessageHelper.AddListener<string, bool>(MessageName.OnBuddyVehicleStateChange, OnVehicleStateChange);
    }

    private void OnVehicleStateChange(string driverUid, bool onVehicle)
    {
        if (driverUid == _playerId) _onVehicle = onVehicle;
    }

    private void OnDestroy()
    {
        MessageHelper.RemoveListener<string, bool>(MessageName.OnBuddyVehicleStateChange, OnVehicleStateChange);
    }

    private static void CollectValid(List<pEmoteData> src, List<pEmoteData> dst)
    {
        if (src == null) return;
        foreach (var e in src)
        {
            if (e != null && (!string.IsNullOrEmpty(e.emoteId) || e.ugcData != null))
                dst.Add(e);
        }
    }

    private static void CollectUgc(List<pEmoteData> src, List<UgcIdleData> dst)
    {
        foreach (var e in src)
        {
            if (string.IsNullOrEmpty(e.emoteId) && e.ugcData != null)
                dst.Add(e.ugcData);
        }
    }

    private void Update()
    {
        if (_loopList.Count == 0) return;

        // 上车瞬间停掉当前正在播放的待机动画，让位给载具坐姿（仅暂停不够，循环动画会一直覆盖坐姿）。
        // 只对载具情形主动停；emote 等状态由状态机自己驱动动画，不在此停以免打断。
        bool onVehicle = _onVehicle || (_stateCtrl != null && _stateCtrl.IsRidingVehicle);
        if (onVehicle && !_vehicleStopped)
        {
            StopCurrentEmote();
            _vehicleStopped = true;
        }
        else if (!onVehicle)
        {
            _vehicleStopped = false;
        }

        // buddy 正在牵手/双人动作/emote 等状态时，动画由状态机接管，待机暂停；
        // 进入忙碌的第一帧停掉当前待机动画及其特效物体，避免动画被中断后特效残留。
        // 空闲后回到 Waiting(duration=0) 立即从主动画恢复。
        if (IsBusy())
        {
            if (!_busyStopped)
            {
                StopCurrentEmote();
                _busyStopped = true;
            }
            _phase = Phase.Waiting;
            _timer = 0f;
            _phaseDuration = 0f;
            return;
        }
        _busyStopped = false;

        _timer += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Waiting:
                if (_timer >= _phaseDuration) PlayMain();
                break;
            case Phase.Main:
                if (_timer >= _phaseDuration) PlayPerform();
                break;
            case Phase.Perform:
                if (_timer >= PerformTimeout) PlayMain();
                break;
        }
    }

    /// <summary>buddy 是否处于由状态机接管动画的忙碌状态（此时不应播待机）。</summary>
    private bool IsBusy()
    {
        if (_onVehicle) return true; // 在载具上，动画由载具乘客姿势接管（消息路径，本人 buddy）
        // buddy 自身 stateCtrl 标志：上/下车代码按正确引用直接设置，兼容地图共享 buddy（消息 key 不匹配的情况）
        if (_stateCtrl != null && _stateCtrl.IsRidingVehicle) return true;
        if (_stateCtrl == null) return false;
        return _stateCtrl.IsInLinkAIBuddy()
            || _stateCtrl.ContainsCurrentState(PlayerState.LinkEmote)
            || _stateCtrl.ContainsCurrentState(PlayerState.LinkEmoteStart)
            || _stateCtrl.ContainsCurrentState(PlayerState.DoubleEmote)
            || _stateCtrl.ContainsCurrentState(PlayerState.SingleEmote)
            || _stateCtrl.ContainsCurrentState(PlayerState.UgcEmote);
    }

    private void PlayMain()
    {
        if (IsBusy()) { _phase = Phase.Waiting; _timer = 0f; _phaseDuration = 0f; return; }

        int idx = PickLoopIndex(_loopList.Count);
        _phase = Phase.Main;
        _timer = 0f;
        _phaseDuration = UnityEngine.Random.Range(7f, 10f);
        PlayEmote(_loopList[idx], isLoop: true, onComplete: null);
    }

    private void PlayPerform()
    {
        if (_performList.Count == 0)
        {
            PlayMain();
            return;
        }

        int idx = UnityEngine.Random.Range(0, _performList.Count);
        _phase = Phase.Perform;
        _timer = 0f;
        PlayEmote(_performList[idx], isLoop: false, onComplete: () =>
        {
            if (this != null && _phase == Phase.Perform) PlayMain();
        });
    }

    private void PlayEmote(pEmoteData data, bool isLoop, Action onComplete)
    {
        bool isUgc = string.IsNullOrEmpty(data.emoteId) && data.ugcData != null;

        // 切换播放层时清理上一个，避免 PGC/UGC 动画叠加
        if (isUgc != _lastWasUgc)
        {
            if (_lastWasUgc) _ikController?.StopAnim();
            else _animCtrl?.ResetEmoteForUICharacter();
        }
        _lastWasUgc = isUgc;

        if (isUgc)
        {
            if (_ugcBehaviour == null) { onComplete?.Invoke(); return; }
            _ugcBehaviour.PlayerAnim(data.ugcData.id, onComplete, isLoop);
        }
        else
        {
            _animCtrl.PlaySingleEmoteByPlayerId(_playerId, data.emoteId, OnCompleteSingleEmote: onComplete, isPlaySound: false);
        }
    }

    /// <summary>停掉当前正在播放的待机动画（上车时让位载具坐姿）。与 PlayEmote 的切层清理一致。</summary>
    private void StopCurrentEmote()
    {
        if (_lastWasUgc) _ikController?.StopAnim();
        else _animCtrl?.ResetEmoteForUICharacter();
    }

    /// <summary>主动画 > 3 个时排除最近 2 次下标，保证连续 3 次不重复。</summary>
    private int PickLoopIndex(int count)
    {
        if (count <= 3)
            return UnityEngine.Random.Range(0, count);

        var candidates = new List<int>();
        for (int i = 0; i < count; i++)
        {
            if (!_recentLoop.Contains(i))
                candidates.Add(i);
        }

        int picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        _recentLoop.Add(picked);
        if (_recentLoop.Count > MaxHistory)
            _recentLoop.RemoveAt(0);

        return picked;
    }
}

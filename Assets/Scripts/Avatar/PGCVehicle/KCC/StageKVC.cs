using System.Collections.Generic;
using Es;
using Game.Avatar;
using Game.KinematicCharacter;
using Game.Vehicle.PGCVehicle.KVC;
using Message;
using UnityEngine;

public class StageKVC : PGCVehicleBaseKVC
{
    private static readonly string[] DanceEmoteIds =
    {
        "40100218", "40100253", "40100067", "40200161",
        "40100068", "40100254", "40100483", "40100062", "40200266"
    };

    private static readonly string[] SkillSoundIds = { "Zeze_Skill01", "Zeze_Skill02", "Zeze_Skill03" };

    private static readonly Dictionary<int, int> VehicleSoundIndexMap = new()
    {
        { 160100009, 0 },
        { 160100010, 1 },
        { 160100011, 2 },
    };

    private const float DefaultSkillRadius = 8f;
    private const float DefaultSkillDuration = 3f;
    private const float SkillMoveThreshold = 0.1f;
    private const string CharSummonAnimPath = "Assets/Loadable/Animations/Vehicle/zeze/summon.anim";
    private const string CharIdleAnimPath = "Assets/Loadable/Animations/Vehicle/zeze/idle.anim";
    private const string CharSkillReadyAnimPath = "Assets/Loadable/Animations/Vehicle/zeze/ready.anim";
    private const string CharSkillAnimPath = "Assets/Loadable/Animations/Vehicle/zeze/skill1.anim";

    private enum SkillPhase { None, Starting, Looping, Ending }
    private SkillPhase _skillPhase = SkillPhase.None;
    private float _skill1CooldownRemaining;
    private BudTimer _skillLoopTimer;
    private BudTimer _danceRecheckTimer;
    private readonly List<string> _playerUids = new();

    private bool _isStartPlaying = false;

    private GameObject _skillEffectRoot;
    private AnimationClip _charSummonClip;
    private AnimationClip _charIdleClip;
    private AnimationClip _charSkillReadyClip;
    private AnimationClip _charSkillClip;

    private Transform _seatTransform;
    private Vector3 _seatOriginalLocalPos;

    public override void OnInit(KinematicCharacterMotor motor, bool isSelf, bool isReconstruction = false)
    {
        base.OnInit(motor, isSelf, isReconstruction);
        Transform effectTrans = null;
        foreach (Transform child in motor.transform)
        {
            var t = child.Find("eff_baofa");
            if (t != null) { effectTrans = t; break; }
        }
        effectTrans.localScale = new Vector3(1.7f, 1.7f, 1.7f);
        foreach (var r in motor.transform.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            r.enabled = false;
        }
        if (effectTrans != null)
        {
            _skillEffectRoot = effectTrans.gameObject;
            _skillEffectRoot.SetActive(false);
        }
        _charSummonClip = Loader.Load<AnimationClip>(CharSummonAnimPath, motor.gameObject);
        _charIdleClip = Loader.Load<AnimationClip>(CharIdleAnimPath, motor.gameObject);
        _charSkillReadyClip = Loader.Load<AnimationClip>(CharSkillReadyAnimPath, motor.gameObject);
        _charSkillClip = Loader.Load<AnimationClip>(CharSkillAnimPath, motor.gameObject);
    }

    public override void ApplyConfig(PgcVehicleConfig config)
    {
        base.ApplyConfig(config);
        if (_isReconstruction)
        {
            _isStartPlaying = false;
            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
            // 重建路径（其他玩家进场重建已召唤的载具）不会走召唤流程，
            // 需在此显示载具，恢复 OnInit 中被禁用的 SkinnedMeshRenderer，否则后进场玩家看到模型全隐藏。
            ShowVehicle();
            return;
        }

        _isStartPlaying = true;

        // Seat_0 由 vehicleController.Init → BuildSeatsIfNeeded 创建，
        // 但 Init 在 InitWithConfig（本方法的调用方）之后才执行，当帧 Find 为 null。
        // 延一帧确保 Seat_0 已存在，再压低座位让 summon 动画从地面起始。
        TimerManager.Inst.RunOnce("", 0f, LowerSeatForSummon);

        float startLength = _pgcVehicleAnimCtrl.GetClipLength("start");
        float eventTime = startLength > 0f ? startLength * 0.9f : 0.9f;
        _pgcVehicleAnimCtrl.AddStateEvent("start", "on_stage_start_end", eventTime, OnStartEnd);
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Start);
        // 预制默认隐藏，延一帧在 Animator 已切入 start 状态后再显示，避免默认姿势闪烁
        TimerManager.Inst.RunOnce("", 0.2f, ShowVehicle);
        PlaySummonAnimForAll();
    }

    private void ShowVehicle()
    {
        if (_pgcVehicleAnimCtrl == null) return;
        // 从 Animator 所在节点向上找名为 "root" 的视觉根节点
        Transform t = _pgcVehicleAnimCtrl.transform;
        while (t.parent != null)
        {
            t = t.parent;
            if (t.name == "root") break;
        }
        if (!t.gameObject.activeSelf)
            t.gameObject.SetActive(true);
        else
        {
            foreach (var r in t.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                r.enabled = true;
        }
    }

    private void LowerSeatForSummon()
    {
        _seatTransform = Motor.transform.Find("Seat_0");
        if (_seatTransform == null && Motor.transform.parent != null)
            _seatTransform = Motor.transform.parent.Find("Seat_0");
        if (_seatTransform == null) return;
        _seatOriginalLocalPos = _seatTransform.localPosition;
        // summon.anim 以 seatOffset.y（未乘 1.7）为参考高度制作，设为该值使角色从正确起点开始
        float startY = (_pgcVehicleConfig != null && _pgcVehicleConfig.seatOffset.Count > 0)
            ? 0.4f
            : 0f;
        _seatTransform.localPosition = new Vector3(_seatOriginalLocalPos.x, startY, _seatOriginalLocalPos.z);
    }

    public override void AddPlayerAnimCtrl(string uid)
    {
        base.AddPlayerAnimCtrl(uid);
        if (!_playerUids.Contains(uid)) _playerUids.Add(uid);
        if (!_isStartPlaying) return;
        // SetPlayerAnimOverride（片段覆盖）在 PGCVehicleState.OnEnter 中调用，晚于此处一个调用栈。
        // 延迟一帧确保 idle 槽已被覆盖为 zeze/idle.anim，再把它替换为 summon.anim。
        TimerManager.Inst.RunOnce("", 0f, () =>
        {
            var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
            if (player != null && player.PlayerAnimCtrl != null)
                ApplySummonAnim(player.PlayerAnimCtrl);
        });
    }

    public override void RemovePlayerAnimCtrl(string uid)
    {
        base.RemovePlayerAnimCtrl(uid);
        _playerUids.Remove(uid);
    }

    private void ApplySummonAnim(PlayerAnimationCtrl ctrl)
    {
        if (_charSummonClip == null || ctrl == null) return;
        ctrl.OverrideAnimationClip("idle", _charSummonClip);
        ctrl.SetPlayerAniState(PlayerAniState.Idle, false);
    }

    private void PlaySummonAnimForAll()
    {
        foreach (var ctrl in _playerAnimCtrls)
            if (ctrl != null) ApplySummonAnim(ctrl);
    }

    private void OnStartEnd()
    {
        _isStartPlaying = false;
        // 先恢复座位高度，下一帧 LateUpdate 会把角色拉到正确的舞台位置
        if (_seatTransform != null)
            _seatTransform.localPosition = _seatOriginalLocalPos;
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
        foreach (var ctrl in _playerAnimCtrls)
        {
            if (ctrl == null) continue;
            if (_charIdleClip != null)
                ctrl.OverrideAnimationClip("idle", _charIdleClip);
            // SetPlayerAniState 在 CurAniState==Idle 时会 early-return，需用 TriggerAgain 强制重进 Idle 播放新片段
            ctrl.SetPlayerAniTriggerAgain(PlayerAniState.Idle);
        }
    }

    public override void OnLanded()
    {
        if (_isStartPlaying) return;
        base.OnLanded();
    }

    protected override void OnStateChange(PGCVehicleAniState state)
    {
        MessageHelper.Broadcast(MessageName.StopGameSound,
            IsSelf ? "Stop_Drive_1P" : "Stop_Drive_3P", Motor.gameObject);
        if (state == PGCVehicleAniState.Start)
        {
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Show", "Zeze_Show", IsSelf ? "Play_Show_1P" : "Play_Show_3P", Motor.gameObject);
        }
        else if (state == PGCVehicleAniState.Run)
        {
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive", "Zeze_Run", IsSelf ? "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
        }
        else if (state == PGCVehicleAniState.Jump)
        {
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Jump", "Zeze_Jump", IsSelf ? "Play_Jump_1P" : "Play_Jump_3P", Motor.gameObject);
        }
    }

    public override void UpdateVehicleVelocity(ref Vector3 currentVelocity, float deltaTime)
    {
        if (_skill1CooldownRemaining > 0f)
            _skill1CooldownRemaining -= deltaTime;
        base.UpdateVehicleVelocity(ref currentVelocity, deltaTime);
    }

    protected override void UpdateAniState(Vector3 currentVelocity)
    {
        if (_isStartPlaying) return;
        if (_skillPhase == SkillPhase.Looping && Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp).magnitude > SkillMoveThreshold)
            TriggerSkillEnd();
        if (_skillPhase != SkillPhase.None) return;
        base.UpdateAniState(currentVelocity);
    }

    public override void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
    {
        base.UpdateRotation(ref currentRotation, deltaTime);
    }

    public override void OnSkill(int skillId, bool isPress, string extraJson = null)
    {
        if (skillId != (int)SkillType.Skill1) return;
        if (!isPress || _skillPhase != SkillPhase.None) return;
        if (_skill1CooldownRemaining > 0f) return;

        _skillPhase = SkillPhase.Starting;
        _pgcVehicleAnimCtrl.SetSkilling(true);
        _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Skill1);
        if (_skillEffectRoot != null) _skillEffectRoot.SetActive(true);
        MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", "Zeze_Skill_Start", IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        foreach (var ctrl in _playerAnimCtrls)
        {
            if (ctrl == null) continue;
            if (_charSkillReadyClip != null) ctrl.OverrideAnimationClip("idle", _charSkillReadyClip);
            ctrl.SetPlayerAniTriggerAgain(PlayerAniState.Idle);
        }

        float startLen = _pgcVehicleAnimCtrl.GetClipLength("skill1Start");
        float startWait = startLen > 0f ? startLen : 0.5f;
        TimerManager.Inst.RunOnce("", startWait, OnSkillLoopEnter);

        {
            float cd = _pgcVehicleConfig != null && _pgcVehicleConfig.skill1CoolDown > 0f
                ? _pgcVehicleConfig.skill1CoolDown
                : DefaultSkillDuration;
            _skill1CooldownRemaining = cd;
            if (IsSelf)
                MessageHelper.Broadcast(MessageName.OnSkill1CoolDown, cd);
        }

        TriggerAOEDance();
    }

    private void OnSkillLoopEnter()
    {
        if (_skillPhase != SkillPhase.Starting) return;
        _skillPhase = SkillPhase.Looping;

        int soundIndex = (_pgcVehicleConfig != null && VehicleSoundIndexMap.TryGetValue(_pgcVehicleConfig.id, out var idx)) ? idx : 0;
        var skillSnd = SkillSoundIds[soundIndex];
        MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Skill", skillSnd, IsSelf ? "Play_Skill_1P" : "Play_Skill_3P", Motor.gameObject);
        foreach (var uid in _playerUids)
        {
            var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
            if (player == null || player.PlayerAnimCtrl == null) continue;
            // 正在跳舞的玩家保持舞蹈动画，不强制覆盖为 skill 动作
            if (player.IsMainState(PlayerState.SingleEmote)) continue;
            var ctrl = player.PlayerAnimCtrl;
            if (_charSkillClip != null) ctrl.OverrideAnimationClip("idle", _charSkillClip);
            ctrl.Play("idle", 0, 0f);
        }

        float loopDuration = _pgcVehicleConfig?.skill1Value > 0f ? _pgcVehicleConfig.skill1Value : DefaultSkillDuration;
        _skillLoopTimer = TimerManager.Inst.RunOnce("", loopDuration, TriggerSkillEnd);
        _danceRecheckTimer = TimerManager.Inst.Run("", 0f, 0.2f, RecheckAOEDance);
    }

    private void RecheckAOEDance()
    {
        if (_skillPhase != SkillPhase.Looping) return;
        // 仅对已自然结束舞蹈（回到 Default 状态）的本机玩家重新触发，正在跳舞的由 TriggerAOEDance 内部守卫跳过
        TriggerAOEDance();
    }

    private void TriggerSkillEnd()
    {
        if (_skillPhase == SkillPhase.Ending || _skillPhase == SkillPhase.None) return;
        _skillPhase = SkillPhase.Ending;

        TimerManager.Inst.Stop(_skillLoopTimer);
        TimerManager.Inst.Stop(_danceRecheckTimer);
        if (_skillEffectRoot != null) _skillEffectRoot.SetActive(false);
        MessageHelper.Broadcast(MessageName.StopGameSound, IsSelf ? "Stop_Skill_1P" : "Stop_Skill_3P", Motor.gameObject);
        if (Vector3.ProjectOnPlane(Motor.Velocity, Motor.CharacterUp).magnitude > SkillMoveThreshold)
        {
            MessageHelper.Broadcast(MessageName.GameSound, "Vehicle_Drive", "Zeze_Run",
                IsSelf ? "Play_Drive_1P" : "Play_Drive_3P", Motor.gameObject);
        }
        _pgcVehicleAnimCtrl.SetSkilling(false);

        foreach (var uid in _playerUids)
        {
            var player = AvatarController.Inst.GetPlayerStateCtrl(uid);
            if (player == null || player.PlayerAnimCtrl == null) continue;
            var ctrl = player.PlayerAnimCtrl;
            if (player.IsMainState(PlayerState.SingleEmote))
                player.ExitState(PlayerState.SingleEmote, false);
            if (_charIdleClip != null) ctrl.OverrideAnimationClip("idle", _charIdleClip);
            ctrl.SetPlayerAniTriggerAgain(PlayerAniState.Idle);
        }

        // 处理被 AOE 影响但不是乘客的本机玩家
        var selfCtrl = AvatarController.Inst.SelfStateController;
        if (selfCtrl != null && selfCtrl.PlayerAnimCtrl != null
            && selfCtrl.IsMainState(PlayerState.SingleEmote)
            && !_playerUids.Contains(selfCtrl.PlayerID))
        {
            selfCtrl.ExitState(PlayerState.SingleEmote, false);
        }

        float endLen = _pgcVehicleAnimCtrl.GetClipLength("skill1End");
        float waitTime = endLen > 0f ? endLen : 1f;
        TimerManager.Inst.RunOnce("", waitTime, () =>
        {
            _skillPhase = SkillPhase.None;
            _pgcVehicleAnimCtrl.SetAniState(PGCVehicleAniState.Idle);
        });
    }

    private void TriggerAOEDance()
    {
        // 每个客户端只处理本机玩家：由 EmoteNetManager 负责本地播放 + 网络广播，
        // 避免直接操作远端 PlayerStateController 被网络同步包覆盖导致动作消失。
        if (Motor == null) return;
        var selfCtrl = AvatarController.Inst.SelfStateController;
        if (selfCtrl == null || selfCtrl.PlayerKCCtrl == null) return;
        if (!selfCtrl.IsMainState(PlayerState.Default)) return;

        float radius = _pgcVehicleConfig != null && _pgcVehicleConfig.skill2Value > 0f ? _pgcVehicleConfig.skill2Value : DefaultSkillRadius;
        if (Vector3.Distance(Motor.transform.position, selfCtrl.PlayerKCCtrl.transform.position) > radius) return;

        int index = Random.Range(0, DanceEmoteIds.Length);
        MessageHelper.Broadcast(MessageName.AOEForceSelfEmote, DanceEmoteIds[index]);
    }

    public override void Release()
    {
        TimerManager.Inst.Stop(_skillLoopTimer);
        TimerManager.Inst.Stop(_danceRecheckTimer);
        base.Release();
    }

    public override void ChangeHonkingState(bool isHonking, string honkingSound)
    {
        base.ChangeHonkingState(isHonking, "Zeze_Whistle");
    }

    public override void ChangeAirBanner(string strParam) { }
}

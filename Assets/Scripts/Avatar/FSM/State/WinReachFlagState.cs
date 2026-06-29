using FSM;
using System;
using Game.Audio;
using Game.Avatar;
using Game.KinematicCharacter;
using Message;
using UnityEngine;

public class WinReachFlagState : PlayerStateTemplate<PlayerStateController>
{
    private Transform m_StartPoint;
    private Vector3 m_OriginPos;
    private BudTimer soundTimer;
    private BudTimer animTimer;

    private Action _onEnterStateAct;
    private Action<string, bool> _playAnimAndEffectAct;
    private Collider _flagCollider;

    private KinematicCharacterController _selfPlayer => AvatarController.Inst.SelfController;

    public WinReachFlagState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        _onEnterStateAct = (Action)args[0];
        _playAnimAndEffectAct = (Action<string, bool>)args[1];
        m_StartPoint = (Transform)args[2];
        _flagCollider = (Collider)args[3];

        MessageHelper.Broadcast(MessageName.CountDownPauseAndRecord);

        _onEnterStateAct?.Invoke();
    }

    public override void ReleaseData()
    {
        base.ReleaseData();
        MessageHelper.Broadcast(MessageName.PassLevelJudging);
    }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void EnterMainState()
    {
        base.EnterMainState();

        if (owner.IsSelf)
        {
            //禁用旗子碰撞，避免被人物碰撞挤出去
            _flagCollider.enabled = false;
            owner.PlayerKCCtrl.SetFreezeCharacter(true);

        }

    }

    public override void ExitMainState()
    {
        base.ExitMainState();

        if (owner.IsSelf)
        {
            _flagCollider.enabled = true;
            owner.PlayerKCCtrl.SetFreezeCharacter(false);
        }
    }

    public override void OnExit()
    {
        base.OnExit();
    }

    public override void CoexistState()
    {
        base.CoexistState();
        PlayAnimation();
    }

    public override void DirectIntoState()
    {
        base.DirectIntoState();
        PlayAnimation();
    }

    public override void CacheState(PlayerState beState)
    {
        base.CacheState(beState);
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
        _playAnimAndEffectAct?.Invoke("idle", false);
        _selfPlayer.Motor.SetPositionAndRotation(m_OriginPos, _selfPlayer.Motor.transform.rotation);
    }

    public override void InterruptState(PlayerState beState)
    {
        base.InterruptState(beState);
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
        _playAnimAndEffectAct?.Invoke("idle", false);
        _selfPlayer.Motor.SetPositionAndRotation(m_OriginPos, _selfPlayer.Motor.transform.rotation);
    }

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
        _playAnimAndEffectAct?.Invoke("idle", false);
        onComplete?.Invoke(this);
        _selfPlayer.Motor.SetPositionAndRotation(m_OriginPos, _selfPlayer.Motor.transform.rotation);
    }

    private void PlayAnimation()
    {
        m_OriginPos = AvatarController.Inst.GetSelfAvatarPosition();
        //人物拉到动画播放点，并调整人和相机朝向
        var rotation = m_StartPoint.rotation;
        _selfPlayer.Motor.SetPositionAndRotation(m_StartPoint.position, rotation);
        AvatarController.Inst.SelfStateController.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Baqi);
        AvatarController.Inst.TurnToFaceCamera();
        _playAnimAndEffectAct?.Invoke("baqi", true);
        AkSoundManager.Inst.PlayInteractable3DSound("PullOutFlag", _selfPlayer.gameObject);
        soundTimer = TimerManager.Inst.RunOnce("winningFlagSound", 2.0f, () =>
        {
            AkSoundManager.Inst.PlayInteractable3DSound("SwingFlag", _selfPlayer.gameObject);
        });

        animTimer = TimerManager.Inst.RunOnce(nameof(animTimer), 5.0f, () =>
        {
            if (owner)
            {
                owner.ExitState(stateID);
            }
        });
    }
}

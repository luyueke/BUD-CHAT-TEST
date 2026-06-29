using FSM;
using System;
using Basic;
using Game.Avatar;
using Game.KinematicCharacter;
using UIAgent;
using UnityEngine;

public class CollectStarState : PlayerStateTemplate<PlayerStateController>
{
    private StarBehaviour m_Star;
    private bool m_IsCollected;
    private string m_StarName;
    private float m_Time;
    private Quaternion camTarget = Quaternion.Euler(5f, 0, 0);
    
    private KinematicCharacterController _selfPlayer => AvatarController.Inst.SelfController;

    public CollectStarState(PlayerState id, PlayerStateController owner) : base(id, owner)
    {
    }

    public override void InitData(params object[] args)
    {
        base.InitData(args);
        m_Star = (StarBehaviour)args[0];
        m_IsCollected = (bool)args[1];
        m_StarName = (string)args[2];
    }

    public override void ReleaseData()
    {
        base.ReleaseData();
    }

    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void EnterMainState()
    {
        base.EnterMainState();
        if (owner.IsSelf && !m_IsCollected)
        {
            SetInputLock(true);
            OpenGotStarPanel();
            m_Star.SetNewbieEmo(false);
            HidePlayerName(true);
            TurnPlayer();
        }

        if (owner.IsSelf)
        {
            // PlayerBaseControl.Inst.waitPosChange = true;
            // owner.playerAnimator.SetBool(AnimatorParameter.IsGround, true);
            // owner.playerAnimator.SetBool(AnimatorParameter.IsMoving, false);
            // owner.playerAnimator.SetBool(AnimatorParameter.IsJump, false);
            // PlayerBaseControl.Inst.isGround = true;
            // PlayerBaseControl.Inst.isMoving = false;
            // PlayerBaseControl.Inst.SetFly(false, true);
        }
    }

    public override void ExitMainState()
    {
        base.ExitMainState();
        if (owner.IsSelf && !m_IsCollected)
        {
            SetInputLock(false);
            //UIControlManager.Inst.CallUIControl("collectStar_exit");
            CloseGotStarPanel();
            m_Star.SetNewbieEmo(true);
            HidePlayerName(false);
        }

        // 帧同步？
        // if (owner.IsSelf)
        // {
        //     PlayerBaseControl.Inst.waitPosChange = false;
        // }
    }

    public override void OnExit()
    {
        base.OnExit();
    }

    public override void CoexistState()
    {
        base.CoexistState();
        // owner.playerAnimator.LoadPlay(m_IsCollected ? StateName.CollectOneDarkStar : StateName.CollectOneBrightStar);

        owner.PlayerAnimCtrl.PlayAnimFromConfig(AnimId.Star);
        m_Star.PlayCollectAnim(owner.transform);
        m_Time = 0;
    }

    public override void DirectIntoState()
    {
        base.DirectIntoState();
        // owner.playerAnimator.LoadPlay(m_IsCollected ? StateName.CollectOneDarkStar : StateName.CollectOneBrightStar);
        owner.PlayerAnimCtrl.PlayAnimFromConfig(AnimId.Star);
        m_Star.PlayCollectAnim(owner.transform);
        m_Time = 0;
    }

    public override void CacheState(PlayerState beState)
    {
        base.CacheState(beState);
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
        m_Star.ResetCollectAnim();
    }

    public override void InterruptState(PlayerState beState)
    {
        base.InterruptState(beState);
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
        m_Star.ResetCollectAnim();
    }

    public override void PlayExitAnimation(Action<PlayerStateBase> onComplete)
    {
        owner.PlayerAnimCtrl.SetPlayerAniState(PlayerAniState.Idle);
        m_Star.ResetCollectAnim();
        onComplete?.Invoke(this);
    }

    private void OpenGotStarPanel()
    {
        UIAgentManager.Inst.OpenPanel(PanelId.GotAStarPanel, m_StarName);
    }

    private void CloseGotStarPanel()
    {
        UIAgentManager.Inst.ClosePanel(WindowId.CollectStarWindow, PanelId.GotAStarPanel);
    }

    public override void MainStateUpdate()
    {
        base.MainStateUpdate();
        m_Time += Time.deltaTime;
        if (m_Time > 3.5f)
        {
            m_Time = 0;
            owner.ExitState(stateID);
            return;
        }

        if (owner.IsSelf && !m_IsCollected)
        {
            var curRot = _selfPlayer.Motor.transform.localRotation;
            _selfPlayer.Motor.SetRotation(Quaternion.Slerp(curRot, camTarget, Time.deltaTime * 2));
            TurnPlayer();
        }
    }

    private void HidePlayerName(bool hide)
    {
        // owner.transform.Find("playerInfo/nick").gameObject.SetActive(!hide); //todo:fsc 人物名字hide
    }

    private void TurnPlayer()
    {
        Transform playerAnimTF = owner.transform;
        Transform cameraTF = GlobalCameraManager.Inst.GlobalMainCamera.transform;
        Vector3 forwardDir = cameraTF.position - playerAnimTF.position;
        forwardDir.y = 0;
        Quaternion rotateQua = Quaternion.LookRotation(forwardDir);
        _selfPlayer.Motor.SetPositionAndRotation(AvatarController.Inst.GetSelfAvatarPosition(), rotateQua);
    }

    public void SetInputLock(bool isLock)
    {
        if (isLock)
        {
            _selfPlayer.SetFreezeCharacter(true);
            InputReceiver.locked = true;
            if (MobileJoystick.Inst != null)
            {
                MobileJoystick.Inst.OnResetJoystick();
            }
        }
        else
        {
            InputReceiver.locked = false;
            _selfPlayer.SetFreezeCharacter(false);
        }
    }
}

public interface StarBehaviour
{
    public abstract void SetNewbieEmo(bool isOn);
    public abstract void PlayCollectAnim(Transform parent);
    public abstract void ResetCollectAnim();
}
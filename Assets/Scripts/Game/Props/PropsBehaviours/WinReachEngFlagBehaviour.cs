using System;
using Game.Avatar;
using Game.Base;
using Game.Config;
using UIAgent;
using UnityEngine;

namespace Game.Props.PropsBehaviours
{
    public class WinReachEngFlagBehaviour : NodeBaseBehaviour
    {
        private Animator _flagAnimator;
        private const float WinAnimTime = 5.0f;
        private Transform _effect;
        private Transform _startAnimTrans;

        public bool IsStartRunning;
        public bool IsReached;

        private BudTimer soundTimer;
        private BudTimer animTimer;

        private Vector3 originPos; //捡旗子动画前玩家的位置

        private Collider _flagCollider;

        public override void OnInitByCreate()
        {
            base.OnInitByCreate();
            _flagAnimator ??= GetComponentInChildren<Animator>();
            _effect ??= transform.Find("banner_effect");
            _startAnimTrans ??= transform.Find("StartAnimPos");
            _flagCollider ??= transform.Find("Flag").GetComponent<Collider>();
            ShowEffect(false);

            // MessageHelper.AddListener<GameMode>(MessageName.ChangeMode, OnChangeMode);
        }


        private void OnDestroy()
        {
        }

        //
        // public void OnChangeMode(GameMode gameMode)
        // {
        //     if (gameMode == GameMode.Edit)
        //     {
        //         PlayerManager.Inst.UnFreezePlayerMove();
        //     }
        // }

        public override void OnTrigEnter()
        {
            if (!IsStartRunning)
            {
                return;
            }

            var playerStateCtrl = AvatarController.Inst.GetPlayerStateCtrl(AccountDataManager.Inst.Uid);
            if (!playerStateCtrl.CanEnterState(PlayerState.WinningFlag)) return;
            if (UIAgentManager.Inst.FindPanel(WindowId.GuestWindow, PanelId.CameraModePanel))
            {
                UIAgentManager.Inst.ClosePanel(WindowId.GuestWindow,PanelId.CameraModePanel);
            }
            playerStateCtrl.EnterState(PlayerState.WinningFlag, new Action(OnEnterState), new Action<string, bool>(PlayAnimAndEffect), _startAnimTrans, _flagCollider);
        }

        private void OnEnterState()
        {
            IsReached = true;
        }

        public override void OnTrigExit()
        {

        }

        public void ShowEffect(bool isShow)
        {
            _effect.gameObject.SetActive(isShow);
        }

        public void OnReset()
        {
            ShowEffect(false);
            IsReached = false;
            // if (PlayerManager.Inst.selfPlayer && PlayerManager.Inst.selfPlayer.gameObject.activeSelf)
            // {
            //     PlayerManager.Inst.selfPlayer.playerAnimator.Play(StateName.Idle);
            // }

            _flagAnimator.Play("idle");
        }

        public void PlayAnimAndEffect(string animName, bool isPlayEffect)
        {
            Play(animName);
            ShowEffect(isPlayEffect);
        }

        public void Play(string animName)
        {
            _flagAnimator.Play(animName);
        }

        // 已交由互斥State实现
        // private void DoPlayerReachFlag(Action beforeAct, Action afterAct)
        // {
        //     beforeAct?.Invoke();
        //     //记录动画前位置
        //     originPos = PlayerManager.Inst.selfPlayer.GetPlayerPosition();
        //     //人物拉到动画播放点，并调整人和相机朝向
        //     var selfPlayer = PlayerManager.Inst.selfPlayer;
        //     var rotation = _startAnimTrans.rotation;
        //     selfPlayer.SetPlayerPositionAndRotation(_startAnimTrans.position,
        //         Quaternion.Euler(rotation.eulerAngles + new Vector3(0, 180, 0)));
        //     selfPlayer.playerModel.transform.rotation = rotation;
        //     //人物动画 拔起->摇旗子->持旗子idle
        //     selfPlayer.playerAnimator.Play(StateName.ReachEndFlag_Baqi);
        //     //旗子动画
        //     _flagAnimator.Play("baqi");
        //     //特效
        //     ShowEffect(true);
        //     //音效
        //     AKSoundManager.Inst.PostEvent("Play_PullOutFlag", PlayerManager.Inst.selfPlayer.gameObject);
        //     soundTimer = TimerManager.Inst.RunOnce(nameof(soundTimer), 2.0f,
        //         () => { AKSoundManager.Inst.PostEvent("Play_SwingFlag", PlayerManager.Inst.selfPlayer.gameObject); });
        //     
        //     animTimer = TimerManager.Inst.RunOnce(nameof(animTimer), WinAnimTime, () =>
        //     {
        //         afterAct?.Invoke();
        //         // selfPlayer.playerAnimator.Play(StateName.Idle);
        //         _flagAnimator.Play("idle");
        //     });
        // }
    }
}
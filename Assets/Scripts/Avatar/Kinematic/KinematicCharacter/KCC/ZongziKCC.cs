using Basic.Extensions;
using Es;
using Game.Avatar;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 海王KCC
/// </summary>
public class ZongziKCC : BaseKCC {
    public override KCCType KCCType => KCCType.Default;
    public virtual string RunName => "run";

    private bool isFirstEnter = true;
    private Animator pgcAnimator;
    private Animator zongZiAnimator;
    private CharacterWrap _characterWrap;
    private Transform _backRoot;
    private GameObject go_ZongZi;

    public ZongziKCC(string uid, bool isSelf) : base(uid, isSelf)
    {

    }

    public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl) {
        base.OnEnter(motor, animCtrl);
        MessageHelper.AddListener<int, string>(MessageName.OnAvatarPutOnSuccess, OnAvatarPutOnSuccess);
        _characterWrap = animCtrl.Wrap as CharacterWrap;
        StateEventManager.Inst.RegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
        if (isFirstEnter && IsSelf) {
            isFirstEnter = false;
            SetKCCSimulate(false);
        }

        _backRoot = animCtrl.Wrap.GetBandNode((int)BodyNode.SpecialBackDeckNode);

        // 有问题，进入该状态时，衣服未创建，获取动画可能为null，若为null
        Motor.SetFrameCallBack(1, () => {
            if (pgcAnimator != null || _backRoot == null) {
                return;
            }
            pgcAnimator = _backRoot.GetComponentInChildren<Animator>(true);
        });
        _gravity = new Vector3(0, -25f, 0);
        animCtrl.SetSpecialPgcId(animCtrl.specialAnimPgcId);
    }

    public override void OnExit() {
        if (PlayerAnimCtrl != null) {
            PlayerAnimCtrl.SetSpecialPgcId("0");
        }

        base.OnExit();
        StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
        MessageHelper.RemoveListener<int, string>(MessageName.OnAvatarPutOnSuccess, OnAvatarPutOnSuccess);
        if(go_ZongZi != null)
            GameObject.Destroy(go_ZongZi);
    }

    public override void SetInputs(ref PlayerCharacterInputs inputs, Vector3 moveInputVector, Quaternion cameraPlanarRotation,
        Vector3 cameraPlanarDirection) {
        base.SetInputs(ref inputs, moveInputVector, cameraPlanarRotation, cameraPlanarDirection);
        if (!Motor.IsOnSimulate && (moveInputVector != Vector3.zero || inputs.JumpDown)) {
            SetKCCSimulate(true);
        }
    }


    private void SetKCCSimulate(bool isOn) {
        Motor.SetIsOnSimulate(isOn);
    }

    protected override void AirMovement(ref Vector3 currentVelocity, float deltaTime)
    {
        Vector3 lastVelocity = currentVelocity;
        base.AirMovement(ref currentVelocity, deltaTime);
        if (lastVelocity.y > 0 && currentVelocity.y < 0)
        {
            //到达顶端
            _gravity = new Vector3(0, -3.7f, 0);
            var tmpPgcAnimator = GetPGCAnimator();
            if (tmpPgcAnimator != null)
            {
                tmpPgcAnimator.Play("jump_down");
                if (zongZiAnimator != null)
                {
                    zongZiAnimator.Play("jump_down");
                }
            }
        }
    }

    /// <summary>
    /// 快跑，慢跑切换
    /// </summary>
    protected override void OnRunStateChange() {
        base.OnRunStateChange();
        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator == null) {
            return;
        }

        if (IsFastRun)
        {
            tmpPgcAnimator.Play("fast_run");
            if (zongZiAnimator != null)
            {
                zongZiAnimator.Play("fast_run");
            }
        }
        else if (PlayerAnimCtrl.CurAniState == PlayerAniState.Run)
        {
            tmpPgcAnimator.Play("run");
            if (zongZiAnimator != null)
            {
                zongZiAnimator.Play("run");
            }
        }
    }

    public override void OnLanded() {
        _gravity = new Vector3(0, -25f, 0);
        base.OnLanded();
        MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Landed, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }


    public override void OnLeaveStableGround() {
        base.OnLeaveStableGround();
        if (PlayerID.StartsWith("pet")) return;
        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator != null) {
            tmpPgcAnimator.Play("jump_up");
            if (zongZiAnimator != null)
            {
                zongZiAnimator.Play("jump_up");
            }
        }
        MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Jump, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }


    /// <summary>
    /// 动作状态切换
    /// </summary>
    /// <param name="newState"></param>
    private void OnAnimationStateChange(PlayerAniState newState)
    {
        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator == null) {
            return;
        }
        if (newState == PlayerAniState.Idle) {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
            tmpPgcAnimator.Play("idle");
            if (zongZiAnimator != null)
            {
                zongZiAnimator.Play("idle");
            }
        } else if (newState == PlayerAniState.Run) {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
            tmpPgcAnimator.Play("run");
            if (zongZiAnimator != null)
            {
                zongZiAnimator.Play("run");
            }
        }
    }
    
    private Animator GetPGCAnimator() {

        if (pgcAnimator == null) {
            var backRoot = PlayerAnimCtrl.specialAnimRoot;
            pgcAnimator = backRoot.GetComponentInChildren<Animator>(true);
        }

        return pgcAnimator;
    }

    private void OnAvatarPutOnSuccess(int avatarHashcode, string pgcId)
    {
        if (avatarHashcode == _characterWrap.Avatar.GetHashCode())
        {
            HandleFloatFlow(_backRoot);
        }
    }
    
    //添加漂浮功能
    private void HandleFloatFlow(Transform backRoot)
    {
        if(backRoot == null)
            return;
        
        if(backRoot.GetChild(0) == null)
            return;
        
        if(go_ZongZi != null)
            GameObject.Destroy(go_ZongZi);

        go_ZongZi = backRoot.GetChild(0).gameObject;
        go_ZongZi.transform.SetParent(null);

        zongZiAnimator = go_ZongZi.GetComponentInChildren<Animator>();
        
        var flowCtr = go_ZongZi.AddComponent<FloatingPartController>();
        flowCtr.SetFlowTarget(_characterWrap.Avatar);
    }
}


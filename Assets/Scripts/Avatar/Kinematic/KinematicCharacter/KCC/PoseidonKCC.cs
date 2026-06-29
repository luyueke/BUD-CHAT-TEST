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
public class PoseidonKCC : BaseKCC {
    public override KCCType KCCType => KCCType.Default;
    public virtual string RunName => "run";

    private bool isFirstEnter = true;
    private Animator pgcAnimator;
    private CharacterWrap _characterWrap;

    private SpecialSkinConfig specialSkinConfig;


    public PoseidonKCC(string uid, bool isSelf) : base(uid, isSelf)
    {

    }

    public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl) {
        base.OnEnter(motor, animCtrl);
        // 因为动作中有两次脚步，因此需要添加两个事件监听
        // 仅第一次进入和自己 才关闭模拟
        StateEventManager.Inst.RegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
        if (isFirstEnter && IsSelf) {
            isFirstEnter = false;
            SetKCCSimulate(false);
        }

        var backRoot = animCtrl.specialAnimRoot;

        // 有问题，进入该状态时，衣服未创建，获取动画可能为null，若为null
        Motor.SetFrameCallBack(1, () => {
            if (pgcAnimator != null || backRoot == null) {
                return;
            }
            pgcAnimator = backRoot.GetComponentInChildren<Animator>(true);
        });
        _gravity = new Vector3(0, -25f, 0);
        animCtrl.SetSpecialPgcId(animCtrl.specialAnimPgcId);
        specialSkinConfig = DataTables.GetSpecialSkinConfig(animCtrl.specialAnimPgcId);
    }

    public override void OnExit() {
        if (PlayerAnimCtrl != null) {
            PlayerAnimCtrl.SetSpecialPgcId("0");
        }

        base.OnExit();
        StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
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

    protected override void AirMovement(ref Vector3 currentVelocity, float deltaTime) {
        Vector3 lastVelocity = currentVelocity;
        base.AirMovement(ref currentVelocity, deltaTime);
        if (lastVelocity.y > 0 && currentVelocity.y < 0) {
            //到达顶端
            _gravity = new Vector3(0, -3.7f, 0);
            var tmpPgcAnimator = GetPGCAnimator();
            if (tmpPgcAnimator != null) {
                tmpPgcAnimator.Play("jump_down");
            }
        }
    }

    /// <summary>
    /// 快跑，慢跑切换
    /// </summary>
    protected override void OnRunStateChange() {
        base.OnRunStateChange();
        //切换手补节点位置
        if (IsFastRun)
        {
            ChangeHandPoint(ChangeAnimType.FastRun);
        }
        else
        {
            ChangeHandPoint(ChangeAnimType.Run);
        }
        
        var tmpPgcAnimator = GetPGCAnimator();
        if (tmpPgcAnimator == null) {
            return;
        }
        if (IsFastRun) {
            tmpPgcAnimator.Play("fast_run");
        } else if (PlayerAnimCtrl.CurAniState == PlayerAniState.Run) {
            tmpPgcAnimator.Play("run");
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
        }
        MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Jump, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
    }


    /// <summary>
    /// 动作状态切换
    /// </summary>
    /// <param name="newState"></param>
    private void OnAnimationStateChange(PlayerAniState newState)
    {
        if (newState == PlayerAniState.Idle)
        {
            if (!string.IsNullOrEmpty(specialSkinConfig.exhibitIdleAnimInfo.anim)) {
                bool isPlayExhibit = Random.Range(0, 100) < 50;
                GetPGCAnimator()?.Play( isPlayExhibit ?  "idle_exhibit" : "idle");
                PlayerAnimCtrl.SetPlayerState(isPlayExhibit
                    ? PlayerState.IdleExhibit
                    : PlayerState.Default);
                if(isPlayExhibit){
                    MessageHelper.Broadcast(MessageName.GameSound, "Locomotion_Group", specialSkinConfig.ExhibitSound, IsSelf ?  "Play_Locomotion_1P" : "Play_Locomotion_3P", Motor.gameObject);
                }else{
                    MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, IsSelf ? 0 : 1);
                }
            }else{
                ChangeHandPoint(ChangeAnimType.Idle);
            }
        }
        else if (newState == PlayerAniState.Run)
        {
            ChangeHandPoint(ChangeAnimType.Run);
        }
        

        if (newState == PlayerAniState.Idle)
        {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
        }
        else if (newState == PlayerAniState.Run)
        {
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, PlayerID.StartsWith("pet") ? 2 : IsSelf ? 0 : 1);
        }
    }
    
    private Animator GetPGCAnimator() {

        if (pgcAnimator == null) {
            var backRoot = PlayerAnimCtrl.specialAnimRoot;
            pgcAnimator = backRoot.GetComponentInChildren<Animator>();
        }

        return pgcAnimator;
    }


    private void ChangeHandPoint(ChangeAnimType type)
    {
        var resType = 10028;
        var pgcId = "11000241";
        var specialSkinConfig = DataTables.GetSpecialSkinConfig(pgcId);
        var Wrap = _characterWrap;
        if(Wrap == null)
            return;
        
        switch (type)
        {
            case ChangeAnimType.Idle:
            case ChangeAnimType.Jump:
                Wrap.Move(resType, specialSkinConfig.idleItemPos);
                Wrap.Rotate(resType, specialSkinConfig.idleItemRot);
                break;
            
            case ChangeAnimType.Run:
                Wrap.Move(resType, specialSkinConfig.runItemPos);
                Wrap.Rotate(resType, specialSkinConfig.runItemRot);
                break;
            
            case ChangeAnimType.FastRun:
                Wrap.Move(resType, specialSkinConfig.runFastItemPos);
                Wrap.Rotate(resType, specialSkinConfig.runFastItemRot);
                break;
        }
    }
    
    public enum ChangeAnimType
    {
        Idle = 1,
        Run = 2,
        FastRun = 3,
        Jump = 4,
    }
}


using Basic.Extensions;
using Es;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

namespace Game.Avatar.Kinematic.KinematicCharacter.KCC {
    public class BaseSpecialKCC : BaseKCC {
        public override KCCType KCCType => KCCType.Default;

        /// <summary>
        /// 道具上的Animator 组件
        /// </summary>
        protected Animator pgcAnimator;
        protected Animator pgcAnimatorOnEffect;
        protected bool isFirstEnter = true;
        protected AnimationEventHandler pgcAnimEventHandler;
        protected AnimationEventHandler pgcAnimEventHandlerOnEffect;
        protected SpecialSkinConfig specialSkinConfig;

        /// <summary>
        /// 特殊道具默认重力
        /// </summary>
        protected virtual float SpecialGravity => -25f;

        /// <summary>
        /// 跳跃到顶端的重力
        /// </summary>
        protected virtual float JumpTopGravity => -3.7f;

        protected BaseSpecialKCC(string uid, bool isSelf) : base(uid, isSelf) {
        }

        public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl) {
            base.OnEnter(motor, animCtrl);
            // 仅第一次进入和自己 才关闭模拟
            StateEventManager.Inst.RegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);
            if (isFirstEnter && IsSelf) {
                isFirstEnter = false;
                Motor.SetIsOnSimulate(false);
            }
            _gravity = new Vector3(0, SpecialGravity, 0);
            animCtrl.SetSpecialPgcId(animCtrl.specialAnimPgcId);
            specialSkinConfig = DataTables.GetSpecialSkinConfig(animCtrl.specialAnimPgcId);
            InitPlayerIdleAnimationEvent();
        }

        public override void OnExit() {
            if (PlayerAnimCtrl != null) {
                PlayerAnimCtrl.SetSpecialPgcId("0");
            }
            RemovePGCAnimationEvent();
            RemovePlayerIdleAnimationEvent();
            MessageHelper.Broadcast<string, GameObject>(MessageName.StopGameSound, IsSelf ?  "Stop_Locomotion_1P" : "Stop_Locomotion_3P", Motor.gameObject);
            pgcAnimator = null;
            pgcAnimatorOnEffect = null;
            pgcAnimEventHandler = null;
            pgcAnimEventHandlerOnEffect = null;
            base.OnExit();
            StateEventManager.Inst.UnRegisterStateEvent<PlayerAniState>(PlayerID, StateEvent.PlayerAniState, OnAnimationStateChange);

        }


        public override void SetInputs(ref PlayerCharacterInputs inputs, Vector3 moveInputVector, Quaternion cameraPlanarRotation,
            Vector3 cameraPlanarDirection) {
            base.SetInputs(ref inputs, moveInputVector, cameraPlanarRotation, cameraPlanarDirection);
            if (!Motor.IsOnSimulate && (moveInputVector != Vector3.zero || inputs.JumpDown)) {
                Motor.SetIsOnSimulate(true);
            }
        }

        protected override void AirMovement(ref Vector3 currentVelocity, float deltaTime) {
            Vector3 lastVelocity = currentVelocity;
            base.AirMovement(ref currentVelocity, deltaTime);
            if (lastVelocity.y > 0 && currentVelocity.y < 0) {
                OnHandleJumpTop();
            }
        }

        /// <summary>
        /// 跳跃到顶端
        /// </summary>
        protected virtual void OnHandleJumpTop() {
            //到达顶端
            _gravity = new Vector3(0, JumpTopGravity, 0);
            GetPGCAnimator()?.Play("jump_down");
        }

        public override void OnLanded() {
            _gravity = new Vector3(0, SpecialGravity, 0);
            base.OnLanded();
        }

        public override void OnLeaveStableGround() {
            base.OnLeaveStableGround();
            GetPGCAnimator()?.Play("jump_up");
            MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Jump, null, Motor.gameObject, IsSelf ? 0 : 1);
        }

        protected override void OnRunStateChange() {
            base.OnRunStateChange();
            if (IsFastRun) {
                GetPGCAnimator()?.Play("fast_run");
                GetPGCAnimatorOnEffect()?.Play("fast_run");
            } else if (PlayerAnimCtrl.CurAniState == PlayerAniState.Run) {
                GetPGCAnimator()?.Play("run");
                GetPGCAnimatorOnEffect()?.Play("run");
            }
        }

        protected virtual Animator GetPGCAnimator() {
            if (pgcAnimator == null) {
                var backRoot = PlayerAnimCtrl.Wrap.GetBandNode((int)BodyNode.SpecialBackDeckNode);
                pgcAnimator = backRoot.GetComponentInChildren<Animator>();
                if (pgcAnimator != null) {
                    pgcAnimEventHandler = pgcAnimator.GetComponent<AnimationEventHandler>();
                    AddPGCRunStateEvents(pgcAnimEventHandler);
                }
            }
            return pgcAnimator;
        }

        protected virtual Animator GetPGCAnimatorOnEffect() {

            if(pgcAnimatorOnEffect == null) {
                if(PlayerAnimCtrl.specialAnimRoot != null) {
                    pgcAnimatorOnEffect = PlayerAnimCtrl.specialAnimRoot.GetComponentInChildren<Animator>(true);
                    if (pgcAnimatorOnEffect != null) {
                        pgcAnimEventHandlerOnEffect = pgcAnimatorOnEffect.GetComponent<AnimationEventHandler>();
                        AddPGCRunStateEvents(pgcAnimEventHandlerOnEffect);
                    }
                }
            }
            return pgcAnimatorOnEffect;
        }

        protected virtual void InitPlayerIdleAnimationEvent() {
            var idleLength = PlayerAnimCtrl.GetClip("idle").length;
            var idleExhibitLength = PlayerAnimCtrl.GetClip("idle_exhibit").length;

            PlayerAnimCtrl.AddStateEvent("idle", $"{GetType().Name}_idle_end", idleLength, OnPlayerIdleEnd);
            PlayerAnimCtrl.AddStateEvent("idle", $"{GetType().Name}_idle_start", 0, OnPlayerIdleStart);
            PlayerAnimCtrl.AddStateEvent("idle_exhibit", $"{GetType().Name}_idle_exhibit_end", idleExhibitLength, OnPlayerIdleEnd);
            PlayerAnimCtrl.AddStateEvent("idle_exhibit", $"{GetType().Name}_idle_exhibit_start", 0, OnPlayerIdleStart);
        }

        protected virtual void RemovePlayerIdleAnimationEvent() {
            if (PlayerAnimCtrl == null) {
                return;
            }
            PlayerAnimCtrl.RemoveStateEvent("idle", $"{GetType().Name}_idle_end");
            PlayerAnimCtrl.RemoveStateEvent("idle", $"{GetType().Name}_idle_start");
            PlayerAnimCtrl.RemoveStateEvent("idle_exhibit", $"{GetType().Name}_idle_exhibit_end");
            PlayerAnimCtrl.RemoveStateEvent("idle_exhibit", $"{GetType().Name}_idle_exhibit_start");
        }

        protected virtual void OnPlayerIdleEnd() {
            if (string.IsNullOrEmpty(specialSkinConfig.exhibitIdleAnimInfo.anim)) {
                return;
            }
            bool isPlayExhibit = Random.Range(0, 100) < 50;
            if (isPlayExhibit && PlayerAnimCtrl.CurPlayState == PlayerState.Default) {
                GetPGCAnimator()?.Play( "idle_exhibit");
                PlayerAnimCtrl.SetPlayerState(PlayerState.IdleExhibit);
                GetPGCAnimatorOnEffect()?.Play( "idle_exhibit");
            } else if (PlayerAnimCtrl.CurPlayState == PlayerState.IdleExhibit && !isPlayExhibit) {
                GetPGCAnimator()?.Play( "idle");
                PlayerAnimCtrl.SetPlayerState(PlayerState.Default);
                GetPGCAnimatorOnEffect()?.Play( "idle");
            }


        }

        protected virtual void OnPlayerIdleStart(string stateName, string eventName) {
            // LoggerUtils.LogError("OnPlayerIdleStart:" + stateName + ", eventName:" + eventName);
            if (Motor == null) {
                return;
            }
            if (stateName == "idle_exhibit") {
                if (!string.IsNullOrEmpty(specialSkinConfig.ExhibitSound)) {
                    MessageHelper.Broadcast(MessageName.GameSound, "Locomotion_Group", specialSkinConfig.ExhibitSound, IsSelf ?  "Play_Locomotion_1P" : "Play_Locomotion_3P", Motor.gameObject);
                }
            } else {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
        }

        protected virtual void InitPGCAnimationEvent() {
            AddPGCRunStateEvents(pgcAnimEventHandler);
            AddPGCRunStateEvents(pgcAnimEventHandlerOnEffect);
        }

        protected virtual void RemovePGCAnimationEvent() {
            RemovePGCRunStateEvents(pgcAnimEventHandler);
            RemovePGCRunStateEvents(pgcAnimEventHandlerOnEffect);
        }

        protected virtual void AddPGCRunStateEvents(AnimationEventHandler handler) {
            if (handler == null) {
                return;
            }
            handler.AddStateEvent("run",$"{GetType().Name}_playFootSound1", 0.4f, OnAnimationEvent);
            handler.AddStateEvent("run",$"{GetType().Name}_playFootSound2", 1.3f, OnAnimationEvent);
            handler.AddStateEvent("fast_run",$"{GetType().Name}_playFootSound1", 0.3f, OnAnimationEvent);
            handler.AddStateEvent("fast_run",$"{GetType().Name}_playFootSound2", 0.9f, OnAnimationEvent);
        }

        protected virtual void RemovePGCRunStateEvents(AnimationEventHandler handler) {
            if (handler == null) {
                return;
            }
            handler.RemoveStateEvent("run",$"{GetType().Name}_playFootSound1");
            handler.RemoveStateEvent("run",$"{GetType().Name}_playFootSound2");
            handler.RemoveStateEvent("fast_run",$"{GetType().Name}_playFootSound1");
            handler.RemoveStateEvent("fast_run",$"{GetType().Name}_playFootSound2");
        }



        protected virtual void OnAnimationEvent(string stateName, string eventName) {
            if (stateName == "run") {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, IsSelf ? 0 : 1);
            } else if (stateName == "fast_run") {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.FastRun, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
        }


        /// <summary>
        /// 动作状态切换
        /// </summary>
        /// <param name="newState"></param>
        protected virtual void OnAnimationStateChange(PlayerAniState newState) {
            if (specialSkinConfig == null) {
                return;
            }
            string pgcStateName = null;
            if (newState == PlayerAniState.Idle) {
                MessageHelper.Broadcast<string, GameObject>(MessageName.StopGameSound, IsSelf ? "Stop_Locomotion_1P" : "Stop_Locomotion_3P", Motor.gameObject);
                if (string.IsNullOrEmpty(specialSkinConfig.exhibitIdleAnimInfo.anim)) {
                    GetPGCAnimator()?.Play("idle");
                    PlayerAnimCtrl.SetPlayerState(PlayerState.Default);
                    GetPGCAnimatorOnEffect()?.Play("idle");
                    pgcStateName = "idle";
                } else {
                    bool isPlayExhibit = Random.Range(0, 100) < 50;
                    var name = isPlayExhibit ? "idle_exhibit" : "idle";
                    GetPGCAnimator()?.Play(name);
                    PlayerAnimCtrl.SetPlayerState(isPlayExhibit
                        ? PlayerState.IdleExhibit
                        : PlayerState.Default);
                    GetPGCAnimatorOnEffect()?.Play(name);
                    pgcStateName = name;
                }
            } else if (newState == PlayerAniState.Run) {
                var runState = IsFastRun ? "fast_run" : "run";
                GetPGCAnimator()?.Play(runState);
                GetPGCAnimatorOnEffect()?.Play(runState);
                pgcStateName = runState;
            }

            // 把云端 Animator 同帧 Update(0) 应用刚才的 Play，再把本体当前 state（已经被 PlayerAniState 更新到目标态）也强制 Play 到 time 0 + Update(0)。
            // 这是为了修：地图入场动画结束后角色才进入 idle，但云在效果加载完就已经在循环了 —— 两边相位错开。
            // 强制同帧 0 启动可在任何 state 切换瞬间重建相位对齐；clip 长度一致（如 huhucloud idle/run 都 1.667s）就能持续同步。
            if (!string.IsNullOrEmpty(pgcStateName) && PlayerAnimCtrl != null && PlayerAnimCtrl.gameObject != null)
            {
                var pgcAnim = GetPGCAnimator();
                if (pgcAnim != null && pgcAnim.gameObject.activeInHierarchy) pgcAnim.Update(0f);
                var effectAnim = GetPGCAnimatorOnEffect();
                if (effectAnim != null && effectAnim != pgcAnim && effectAnim.gameObject.activeInHierarchy) effectAnim.Update(0f);

                var mainAnim = PlayerAnimCtrl.GetComponent<Animator>();
                if (mainAnim != null && mainAnim.gameObject.activeInHierarchy)
                {
                    mainAnim.Update(0f);
                    var info = mainAnim.GetCurrentAnimatorStateInfo(0);
                    mainAnim.Play(info.fullPathHash, 0, 0f);
                    mainAnim.Update(0f);
                }
            }
        }
    }
}

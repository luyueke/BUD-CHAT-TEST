using Es;
using Game.Audio;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

namespace Game.Avatar.Kinematic.KinematicCharacter.KCC {
    public class GhostCatKCC : BaseSpecialKCC {
        public GhostCatKCC(string uid, bool isSelf) : base(uid, isSelf) {
        }
        
        public override void OnEnter(KinematicCharacterMotor motor, PlayerAnimationCtrl animCtrl) {
            base.OnEnter(motor, animCtrl);
            PlayerAnimCtrl.AddStateEvent("run", $"{GetType().Name}_playFootSound1", 0.066f, OnAnimationEvent);
            PlayerAnimCtrl.AddStateEvent("run", $"{GetType().Name}_playFootSound2", 0.366f, OnAnimationEvent);
            PlayerAnimCtrl.AddStateEvent("run_fast", $"{GetType().Name}_playFootSound1", 0.037f, OnAnimationEvent);
            PlayerAnimCtrl.AddStateEvent("run_fast", $"{GetType().Name}_playFootSound2", 0.185f, OnAnimationEvent);
        }

        public override void OnExit() {
            PlayerAnimCtrl.RemoveStateEvent("run", $"{GetType().Name}_playFootSound1");
            PlayerAnimCtrl.RemoveStateEvent("run", $"{GetType().Name}_playFootSound2");
            PlayerAnimCtrl.RemoveStateEvent("run_fast", $"{GetType().Name}_playFootSound1");
            PlayerAnimCtrl.RemoveStateEvent("run_fast", $"{GetType().Name}_playFootSound2");
            base.OnExit();
        }

        protected override void OnPlayerIdleStart(string stateName, string eventName)
        {
            MessageHelper.Broadcast<string, GameObject>(MessageName.StopGameSound, IsSelf ? "Stop_Locomotion_1P" : "Stop_Locomotion_3P", Motor.gameObject);
            base.OnPlayerIdleStart(stateName, eventName);
        }
        protected override void OnAnimationEvent(string stateName, string eventName) {
            if (stateName == "run") {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, IsSelf ? 0 : 1);
            } else if (stateName == "run_fast") {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.FastRun, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
        }

        protected override void OnAnimationStateChange(PlayerAniState newState) {
            if (specialSkinConfig == null) {
                return;
            }
            MessageHelper.Broadcast<string, GameObject>(MessageName.StopGameSound, IsSelf ? "Stop_Locomotion_1P" : "Stop_Locomotion_3P", Motor.gameObject);
            if (newState == PlayerAniState.Idle) {
                if (string.IsNullOrEmpty(specialSkinConfig.exhibitIdleAnimInfo.anim)) {
                    GetPGCAnimator()?.Play("idle");
                    PlayerAnimCtrl.SetPlayerState(PlayerState.Default);
                    GetPGCAnimatorOnEffect()?.Play("idle");
                } else {
                    bool isPlayExhibit = Random.Range(0, 100) < 50;
                    GetPGCAnimator()?.Play( isPlayExhibit ?  "idle_exhibit" : "idle");
                    PlayerAnimCtrl.SetPlayerState(isPlayExhibit
                        ? PlayerState.IdleExhibit
                        : PlayerState.Default);
                    GetPGCAnimatorOnEffect()?.Play( isPlayExhibit ?  "idle_exhibit" : "idle");
                    if(isPlayExhibit){
                        MessageHelper.Broadcast(MessageName.GameSound, "Locomotion_Group", specialSkinConfig.ExhibitSound, IsSelf ?  "Play_Locomotion_1P" : "Play_Locomotion_3P", Motor.gameObject);
                    }else{
                        MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Idle, null, Motor.gameObject, IsSelf ? 0 : 1);
                    }
                }
            } else if (newState == PlayerAniState.Run) {
                GetPGCAnimator()?.Play("run");
                GetPGCAnimatorOnEffect()?.Play("run");
            } else if (newState == PlayerAniState.Jump) {
                GetPGCAnimatorOnEffect()?.Play("jump_up");
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Jump, null, Motor.gameObject, IsSelf ? 0 : 1);
            } else if (newState == PlayerAniState.Land) {
                GetPGCAnimatorOnEffect()?.Play("jump_down");
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Landed, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
        }
    }
}

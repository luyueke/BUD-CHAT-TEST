using GameData;
using Message;
using UnityEngine;

namespace Game.Avatar.Kinematic.KinematicCharacter.KCC {
    public class SkatingKCC : BaseSpecialKCC {
        public SkatingKCC(string uid, bool isSelf) : base(uid, isSelf) {

        }

        protected override void OnPlayerIdleStart(string stateName, string eventName)
        {
            MessageHelper.Broadcast<string, GameObject>(MessageName.StopGameSound, IsSelf ? "Stop_Locomotion_1P" : "Stop_Locomotion_3P", Motor.gameObject);
            base.OnPlayerIdleStart(stateName, eventName);
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
                }
            } else if (newState == PlayerAniState.Run) {
                GetPGCAnimator()?.Play("run");
                GetPGCAnimatorOnEffect()?.Play("run");
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
        }

        protected override void OnRunStateChange()
        {
            base.OnRunStateChange();
            if(IsFastRun)
            {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.FastRun, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
            else
            {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
        }
    }
}

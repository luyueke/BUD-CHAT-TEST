using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

namespace Game.Avatar.Kinematic.KinematicCharacter.KCC {
    public class SnowboardKCC : BaseSpecialKCC {
        public SnowboardKCC(string uid, bool isSelf) : base(uid, isSelf) {
        }

        protected override void OnPlayerIdleStart(string stateName, string eventName)
        {
            MessageHelper.Broadcast<string, GameObject>(MessageName.StopGameSound, IsSelf ? "Stop_Locomotion_1P" : "Stop_Locomotion_3P", Motor.gameObject);
            base.OnPlayerIdleStart(stateName, eventName);
        }


        protected override void OnAnimationStateChange(PlayerAniState newState) {
            if (newState == PlayerAniState.Run) {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, IsSelf ? 0 : 1);
            }else{
                MessageHelper.Broadcast<string, GameObject>(MessageName.StopGameSound, IsSelf ? "Stop_Locomotion_1P" : "Stop_Locomotion_3P", Motor.gameObject);
            }
            base.OnAnimationStateChange(newState);

        }

        protected override void OnRunStateChange() {
            base.OnRunStateChange();
            if (IsFastRun) {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.FastRun, null, Motor.gameObject, IsSelf ? 0 : 1);
            } else {
                MessageHelper.Broadcast<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, CharacterFootType.Run, null, Motor.gameObject, IsSelf ? 0 : 1);
            }
        }
    }
}

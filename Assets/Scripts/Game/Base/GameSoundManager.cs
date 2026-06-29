using Es;
using Game.Audio;
using Game.Utils;
using Game.KinematicCharacter;
using GameData;
using Message;
using UnityEngine;

namespace Game.Base
{
    public class GameSoundManager : GameInstance<GameSoundManager>, IAutoInit
    {
        public bool IsOpenFootSound
        {
            get;
            set;
        } = true;


        public void Init()
        {
            MessageHelper.AddListener<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, OnCharacterPlayFoodSound);
            MessageHelper.AddListener<string, string, string, GameObject>(MessageName.GameSound, OnGamePlaySound);
            MessageHelper.AddListener<string, GameObject>(MessageName.StopGameSound, OnGameStopSound);
        }

        public override void Release()
        {
            base.Release();
            MessageHelper.RemoveListener<CharacterFootType, GameObject, GameObject, int>(MessageName.FootSound, OnCharacterPlayFoodSound);
            MessageHelper.RemoveListener<string, string, string, GameObject>(MessageName.GameSound, OnGamePlaySound);
            MessageHelper.RemoveListener<string, GameObject>(MessageName.StopGameSound, OnGameStopSound);
        }

        private void OnGameStopSound(string eventName, GameObject owner) {
            AkSoundManager.Inst.StopSound(eventName, owner);
        }

        private void OnGamePlaySound(string switchGroup, string switchStateName, string eventName, GameObject owner) {
            AkSoundManager.Inst.PlaySound(switchGroup, switchStateName, eventName, owner);
        }

        private CharacterFootType previousFootType = CharacterFootType.Idle;
        private void OnCharacterPlayFoodSound(CharacterFootType footType, GameObject collider, GameObject owner, int isSelf)
        {
            if (!IsOpenFootSound || !AkSoundManager.HasInstance) return;
            if (owner == null)
            {
                LoggerUtils.LogError("播放脚步声 owner 不可为 null");
                return;
            }

            // 有载具（召唤/乘坐/驾驶）时，不播放走路脚步音效
            if (footType == CharacterFootType.Run || footType == CharacterFootType.FastRun)
            {
                var motor = owner.GetComponent<KinematicCharacterMotor>();
                if (motor != null && motor.IsVehicleSummoned)
                {
                    return;
                }
            }
            string groupName = isSelf switch
            {
                2 => "Pet_Group",
                _ => "Locomotion_Group"
            };

            string eventName = isSelf switch
            {
                0 => "Play_Locomotion_1P",
                2 => "Play_Pet_Locomotion",
                _ => "Play_Locomotion_3P"
            };

            string stopEventName = isSelf switch {
                0 => "Stop_Locomotion_1P",
                2 => "Stop_Pet_Locomotion",
                _ => "Stop_Locomotion_3P"
            };

            string switchName = null;
            var playerAnimController = owner.GetComponentInChildren<PlayerAnimationCtrl>();
            if (playerAnimController != null && !string.IsNullOrEmpty(playerAnimController.specialAnimPgcId)) {
                var specialAnimConfig = DataTables.GetSpecialSkinConfig(playerAnimController.specialAnimPgcId);

                if (footType == CharacterFootType.Jump && previousFootType == CharacterFootType.FastRun){
                    AkSoundManager.Inst.StopSound(stopEventName, owner);
                    switchName = specialAnimConfig.jumpFastRunAnimInfo.audio;
                } else if (footType == CharacterFootType.Jump && previousFootType == CharacterFootType.Run) {
                    AkSoundManager.Inst.StopSound(stopEventName, owner);
                    switchName = specialAnimConfig.jumpRunAnimInfo.audio;
                }else if (footType == CharacterFootType.Run) {
                    AkSoundManager.Inst.StopSound(stopEventName, owner);
                    switchName = specialAnimConfig.runAnimInfo.audio;
                } else if (footType == CharacterFootType.FastRun) {
                    AkSoundManager.Inst.StopSound(stopEventName, owner);
                    switchName = specialAnimConfig.fastRunAnimInfo.audio;
                } else if (footType == CharacterFootType.Jump) {
                    AkSoundManager.Inst.StopSound(stopEventName, owner);
                    switchName = specialAnimConfig.jumpAnimInfo.audio;
                } else if (footType == CharacterFootType.Idle ) {
                    AkSoundManager.Inst.StopSound(stopEventName, owner);
                }
                if (!string.IsNullOrEmpty(switchName)) {
                    AkSoundManager.Inst.PlaySound(groupName, switchName, eventName, owner);
                }

                if(footType != previousFootType){
                    previousFootType = footType;
                }
                return;
            }

            string matSoundName = collider == null ? "Default" : collider.gameObject.GetMatSound();
            switchName = isSelf switch
            {
                2 => footType switch
                {
                    CharacterFootType.Jump => "Jump",
                    CharacterFootType.Landed => "Land",
                    CharacterFootType.Run => "Footstep",
                    CharacterFootType.FastRun => "Footstep",
                    _ => ""
                },
                _ => footType switch
                {
                    CharacterFootType.Jump => "Default_Jump",
                    CharacterFootType.Landed => $"{matSoundName}_Land",
                    CharacterFootType.Run => $"{matSoundName}_Footstep",
                    CharacterFootType.FastRun => $"{matSoundName}_Footstep",
                    _ => ""
                }
            };

            AkSoundManager.Inst.PlaySound(groupName, switchName, eventName, owner);
        }
    }
}

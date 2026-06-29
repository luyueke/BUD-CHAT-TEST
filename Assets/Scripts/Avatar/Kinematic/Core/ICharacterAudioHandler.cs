using UnityEngine;

namespace Game.KinematicCharacter {
    public interface ICharacterAudioHandler {

        public void OnLanded(GameObject character, GameObject collider, bool isSelf);
        public void OnLeaveStableGround(GameObject character, bool isSelf);
        public void OnFootStep(GameObject character, GameObject collider, bool isSelf);
    }
}

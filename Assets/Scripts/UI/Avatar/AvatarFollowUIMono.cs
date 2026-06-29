using Game.KinematicCharacter;
using Game.Utils;
using UnityEngine;
using UnityEngine.Animations;

namespace UI.Avatar
{
    public class AvatarFollowUIMono : MonoBehaviour
    {
        KinematicCharacterController characterController;
        GameObject uiRoot;
        LookAtConstraint lookAtConstraint;

        [HideInInspector]public AvatarChatMessage ChatMessageModule;

        private void Awake()
        {
            characterController = GetComponent<KinematicCharacterController>();
            var uiRootTF = characterController.transform.Find("AvatarFollowUI");
            if (uiRootTF != null)
            {
                uiRoot = uiRootTF.gameObject;
            } else {
                uiRoot = new GameObject("AvatarFollowUI");
                uiRoot.transform.SetParent(characterController.transform);
            }

            lookAtConstraint = uiRoot.AddComponent<LookAtConstraint>();
            ConstraintSource constraintSource = new ConstraintSource();
            constraintSource.weight = 1;
            constraintSource.sourceTransform = GameCameraUtils.Inst.GetMainCamera().transform;
            lookAtConstraint.constraintActive = true;
            lookAtConstraint.AddSource(constraintSource);
            uiRoot.transform.localPosition = Vector3.zero;
        }

        private void Start()
        {
            LoadAllModule();
        }

        void LoadAllModule()
        {
            ChatMessageModule = uiRoot.AddComponent<AvatarChatMessage>();
        }
    }
}
using Game.Avatar;
using Game.SurfaceDetection.Base;
using UnityEngine;

namespace Game.SurfaceDetection
{
    public class IceCubeSurfaceHandler : BaseSurfaceHandler
    {
        public IceCubeSurfaceHandler()
        {
            Tag = "IceCube";
            Priority = SurfaceDetectPriority.IceCube;
        }

        public override bool HandleOverlapRaycastResult(Collider[] colliders)
        {
            return true;
        }

        public override void OnEnter()
        {
            LoggerUtils.Log("IceCubeSurfaceHandler OnEnter");
            AvatarController.Inst.SelfStateController.EnterState(PlayerState.Skate);
        }

        public override void OnChange(GameObject oldGo, GameObject newGo)
        {
            LoggerUtils.Log("IceCubeSurfaceHandler OnChange");

        }

        public override void OnExit()
        {
            LoggerUtils.Log("IceCubeSurfaceHandler OnExit");
            AvatarController.Inst.SelfStateController.ExitState(PlayerState.Skate);
        }
    }
}
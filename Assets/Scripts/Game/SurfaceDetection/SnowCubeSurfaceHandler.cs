using Game.Avatar;
using Game.SurfaceDetection.Base;
using UnityEngine;

namespace Game.SurfaceDetection
{
    public class SnowCubeSurfaceHandler : BaseSurfaceHandler
    {
        public SnowCubeSurfaceHandler()
        {
            Tag = "SnowCube";
            Priority = SurfaceDetectPriority.SnowCube;
        }

        public override bool HandleOverlapRaycastResult(Collider[] colliders)
        {
            return true;
        }

        public override void OnEnter()
        {
            LoggerUtils.Log("SnowCubeSurfaceHandler OnEnter");
            StateEventManager.Inst.RegisterStateEvent<bool>(AccountDataManager.Inst.Uid, StateEvent.FastRun, EnterSkiState);


            if (AvatarController.Inst.SelfController.CurIKCController.IsFastRun)
            {
                AvatarController.Inst.SelfStateController.EnterState(PlayerState.Ski);
            }
        }

        public override void OnChange(GameObject oldGo, GameObject newGo)
        {
            LoggerUtils.Log("SnowCubeSurfaceHandler OnChange");

        }

        private void EnterSkiState(bool isFastRun)
        {
            if (isFastRun)
            {
                AvatarController.Inst.SelfStateController.EnterState(PlayerState.Ski);
            }
            else
            {
                AvatarController.Inst.SelfStateController.ExitState(PlayerState.Ski);
            }
        }

        public override void OnExit()
        {
            LoggerUtils.Log("SnowCubeSurfaceHandler OnExit");

            StateEventManager.Inst.UnRegisterStateEvent<bool>(AccountDataManager.Inst.Uid, StateEvent.FastRun, EnterSkiState);

            AvatarController.Inst.SelfStateController.ExitState(PlayerState.Ski);
        }
    }
}
using Game.Avatar;
using Game.SurfaceDetection.Base;
using UnityEngine;

namespace Game.SurfaceDetection
{
    public class DefaultHandler : BaseSurfaceHandler
    {
        public DefaultHandler()
        {
            Tag = "DefaultGround";
            Priority = SurfaceDetectPriority.Default;
        }

        public override bool HandleOverlapRaycastResult(Collider[] colliders)
        {
            return true;
        }

        public override void OnEnter()
        {
            LoggerUtils.Log("DefaultHandler OnEnter");
        }

        public override void OnChange(GameObject oldGo, GameObject newGo)
        {
            LoggerUtils.Log("DefaultHandler OnChange");

        }

        public override void OnExit()
        {
            LoggerUtils.Log("DefaultHandler OnExit");

        }
    }
}
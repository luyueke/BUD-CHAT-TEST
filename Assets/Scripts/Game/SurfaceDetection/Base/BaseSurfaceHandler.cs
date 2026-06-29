using UnityEngine;

namespace Game.SurfaceDetection.Base
{
    public abstract class BaseSurfaceHandler : ISurfaceHandler
    {
        public string Tag;
        public SurfaceDetectPriority Priority; //优先级，越高的优先检测

        public SurfaceRaycastController RaycastCtr;
        public GameObject HitGameObject;

        public BaseSurfaceHandler()
        {
            RaycastCtr = SurfaceDetectManager.Inst.RaycastCtr;
        }

        public abstract bool HandleOverlapRaycastResult(Collider[] colliders);

        public abstract void OnEnter();

        public abstract void OnChange(GameObject oldGo, GameObject newGo);

        public abstract void OnExit();

        public virtual bool IsCanSurfaceDetect()
        {
            return true;
        }
    }
}
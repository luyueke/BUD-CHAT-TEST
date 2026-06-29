// @Author: YangJie
// @Description:
// @Date:  2023/10/11
// @Modify:

using UnityEngine;

namespace HLOD
{
    public class HLODCameraParams
    {
        public Vector3 pos;

        public float relative;

        public Plane[] planes;

        public float farClipPlane = 1000f;

        public HLODCameraParams(Camera camera)
        {
            if (camera == null)
            {
                return;
            }
            pos = camera.transform.position;
            relative = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            planes = GeometryUtility.CalculateFrustumPlanes(camera);
            farClipPlane = camera.farClipPlane;
        }

        public HLODCameraParams(Vector3 pos)
        {
            this.pos = pos;
            relative = 0.4663f;
        }
        public void Update(Vector3 tmpPos, Camera camera)
        {
            pos = tmpPos;
            relative = Mathf.Tan(Mathf.Deg2Rad * camera.fieldOfView * 0.5F);
            planes = GeometryUtility.CalculateFrustumPlanes(camera);
            farClipPlane = camera.farClipPlane;
        }

        public void Update(Vector3 tmpPos) {
            pos = tmpPos;
        }

    }
}

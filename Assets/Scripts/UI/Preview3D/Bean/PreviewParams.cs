using Es;
using UnityEngine;

namespace UI.Preview3D.Bean
{
    public class PreviewParams
    {
        public Vector3 ModelLocalPos;
        public Vector3 ModelLocalEulerAngles;
        public Vector3 ModelLocalScale = Vector3.one;

        public Vector3 CameraLocalPos;
        public Vector3 CameraLocalEulerAngles;
        public float CameraFov;

        public static PreviewParams ParseFromExcel(int previewParamId)
        {
            var esCfg = DataTables.GetPreviewParamsConfig(previewParamId);
            if (esCfg != null)
            {
                return new PreviewParams()
                {
                    ModelLocalPos =  esCfg.ModelLocalPos,
                    ModelLocalEulerAngles =  esCfg.ModelLocalEulerAngles,
                    ModelLocalScale =  esCfg.ModelLocalScale,
                    
                    CameraLocalPos =  esCfg.CameraLocalPos,
                    CameraLocalEulerAngles =  esCfg.CameraLocalEulerAngles,
                    CameraFov =  esCfg.CameraFov,
                };
            }

            return null;
        }
    }
}
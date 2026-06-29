
using GameData.Manager;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraJoyStick : MonoBehaviour, IDragHandler
{
    public Transform CamereTarget;
    private float cameraUpDownSpeed = 0.31f;
    private float[] cameraUpDownLimits = {-45, 60};


    public void OnDrag(PointerEventData eventData)
    {
        if (GameDataManager.Inst.globalSettingData!=null)
        {
            cameraUpDownSpeed = GameDataManager.Inst.globalSettingData.cameraPanSensitivity;
            //海外服兼容相机灵敏度
            if (cameraUpDownSpeed > 1)
            {
                cameraUpDownSpeed = 0.31f;
            }
        }
        var rot = CamereTarget.transform.localEulerAngles;
        rot.x = rot.x > 180 ? rot.x - 360: rot.x;
        rot.x -= eventData.delta.y * cameraUpDownSpeed;
        rot.x = Mathf.Clamp(rot.x, cameraUpDownLimits[0], cameraUpDownLimits[1]);
        rot.y += eventData.delta.x* cameraUpDownSpeed;
        CamereTarget.transform.localEulerAngles = rot;
    }
    
}

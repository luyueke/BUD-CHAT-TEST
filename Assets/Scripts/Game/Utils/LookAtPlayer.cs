using Game.Utils;
using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = GameCameraUtils.Inst.GetMainCamera();
    }

    void OnEnable()
    {
        SetLookAtDir();
    }
    void Update()
    {
        SetLookAtDir();
    }

    private void SetLookAtDir()
    {
        Vector3 lookAt = mainCamera.transform.position;
        lookAt.y = this.transform.position.y;
        transform.LookAt(lookAt);
    }
}
